using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDietRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "MealItems");

            migrationBuilder.AddColumn<string>(
                name: "Instruction",
                table: "DietMeals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentDietMealId",
                table: "DietMeals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IngredientsJson",
                table: "CoachMealDictionaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instruction",
                table: "CoachMealDictionaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                table: "AlternativeItems",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "AlternativeItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DietMeals_ParentDietMealId",
                table: "DietMeals",
                column: "ParentDietMealId");

            migrationBuilder.AddForeignKey(
                name: "FK_DietMeals_DietMeals_ParentDietMealId",
                table: "DietMeals",
                column: "ParentDietMealId",
                principalTable: "DietMeals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DietMeals_DietMeals_ParentDietMealId",
                table: "DietMeals");

            migrationBuilder.DropIndex(
                name: "IX_DietMeals_ParentDietMealId",
                table: "DietMeals");

            migrationBuilder.DropColumn(
                name: "Instruction",
                table: "DietMeals");

            migrationBuilder.DropColumn(
                name: "ParentDietMealId",
                table: "DietMeals");

            migrationBuilder.DropColumn(
                name: "IngredientsJson",
                table: "CoachMealDictionaries");

            migrationBuilder.DropColumn(
                name: "Instruction",
                table: "CoachMealDictionaries");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "AlternativeItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "AlternativeItems");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MealItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
