// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse.Tests;

public class YearParserTests
{
    [Fact]
    public void ParseYearsValidCases()
    {
        DoTest("", [], "");
        DoTest("2026– 2027", [new CopyrightNotice.YearItem.YearRange(2026, 2027)], "");
        DoTest(" 2026– 2027", [new CopyrightNotice.YearItem.YearRange(2026, 2027)], "");
        DoTest("2026, 2027", [
            new CopyrightNotice.YearItem.SingleYear(2026),
            new CopyrightNotice.YearItem.SingleYear(2027)
        ], "");
        DoTest("2022-2023, 2025", [
            new CopyrightNotice.YearItem.YearRange(2022, 2023),
            new CopyrightNotice.YearItem.SingleYear(2025)
        ], "");
        DoTest("2026, 2026– 2027 test", [
            new CopyrightNotice.YearItem.SingleYear(2026),
            new CopyrightNotice.YearItem.YearRange(2026, 2027)
        ], "test");
    }

    [Fact]
    public void ParseYearsErrorHandling()
    {
        DoTest(",2026", [], ",2026");
        DoTest("-2026", [], "-2026");
        DoTest("2026–", [new CopyrightNotice.YearItem.SingleYear(2026)], "–");
        DoTest("2026,", [new CopyrightNotice.YearItem.SingleYear(2026)], ",");
        DoTest("2026,,", [new CopyrightNotice.YearItem.SingleYear(2026)], ",,");
        DoTest("2026–2027–2028", [new CopyrightNotice.YearItem.YearRange(2026, 2027)], "–2028");
        DoTest("2026 2027", [new CopyrightNotice.YearItem.SingleYear(2026)], "2027");
    }

    private static void DoTest(string input, CopyrightNotice.YearItem[] expectedYears, string expectedRest)
    {
        var (years, rest) = YearParser.Parse(input);
        Assert.Equal(expectedYears, years);
        Assert.Equal(expectedRest, rest);
    }
}
