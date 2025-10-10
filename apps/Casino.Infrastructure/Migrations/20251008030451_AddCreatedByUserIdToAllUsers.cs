using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToAllUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByCashierId",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "CreatedByCashierId",
                table: "Players",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Players_CreatedByCashierId",
                table: "Players",
                newName: "IX_Players_CreatedByUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "BackofficeUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackofficeUsers_CreatedByUserId",
                table: "BackofficeUsers",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BackofficeUsers_BackofficeUsers_CreatedByUserId",
                table: "BackofficeUsers",
                column: "CreatedByUserId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByUserId",
                table: "Players",
                column: "CreatedByUserId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackofficeUsers_BackofficeUsers_CreatedByUserId",
                table: "BackofficeUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByUserId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_BackofficeUsers_CreatedByUserId",
                table: "BackofficeUsers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "BackofficeUsers");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Players",
                newName: "CreatedByCashierId");

            migrationBuilder.RenameIndex(
                name: "IX_Players_CreatedByUserId",
                table: "Players",
                newName: "IX_Players_CreatedByCashierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_BackofficeUsers_CreatedByCashierId",
                table: "Players",
                column: "CreatedByCashierId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
