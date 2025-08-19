// SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class UpdateFileTests
{
    [Fact]
    public Task UpdateEmptyFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.cs", ["MIT"], ["Friedrich von Never <friedrich@fornever.me>"]),
        """
        // SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>
        //
        // SPDX-License-Identifier: MIT
        """);

    [Fact]
    public Task UpdateExistingFile() => DoTest(
        """
        // SPDX-FileCopyrightText: None
        //

        namespace Foo;
        """,
        x => new ReuseFileEntry(x / "file.cs", ["MIT"], ["Friedrich von Never <friedrich@fornever.me>"]),
        """
        // SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>
        //
        // SPDX-License-Identifier: MIT
        //

        namespace Foo;
        """);

    [Fact]
    public Task UpdateXmlFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.xml", ["MIT"], ["Friedrich von Never <friedrich@fornever.me>"]),
        """
        <!--
        SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

        SPDX-License-Identifier: MIT
        -->
        """);

    [Fact]
    public Task UpdatePowerShellFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.ps1", ["MIT"], ["Friedrich von Never <friedrich@fornever.me>"]),
        """
        # SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>
        #
        # SPDX-License-Identifier: MIT
        """);

    [Fact]
    public Task UpdateTextFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.txt", ["MIT"], ["Friedrich von Never <friedrich@fornever.me>"]),
        """
        SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

        SPDX-License-Identifier: MIT
        """);

    private static async Task DoTest(
        string initContent,
        Func<AbsolutePath, ReuseFileEntry> entryGenerator,
        string expectedContent)
    {
        var tempDir = Temporary.CreateTempFolder();
        try
        {
            var entry = entryGenerator(tempDir);
            await entry.Path.WriteAllTextAsync(initContent);
            await entry.UpdateFileContents();
            var resultContent = await entry.Path.ReadAllTextAsync();
            Assert.Equal(
                expectedContent.ReplaceLineEndings("\n").TrimEnd(),
                resultContent.ReplaceLineEndings("\n").TrimEnd());
        }
        finally
        {
            tempDir.DeleteDirectoryRecursively();
        }
    }
}
// REUSE-IgnoreEnd
