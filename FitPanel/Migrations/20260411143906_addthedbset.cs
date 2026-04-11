using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class addthedbset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlternativeItem_MealItems_MealItemId",
                table: "AlternativeItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AlternativeItem",
                table: "AlternativeItem");

            migrationBuilder.RenameTable(
                name: "AlternativeItem",
                newName: "AlternativeItems");

            migrationBuilder.RenameIndex(
                name: "IX_AlternativeItem_MealItemId",
                table: "AlternativeItems",
                newName: "IX_AlternativeItems_MealItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AlternativeItems",
                table: "AlternativeItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlternativeItems_MealItems_MealItemId",
                table: "AlternativeItems",
                column: "MealItemId",
                principalTable: "MealItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlternativeItems_MealItems_MealItemId",
                table: "AlternativeItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AlternativeItems",
                table: "AlternativeItems");

            migrationBuilder.RenameTable(
                name: "AlternativeItems",
                newName: "AlternativeItem");

            migrationBuilder.RenameIndex(
                name: "IX_AlternativeItems_MealItemId",
                table: "AlternativeItem",
                newName: "IX_AlternativeItem_MealItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AlternativeItem",
                table: "AlternativeItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlternativeItem_MealItems_MealItemId",
                table: "AlternativeItem",
                column: "MealItemId",
                principalTable: "MealItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
