// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse;

/// <summary>A copyright notice with all its parsed information, when possible.</summary>
public class CopyrightNotice
{
    // REUSE-IgnoreStart

    /// <summary>
    /// Parses the copyright notice. Note that it will parse even an invalid string (for support of erroneous texts).
    /// Test with <see cref="ContainsCopyrightNotice"/> if you want to check the input for correctness.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This does the best effort to parse and store the copyright notice according to the
    ///     <a href="https://reuse.software/spec-3.3/">REUSE Specification – Version 3.3</a>.
    /// </para>
    /// <para>
    ///     Note that most of the specification regarding the copyright notice format is a recommendation and not a
    ///     strict requirement. So, not every aspect of the copyright notice <b>MUST</b> be parseable.
    /// </para>
    /// </remarks>
    public static CopyrightNotice Parse(string fullText)
    {
        return ParsePrefixless(SkipMandatoryPrefix(fullText));
    }

    /// <summary>
    /// <para>
    ///     According to the <a href="https://reuse.software/spec-3.3/">REUSE Specification – Version 3.3</a>, a
    ///     copyright notice <b>MUST</b> start with "SPDX-FileCopyrightText:", "SPDX-SnippetCopyrightText:",
    ///     "Copyright", or "©".
    /// </para>
    /// <para>
    ///     Here we also impose a requirement that the word "Copyright" should be followed by a whitespace character.
    /// </para>
    /// </summary>
    public static bool ContainsCopyrightNotice(string line)
    {
        if (line.Contains("SPDX-FileCopyrightText:")
            || line.Contains("SPDX-SnippetCopyrightText:")
            || line.Contains("©"))
        {
            return true;
        }

        const string copyright = "Copyright";
        var copyrightIndex = line.IndexOf(copyright, StringComparison.Ordinal);
        if (copyrightIndex < 0) return false;

        if (line.Length > copyrightIndex + copyright.Length)
        {
            var nextChar = line[copyrightIndex + copyright.Length];
            if (char.IsWhiteSpace(nextChar))
                return true;
        }

        return false;
    }

    private static string SkipMandatoryPrefix(string fullText) =>
        throw new Exception("TODO: Should follow the same logic as ContainsCopyrightNotice");

    internal static CopyrightNotice ParsePrefixless(string fullText)
    {
        throw new Exception("TODO: Find the start index");
        // TODO: Skip the copyright signs as required
        // TODO: Parse the year range
        // TODO: Everything else is the name
    }

    // REUSE-IgnoreEnd

    /// <summary>Full text of the copyright notice, as presented in the original document.</summary>
    public string FullText { get; }

    /// <summary>Copyright holder name.</summary>
    public string HolderName { get; }

    /// <summary>A copyright notice with all its parsed information, when possible.</summary>
    /// <param name="fullText">Full text of the copyright notice, as presented in the original document.</param>
    /// <param name="holderName">Copyright holder name.</param>
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
    internal CopyrightNotice(
        string fullText,
        string holderName)
    {
        FullText = fullText;
        HolderName = holderName;
    }

    /// <inheritdoc/>
    public override string ToString() => FullText;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CopyrightNotice other && FullText == other.FullText;

    /// <inheritdoc/>
    public override int GetHashCode() => FullText.GetHashCode();
}
