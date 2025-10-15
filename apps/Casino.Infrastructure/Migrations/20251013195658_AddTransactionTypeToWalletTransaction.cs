using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionTypeToWalletTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "WalletTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "Transaction type: DEPOSIT, WITHDRAWAL, TRANSFER, BONUS, MINT, BURN, BET, WIN, ROLLBACK, ADJUSTMENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "WalletTransactions");
        }
    }
}
