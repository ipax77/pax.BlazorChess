using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Board.Storage;
using pax.BlazorChess.Db.Entities;
using pax.chess;
using pax.chess.Analyze;
using pax.uciChessEngine.EngineServices;

namespace pax.BlazorChess.Db;

public sealed class EfChessBoardRepository : IChessBoardRepository
{
    private static readonly FieldInfo EngineIdField =
        typeof(EngineRunOptions).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find EngineRunOptions Id backing field.");

    private readonly ChessContext _context;

    public EfChessBoardRepository(ChessContext context)
    {
        _context = context;
    }

    public async Task<List<EngineRunOptions>> GetEngineRunOptions(CancellationToken cancellationToken = default)
    {
        var entities = await _context.EngineRunOptions
            .AsNoTracking()
            .OrderBy(e => e.Name ?? e.BinaryPath)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task StoreEngineRunOptions(IReadOnlyList<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engineRunOptions);

        var incomingById = engineRunOptions.ToDictionary(o => o.Id);
        var existing = await _context.EngineRunOptions.ToListAsync(cancellationToken);
        var existingById = existing.ToDictionary(e => e.Id);

        var now = DateTimeOffset.UtcNow;

        foreach (var entity in existing)
        {
            if (!incomingById.ContainsKey(entity.Id))
                _context.EngineRunOptions.Remove(entity);
        }

        foreach (var option in engineRunOptions)
        {
            if (existingById.TryGetValue(option.Id, out var entity))
            {
                Apply(option, entity, now);
                continue;
            }

            entity = new EngineRunOptionEntity
            {
                Id = option.Id,
                CreatedAt = now
            };
            Apply(option, entity, now);
            await _context.EngineRunOptions.AddAsync(entity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalyzedGameSummary>> ListAnalyzedGames(CancellationToken cancellationToken = default)
    {
        var items = await _context.Database
            .SqlQueryRaw<AnalyzedGameSummaryProjection>(
                "SELECT Id, Name, UpdatedAt FROM AnalyzedGames ORDER BY UpdatedAt DESC")
            .ToListAsync(cancellationToken);

        return items
            .Select(static a => new AnalyzedGameSummary(a.Id, a.Name, a.UpdatedAt))
            .ToList();
    }

    public async Task<AnalysisBoard?> LoadAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AnalyzedGames
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity is null)
            return null;

        return AnalysisSerializer.Restore(entity.AnalysisJson);
    }

    public async Task<AnalyzedGameDetails?> LoadAnalyzedGameDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AnalyzedGames
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity is null)
            return null;

        return new AnalyzedGameDetails(
            entity.Id,
            entity.Name,
            AnalysisSerializer.Restore(entity.AnalysisJson),
            ToMetadata(entity),
            entity.UpdatedAt);
    }

    public async Task<Guid> SaveAnalyzedGame(
        string name,
        AnalysisBoard analysisBoard,
        Guid? id = default,
        AnalyzedGameMetadata? metadata = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(analysisBoard);

        var json = AnalysisSerializer.Serialize(analysisBoard);
        var now = DateTimeOffset.UtcNow;
        var targetId = id ?? Guid.NewGuid();

        var entity = await _context.AnalyzedGames.FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken);

        if (entity is null)
        {
            entity = new AnalyzedGameEntity
            {
                Id = targetId,
                CreatedAt = now
            };
            _context.AnalyzedGames.Add(entity);
        }

        entity.Name = name;
        entity.InitialFen = FenSerializer.Serialize(analysisBoard.ChessGame.InitialPosition);
        entity.Pgn = PgnSerializer.Serialize(analysisBoard.ChessGame);
        entity.AnalysisJson = json;
        Apply(metadata, entity);
        entity.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task DeleteAnalyzedGame(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AnalyzedGames.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
            return;

        _context.AnalyzedGames.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalyzedGameAnalysisRunSummary>> ListAnalyzedGameAnalysisRuns(
        Guid analyzedGameId,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.Database
            .SqlQueryRaw<AnalyzedGameAnalysisRunSummaryProjection>(
                """
                SELECT Id, AnalyzedGameId, Name, UpdatedAt
                FROM AnalyzedGameAnalysisRuns
                WHERE AnalyzedGameId = @analyzedGameId
                ORDER BY UpdatedAt DESC
                """,
                new SqliteParameter("@analyzedGameId", analyzedGameId))
            .ToListAsync(cancellationToken);

        return items
            .Select(static a => new AnalyzedGameAnalysisRunSummary(a.Id, a.AnalyzedGameId, a.Name, a.UpdatedAt))
            .ToList();
    }

    public async Task<AnalyzedGameAnalysisRunDetails?> LoadAnalyzedGameAnalysisRun(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AnalyzedGameAnalysisRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity is null)
            return null;

        return new AnalyzedGameAnalysisRunDetails(
            entity.Id,
            entity.AnalyzedGameId,
            entity.Name,
            GameAnalysisRunSerializer.Restore(entity.AnalysisJson),
            entity.UpdatedAt);
    }

    public async Task<Guid> SaveAnalyzedGameAnalysisRun(
        Guid analyzedGameId,
        string name,
        GameAnalysisRunSnapshot snapshot,
        Guid? id = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(snapshot);

        var now = DateTimeOffset.UtcNow;
        var targetId = id ?? Guid.NewGuid();
        var json = GameAnalysisRunSerializer.Serialize(snapshot);
        var entity = await _context.AnalyzedGameAnalysisRuns
            .FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken);

        if (entity is null)
        {
            entity = new AnalyzedGameAnalysisRunEntity
            {
                Id = targetId,
                AnalyzedGameId = analyzedGameId,
                CreatedAt = now
            };
            _context.AnalyzedGameAnalysisRuns.Add(entity);
        }

        entity.AnalyzedGameId = analyzedGameId;
        entity.Name = NormalizeRequired(name);
        entity.AnalysisJson = json;
        entity.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public Task DeleteAnalyzedGameAnalysisRun(Guid id, CancellationToken cancellationToken = default)
        => _context.AnalyzedGameAnalysisRuns
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

    private static AnalyzedGameMetadata ToMetadata(AnalyzedGameEntity entity)
        => new()
        {
            Event = entity.Event,
            Site = entity.Site,
            Date = entity.Date,
            Round = entity.Round,
            White = entity.White,
            Black = entity.Black,
            Result = entity.Result
        };

    private static void Apply(AnalyzedGameMetadata? metadata, AnalyzedGameEntity entity)
    {
        var value = metadata ?? AnalyzedGameMetadata.Empty;
        entity.Event = Normalize(value.Event);
        entity.Site = Normalize(value.Site);
        entity.Date = Normalize(value.Date);
        entity.Round = Normalize(value.Round);
        entity.White = Normalize(value.White);
        entity.Black = Normalize(value.Black);
        entity.Result = Normalize(value.Result);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(string value)
        => string.IsNullOrWhiteSpace(value) ? "Analysis" : value.Trim();

    private sealed class AnalyzedGameSummaryProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private sealed class AnalyzedGameAnalysisRunSummaryProjection
    {
        public Guid Id { get; set; }
        public Guid AnalyzedGameId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private static EngineRunOptions ToDomain(EngineRunOptionEntity entity)
    {
        var opt = new EngineRunOptions
        {
            BinaryPath = entity.BinaryPath,
            Name = entity.Name,
            EngineType = string.IsNullOrWhiteSpace(entity.EngineType)
                ? EngineRunOptions.UciEngineType
                : entity.EngineType,
            WeightsPath = entity.WeightsPath,
            ExtraOptions = entity.ExtraOptions,
            IsEnabled = entity.IsEnabled,
            Threads = entity.Threads,
            Pvs = entity.Pvs,
            HashMb = entity.HashMb,
            PoolSize = entity.PoolSize,
            IdelTimeoutMs = entity.IdelTimeoutMs
        };

        EngineIdField.SetValue(opt, entity.Id);
        return opt;
    }

    private static void Apply(EngineRunOptions option, EngineRunOptionEntity entity, DateTimeOffset now)
    {
        entity.BinaryPath = option.BinaryPath;
        entity.Name = option.Name;
        entity.EngineType = string.IsNullOrWhiteSpace(option.EngineType)
            ? EngineRunOptions.UciEngineType
            : option.EngineType;
        entity.WeightsPath = option.WeightsPath;
        entity.ExtraOptions = option.ExtraOptions;
        entity.IsEnabled = option.IsEnabled;
        entity.Threads = option.Threads;
        entity.Pvs = option.Pvs;
        entity.HashMb = option.HashMb;
        entity.PoolSize = option.PoolSize;
        entity.IdelTimeoutMs = option.IdelTimeoutMs;
        entity.UpdatedAt = now;
    }
}
