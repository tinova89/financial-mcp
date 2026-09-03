using FinancialMcp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinancialMcp.Application.Tests.Support;

/// <summary>
/// A throwaway relational database for handler tests that need real EF Core behavior the
/// pure in-memory provider can't give — most importantly working transactions, so
/// <c>TransactionBehavior</c>'s commit/rollback (and thus the atomicity of
/// <c>approve_revision</c>'s insert + delete) can actually be exercised.
///
/// Backed by SQLite <c>:memory:</c>; the connection is held open for the lifetime of the
/// instance so the schema/data survive between <see cref="NewContext"/> calls.
/// </summary>
internal sealed class SqliteInMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    /// <summary>A fresh context over the same underlying database (no shared change tracker).</summary>
    public ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
