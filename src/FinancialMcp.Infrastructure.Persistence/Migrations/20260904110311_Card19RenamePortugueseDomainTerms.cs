using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Card19RenamePortugueseDomainTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BudgetAmount",
                table: "budget_goals",
                newName: "GoalAmount");

            migrationBuilder.RenameColumn(
                name: "ConfirmedDate",
                table: "transactions",
                newName: "ConfirmationDate");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_confirmed_date",
                table: "transactions",
                newName: "ix_transactions_confirmation_date");

            migrationBuilder.RenameColumn(
                name: "ConfirmedDate",
                table: "transaction_revisions",
                newName: "ConfirmationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ConfirmationDate",
                table: "transactions",
                newName: "ConfirmedDate");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_confirmation_date",
                table: "transactions",
                newName: "ix_transactions_confirmed_date");

            migrationBuilder.RenameColumn(
                name: "ConfirmationDate",
                table: "transaction_revisions",
                newName: "ConfirmedDate");

            migrationBuilder.RenameColumn(
                name: "GoalAmount",
                table: "budget_goals",
                newName: "BudgetAmount");
        }
    }
}
