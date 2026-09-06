using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class ReuseFileEntryTests
{
    [Theory]
    [InlineData("SPDX-FileCopyrightText: frob", "frob")]
    [InlineData("Copyright (C) frob", "frob")]
    [InlineData("Copyright © frob", "frob")]
    [InlineData("© © © frob", "frob")]
    [InlineData("SPDX-FileCopyrightText: (C) © (C) frob", "frob")]
    [InlineData("Copyright ©", "")]
    public async Task NameIsParsedCorrectly(string fileContent, string holderName)
    {
        var notice = await ParseCopyrightNotice(fileContent);
        Assert.Equal(holderName, notice.HolderName);
    }

    [Theory]
    [InlineData("SPDX-FileCopyrightText frob")]
    [InlineData("Copyright")]
    public async Task InvalidCopyrightIsNotParsed(string fileContent)
    {
        var content = ParseFileEntry(fileContent);
        Assert.Null(content);
    }

    [Fact]
    public async Task YearsGetParsedCorrectly()
    {
        var copyright = await ParseCopyrightNotice("Copyright 2022-2023, 2025 Friedrich von Never");
        Assert.Equal([
            new CopyrightNotice.YearItem.YearRange(2022, 2023),
            new CopyrightNotice.YearItem.SingleYear(2025)
        ], copyright.Years);
    }

    [Fact]
    public async Task YearsFailOnBrokenSeparators()
    {
        var copyright = await ParseCopyrightNotice("Copyright 2022-2023,, 2025 Friedrich von Never");
        Assert.Equal([
            new CopyrightNotice.YearItem.YearRange(2022, 2023)
        ], copyright.Years);
        Assert.Equal(", 2025 Friedrich von Never", copyright.HolderName);
    }

    private static async Task<ReuseFileEntry?> ParseFileEntry(string content)
    {
        var file = Temporary.CreateTempFile();
        try
        {
            await file.WriteAllTextAsync(content, TestContext.Current.CancellationToken);
            return await ReuseFileEntry.ReadFromFile(file);
        }
        finally
        {
            file.Delete();
        }
    }

    private static async Task<CopyrightNotice> ParseCopyrightNotice(string content)
    {
        var data = await ParseFileEntry(content);
        Assert.NotNull(data);
        return data.CopyrightNotices.Single();
    }
}
// REUSE-IgnoreEnd
