// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using static FVNever.Reuse.CopyrightNotice.YearItem;

namespace FVNever.Reuse.Tests;

public class YearItemTests
{
    [Fact]
    public void MergeExpansiveEmpty()
    {
        DoTestExpansive([], [], null);
    }

    [Fact]
    public void MergeExpansiveOneSideEmpty()
    {
        DoTestExpansive([new SingleYear(2022)], [], new SingleYear(2022));
        DoTestExpansive([], [new YearRange(2020, 2022)], new YearRange(2020, 2022));
    }

    [Fact]
    public void MergeExpansiveSameYear()
    {
        DoTestExpansive([new SingleYear(2022)], [new SingleYear(2022)], new SingleYear(2022));
        DoTestExpansive([new SingleYear(2022)], [new YearRange(2022, 2022)], new SingleYear(2022));
    }

    [Fact]
    public void MergeExpansiveFillsGaps()
    {
        DoTestExpansive(
            [new SingleYear(2020), new YearRange(2024, 2025)],
            [new SingleYear(2022)],
            new YearRange(2020, 2025));
        DoTestExpansive([new SingleYear(2020)], [new SingleYear(2026)], new YearRange(2020, 2026));
    }

    [Fact]
    public void MergeExpansiveUnsortedInput()
    {
        DoTestExpansive(
            [new SingleYear(2025), new SingleYear(2019)],
            [new YearRange(2021, 2023), new SingleYear(2020)],
            new YearRange(2019, 2025));
    }

    [Fact]
    public void MergeExpansiveReversedRange()
    {
        DoTestExpansive([new YearRange(2027, 2026)], [], new YearRange(2026, 2027));
        DoTestExpansive([new YearRange(2027, 2026)], [new SingleYear(2028)], new YearRange(2026, 2028));
    }

    [Fact]
    public void MergeCompactEmpty()
    {
        DoTestCompact([], [], []);
    }

    [Fact]
    public void MergeCompactDuplicates()
    {
        DoTestCompact([new SingleYear(2022)], [new SingleYear(2022)], [new SingleYear(2022)]);
        DoTestCompact(
            [new YearRange(2020, 2022), new YearRange(2020, 2022)],
            [new YearRange(2020, 2022)],
            [new YearRange(2020, 2022)]);
        DoTestCompact([new YearRange(2022, 2022)], [], [new SingleYear(2022)]);
    }

    [Fact]
    public void MergeCompactAdjacentYears()
    {
        DoTestCompact(
            [new SingleYear(2022), new SingleYear(2023)],
            [new SingleYear(2024)],
            [new YearRange(2022, 2024)]);
        DoTestCompact([new YearRange(2020, 2021)], [new YearRange(2022, 2023)], [new YearRange(2020, 2023)]);
        DoTestCompact([new YearRange(2020, 2021)], [new SingleYear(2022)], [new YearRange(2020, 2022)]);
    }

    [Fact]
    public void MergeCompactOverlaps()
    {
        DoTestCompact([new YearRange(2020, 2025)], [new SingleYear(2022)], [new YearRange(2020, 2025)]);
        DoTestCompact([new YearRange(2020, 2023)], [new YearRange(2022, 2025)], [new YearRange(2020, 2025)]);
        DoTestCompact([new YearRange(2020, 2025)], [new YearRange(2021, 2022)], [new YearRange(2020, 2025)]);
    }

    [Fact]
    public void MergeCompactPreservesGaps()
    {
        DoTestCompact([new SingleYear(2020)], [new SingleYear(2022)], [new SingleYear(2020), new SingleYear(2022)]);
        DoTestCompact(
            [new YearRange(2018, 2019), new SingleYear(2024)],
            [new YearRange(2021, 2022), new SingleYear(2025)],
            [new YearRange(2018, 2019), new YearRange(2021, 2022), new YearRange(2024, 2025)]);
    }

    [Fact]
    public void MergeCompactUnsortedInput()
    {
        DoTestCompact(
            [new SingleYear(2026), new SingleYear(2020), new SingleYear(2022)],
            [new SingleYear(2021), new YearRange(2016, 2017)],
            [new YearRange(2016, 2017), new YearRange(2020, 2022), new SingleYear(2026)]);
    }

    [Fact]
    public void MergeCompactReversedRange()
    {
        DoTestCompact([new YearRange(2027, 2026)], [], [new YearRange(2026, 2027)]);
        DoTestCompact([new YearRange(2027, 2026)], [new SingleYear(2025)], [new YearRange(2025, 2027)]);
    }

    private static void DoTestExpansive(
        CopyrightNotice.YearItem[] first,
        CopyrightNotice.YearItem[] second,
        CopyrightNotice.YearItem? expected)
    {
        Assert.Equal(expected, MergeExpansive(first, second));
    }

    private static void DoTestCompact(
        CopyrightNotice.YearItem[] first,
        CopyrightNotice.YearItem[] second,
        CopyrightNotice.YearItem[] expected)
    {
        Assert.Equal(expected, MergeCompact(first, second).ToArray());
    }
}
