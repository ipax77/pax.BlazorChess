using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Event = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Site = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UTCDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UTCTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    White = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Black = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WhiteElo = table.Column<short>(type: "INTEGER", nullable: false),
                    BlackElo = table.Column<short>(type: "INTEGER", nullable: false),
                    Variant = table.Column<byte>(type: "INTEGER", nullable: false),
                    ECO = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Opening = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Annotator = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TimeControl = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Result = table.Column<byte>(type: "INTEGER", nullable: false),
                    Termination = table.Column<byte>(type: "INTEGER", nullable: false),
                    EngineMoves = table.Column<string>(type: "TEXT", nullable: false),
                    HalfMoves = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Position = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbGameDbPosition",
                columns: table => new
                {
                    GamesId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbGameDbPosition", x => new { x.GamesId, x.PositionsId });
                    table.ForeignKey(
                        name: "FK_DbGameDbPosition_Games_GamesId",
                        column: x => x.GamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbGameDbPosition_Positions_PositionsId",
                        column: x => x.PositionsId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbGameDbPosition_PositionsId",
                table: "DbGameDbPosition",
                column: "PositionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Position",
                table: "Positions",
                column: "Position",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbGameDbPosition");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
