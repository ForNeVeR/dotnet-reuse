// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using FVNever.Reuse.ReuseToml;

namespace FVNever.Reuse.Tests;

public class ReuseTomlGlobTests
{
    [Theory]
    [InlineData("*.txt", "a.txt", true)]
    [InlineData("*.txt", "d/a.txt", false)]
    [InlineData("*.TXT", "a.txt", false)]
    [InlineData("**/*.txt", "a.txt", true)]
    [InlineData("**/*.txt", "d/e/a.txt", true)]
    [InlineData("**", "a/b.txt", true)]
    [InlineData("src/**", "src/a/b.txt", true)]
    [InlineData("src/**", "srcx/a.txt", false)]
    [InlineData("src**", "src/a/b.txt", true)]
    [InlineData(".idea/**/**", ".idea/workspace.xml", true)]
    [InlineData(".idea/**/**", "idea/workspace.xml", false)]
    // Per the specification, "**/" is the same as "**": it matches everything including the path separators,
    // and so it is not anchored to a separator.
    [InlineData("doc/**/README", "doc/a/b/README", true)]
    [InlineData("doc/**/README", "doc/MYREADME", true)]
    [InlineData("foo*bar", "fooXbar", true)]
    [InlineData("foo*bar", "foobar", true)]
    [InlineData("foo*bar", "foo/bar", false)]
    [InlineData("a/b.txt", "a/b.txt", true)]
    [InlineData("a/b.txt", "a/bXtxt", false)]
    [InlineData("a+(b).txt", "a+(b).txt", true)]
    public void GlobMatches(string glob, string path, bool expected)
    {
        Assert.Equal(expected, ReuseTomlGlob.Translate(glob).IsMatch(path));
    }

    [Theory]
    [InlineData(@"\*.txt", "*.txt", true)]
    [InlineData(@"\*.txt", "a.txt", false)]
    [InlineData(@"\*.txt", "*foo.txt", false)]
    [InlineData(@"a\\b", @"a\b", true)]
    [InlineData(@"a\\*", @"a\foo", true)]
    [InlineData(@"\a.txt", "a.txt", true)]
    public void EscapesAreHandled(string glob, string path, bool expected)
    {
        Assert.Equal(expected, ReuseTomlGlob.Translate(glob).IsMatch(path));
    }

    [Theory]
    [InlineData(@"assets\")]
    [InlineData(@"a\\b\")] // a verbatim backslash, and then a dangling one
    public void TrailingBackslashIsRejected(string glob)
    {
        Assert.Throws<Exception>(() => ReuseTomlGlob.Translate(glob));
    }
}
