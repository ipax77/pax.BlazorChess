﻿using Microsoft.EntityFrameworkCore;
using pax.chess;

namespace pax.BlazorChess.Models;

public class ChessContext : DbContext
{
    public virtual DbSet<DbGame> Games { get; set; }
    public virtual DbSet<DbPosition> Positions { get; set; }

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