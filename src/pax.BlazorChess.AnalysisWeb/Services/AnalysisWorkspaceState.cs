using Microsoft.Extensions.Options;
using System.Text;
using pax.BlazorChess.Board.Storage;
using pax.chess;
using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.AnalysisWeb.Services;

public sealed class AnalysisWorkspaceState : IAsyncDisposable
{
    private const string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private readonly IChessBoardRepository _repository;
    private readonly TimeSpan _engineSettingsSaveDebounce;
    private readonly SemaphoreSlim _engineSettingsPersistenceGate = new(1, 1);
    private CancellationTokenSource? _engineSettingsSaveCts;
    private bool _engineSettingsLoaded;
    private bool _disposed;

    public AnalysisWorkspaceState(
        IChessBoardRepository repository,
        IOptions<AnalysisPersistenceOptions> persistenceOptions)
    {
        _repository = repository;
        var debounceMs = persistenceOptions.Value.EngineSettingsSaveDebounceMs;
        _engineSettingsSaveDebounce = TimeSpan.FromMilliseconds(Math.Max(1, debounceMs));
    }

    public AnalysisBoard AnalysisBoard { get; private set; } = CreateInitialAnalysisBoard();
    public List<EngineRunOptions> EngineRunOptions { get; } = [];
    public Guid? CurrentAnalyzedGameId { get; private set; }
    public string GameName { get; private set; } = "New game";
    public AnalyzedGameMetadata GameMetadata { get; private set; } = AnalyzedGameMetadata.Empty;
    public bool IsDirty { get; private set; }
    public Guid SelectedEngineId { get; set; }
    public Move? LastMove { get; private set; }
    public int CurrentPly { get; private set; }
    public string ImportError { get; private set; } = string.Empty;

    public string Fen => FenSerializer.Serialize(AnalysisBoard.CurrentPosition);
    public string Pgn => BuildMainLinePgn();
    public EngineRunOptions? SelectedEngine => EngineRunOptions.FirstOrDefault(e => e.Id == SelectedEngineId);

    public async Task LoadEngineSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_engineSettingsLoaded)
            return;

        await _engineSettingsPersistenceGate.WaitAsync(cancellationToken);
        try
        {
            if (_engineSettingsLoaded)
                return;

            var engineRunOptions = await _repository.GetEngineRunOptions(cancellationToken);
            EngineRunOptions.Clear();
            EngineRunOptions.AddRange(engineRunOptions);
            SelectedEngineId = EngineRunOptions.FirstOrDefault()?.Id ?? Guid.Empty;
            _engineSettingsLoaded = true;
        }
        finally
        {
            _engineSettingsPersistenceGate.Release();
        }
    }

    public void ScheduleEngineSettingsSave()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var cts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _engineSettingsSaveCts, cts);
        previous?.Cancel();

        _ = SaveEngineSettingsWhenQuietAsync(cts);
    }

    public async Task SaveEngineSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _engineSettingsPersistenceGate.WaitAsync(cancellationToken);
        try
        {
            await _repository.StoreEngineRunOptions(EngineRunOptions, cancellationToken);
        }
        finally
        {
            _engineSettingsPersistenceGate.Release();
        }
    }

    public void ApplyMove(Move move)
    {
        AnalysisBoard.AddVariation(move);
        UpdateCurrentNodeState();
        ImportError = string.Empty;
        MarkDirty();
    }

    public void MoveBackward()
    {
        AnalysisBoard.MoveBackward();
        UpdateCurrentNodeState();
    }

    public void MoveForward()
    {
        AnalysisBoard.MoveForward();
        UpdateCurrentNodeState();
    }

    public void MoveToNode(MoveNode node)
    {
        AnalysisBoard.MoveToNode(node);
        UpdateCurrentNodeState();
    }

    public void Reset()
    {
        AnalysisBoard = CreateInitialAnalysisBoard();
        LastMove = null;
        CurrentPly = 0;
        CurrentAnalyzedGameId = null;
        GameName = "New game";
        GameMetadata = AnalyzedGameMetadata.Empty;
        ImportError = string.Empty;
        MarkDirty();
    }

    public bool TryLoadFen(string fen)
    {
        try
        {
            var position = FenSerializer.Parse(fen);
            AnalysisBoard = new AnalysisBoard(new ChessGame(position));
            LastMove = null;
            CurrentPly = 0;
            CurrentAnalyzedGameId = null;
            GameName = "Position";
            GameMetadata = AnalyzedGameMetadata.Empty;
            ImportError = string.Empty;
            MarkDirty();
            return true;
        }
        catch (Exception ex)
        {
            ImportError = $"Could not load FEN: {ex.Message}";
            return false;
        }
    }

    public bool TryLoadPgn(string pgn)
    {
        try
        {
            var metadata = ExtractPgnMetadata(pgn);
            var game = PgnSerializer.Parse(RemovePgnTagSection(pgn));
            AnalysisBoard = new AnalysisBoard(game);
            MoveToEnd();
            CurrentAnalyzedGameId = null;
            GameMetadata = metadata;
            GameName = CreateDefaultGameName(metadata);
            ImportError = string.Empty;
            MarkDirty();
            return true;
        }
        catch (Exception ex)
        {
            ImportError = $"Could not load PGN: {ex.Message}";
            return false;
        }
    }

    public EngineRunOptions AddEngine()
    {
        var engine = new EngineRunOptions
        {
            Name = EngineRunOptions.Count == 0 ? "Stockfish" : $"Engine {EngineRunOptions.Count + 1}",
            EngineType = pax.uciChessEngine.EngineServices.EngineRunOptions.UciEngineType,
            IsEnabled = true,
            Threads = Math.Max(1, Environment.ProcessorCount / 4),
            Pvs = 3,
            HashMb = 16,
            PoolSize = 1,
            IdelTimeoutMs = 5000
        };

        EngineRunOptions.Add(engine);
        SelectedEngineId = engine.Id;
        return engine;
    }

    public void DeleteEngine(EngineRunOptions engine)
    {
        EngineRunOptions.Remove(engine);
        if (SelectedEngineId == engine.Id)
            SelectedEngineId = EngineRunOptions.FirstOrDefault()?.Id ?? Guid.Empty;
    }

    public bool DeleteFromNode(MoveNode node)
    {
        if (node.Parent is null)
            return false;

        var parent = node.Parent;
        var currentWasDeleted = IsDescendantOrSelf(AnalysisBoard.CurrentNode, node);
        var removed = parent.Children.Remove(node);
        if (!removed)
            return false;

        if (currentWasDeleted)
            MoveToNode(parent);

        ImportError = string.Empty;
        MarkDirty();
        return true;
    }

    public bool MakeMainVariation(MoveNode node)
    {
        if (node.Parent is null)
            return false;

        var siblings = node.Parent.Children;
        var index = siblings.IndexOf(node);
        if (index <= 0)
            return false;

        siblings.RemoveAt(index);
        siblings.Insert(0, node);
        MoveToNode(node);
        ImportError = string.Empty;
        MarkDirty();
        return true;
    }

    public void RenameGame(string name)
    {
        GameName = NormalizeName(name);
        MarkDirty();
    }

    public void UpdateGameMetadata(AnalyzedGameMetadata metadata)
    {
        GameMetadata = Normalize(metadata);
        MarkDirty();
    }

    public void MarkAnalysisChanged()
    {
        MarkDirty();
    }

    public Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGamesAsync(CancellationToken cancellationToken = default)
        => _repository.ListAnalyzedGames(cancellationToken);

    public async Task LoadAnalyzedGameAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var details = await _repository.LoadAnalyzedGameDetails(id, cancellationToken)
            ?? throw new InvalidOperationException("Selected analysis was not found.");

        AnalysisBoard = details.AnalysisBoard;
        CurrentAnalyzedGameId = details.Id;
        GameName = NormalizeName(details.Name);
        GameMetadata = Normalize(details.Metadata);
        ImportError = string.Empty;
        IsDirty = false;
        UpdateCurrentNodeState();
    }

    public async Task SaveCurrentAnalysisAsync(bool saveAs = false, CancellationToken cancellationToken = default)
    {
        var targetId = saveAs ? null : CurrentAnalyzedGameId;
        var savedId = await _repository.SaveAnalyzedGame(
            NormalizeName(GameName),
            AnalysisBoard,
            targetId,
            GameMetadata,
            cancellationToken);

        CurrentAnalyzedGameId = savedId;
        IsDirty = false;
    }

    public Task<IReadOnlyList<AnalyzedGameAnalysisRunSummary>> ListCurrentGameAnalysisRunsAsync(CancellationToken cancellationToken = default)
        => CurrentAnalyzedGameId is { } id
            ? _repository.ListAnalyzedGameAnalysisRuns(id, cancellationToken)
            : Task.FromResult<IReadOnlyList<AnalyzedGameAnalysisRunSummary>>([]);

    public Task<AnalyzedGameAnalysisRunDetails?> LoadGameAnalysisRunAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _repository.LoadAnalyzedGameAnalysisRun(id, cancellationToken);

    public async Task<Guid> SaveCurrentGameAnalysisRunAsync(
        string name,
        GameAnalysisRunSnapshot snapshot,
        Guid? id = default,
        CancellationToken cancellationToken = default)
    {
        if (CurrentAnalyzedGameId is not { } analyzedGameId)
            throw new InvalidOperationException("Save the game before saving game analysis runs.");

        return await _repository.SaveAnalyzedGameAnalysisRun(
            analyzedGameId,
            NormalizeName(name),
            snapshot,
            id,
            cancellationToken);
    }

    private void MoveToEnd()
    {
        var current = AnalysisBoard.Root;
        while (current.MainLine is not null)
            current = current.MainLine;

        AnalysisBoard.MoveToNode(current);
        UpdateCurrentNodeState();
    }

    private void UpdateCurrentNodeState()
    {
        LastMove = AnalysisBoard.CurrentNode.Move;
        CurrentPly = GetNodePly(AnalysisBoard.CurrentNode);
    }

    private static int GetNodePly(MoveNode node)
    {
        var ply = 0;
        var current = node;
        while (current.Move is not null)
        {
            ply++;
            current = current.Parent!;
        }

        return ply;
    }

    private string BuildMainLinePgn()
    {
        var builder = new StringBuilder();
        var current = AnalysisBoard.Root.MainLine;
        var ply = 0;
        while (current is not null)
        {
            if (builder.Length > 0)
                builder.Append(' ');

            if (ply % 2 == 0)
                builder.Append((ply / 2) + 1).Append('.');

            if (ply % 2 == 0)
                builder.Append(' ');

            builder.Append(current.San);
            current = current.MainLine;
            ply++;
        }

        return builder.ToString();
    }

    private static AnalysisBoard CreateInitialAnalysisBoard()
        => new(new ChessGame(FenSerializer.Parse(DefaultFen)));

    private static AnalyzedGameMetadata ExtractPgnMetadata(string pgn)
    {
        Dictionary<string, string>? tags = null;

        foreach (var rawLine in pgn.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                if (tags is not null)
                    break;

                continue;
            }

            if (line[0] != '[')
                break;

            var close = line.LastIndexOf(']');
            var quoteStart = line.IndexOf('"');
            var quoteEnd = line.LastIndexOf('"');
            if (close < 0 || quoteStart < 2 || quoteEnd <= quoteStart)
                continue;

            var tagName = line[1..quoteStart].Trim();
            var value = UnescapePgnTagValue(line[(quoteStart + 1)..quoteEnd]);
            tags ??= new(StringComparer.OrdinalIgnoreCase);
            tags[tagName] = value;
        }

        if (tags is null)
            return AnalyzedGameMetadata.Empty;

        return Normalize(new AnalyzedGameMetadata
        {
            Event = GetTag(tags, "Event"),
            Site = GetTag(tags, "Site"),
            Date = GetTag(tags, "Date"),
            Round = GetTag(tags, "Round"),
            White = GetTag(tags, "White"),
            Black = GetTag(tags, "Black"),
            Result = GetTag(tags, "Result")
        });
    }

    private static string RemovePgnTagSection(string pgn)
    {
        var lines = pgn.Split('\n');
        var firstMoveLine = 0;
        var sawTag = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                if (sawTag)
                {
                    firstMoveLine = i + 1;
                    break;
                }

                continue;
            }

            if (line[0] == '[')
            {
                sawTag = true;
                firstMoveLine = i + 1;
                continue;
            }

            firstMoveLine = sawTag ? i : 0;
            break;
        }

        if (!sawTag)
            return pgn;

        return string.Join('\n', lines.Skip(firstMoveLine));
    }

    private static string? GetTag(Dictionary<string, string> tags, string name)
        => tags.TryGetValue(name, out var value) ? value : null;

    private static string UnescapePgnTagValue(string value)
    {
        if (!value.Contains('\\'))
            return value;

        return value.Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    private static string CreateDefaultGameName(AnalyzedGameMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.White) && !string.IsNullOrWhiteSpace(metadata.Black))
            return $"{metadata.White} vs {metadata.Black}";

        if (!string.IsNullOrWhiteSpace(metadata.Event))
            return metadata.Event!;

        return "Imported game";
    }

    private static string NormalizeName(string name)
        => string.IsNullOrWhiteSpace(name) ? "Analysis" : name.Trim();

    private static AnalyzedGameMetadata Normalize(AnalyzedGameMetadata? metadata)
    {
        var value = metadata ?? AnalyzedGameMetadata.Empty;
        return new()
        {
            Event = NormalizeTag(value.Event),
            Site = NormalizeTag(value.Site),
            Date = NormalizeTag(value.Date),
            Round = NormalizeTag(value.Round),
            White = NormalizeTag(value.White),
            Black = NormalizeTag(value.Black),
            Result = NormalizeTag(value.Result)
        };
    }

    private static string? NormalizeTag(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsDescendantOrSelf(MoveNode candidate, MoveNode branchRoot)
    {
        var current = candidate;
        while (current is not null)
        {
            if (current == branchRoot)
                return true;

            current = current.Parent;
        }

        return false;
    }

    private void MarkDirty()
    {
        IsDirty = true;
    }

    private async Task SaveEngineSettingsWhenQuietAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_engineSettingsSaveDebounce, cts.Token);
            await SaveEngineSettingsAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            Interlocked.CompareExchange(ref _engineSettingsSaveCts, null, cts);
            cts.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        var cts = Interlocked.Exchange(ref _engineSettingsSaveCts, null);
        if (cts is not null)
        {
            cts.Cancel();

            try
            {
                await SaveEngineSettingsAsync();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        _engineSettingsPersistenceGate.Dispose();
    }
}
