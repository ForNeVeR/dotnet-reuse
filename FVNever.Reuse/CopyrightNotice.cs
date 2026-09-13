// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;

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
    /// <para>
    ///     The specification does not document how year ranges are supposed to be parsed, so we follow
    ///     <a href="https://github.com/fsfe/reuse-website/issues/128">the proposal</a> that doesn't contradict anything
    ///     said in the spec.
    /// </para>
    /// <para>
    ///     Following the order recommended by the specification (year, name, contact address), the contact address of
    ///     the copyright holder is parsed from in between angle brackets at the very end of the notice (e.g.
    ///     <c>&lt;jane@example.com&gt;</c>). If the angle brackets are not correctly paired, or if there is any text
    ///     after the closing bracket, then no contact address is parsed, and the whole remaining text is considered to
    ///     be a part of the <see cref="HolderName"/>.
    /// </para>
    /// </remarks>
    public static CopyrightNotice Parse(string fullText)
    {
        return ParseNoMandatoryPrefix(SkipMandatoryPrefix(fullText));
    }

    private static readonly Regex MandatoryPrefixPattern = new(
        @"SPDX-FileCopyrightText:|SPDX-SnippetCopyrightText:|©|Copyright\s",
        RegexOptions.Compiled);

    private static (int, int)? FindMandatoryPrefix(string line)
    {
        var mandatoryPrefixMatch = MandatoryPrefixPattern.Match(line);
        if (!mandatoryPrefixMatch.Success)
            return null;

        return (mandatoryPrefixMatch.Index, mandatoryPrefixMatch.Length);
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
    public static bool ContainsCopyrightNotice(string line) =>
        FindMandatoryPrefix(line) is not null;

    private static string SkipMandatoryPrefix(string fullText)
    {
        string result = fullText;
        if (FindMandatoryPrefix(fullText) is var (index, length))
        {
            result = fullText[(index + length)..];
        }

        return result.Trim();
    }

    private static readonly Regex NonMandatoryCopyrightSignsWithWhitespace = new(@"^((\(C\)|\(c\)|©)\s*)*");

    internal static CopyrightNotice ParseNoMandatoryPrefix(string fullText)
    {
        var toParse = fullText;
        var matches = NonMandatoryCopyrightSignsWithWhitespace.Match(fullText);
        if (matches.Success)
        {
            toParse = fullText.Substring(matches.Length).Trim();
        }

        (var years, toParse) = YearParser.Parse(toParse);
        var (holderName, contactAddress) = SplitContactAddress(toParse);
        return new CopyrightNotice(
            fullText: fullText,
            years: years,
            holderName: holderName,
            contactAddress: contactAddress);
    }

    private static (string HolderName, string? ContactAddress) SplitContactAddress(string text)
    {
        text = text.TrimEnd();
        if (!text.EndsWith('>'))
            return (text, null);

        var open = text.LastIndexOf('<');
        if (open == -1)
            return (text, null);

        var contactAddress = text[(open + 1)..^1];
        var holderName = text[..open].TrimEnd();
        if (contactAddress.IndexOfAny(['<', '>']) != -1 || holderName.IndexOfAny(['<', '>']) != -1)
            return (text, null);

        return (holderName, contactAddress);
    }

    // REUSE-IgnoreEnd

    /// <summary>Full text of the copyright notice, as presented in the original document.</summary>
    public string FullText { get; }

    /// <summary>Years of publication, if present in the original text.</summary>
    public IReadOnlyList<YearItem> Years { get; }

    /// <summary>Name of the copyright holder.</summary>
    public string HolderName { get; }

    /// <summary>
    /// The contact address of the copyright holder (e.g. <c>jane@example.com</c> or
    /// <c>https://project.example.com</c>), if present in between angle brackets in the original text. Stored without
    /// the angle brackets.
    /// </summary>
    public string? ContactAddress { get; }

    /// <summary>A copyright notice with all its parsed information, when possible.</summary>
    /// <param name="fullText">Full text of the copyright notice, as presented in the original document.</param>
    /// <param name="holderName">Name of the copyright holder.</param>
    /// <param name="years">Years of publication, if present in the original text.</param>
    /// <param name="contactAddress">
    /// The contact address of the copyright holder, if present in between angle brackets in the original text. Stored
    /// without the angle brackets.
    /// </param>
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
        IReadOnlyList<YearItem> years,
        string holderName,
        string? contactAddress)
    {
        FullText = fullText;
        HolderName = holderName;
        Years = years;
        ContactAddress = contactAddress;
    }

    /// <inheritdoc/>
    public override string ToString() => FullText;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CopyrightNotice other && FullText == other.FullText;

    /// <inheritdoc/>
    public override int GetHashCode() => FullText.GetHashCode();

    /// <summary>Container for either a year or a year range (like 2025–2026).</summary>
    public abstract record YearItem
    {
        private YearItem()
        {
        }

        /// <summary>Single year item.</summary>
        /// <param name="Year">The year in question.</param>
        public record SingleYear(int Year) : YearItem;

        /// <summary>Year range (usually inclusive).</summary>
        /// <param name="StartYear">The start year of the range.</param>
        /// <param name="EndYear">The end year of the range.</param>
        public record YearRange(int StartYear, int EndYear) : YearItem;
    }
}
