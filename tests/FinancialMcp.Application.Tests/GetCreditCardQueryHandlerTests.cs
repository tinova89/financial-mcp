using FinancialMcp.Application.Common.Exceptions;
using FinancialMcp.Application.CreditCards.GetCreditCard;
using FinancialMcp.Application.Tests.Support;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `get_credit_card`: known id, unknown id, and a checking account's own id
/// (also not-found, since `db.CreditCards` is already TPH-scoped to credit cards).
/// </summary>
public class GetCreditCardQueryHandlerTests
{
    [Fact]
    public async Task Returns_the_dto_for_a_known_credit_card_id()
    {
        using var db = new SqliteInMemoryDatabase();
        var payer = RevisionSeed.NewAccount("Payer");
        var card = RevisionSeed.NewCreditCard("Card", payer);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(payer);
            seed.CreditCards.Add(card);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var dto = await new GetCreditCardQueryHandler(ctx).Handle(new GetCreditCardQuery(card.Id), CancellationToken.None);

        dto.Id.Should().Be(card.Id);
        dto.DisplayName.Should().Be("Card");
        dto.PaymentAccountId.Should().Be(payer.Id);
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_id()
    {
        using var db = new SqliteInMemoryDatabase();
        await using var ctx = db.NewContext();

        var act = async () => await new GetCreditCardQueryHandler(ctx)
            .Handle(new GetCreditCardQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_not_found_for_a_checking_account_id_passed_as_the_credit_card_id()
    {
        using var db = new SqliteInMemoryDatabase();
        var checkingAccount = RevisionSeed.NewAccount("Checking");

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(checkingAccount);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var act = async () => await new GetCreditCardQueryHandler(ctx)
            .Handle(new GetCreditCardQuery(checkingAccount.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("db.CreditCards is already TPH-scoped, so a checking account id never matches");
    }
}
