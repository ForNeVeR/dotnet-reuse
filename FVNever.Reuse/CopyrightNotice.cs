// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse;

/// <summary>A copyright notice with all its parsed information, when possible.</summary>
/// <param name="FullText">Full text of the copyright notice, as presented in the original document.</param>
/// <remarks>
/// <para>
///     This does the best effort to parse and store the copyright notice according to the
///     <a href="https://reuse.software/spec-3.3/">REUSE Specification – Version 3.3</a>.
/// </para>
/// <para>
///     Note that the specification regarding the copyright notice format is a recommendation and not a strict
///     requirement. So, not every copyright notice <b>MUST</b> be parseable.
/// </para>
/// </remarks>
public record CopyrightNotice(
    string FullText
)
{
    /// <inheritdoc/>
    public override string ToString() => FullText;
}
