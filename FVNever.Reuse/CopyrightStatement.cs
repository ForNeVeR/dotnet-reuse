// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse;

/// <summary>A copyright statement with all its parsed information, when possible.</summary>
/// <param name="FullText">Full text of the copyright statement, as presented in the original document.</param>
public record CopyrightStatement(
    string FullText
)
{
    /// <inheritdoc/>
    public override string ToString() => FullText;
}
