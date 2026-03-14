using Microsoft.EntityFrameworkCore;
using pax.BlazorChess.Db.Entities;

namespace pax.BlazorChess.Db;

public class ChessContext : DbContext
{
    public ChessContext(DbContextOptions<ChessContext> options) : base(options)
    {
    }

    public DbSet<EngineRunOptionEntity> EngineRunOptions => Set<EngineRunOptionEntity>();
    public DbSet<AnalyzedGameEntity> AnalyzedGames => Set<AnalyzedGameEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EngineRunOptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BinaryPath).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AnalyzedGameEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.InitialFen).IsRequired();
            entity.Property(e => e.AnalysisJson).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
