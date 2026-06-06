using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.BlazorChess.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiEngineOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EngineType",
                table: "EngineRunOptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "UCI");

            migrationBuilder.AddColumn<string>(
                name: "ExtraOptions",
                table: "EngineRunOptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "EngineRunOptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "WeightsPath",
                table: "EngineRunOptions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EngineType",
                table: "EngineRunOptions");

            migrationBuilder.DropColumn(
                name: "ExtraOptions",
                table: "EngineRunOptions");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "EngineRunOptions");

            migrationBuilder.DropColumn(
                name: "WeightsPath",
                table: "EngineRunOptions");
        }
    }
}
