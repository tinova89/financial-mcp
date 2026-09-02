using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Card14TransactionStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReconciledDate",
                table: "transactions",
                newName: "ConfirmedDate");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_reconciled_date",
                table: "transactions",
                newName: "ix_transactions_confirmed_date");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "transactions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "transactions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "transactions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedForReviewAt",
                table: "transactions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transaction_revisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Recurrence = table.Column<int>(type: "integer", nullable: false),
                    CurrentInstallment = table.Column<int>(type: "integer", nullable: true),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transaction_revisions_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transaction_revisions_transaction_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "transaction_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transaction_revisions_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_revisions_AccountId",
                table: "transaction_revisions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_revisions_CategoryId",
                table: "transaction_revisions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_revisions_transaction_id",
                table: "transaction_revisions",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_revisions");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "ConfirmedDate",
                table: "transactions",
                newName: "ReconciledDate");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_confirmed_date",
                table: "transactions",
                newName: "ix_transactions_reconciled_date");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
