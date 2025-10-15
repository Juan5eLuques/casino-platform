using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionTypeToWalletTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing varchar column first
            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "WalletTransactions");

            // Add the new integer column for enum
            migrationBuilder.AddColumn<int>(
                name: "TransactionType",
                table: "WalletTransactions",
                type: "integer",
                nullable: true,
                comment: "Transaction type enum: 0=MINT, 1=TRANSFER, 2=BET, 3=WIN, 4=ROLLBACK, 5=DEPOSIT, 6=WITHDRAWAL, 7=BONUS, 8=ADJUSTMENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the integer column
            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "WalletTransactions");

            // Restore the varchar column
            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "WalletTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "Transaction type: DEPOSIT, WITHDRAWAL, TRANSFER, BONUS, MINT, BURN, BET, WIN, ROLLBACK, ADJUSTMENT");
        }
    }
}
