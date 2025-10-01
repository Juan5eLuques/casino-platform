using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Casino.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandDomainSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminDomain",
                table: "Brands",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorsOrigins",
                table: "Brands",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Brands",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_AdminDomain",
                table: "Brands",
                column: "AdminDomain",
                unique: true,
                filter: "\"AdminDomain\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Domain",
                table: "Brands",
                column: "Domain",
                unique: true,
                filter: "\"Domain\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_AdminDomain",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Domain",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "AdminDomain",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CorsOrigins",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Brands");
        }
    }
}
