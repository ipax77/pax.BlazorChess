using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyzedGameMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Black",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Event",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Round",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Site",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "White",
                table: "AnalyzedGames",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Black",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "Event",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "Round",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "Site",
                table: "AnalyzedGames");

            migrationBuilder.DropColumn(
                name: "White",
                table: "AnalyzedGames");
        }
    }
}
