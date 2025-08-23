// SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class UpdateFileTests
{
    private static readonly ImmutableArray<string> Licenses = ["MIT"];
    private static readonly ImmutableArray<string> Contributors = ["Friedrich von Never <friedrich@fornever.me>"];

    [Fact]
    public Task UpdateEmptyFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.cs", Licenses, Contributors),
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
        x => new ReuseFileEntry(x / "file.cs", Licenses, Contributors),
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
        x => new ReuseFileEntry(x / "file.xml", Licenses, Contributors),
        """
        <!--
        SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

        SPDX-License-Identifier: MIT
        -->
        """);

    [Fact]
    public Task UpdatePowerShellFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.ps1", Licenses, Contributors),
        """
        # SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>
        #
        # SPDX-License-Identifier: MIT
        """);

    [Fact]
    public Task UpdateCssFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.css", Licenses, Contributors),
        """
        /*
        SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

        SPDX-License-Identifier: MIT
        */
        """);

    [Fact]
    public Task UpdateSqlFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.sql", Licenses, Contributors),
        """
        -- SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>
        --
        -- SPDX-License-Identifier: MIT
        """);

    [Fact]
    public Task UpdateTextFile() => DoTest(
        "",
        x => new ReuseFileEntry(x / "file.txt", Licenses, Contributors),
        """
        SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

        SPDX-License-Identifier: MIT
        """);

    [Fact]
    public Task UpdateBinaryFile() => DoWithTempDir(async dir =>
    {
        var file = dir / "file.png";
        byte[] bytes = [0];
        await file.WriteAllBytesAsync(bytes);

        var entry = new ReuseFileEntry(file, Licenses, Contributors);
        await entry.UpdateFileContents();

        Assert.Equal(bytes, await entry.Path.ReadAllBytesAsync());
        var licenseFile = file.Parent!.Value / "file.png.license";
        var actualLicenseContent = await licenseFile.ReadAllTextAsync();
        var expectedLicense = """
                              SPDX-FileCopyrightText: Friedrich von Never <friedrich@fornever.me>

                              SPDX-License-Identifier: MIT
                              """;
        Assert.Equal(
            expectedLicense.ReplaceLineEndings("\n").TrimEnd(),
            actualLicenseContent.ReplaceLineEndings("\n").TrimEnd());
    });

    private static Task DoTest(
        string initContent,
        Func<AbsolutePath, ReuseFileEntry> entryGenerator,
        string expectedContent) => DoWithTempDir(async tempDir =>
    {
        var entry = entryGenerator(tempDir);
        await entry.Path.WriteAllTextAsync(initContent);
        await entry.UpdateFileContents();
        var resultContent = await entry.Path.ReadAllTextAsync();
        Assert.Equal(
            expectedContent.ReplaceLineEndings("\n").TrimEnd(),
            resultContent.ReplaceLineEndings("\n").TrimEnd());
    });

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
