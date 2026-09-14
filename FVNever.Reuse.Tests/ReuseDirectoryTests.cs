// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class ReuseDirectoryTests
{
    [Fact]
    public Task ReuseTomlFilesAreApplied() => DoWithTempDir(async dir =>
    {
        await WriteFile(dir, "REUSE.toml", """
            version = 1

            [[annotations]]
            path = "**"
            SPDX-FileCopyrightText = "2026 Root Holder"
            SPDX-License-Identifier = "MIT"
            """);
        await WriteFile(dir, "sub/REUSE.toml", """
            version = 1

            [[annotations]]
            path = "*.bin"
            precedence = "aggregate"
            SPDX-FileCopyrightText = "2026 Sub Holder"
            SPDX-License-Identifier = "CC0-1.0"
            """);
        await WriteFile(dir, "plain.txt", "Nothing here.");
        await WriteFile(dir, "header.cs", """
            // SPDX-FileCopyrightText: 2026 Header Holder
            //
            // SPDX-License-Identifier: Apache-2.0
            """);
        await WriteFile(dir, "sub/data.bin", "data");
        await WriteFile(dir, "sub/image.bin", "image");
        await WriteFile(dir, "sub/image.bin.license", """
            SPDX-FileCopyrightText: 2026 Sidecar Holder

            SPDX-License-Identifier: GPL-3.0-or-later
            """);

        var entries = await ReadEntries(dir);

        AssertEntry(entries["plain.txt"], ["MIT"], ["2026 Root Holder"]);
        AssertEntry(entries["header.cs"], ["Apache-2.0"], ["2026 Header Holder"]);
        AssertEntry(entries["sub/data.bin"], ["CC0-1.0", "MIT"], ["2026 Sub Holder", "2026 Root Holder"]);
        AssertEntry(
            entries["sub/image.bin"],
            ["CC0-1.0", "GPL-3.0-or-later"],
            ["2026 Sub Holder", "2026 Sidecar Holder"]);
    });

    [Fact]
    public Task IgnoredReuseTomlIsNotRead() => DoWithTempDir(async dir =>
    {
        await WriteFile(dir, ".gitignore", "ignored/\n");
        await WriteFile(dir, "ignored/REUSE.toml", "this is not a valid TOML file");
        await WriteFile(dir, "plain.txt", "Nothing here.");
        await WriteFile(dir, "REUSE.toml", """
            version = 1

            [[annotations]]
            path = "plain.txt"
            SPDX-License-Identifier = "MIT"
            """);

        var entries = await ReadEntries(dir);

        AssertEntry(entries["plain.txt"], ["MIT"], []);
    });

    [Fact]
    public Task Dep5IsApplied() => DoWithTempDir(async dir =>
    {
        await WriteFile(dir, ".reuse/dep5", """
            Files: *.txt
            Copyright: 2026 Dep5 Holder
            License: MIT
            """);
        await WriteFile(dir, "plain.txt", "Nothing here.");

        var entries = await ReadEntries(dir);

        AssertEntry(entries["plain.txt"], ["MIT"], ["2026 Dep5 Holder"]);
    });

    [Fact]
    public Task Dep5AndReuseTomlCannotBeUsedTogether() => DoWithTempDir(async dir =>
    {
        await WriteFile(dir, ".reuse/dep5", """
            Files: *
            Copyright: 2026 Dep5 Holder
            License: MIT
            """);
        await WriteFile(dir, "REUSE.toml", "version = 1");

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => ReuseDirectory.ReadEntries(dir));
        Assert.Contains("must not be used simultaneously", exception.Message);
    });

    private static async Task<Dictionary<string, ReuseFileEntry>> ReadEntries(AbsolutePath dir)
    {
        var entries = await ReuseDirectory.ReadEntries(dir);
        return entries.ToDictionary(x => ((LocalPath)x.Path).RelativeTo(dir).Value.Replace('\\', '/'));
    }

    private static void AssertEntry(ReuseFileEntry entry, string[] licenses, string[] copyrights)
    {
        Assert.Equal(licenses, entry.LicenseIdentifiers);
        Assert.Equal(copyrights, entry.CopyrightNotices.Select(x => x.FullText));
    }

    private static async Task WriteFile(AbsolutePath dir, string relativePath, string content)
    {
        var path = dir / relativePath;
        Directory.CreateDirectory(path.Parent!.Value.Value);
        await path.WriteAllTextAsync(content);
    }

    private static async Task DoWithTempDir(Func<AbsolutePath, Task> action)
    {
        var tempDir = Temporary.CreateTempFolder();
        try
        {
            await action(tempDir);
        }
        finally
        {
            tempDir.DeleteDirectoryRecursively();
        }
    }
}
// REUSE-IgnoreEnd
