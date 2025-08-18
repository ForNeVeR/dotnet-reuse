// SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse.Dep5;

/// <summary>
/// Represents a single stanza (section) in a Debian control-format file.
/// Each stanza is a collection of key/value fields in their original order.
/// </summary>
internal class Stanza
{
    /// <summary>
    /// The fields of the stanza as a list of key/value tuples in the order they were parsed.
    /// The first item is the field name (key) and the second is the field value.
    /// </summary>
    public List<(string, string)> Fields { get; } = new();
}
