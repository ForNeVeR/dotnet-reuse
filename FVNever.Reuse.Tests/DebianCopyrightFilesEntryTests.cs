// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using FVNever.Reuse.Dep5;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class DebianCopyrightFilesEntryTests
{
    [Fact]
    public void CopyrightLinesAreTrimmedFromStart()
    {
        var stanza = new Stanza();
        stanza.Fields.Add(("Files", "*"));
        stanza.Fields.Add(("Copyright", "2020 Jane <j@x>\n  2021 Bob"));
        stanza.Fields.Add(("License", "MIT"));

        var entry = DebianCopyrightFilesEntry.Read(stanza);
        Assert.NotNull(entry);
        Assert.Equal(2, entry.Copyright.Length);

        var notice = entry.Copyright[1];
        Assert.Equal("2021 Bob", notice.FullText);
        Assert.Equal([new CopyrightNotice.YearItem.SingleYear(2021)], notice.Years);
        Assert.Equal("Bob", notice.HolderName);
    }
}
// REUSE-IgnoreEnd
