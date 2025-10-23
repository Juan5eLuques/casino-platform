using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTypeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Games",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SLOT");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Type",
                table: "Games",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Type",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Games");
        }
    }
}
