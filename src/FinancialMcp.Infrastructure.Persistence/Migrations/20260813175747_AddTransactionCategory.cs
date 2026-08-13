using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "transaction_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transaction_categories_transaction_categories_ParentCategor~",
                        column: x => x.ParentCategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_category_id",
                table: "transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_categories_parent_name",
                table: "transaction_categories",
                columns: new[] { "ParentCategoryId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_transaction_categories_CategoryId",
                table: "transactions",
                column: "CategoryId",
                principalTable: "transaction_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_transaction_categories_CategoryId",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "transaction_categories");

            migrationBuilder.DropIndex(
                name: "ix_transactions_category_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "transactions");
        }
    }
}
