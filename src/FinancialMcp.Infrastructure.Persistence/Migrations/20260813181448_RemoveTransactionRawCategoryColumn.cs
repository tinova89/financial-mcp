using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTransactionRawCategoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawCategory",
                table: "transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawCategory",
                table: "transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
