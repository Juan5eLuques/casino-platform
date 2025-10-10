using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToUsersAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CommissionRate",
                table: "BackofficeUsers",
                newName: "CommissionPercent");

            migrationBuilder.AddColumn<decimal>(
                name: "NewBalanceFrom",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: true,
                comment: "Balance of sender AFTER transaction (null for MINT)");

            migrationBuilder.AddColumn<decimal>(
                name: "NewBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Balance of receiver AFTER transaction");

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousBalanceFrom",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: true,
                comment: "Balance of sender BEFORE transaction (null for MINT)");

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Balance of receiver BEFORE transaction");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "Players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "BackofficeUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewBalanceFrom",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "NewBalanceTo",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PreviousBalanceFrom",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PreviousBalanceTo",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "BackofficeUsers");

            migrationBuilder.RenameColumn(
                name: "CommissionPercent",
                table: "BackofficeUsers",
                newName: "CommissionRate");
        }
    }
}
