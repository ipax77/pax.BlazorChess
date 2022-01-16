using Microsoft.EntityFrameworkCore;
using pax.chess;

namespace pax.BlazorChess.Models;

public class ChessContext : DbContext
{
    # nullable disable
    public virtual DbSet<DbGame> Games { get; set; }
    public virtual DbSet<DbPosition> Positions { get; set; }
    public virtual DbSet<DbVariation> Variations { get; set; }
    public virtual DbSet<DbEvaluation> Evaluations { get; set; }
    public virtual DbSet<DbSubVariation> SubVariations { get; set; }
    public virtual DbSet<DbMoveEvaluation> MoveEvaluations { get; set; }
    # nullable enable
    public ChessContext(DbContextOptions<ChessContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPosition>(entity =>
        {
            entity.HasIndex(i => i.Position).IsUnique();
        });
    }
}
