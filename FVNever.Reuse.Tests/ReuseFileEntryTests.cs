// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

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
    [InlineData("Copyright 3M Company", "3M Company")]
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
        var content = await ParseFileEntry(fileContent);
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
    public async Task OptionalCopyrightSignIsKeptInFullText()
    {
        var copyright = await ParseCopyrightNotice("Copyright (C) 2026 Foo");
        Assert.Equal("(C) 2026 Foo", copyright.FullText);
        Assert.Equal([new CopyrightNotice.YearItem.SingleYear(2026)], copyright.Years);
        Assert.Equal("Foo", copyright.HolderName);
    }

    [Fact]
    public async Task YearsFailOnBrokenSeparators()
    {
        var copyright = await ParseCopyrightNotice("Copyright 2022-2023,, 2025 Friedrich von Never");
        Assert.Equal([
            new CopyrightNotice.YearItem.YearRange(2022, 2023)
        ], copyright.Years);
        Assert.Equal(",, 2025 Friedrich von Never", copyright.HolderName);
    }

    [Fact]
    public async Task ContactAddressIsParsedCorrectly()
    {
        var copyright = await ParseCopyrightNotice(
            "SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>");
        Assert.Equal([new CopyrightNotice.YearItem.SingleYear(2026)], copyright.Years);
        Assert.Equal("Friedrich von Never", copyright.HolderName);
        Assert.Equal("friedrich@fornever.me", copyright.ContactAddress);
    }

    [Theory]
    [InlineData("Copyright frob <frob@example.com", "frob <frob@example.com")]
    [InlineData("Copyright frob <frob@example.com> trailing", "frob <frob@example.com> trailing")]
    [InlineData("Copyright frob <frob<@example.com>", "frob <frob<@example.com>")]
    public async Task CorruptedContactAddressIsPartOfName(string fileContent, string holderName)
    {
        var copyright = await ParseCopyrightNotice(fileContent);
        Assert.Equal(holderName, copyright.HolderName);
        Assert.Null(copyright.ContactAddress);
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
