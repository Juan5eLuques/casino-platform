using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GameCatalogSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalTags",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Games",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LaunchId",
                table: "Games",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxBet",
                table: "Games",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinBet",
                table: "Games",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RTP",
                table: "Games",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Volatility",
                table: "Games",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameLaunchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LaunchUrl = table.Column<string>(type: "text", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLaunchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameLaunchLogs_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameLaunchLogs_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameLaunchLogs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameLaunchLogs_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LaunchEndpointTemplate = table.Column<string>(type: "text", nullable: false),
                    RequiresSessionToken = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsRealMode = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsDemoMode = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultMeta = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_ProviderId",
                table: "Games",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_BrandId",
                table: "GameLaunchLogs",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_CreatedAt",
                table: "GameLaunchLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_GameId",
                table: "GameLaunchLogs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_PlayerId",
                table: "GameLaunchLogs",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_Provider_CreatedAt",
                table: "GameLaunchLogs",
                columns: new[] { "Provider", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameLaunchLogs_SessionId",
                table: "GameLaunchLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameProviders_Code",
                table: "GameProviders",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_GameProviders_ProviderId",
                table: "Games",
                column: "ProviderId",
                principalTable: "GameProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_GameProviders_ProviderId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameLaunchLogs");

            migrationBuilder.DropTable(
                name: "GameProviders");

            migrationBuilder.DropIndex(
                name: "IX_Games_ProviderId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AdditionalTags",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "LaunchId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MaxBet",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MinBet",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RTP",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Volatility",
                table: "Games");
        }
    }
}
