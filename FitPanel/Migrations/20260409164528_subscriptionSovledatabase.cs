using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class subscriptionSovledatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "subscription",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "subscription",
                table: "Clients");
        }
    }
}
