using Microsoft.Extensions.Options;
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
            await _repository.StoreEngineRunOptions(EngineRunOptions.ToList(), cancellationToken);
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
        ImportError = string.Empty;
    }

    public bool TryLoadFen(string fen)
    {
        try
        {
            var position = FenSerializer.Parse(fen);
            AnalysisBoard = new AnalysisBoard(new ChessGame(position));
            LastMove = null;
            CurrentPly = 0;
            ImportError = string.Empty;
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
            var game = PgnSerializer.Parse(pgn);
            AnalysisBoard = new AnalysisBoard(game);
            MoveToEnd();
            ImportError = string.Empty;
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
        return true;
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
        List<string> parts = [];
        var current = AnalysisBoard.Root.MainLine;
        var ply = 0;
        while (current is not null)
        {
            if (ply % 2 == 0)
                parts.Add($"{(ply / 2) + 1}.");

            parts.Add(current.San);
            current = current.MainLine;
            ply++;
        }

        return parts.Count == 0 ? string.Empty : string.Join(' ', parts);
    }

    private static AnalysisBoard CreateInitialAnalysisBoard()
        => new(new ChessGame(FenSerializer.Parse(DefaultFen)));

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
