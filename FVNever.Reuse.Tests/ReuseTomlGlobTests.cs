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
}
