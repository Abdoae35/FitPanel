using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitPanel.Migrations
{
    /// <inheritdoc />
    public partial class updateDatabaseDeleteionAndTwoProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diets_Clients_ClientId",
                table: "Diets");

            migrationBuilder.DropForeignKey(
                name: "FK_Excercises_WorkOutDays_WorkOutDayId",
                table: "Excercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOuts_Clients_ClientId",
                table: "WorkOuts");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Diets_Clients_ClientId",
                table: "Diets",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Excercises_WorkOutDays_WorkOutDayId",
                table: "Excercises",
                column: "WorkOutDayId",
                principalTable: "WorkOutDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOuts_Clients_ClientId",
                table: "WorkOuts",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diets_Clients_ClientId",
                table: "Diets");

            migrationBuilder.DropForeignKey(
                name: "FK_Excercises_WorkOutDays_WorkOutDayId",
                table: "Excercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOuts_Clients_ClientId",
                table: "WorkOuts");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Clients");

            migrationBuilder.AddForeignKey(
                name: "FK_Diets_Clients_ClientId",
                table: "Diets",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Excercises_WorkOutDays_WorkOutDayId",
                table: "Excercises",
                column: "WorkOutDayId",
                principalTable: "WorkOutDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOuts_Clients_ClientId",
                table: "WorkOuts",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
