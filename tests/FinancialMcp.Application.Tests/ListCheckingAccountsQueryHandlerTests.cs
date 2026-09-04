using FinancialApp.Model;
using FinancialMcp.Application.Accounts.ListAccounts;
using FinancialMcp.Application.Tests.Support;
using FinancialMcp.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Card #20 — `list_checking_accounts`: ordering, CreditCardIds aggregation, TPH exclusion
/// of credit-card rows, and DTO mapping. Requires a real <see cref="SqliteInMemoryDatabase"/>
/// for the TPH <c>is CreditCard</c> filter and the CreditCards navigation fix-up.
/// </summary>
public class ListCheckingAccountsQueryHandlerTests
{
    [Fact]
    public async Task Orders_accounts_by_display_name()
    {
        using var db = new SqliteInMemoryDatabase();
        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(RevisionSeed.NewAccount("Zurich"));
            seed.Accounts.Add(RevisionSeed.NewAccount("Alpha"));
            seed.Accounts.Add(RevisionSeed.NewAccount("Mercury"));
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new ListCheckingAccountsQueryHandler(ctx)
            .Handle(new ListCheckingAccountsQuery(), CancellationToken.None);

        result.Select(a => a.DisplayName).Should().Equal("Alpha", "Mercury", "Zurich");
    }

    [Fact]
    public async Task Maps_credit_card_ids_for_zero_one_and_two_cards_per_account()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid noCards, oneCard, twoCards, card1, card2;

        await using (var seed = db.NewContext())
        {
            var noCardsAccount = RevisionSeed.NewAccount("NoCards");
            var oneCardAccount = RevisionSeed.NewAccount("OneCard");
            var twoCardsAccount = RevisionSeed.NewAccount("TwoCards");
            seed.Accounts.AddRange(noCardsAccount, oneCardAccount, twoCardsAccount);

            var cardA = RevisionSeed.NewCreditCard("CardA", oneCardAccount);
            var cardB = RevisionSeed.NewCreditCard("CardB", twoCardsAccount);
            var cardC = RevisionSeed.NewCreditCard("CardC", twoCardsAccount);
            seed.CreditCards.AddRange(cardA, cardB, cardC);

            await seed.SaveChangesAsync();

            noCards = noCardsAccount.Id;
            oneCard = oneCardAccount.Id;
            twoCards = twoCardsAccount.Id;
            card1 = cardB.Id;
            card2 = cardC.Id;
        }

        await using var ctx = db.NewContext();
        var result = await new ListCheckingAccountsQueryHandler(ctx)
            .Handle(new ListCheckingAccountsQuery(), CancellationToken.None);

        result.Single(a => a.Id == noCards).CreditCardIds.Should().BeEmpty();
        result.Single(a => a.Id == oneCard).CreditCardIds.Should().HaveCount(1);
        result.Single(a => a.Id == twoCards).CreditCardIds.Should().BeEquivalentTo([card1, card2]);
    }

    [Fact]
    public async Task Excludes_credit_card_kind_rows_from_the_result_even_though_they_share_the_accounts_table()
    {
        using var db = new SqliteInMemoryDatabase();
        Guid checkingId;

        await using (var seed = db.NewContext())
        {
            var checking = RevisionSeed.NewAccount("Checking");
            seed.Accounts.Add(checking);
            seed.CreditCards.Add(RevisionSeed.NewCreditCard("Card", checking));
            await seed.SaveChangesAsync();
            checkingId = checking.Id;
        }

        await using var ctx = db.NewContext();
        var result = await new ListCheckingAccountsQueryHandler(ctx)
            .Handle(new ListCheckingAccountsQuery(), CancellationToken.None);

        result.Should().ContainSingle(a => a.Id == checkingId);
    }

    [Fact]
    public async Task Maps_every_dto_field()
    {
        using var db = new SqliteInMemoryDatabase();
        var account = RevisionSeed.NewAccount("Nubank");
        account.InitialAmount = 1500m;

        await using (var seed = db.NewContext())
        {
            seed.Accounts.Add(account);
            await seed.SaveChangesAsync();
        }

        await using var ctx = db.NewContext();
        var result = await new ListCheckingAccountsQueryHandler(ctx)
            .Handle(new ListCheckingAccountsQuery(), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(account.Id);
        dto.DisplayName.Should().Be("Nubank");
        dto.BankCode.Should().Be("260");
        dto.InitialAmount.Should().Be(1500m);
        dto.Kind.Should().Be(nameof(FinancialAccountKind.Debit));
        dto.BaseCurrencyCode.Should().Be("BRL");
        dto.CreditCardIds.Should().BeEmpty();
    }
}
