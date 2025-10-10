using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Players",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "BackofficeUsers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromUserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "BACKOFFICE or PLAYER"),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "BACKOFFICE or PLAYER"),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, comment: "Always positive amount"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Actor role"),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Unique key for idempotency"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_BackofficeUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "BackofficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_BrandId",
                table: "WalletTransactions",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedByUserId",
                table: "WalletTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_FromUserId",
                table: "WalletTransactions",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_IdempotencyKey",
                table: "WalletTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ToUserId",
                table: "WalletTransactions",
                column: "ToUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "BackofficeUsers");
        }
    }
}
