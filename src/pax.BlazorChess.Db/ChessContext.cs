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
    public DbSet<AnalyzedGameAnalysisRunEntity> AnalyzedGameAnalysisRuns => Set<AnalyzedGameAnalysisRunEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EngineRunOptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BinaryPath).IsRequired();
            entity.Property(e => e.EngineType).IsRequired().HasDefaultValue("UCI");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AnalyzedGameEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.InitialFen).IsRequired();
            entity.Property(e => e.AnalysisJson).IsRequired();
            entity.Property(e => e.Event);
            entity.Property(e => e.Site);
            entity.Property(e => e.Date);
            entity.Property(e => e.Round);
            entity.Property(e => e.White);
            entity.Property(e => e.Black);
            entity.Property(e => e.Result);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AnalyzedGameAnalysisRunEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.AnalysisJson).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.AnalyzedGameId, e.UpdatedAt });
            entity.HasOne(e => e.AnalyzedGame)
                .WithMany()
                .HasForeignKey(e => e.AnalyzedGameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
