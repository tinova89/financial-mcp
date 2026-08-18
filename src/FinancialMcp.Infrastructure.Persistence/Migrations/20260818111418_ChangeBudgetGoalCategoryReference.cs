using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBudgetGoalCategoryReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_budget_goals_category_year_month",
                table: "budget_goals");

            migrationBuilder.DropColumn(
                name: "RawCategory",
                table: "budget_goals");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "budget_goals",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "budget_goals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RawCategoryId",
                table: "budget_goals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ux_budget_goals_category_year_month",
                table: "budget_goals",
                columns: new[] { "RawCategoryId", "Year", "Month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_budget_goals_transaction_categories_RawCategoryId",
                table: "budget_goals",
                column: "RawCategoryId",
                principalTable: "transaction_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_goals_transaction_categories_RawCategoryId",
                table: "budget_goals");

            migrationBuilder.DropIndex(
                name: "ux_budget_goals_category_year_month",
                table: "budget_goals");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "budget_goals");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "budget_goals");

            migrationBuilder.DropColumn(
                name: "RawCategoryId",
                table: "budget_goals");

            migrationBuilder.AddColumn<string>(
                name: "RawCategory",
                table: "budget_goals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_budget_goals_category_year_month",
                table: "budget_goals",
                columns: new[] { "RawCategory", "Year", "Month" },
                unique: true);
        }
    }
}
