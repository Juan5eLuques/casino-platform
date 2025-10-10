using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByCashierIdToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByCashierId",
                table: "Players",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedByCashierId",
                table: "Players",
                column: "CreatedByCashierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByCashierId",
                table: "Players",
                column: "CreatedByCashierId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByCashierId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_CreatedByCashierId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedByCashierId",
                table: "Players");
        }
    }
}
