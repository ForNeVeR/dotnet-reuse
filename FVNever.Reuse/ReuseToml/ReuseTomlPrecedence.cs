// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse.ReuseToml;

/// <summary>
/// The <c>precedence</c> value of an <c>[[annotations]]</c> table in a <c>REUSE.toml</c> file.
/// See <a href="https://reuse.software/spec-3.3/#reusetoml">the specification</a>.
/// </summary>
internal enum ReuseTomlPrecedence
{
    /// <summary>
    /// Use the licensing information from the file itself (or its <c>.license</c> file) if available; otherwise, use
    /// the information from the closest <c>REUSE.toml</c> covering the file. This is the default.
    /// </summary>
    Closest,

    /// <summary>
    /// Always associate the table's licensing information with the covered files, and then apply the
    /// <see cref="Closest"/> logic as well.
    /// </summary>
    Aggregate,

    /// <summary>
    /// Associate the table's licensing information with the covered files, ignoring any information closer to the
    /// files. The table closest to the project root is authoritative.
    /// </summary>
    Override
}
