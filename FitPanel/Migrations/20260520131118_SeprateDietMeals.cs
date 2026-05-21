using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class SeprateDietMeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealItems_Diets_DietId",
                table: "MealItems");

            migrationBuilder.RenameColumn(
                name: "DietId",
                table: "MealItems",
                newName: "DietMealId");

            migrationBuilder.RenameIndex(
                name: "IX_MealItems_DietId",
                table: "MealItems",
                newName: "IX_MealItems_DietMealId");

            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                table: "MealItems",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "MealItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DietMeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DietId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietMeals_Diets_DietId",
                        column: x => x.DietId,
                        principalTable: "Diets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietMeals_DietId",
                table: "DietMeals",
                column: "DietId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealItems_DietMeals_DietMealId",
                table: "MealItems",
                column: "DietMealId",
                principalTable: "DietMeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealItems_DietMeals_DietMealId",
                table: "MealItems");

            migrationBuilder.DropTable(
                name: "DietMeals");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "MealItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MealItems");

            migrationBuilder.RenameColumn(
                name: "DietMealId",
                table: "MealItems",
                newName: "DietId");

            migrationBuilder.RenameIndex(
                name: "IX_MealItems_DietMealId",
                table: "MealItems",
                newName: "IX_MealItems_DietId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealItems_Diets_DietId",
                table: "MealItems",
                column: "DietId",
                principalTable: "Diets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
