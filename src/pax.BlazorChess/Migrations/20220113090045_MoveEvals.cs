using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Migrations
{
    public partial class MoveEvals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlack",
                table: "Evaluations");

            migrationBuilder.CreateTable(
                name: "DbMoveEvaluation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartMove = table.Column<int>(type: "INTEGER", nullable: false),
                    Pv = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineMoves = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: false),
                    MoveQuality = table.Column<byte>(type: "INTEGER", nullable: false),
                    DbGameId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMoveEvaluation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbMoveEvaluation_Games_DbGameId",
                        column: x => x.DbGameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbMoveEvaluation_DbGameId",
                table: "DbMoveEvaluation",
                column: "DbGameId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbMoveEvaluation");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlack",
                table: "Evaluations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
