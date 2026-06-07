using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyzedGameAnalysisRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyzedGameAnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnalyzedGameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AnalysisJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzedGameAnalysisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyzedGameAnalysisRuns_AnalyzedGames_AnalyzedGameId",
                        column: x => x.AnalyzedGameId,
                        principalTable: "AnalyzedGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedGameAnalysisRuns_AnalyzedGameId_UpdatedAt",
                table: "AnalyzedGameAnalysisRuns",
                columns: new[] { "AnalyzedGameId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzedGameAnalysisRuns");
        }
    }
}
