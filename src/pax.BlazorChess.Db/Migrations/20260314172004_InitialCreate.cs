using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Db.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyzedGames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    InitialFen = table.Column<string>(type: "TEXT", nullable: false),
                    Pgn = table.Column<string>(type: "TEXT", nullable: true),
                    AnalysisJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzedGames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EngineRunOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BinaryPath = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Threads = table.Column<int>(type: "INTEGER", nullable: false),
                    Pvs = table.Column<int>(type: "INTEGER", nullable: false),
                    HashMb = table.Column<int>(type: "INTEGER", nullable: false),
                    PoolSize = table.Column<int>(type: "INTEGER", nullable: false),
                    IdelTimeoutMs = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineRunOptions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzedGames");

            migrationBuilder.DropTable(
                name: "EngineRunOptions");
        }
    }
}
