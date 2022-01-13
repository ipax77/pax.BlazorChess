using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Migrations
{
    public partial class MoveEvals2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbMoveEvaluation_Games_DbGameId",
                table: "DbMoveEvaluation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DbMoveEvaluation",
                table: "DbMoveEvaluation");

            migrationBuilder.RenameTable(
                name: "DbMoveEvaluation",
                newName: "MoveEvaluations");

            migrationBuilder.RenameIndex(
                name: "IX_DbMoveEvaluation_DbGameId",
                table: "MoveEvaluations",
                newName: "IX_MoveEvaluations_DbGameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MoveEvaluations",
                table: "MoveEvaluations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MoveEvaluations_Games_DbGameId",
                table: "MoveEvaluations",
                column: "DbGameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MoveEvaluations_Games_DbGameId",
                table: "MoveEvaluations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MoveEvaluations",
                table: "MoveEvaluations");

            migrationBuilder.RenameTable(
                name: "MoveEvaluations",
                newName: "DbMoveEvaluation");

            migrationBuilder.RenameIndex(
                name: "IX_MoveEvaluations_DbGameId",
                table: "DbMoveEvaluation",
                newName: "IX_DbMoveEvaluation_DbGameId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbMoveEvaluation",
                table: "DbMoveEvaluation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DbMoveEvaluation_Games_DbGameId",
                table: "DbMoveEvaluation",
                column: "DbGameId",
                principalTable: "Games",
                principalColumn: "Id");
        }
    }
}
