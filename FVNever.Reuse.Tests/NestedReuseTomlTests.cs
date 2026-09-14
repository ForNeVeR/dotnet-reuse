// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using FVNever.Reuse.ReuseToml;
using TruePath;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class NestedReuseTomlTests
{
    private static readonly AbsolutePath Root = AbsolutePath.CurrentWorkingDirectory / "project";
    private static readonly AbsolutePath File = Root / "sub/file.txt";

    [Fact]
    public void ClosestIsUsedWithoutLocalInformation()
    {
        var toml = new NestedReuseToml([Toml(Root, "closest", "**", "2026 Root", "MIT")]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["MIT"], ["2026 Root"]);
    }

    [Fact]
    public void LocalInformationWinsOverClosest()
    {
        var toml = new NestedReuseToml([Toml(Root, "closest", "**", "2026 Root", "MIT")]);
        var entry = toml.Resolve(File, LocalEntry());
        AssertEntry(entry, ["Apache-2.0"], ["2026 Local"]);
    }

    [Fact]
    public void AggregateIsCombinedWithLocalInformation()
    {
        var toml = new NestedReuseToml([Toml(Root, "aggregate", "**", "2026 Root", "MIT")]);
        var entry = toml.Resolve(File, LocalEntry());
        AssertEntry(entry, ["MIT", "Apache-2.0"], ["2026 Root", "2026 Local"]);
    }

    [Fact]
    public void AggregateIsUsedWithoutLocalInformation()
    {
        var toml = new NestedReuseToml([Toml(Root, "aggregate", "**", "2026 Root", "MIT")]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["MIT"], ["2026 Root"]);
    }

    [Fact]
    public void AggregateIsCombinedWithClosestFromDeeperFile()
    {
        var toml = new NestedReuseToml([
            Toml(Root, "aggregate", "**", "2026 Root", "MIT"),
            Toml(Root / "sub", "closest", "*.txt", "2026 Sub", "CC0-1.0")
        ]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["MIT", "CC0-1.0"], ["2026 Root", "2026 Sub"]);
    }

    [Fact]
    public void OverrideIgnoresLocalInformationAndDeeperFiles()
    {
        var toml = new NestedReuseToml([
            Toml(Root / "sub", "aggregate", "*.txt", "2026 Sub", "CC0-1.0"),
            Toml(Root, "override", "**", "2026 Root", "MIT")
        ]);
        var entry = toml.Resolve(File, LocalEntry());
        AssertEntry(entry, ["MIT"], ["2026 Root"]);
    }

    [Fact]
    public void OverrideClosestToRootWins()
    {
        var toml = new NestedReuseToml([
            Toml(Root / "sub", "override", "*.txt", "2026 Sub", "CC0-1.0"),
            Toml(Root, "override", "**", "2026 Root", "MIT")
        ]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["MIT"], ["2026 Root"]);
    }

    [Fact]
    public void OverrideKeepsAggregateFromCloserToRoot()
    {
        var toml = new NestedReuseToml([
            Toml(Root, "aggregate", "**", "2026 Root", "MIT"),
            Toml(Root / "sub", "override", "*.txt", "2026 Sub", "CC0-1.0")
        ]);
        var entry = toml.Resolve(File, LocalEntry());
        AssertEntry(entry, ["MIT", "CC0-1.0"], ["2026 Root", "2026 Sub"]);
    }

    [Fact]
    public void DeepestClosestWins()
    {
        var toml = new NestedReuseToml([
            Toml(Root / "sub", "closest", "*.txt", "2026 Sub", "CC0-1.0"),
            Toml(Root, "closest", "**", "2026 Root", "MIT")
        ]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["CC0-1.0"], ["2026 Sub"]);
    }

    [Fact]
    public void ClosestCopyrightAndLicenseAreChosenSeparately()
    {
        var toml = new NestedReuseToml([
            Toml(Root, "closest", "**", "2026 Root", "MIT"),
            Toml(Root / "sub", "closest", "*.txt", null, "CC0-1.0")
        ]);
        var entry = toml.Resolve(File, null);
        AssertEntry(entry, ["CC0-1.0"], ["2026 Root"]);
    }

    [Fact]
    public void FilesOutsideOfTomlDirectoryAreNotCovered()
    {
        var toml = new NestedReuseToml([Toml(Root / "sub", "closest", "**", "2026 Sub", "MIT")]);
        Assert.Null(toml.Resolve(Root / "file.txt", null));
        Assert.Null(toml.Resolve(Root / "subx/file.txt", null));
    }

    [Fact]
    public void LocalInformationIsKeptWithoutMatches()
    {
        var toml = new NestedReuseToml([Toml(Root, "override", "*.cs", "2026 Root", "MIT")]);
        Assert.Null(toml.Resolve(File, null));
        AssertEntry(toml.Resolve(File, LocalEntry()), ["Apache-2.0"], ["2026 Local"]);
    }

    private static ReuseTomlFile Toml(
        AbsolutePath directory,
        string precedence,
        string path,
        string? copyright,
        string license)
    {
        var copyrightLine = copyright == null ? "" : $"SPDX-FileCopyrightText = \"{copyright}\"";
        return ReuseTomlFile.Read(
            $"""
             version = 1

             [[annotations]]
             path = "{path}"
             precedence = "{precedence}"
             {copyrightLine}
             SPDX-License-Identifier = "{license}"
             """,
            directory);
    }

    private static ReuseFileEntry LocalEntry() =>
        new(File, ["Apache-2.0"], [CopyrightNotice.Parse("SPDX-FileCopyrightText: 2026 Local")]);

    private static void AssertEntry(ReuseFileEntry? entry, string[] licenses, string[] copyrights)
    {
        Assert.NotNull(entry);
        Assert.Equal(File, entry.Path);
        Assert.Equal(licenses, entry.LicenseIdentifiers);
        Assert.Equal(copyrights, entry.CopyrightNotices.Select(x => x.FullText));
    }
}
// REUSE-IgnoreEnd
