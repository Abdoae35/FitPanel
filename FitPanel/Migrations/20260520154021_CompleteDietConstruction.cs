using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDietConstruction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Link",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "Link",
                table: "AlternativeItems");

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "DietMeals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Link",
                table: "DietMeals");

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "MealItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "AlternativeItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
