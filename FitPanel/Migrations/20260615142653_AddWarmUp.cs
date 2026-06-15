using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddWarmUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WarmUpLink",
                table: "WorkOutDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarmUpName",
                table: "WorkOutDays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachWarmUpDictionaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WarmUpName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarmUpLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachWarmUpDictionaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachWarmUpDictionaries_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachWarmUpDictionaries_CoachId",
                table: "CoachWarmUpDictionaries",
                column: "CoachId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachWarmUpDictionaries");

            migrationBuilder.DropColumn(
                name: "WarmUpLink",
                table: "WorkOutDays");

            migrationBuilder.DropColumn(
                name: "WarmUpName",
                table: "WorkOutDays");
        }
    }
}
