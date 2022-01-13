﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pax.BlazorChess.Models;

#nullable disable

namespace pax.BlazorChess.Migrations
{
    [DbContext(typeof(ChessContext))]
    partial class ChessContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.1");

            modelBuilder.Entity("DbGameDbPosition", b =>
                {
                    b.Property<int>("GamesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PositionsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GamesId", "PositionsId");

                    b.HasIndex("PositionsId");

                    b.ToTable("DbGameDbPosition");
                });

            modelBuilder.Entity("pax.chess.DbEvaluation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<sbyte>("Mate")
                        .HasColumnType("INTEGER");

                    b.Property<short>("Score")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Evaluations");
                });

            modelBuilder.Entity("pax.chess.DbGame", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotator")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Black")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<short>("BlackElo")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ECO")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<string>("EngineMoves")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Event")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int>("HalfMoves")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Opening")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<byte>("Result")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Site")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<byte>("Termination")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TimeControl")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<DateOnly>("UTCDate")
                        .HasColumnType("TEXT");

                    b.Property<TimeOnly>("UTCTime")
                        .HasColumnType("TEXT");

                    b.Property<byte>("Variant")
                        .HasColumnType("INTEGER");

                    b.Property<string>("White")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<short>("WhiteElo")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("pax.chess.DbMoveEvaluation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DbGameId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EngineMoves")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte>("MoveQuality")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Pv")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Score")
                        .HasColumnType("TEXT");

                    b.Property<int>("StartMove")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DbGameId");

                    b.ToTable("MoveEvaluations");
                });

            modelBuilder.Entity("pax.chess.DbPosition", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("Position")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Position")
                        .IsUnique();

                    b.ToTable("Positions");
                });

            modelBuilder.Entity("pax.chess.DbSubVariation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("EngineMovesWithSubs")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("RootStartMove")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RootVariationId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("RootVariationId");

                    b.ToTable("SubVariations");
                });

            modelBuilder.Entity("pax.chess.DbVariation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DbGameId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EngineMoves")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("EvaluationId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StartMove")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DbGameId");

                    b.HasIndex("EvaluationId");

                    b.ToTable("Variations");
                });

            modelBuilder.Entity("DbGameDbPosition", b =>
                {
                    b.HasOne("pax.chess.DbGame", null)
                        .WithMany()
                        .HasForeignKey("GamesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.chess.DbPosition", null)
                        .WithMany()
                        .HasForeignKey("PositionsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("pax.chess.DbMoveEvaluation", b =>
                {
                    b.HasOne("pax.chess.DbGame", null)
                        .WithMany("MoveEvaluations")
                        .HasForeignKey("DbGameId");
                });

            modelBuilder.Entity("pax.chess.DbSubVariation", b =>
                {
                    b.HasOne("pax.chess.DbVariation", "RootVariation")
                        .WithMany("SubVariations")
                        .HasForeignKey("RootVariationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RootVariation");
                });

            modelBuilder.Entity("pax.chess.DbVariation", b =>
                {
                    b.HasOne("pax.chess.DbGame", null)
                        .WithMany("Variations")
                        .HasForeignKey("DbGameId");

                    b.HasOne("pax.chess.DbEvaluation", "Evaluation")
                        .WithMany()
                        .HasForeignKey("EvaluationId");

                    b.Navigation("Evaluation");
                });

            modelBuilder.Entity("pax.chess.DbGame", b =>
                {
                    b.Navigation("MoveEvaluations");

                    b.Navigation("Variations");
                });

            modelBuilder.Entity("pax.chess.DbVariation", b =>
                {
                    b.Navigation("SubVariations");
                });
#pragma warning restore 612, 618
        }
    }
}
