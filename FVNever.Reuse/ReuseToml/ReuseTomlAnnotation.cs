// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace FVNever.Reuse.ReuseToml;

/// <summary>A single <c>[[annotations]]</c> table from a <c>REUSE.toml</c> file.</summary>
/// <param name="Paths">Regular expressions translated from the <c>path</c> globs.</param>
/// <param name="Precedence">The <c>precedence</c> value of the table.</param>
/// <param name="CopyrightNotices">Copyright notices from the <c>SPDX-FileCopyrightText</c> key.</param>
/// <param name="LicenseIdentifiers">License expressions from the <c>SPDX-License-Identifier</c> key.</param>
internal record ReuseTomlAnnotation(
    ImmutableArray<Regex> Paths,
    ReuseTomlPrecedence Precedence,
    ImmutableArray<CopyrightNotice> CopyrightNotices,
    ImmutableArray<string> LicenseIdentifiers)
{
    /// <summary>Checks whether the table covers the file.</summary>
    /// <param name="relativePath">
    /// Path of the file relative to the <c>REUSE.toml</c> directory, with forward slashes as separators.
    /// </param>
    public bool Matches(string relativePath) => Paths.Any(path => path.IsMatch(relativePath));
}
