using FinancialMcp.Application.Common.Services;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Covers CLAUDE.md > Testing Guidelines > "Billing cycle closing/due date,
/// including rolling to the next business day on weekends".
/// </summary>
public class BusinessDayHelperTests
{
    [Theory]
    [InlineData(2026, 8, 8, 2026, 8, 10)]  // Saturday -> Monday
    [InlineData(2026, 8, 9, 2026, 8, 10)]  // Sunday -> Monday
    [InlineData(2026, 8, 10, 2026, 8, 10)] // Monday stays
    public void NextBusinessDay_should_roll_weekend_to_monday(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var result = BusinessDayHelper.NextBusinessDay(new DateOnly(year, month, day));

        result.Should().Be(new DateOnly(expectedYear, expectedMonth, expectedDay));
    }
}
