using FinancialApp.Model;
using FinancialMcp.Application.CreditCards.ListCreditCards;
using FinancialMcp.Application.Tests.Support;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `list_credit_cards`: ordering, ClosingDay/DueDay/PaymentAccountId mapping,
/// the always-`Credit` Kind (sourced from the entity's override, not handler branching),
/// and full DTO mapping.
/// </summary>
public class ListCreditCardsQueryHandlerTests
{
    [Fact]
    public async Task Orders_credit_cards_by_display_name()
    {
        using var db = new SqliteInMemoryDatabase();
        await using (var seed = db.NewContext())
        {
            var payer = RevisionSeed.NewAccount("Payer");
            seed.Accounts.Add(payer);
            seed.CreditCards.Add(RevisionSeed.NewCreditCard("Zeta Card", payer));
            seed.CreditCards.Add(RevisionSeed.NewCreditCard("Alpha Card", payer));
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new ListCreditCardsQueryHandler(ctx).Handle(new ListCreditCardsQuery(), CancellationToken.None);

        result.Select(c => c.DisplayName).Should().Equal("Alpha Card", "Zeta Card");
    }

    [Fact]
    public async Task Maps_closing_day_due_day_and_payment_account_id_correctly()
    {
        using var db = new SqliteInMemoryDatabase();
        var payer = RevisionSeed.NewAccount("Payer");
        var card = RevisionSeed.NewCreditCard("Card", payer, closingDay: 10, dueDay: 20);

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(payer);
            seed.CreditCards.Add(card);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new ListCreditCardsQueryHandler(ctx).Handle(new ListCreditCardsQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.ClosingDay.Should().Be(10);
        dto.DueDay.Should().Be(20);
        dto.PaymentAccountId.Should().Be(payer.Id);
    }

    [Fact]
    public async Task Kind_is_always_credit()
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
        var result = await new ListCreditCardsQueryHandler(ctx).Handle(new ListCreditCardsQuery(), CancellationToken.None);

        result.Should().ContainSingle().Which.Kind.Should().Be(nameof(FinancialAccountKind.Credit));
    }

    [Fact]
    public async Task Maps_every_dto_field()
    {
        using var db = new SqliteInMemoryDatabase();
        var payer = RevisionSeed.NewAccount("Payer");
        var card = RevisionSeed.NewCreditCard("Nubank Card", payer, closingDay: 7, dueDay: 14);
        card.InitialAmount = -250m;

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(payer);
            seed.CreditCards.Add(card);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new ListCreditCardsQueryHandler(ctx).Handle(new ListCreditCardsQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(card.Id);
        dto.DisplayName.Should().Be("Nubank Card");
        dto.BankCode.Should().Be("260");
        dto.InitialAmount.Should().Be(-250m);
        dto.Kind.Should().Be(nameof(FinancialAccountKind.Credit));
        dto.BaseCurrencyCode.Should().Be("BRL");
        dto.ClosingDay.Should().Be(7);
        dto.DueDay.Should().Be(14);
        dto.PaymentAccountId.Should().Be(payer.Id);
    }
}
