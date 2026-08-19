using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "accounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Group",
                table: "accounts",
                column: "Group");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_accounts_Group",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "accounts");
        }
    }
}
