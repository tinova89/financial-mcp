using FinancialMcp.Application.Accounts.GetAccount;
using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.Tests.Support;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `get_checking_account`: known id, unknown id, and a credit card's own id
/// (indistinguishable from unknown, since the query filters `!(a is CreditCard)`).
/// </summary>
public class GetCheckingAccountQueryHandlerTests
{
    [Fact]
    public async Task Returns_the_dto_for_a_known_checking_account_id()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Nubank");

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var dto = await new GetCheckingAccountQueryHandler(ctx)
            .Handle(new GetCheckingAccountQuery(account.Id), CancellationToken.None);

        dto.Id.Should().Be(account.Id);
        dto.DisplayName.Should().Be("Nubank");
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_id()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();

        var act = async () => await new GetCheckingAccountQueryHandler(ctx)
            .Handle(new GetCheckingAccountQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_not_found_for_a_credit_card_id_passed_as_the_checking_account_id()
    {
        using var db = new SqliteInMemoryDatabase();
        var paymentAccount = RevisionSeed.NewAccount("Payer");
        var creditCard = RevisionSeed.NewCreditCard("Card", paymentAccount);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(paymentAccount);
            seed.CreditCards.Add(creditCard);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var act = async () => await new GetCheckingAccountQueryHandler(ctx)
            .Handle(new GetCheckingAccountQuery(creditCard.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>(
            "a credit card id looks exactly like an unknown id to this tool, since it filters !(a is CreditCard)");
    }
}
