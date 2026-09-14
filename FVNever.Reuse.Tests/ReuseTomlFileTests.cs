// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using FVNever.Reuse.ReuseToml;
using TruePath;

namespace FVNever.Reuse.Tests;

// REUSE-IgnoreStart
public class ReuseTomlFileTests
{
    private static readonly AbsolutePath Root = AbsolutePath.CurrentWorkingDirectory / "project";

    [Fact]
    public void RepositoryFileIsParsed()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1
            SPDX-PackageName = "dotnet-reuse"
            SPDX-PackageSupplier = "Friedrich von Never <friedrich@fornever.me>"
            SPDX-PackageDownloadLocation = "https://github.com/ForNeVeR/dotnet-reuse"

            [[annotations]]
            path = ".idea/**/**"
            precedence = "aggregate"
            SPDX-FileCopyrightText = "2025 Friedrich von Never <friedrich@fornever.me>"
            SPDX-License-Identifier = "MIT"
            """,
            Root);

        var annotation = Assert.Single(file.Annotations);
        Assert.Equal(ReuseTomlPrecedence.Aggregate, annotation.Precedence);
        Assert.Equal<string>(["MIT"], annotation.LicenseIdentifiers);

        var notice = Assert.Single(annotation.CopyrightNotices);
        Assert.Equal("2025 Friedrich von Never <friedrich@fornever.me>", notice.FullText);
        Assert.Equal([new CopyrightNotice.YearItem.SingleYear(2025)], notice.Years);
        Assert.Equal("Friedrich von Never", notice.HolderName);
        Assert.Equal("friedrich@fornever.me", notice.ContactAddress);

        Assert.Same(annotation, file.FindAnnotation(Root / ".idea/workspace.xml"));
        Assert.Null(file.FindAnnotation(Root / "README.md"));
    }

    [Fact]
    public void ListFormsAreSupported()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1

            [[annotations]]
            path = ["*.txt", "docs/**"]
            SPDX-FileCopyrightText = ["2020 Jane", "  2021 Bob"]
            SPDX-License-Identifier = ["MIT", "CC0-1.0"]
            """,
            Root);

        var annotation = Assert.Single(file.Annotations);
        Assert.Equal<string>(["MIT", "CC0-1.0"], annotation.LicenseIdentifiers);
        Assert.Equal(["2020 Jane", "2021 Bob"], annotation.CopyrightNotices.Select(x => x.FullText));
        Assert.Same(annotation, file.FindAnnotation(Root / "a.txt"));
        Assert.Same(annotation, file.FindAnnotation(Root / "docs/a/b.md"));
        Assert.Null(file.FindAnnotation(Root / "src/a.txt"));
    }

    [Fact]
    public void InlineTablesAreSupported()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1
            annotations = [{ path = "a.txt", SPDX-License-Identifier = "MIT" }]
            """,
            Root);
        var annotation = Assert.Single(file.Annotations);
        Assert.Equal<string>(["MIT"], annotation.LicenseIdentifiers);
    }

    [Fact]
    public void OptionalKeysHaveDefaults()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1

            [[annotations]]
            path = "a.txt"
            """,
            Root);

        var annotation = Assert.Single(file.Annotations);
        Assert.Equal(ReuseTomlPrecedence.Closest, annotation.Precedence);
        Assert.Empty(annotation.CopyrightNotices);
        Assert.Empty(annotation.LicenseIdentifiers);
    }

    [Fact]
    public void FileWithoutAnnotationsIsParsed()
    {
        var file = ReuseTomlFile.Read("version = 1", Root);
        Assert.Empty(file.Annotations);
    }

    [Theory]
    [InlineData("closest", nameof(ReuseTomlPrecedence.Closest))]
    [InlineData("aggregate", nameof(ReuseTomlPrecedence.Aggregate))]
    [InlineData("override", nameof(ReuseTomlPrecedence.Override))]
    public void PrecedenceIsParsed(string value, string expected)
    {
        var file = ReuseTomlFile.Read(
            $"""
             version = 1

             [[annotations]]
             path = "a.txt"
             precedence = "{value}"
             """,
            Root);
        Assert.Equal(expected, Assert.Single(file.Annotations).Precedence.ToString());
    }

    [Fact]
    public void EscapedAsteriskIsReadFromToml()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1

            [[annotations]]
            path = "\\*.txt"
            """,
            Root);
        Assert.NotNull(file.FindAnnotation(Root / "*.txt"));
        Assert.Null(file.FindAnnotation(Root / "a.txt"));
    }

    [Fact]
    public void LastMatchingAnnotationWins()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1

            [[annotations]]
            path = "**"
            SPDX-License-Identifier = "MIT"

            [[annotations]]
            path = "src/**"
            SPDX-License-Identifier = "Apache-2.0"
            """,
            Root);

        Assert.Equal<string>(["Apache-2.0"], file.FindAnnotation(Root / "src/a/b.cs")!.LicenseIdentifiers);
        Assert.Equal<string>(["MIT"], file.FindAnnotation(Root / "b.cs")!.LicenseIdentifiers);
    }

    [Fact]
    public void FilesOutsideOfDirectoryAreNotMatched()
    {
        var file = ReuseTomlFile.Read(
            """
            version = 1

            [[annotations]]
            path = "**"
            """,
            Root / "sub");

        Assert.NotNull(file.FindAnnotation(Root / "sub/a.txt"));
        Assert.Null(file.FindAnnotation(Root / "a.txt"));
        Assert.Null(file.FindAnnotation(Root / "subx/a.txt"));
    }

    [Theory]
    [InlineData("version = = 1")]
    [InlineData("")]
    [InlineData("version = \"1\"")]
    [InlineData("version = 2")]
    [InlineData("version = 1\nannotations = 1")]
    [InlineData("version = 1\nannotations = [1]")]
    [InlineData("version = 1\n[[annotations]]\nprecedence = \"closest\"")]
    [InlineData("version = 1\n[[annotations]]\npath = []")]
    [InlineData("version = 1\n[[annotations]]\npath = 1")]
    [InlineData("version = 1\n[[annotations]]\npath = [\"a\", 1]")]
    [InlineData("version = 1\n[[annotations]]\npath = \"a\"\nprecedence = \"closer\"")]
    [InlineData("version = 1\n[[annotations]]\npath = \"a\"\nSPDX-License-Identifier = 1")]
    [InlineData("version = 1\n[[annotations]]\npath = \"a\"\nSPDX-FileCopyrightText = { a = \"b\" }")]
    public void InvalidFileThrows(string text)
    {
        var exception = Assert.ThrowsAny<Exception>(() => ReuseTomlFile.Read(text, Root));
        Assert.Contains(ReuseTomlFile.FileName, exception.Message);
    }
}
// REUSE-IgnoreEnd
