using System.Reflection;
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

    public async Task StoreEngineRunOptions(List<EngineRunOptions> engineRunOptions, CancellationToken cancellationToken = default)
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
        var items = await _context.AnalyzedGames
            .AsNoTracking()
            .Select(a => new AnalyzedGameSummary(a.Id, a.Name, a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(a => a.UpdatedAt)
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

    public async Task<Guid> SaveAnalyzedGame(string name, AnalysisBoard analysisBoard, Guid? id = default, CancellationToken cancellationToken = default)
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
