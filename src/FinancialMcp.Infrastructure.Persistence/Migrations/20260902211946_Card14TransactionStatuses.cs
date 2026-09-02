using System;
using FinancialMcp.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialMcp.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Card #14 — new transaction statuses (Revision / Scheduled / Confirmed).
    ///
    /// Schema: shrinks every free-text field on <c>transactions</c> to <c>varchar(256)</c>,
    /// adds the per-status timestamps (<c>SubmittedForReviewAt</c>/<c>ScheduledAt</c>/
    /// <c>ConfirmedAt</c>), and creates <c>transaction_revisions</c>.
    ///
    /// Data: truncates any over-long description, remaps the stored <c>Status</c> values
    /// (<c>Conciliado</c> → <c>Confirmed</c>, <c>Agendado</c>/<c>Nconciliado</c> → <c>Scheduled</c>,
    /// via <see cref="TransactionStatusRemap"/>), and backfills <c>ConfirmedAt</c>/<c>ScheduledAt</c>
    /// for existing rows from the best available existing date column. Nothing maps to
    /// <c>Revision</c>, so <c>SubmittedForReviewAt</c> stays null for every existing row.
    /// </summary>
    public partial class Card14TransactionStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trim descriptions that won't fit the new 256-char column before altering it,
            // so the ALTER can't fail on legacy data.
            migrationBuilder.Sql(
                "UPDATE transactions SET \"Description\" = left(\"Description\", 256) WHERE length(\"Description\") > 256;");

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

            // Remap legacy status values to the Card #14 enum (single source of truth: TransactionStatusRemap).
            migrationBuilder.Sql(
                $"UPDATE transactions SET \"Status\" = {TransactionStatusRemap.ToSqlCase("\"Status\"")};");

            // Backfill the per-status timestamps for existing rows from the best available date column.
            migrationBuilder.Sql(
                $"UPDATE transactions SET \"ConfirmedAt\" = COALESCE(" +
                "\"ReconciledDate\"::timestamptz, \"InvoiceDueDate\"::timestamptz, " +
                "\"ActualDate\"::timestamptz, \"ExpectedDate\"::timestamptz, \"CreatedAt\") " +
                $"WHERE \"Status\" = {(int)TransactionStatus.Confirmed} AND \"ConfirmedAt\" IS NULL;");

            migrationBuilder.Sql(
                $"UPDATE transactions SET \"ScheduledAt\" = COALESCE(" +
                "\"ExpectedDate\"::timestamptz, \"ActualDate\"::timestamptz, \"CreatedAt\") " +
                $"WHERE \"Status\" = {(int)TransactionStatus.Scheduled} AND \"ScheduledAt\" IS NULL;");

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
                    ReconciledDate = table.Column<DateOnly>(type: "date", nullable: true),
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

            // Best-effort reverse remap: Confirmed → legacy Reconciled (1); Scheduled stays 2.
            // The legacy Reconciled/Unreconciled split is not recoverable — every pre-migration
            // Scheduled or Unreconciled row is now Scheduled.
            migrationBuilder.Sql(
                $"UPDATE transactions SET \"Status\" = {TransactionStatusRemap.LegacyReconciled} " +
                $"WHERE \"Status\" = {(int)TransactionStatus.Confirmed};");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "transactions");

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
