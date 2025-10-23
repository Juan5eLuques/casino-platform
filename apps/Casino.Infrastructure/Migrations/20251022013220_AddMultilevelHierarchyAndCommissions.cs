using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultilevelHierarchyAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PreviousBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: true,
                comment: "Balance of receiver BEFORE transaction",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldComment: "Balance of receiver BEFORE transaction");

            migrationBuilder.AlterColumn<decimal>(
                name: "NewBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: true,
                comment: "Balance of receiver AFTER transaction",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldComment: "Balance of receiver AFTER transaction");

            migrationBuilder.AddColumn<string>(
                name: "ActorIp",
                table: "WalletTransactions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "WalletTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "WalletTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "WalletTransactions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WalletTransactions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HierarchyLevel",
                table: "BackofficeUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HierarchyPath",
                table: "BackofficeUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAdminId",
                table: "BackofficeUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionAccruals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    BaseAmount = table.Column<long>(type: "bigint", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CommissionAmount = table.Column<long>(type: "bigint", nullable: false),
                    Settled = table.Column<bool>(type: "boolean", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceRoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionAccruals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_BackofficeUsers_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "BackofficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_BackofficeUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BackofficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_Players_SourcePlayerId",
                        column: x => x.SourcePlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_Rounds_SourceRoundId",
                        column: x => x.SourceRoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_WalletTransactions_SettledTransactionId",
                        column: x => x.SettledTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommissionAccruals_WalletTransactions_SourceTransactionId",
                        column: x => x.SourceTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    TotalHandle = table.Column<long>(type: "bigint", nullable: false),
                    TotalPayouts = table.Column<long>(type: "bigint", nullable: false),
                    GrossGamingRevenue = table.Column<long>(type: "bigint", nullable: false),
                    TotalCommissionsPaid = table.Column<long>(type: "bigint", nullable: false),
                    ClosureStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyClosures_BackofficeUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "BackofficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonthlyClosures_BackofficeUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BackofficeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyClosures_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ApprovedByUserId",
                table: "WalletTransactions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackofficeUsers_HierarchyPath",
                table: "BackofficeUsers",
                column: "HierarchyPath");

            migrationBuilder.CreateIndex(
                name: "IX_BackofficeUsers_ParentAdminId",
                table: "BackofficeUsers",
                column: "ParentAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_BrandId_PeriodYear_PeriodMonth",
                table: "CommissionAccruals",
                columns: new[] { "BrandId", "PeriodYear", "PeriodMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_BrandId_UserId_PeriodYear_PeriodMonth_So~",
                table: "CommissionAccruals",
                columns: new[] { "BrandId", "UserId", "PeriodYear", "PeriodMonth", "SourceTransactionId", "SourceRoundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_ParentUserId",
                table: "CommissionAccruals",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_Settled",
                table: "CommissionAccruals",
                column: "Settled",
                filter: "\"Settled\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_SettledTransactionId",
                table: "CommissionAccruals",
                column: "SettledTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_SourcePlayerId",
                table: "CommissionAccruals",
                column: "SourcePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_SourceRoundId",
                table: "CommissionAccruals",
                column: "SourceRoundId",
                filter: "\"SourceRoundId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_SourceTransactionId",
                table: "CommissionAccruals",
                column: "SourceTransactionId",
                filter: "\"SourceTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAccruals_UserId_PeriodYear_PeriodMonth",
                table: "CommissionAccruals",
                columns: new[] { "UserId", "PeriodYear", "PeriodMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosures_BrandId_PeriodYear_PeriodMonth",
                table: "MonthlyClosures",
                columns: new[] { "BrandId", "PeriodYear", "PeriodMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosures_BrandId_UserId_PeriodYear_PeriodMonth",
                table: "MonthlyClosures",
                columns: new[] { "BrandId", "UserId", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosures_ClosedByUserId",
                table: "MonthlyClosures",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosures_ClosureStatus",
                table: "MonthlyClosures",
                column: "ClosureStatus",
                filter: "\"ClosureStatus\" != 'COMPLETED'");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosures_UserId_PeriodYear_PeriodMonth",
                table: "MonthlyClosures",
                columns: new[] { "UserId", "PeriodYear", "PeriodMonth" },
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BackofficeUsers_BackofficeUsers_ParentAdminId",
                table: "BackofficeUsers",
                column: "ParentAdminId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_BackofficeUsers_ApprovedByUserId",
                table: "WalletTransactions",
                column: "ApprovedByUserId",
                principalTable: "BackofficeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackofficeUsers_BackofficeUsers_ParentAdminId",
                table: "BackofficeUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_BackofficeUsers_ApprovedByUserId",
                table: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "CommissionAccruals");

            migrationBuilder.DropTable(
                name: "MonthlyClosures");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_ApprovedByUserId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BackofficeUsers_HierarchyPath",
                table: "BackofficeUsers");

            migrationBuilder.DropIndex(
                name: "IX_BackofficeUsers_ParentAdminId",
                table: "BackofficeUsers");

            migrationBuilder.DropColumn(
                name: "ActorIp",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "HierarchyLevel",
                table: "BackofficeUsers");

            migrationBuilder.DropColumn(
                name: "HierarchyPath",
                table: "BackofficeUsers");

            migrationBuilder.DropColumn(
                name: "ParentAdminId",
                table: "BackofficeUsers");

            migrationBuilder.AlterColumn<decimal>(
                name: "PreviousBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Balance of receiver BEFORE transaction",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                oldComment: "Balance of receiver BEFORE transaction");

            migrationBuilder.AlterColumn<decimal>(
                name: "NewBalanceTo",
                table: "WalletTransactions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Balance of receiver AFTER transaction",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                oldComment: "Balance of receiver AFTER transaction");
        }
    }
}
