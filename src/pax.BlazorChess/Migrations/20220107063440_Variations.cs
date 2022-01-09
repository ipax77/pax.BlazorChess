using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Migrations
{
    public partial class Variations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Score = table.Column<short>(type: "INTEGER", nullable: false),
                    Mate = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    IsBlack = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Variations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartMove = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineMoves = table.Column<string>(type: "TEXT", nullable: false),
                    EvaluationId = table.Column<int>(type: "INTEGER", nullable: true),
                    DbGameId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variations_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Variations_Games_DbGameId",
                        column: x => x.DbGameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubVariations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RootStartMove = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineMovesWithSubs = table.Column<string>(type: "TEXT", nullable: false),
                    RootVariationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVariations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubVariations_Variations_RootVariationId",
                        column: x => x.RootVariationId,
                        principalTable: "Variations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubVariations_RootVariationId",
                table: "SubVariations",
                column: "RootVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_Variations_DbGameId",
                table: "Variations",
                column: "DbGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Variations_EvaluationId",
                table: "Variations",
                column: "EvaluationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubVariations");

            migrationBuilder.DropTable(
                name: "Variations");

            migrationBuilder.DropTable(
                name: "Evaluations");
        }
    }
}
