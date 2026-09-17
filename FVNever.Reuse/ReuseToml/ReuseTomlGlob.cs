// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;
using TruePath;

namespace FVNever.Reuse.ReuseToml;

/// <summary>
/// <para>Translates the <c>path</c> globs of <c>REUSE.toml</c> annotations into regular expressions.</para>
/// </summary>
/// <remarks>
/// The specification rules are: <c>*</c> matches everything except forward slashes, <c>**</c> matches everything
/// including forward slashes, <c>\*</c> is a verbatim asterisk, <c>\\</c> is a verbatim backslash, and a backslash
/// followed by any other character is equal to just that character (the backslashes here are already unescaped from
/// TOML). Matching is case-sensitive.
/// <para>
/// The specification has no rule for a glob ending with an unescaped backslash, so such a glob is rejected as a
/// format error.
/// </para>
/// </remarks>
internal static class ReuseTomlGlob
{
    /// <summary>Translates the glob into an anchored regular expression.</summary>
    /// <param name="glob">The glob from the <c>path</c> key, with forward slashes as separators.</param>
    /// <param name="source">The file the glob comes from, to be mentioned in error messages.</param>
    /// <exception cref="Exception">The glob ends with an unescaped backslash.</exception>
    public static Regex Translate(string glob, AbsolutePath? source = null)
    {
        var result = new StringBuilder();
        var characters = new Queue<char>(glob);
        while (characters.Count > 0)
        {
            var c = characters.Dequeue();
            switch (c)
            {
                case '\\':
                {
                    if (!characters.TryDequeue(out var next))
                        throw new Exception(
                            (source is { } s ? $"Format error in \"{s.Value}\": " : "Format error: ") +
                            $"the path glob \"{glob}\" ends with an unescaped backslash.");

                    result.Append(Regex.Escape(next.ToString()));
                    break;
                }
                case '*':
                {
                    char? next = null;
                    if (characters.TryPeek(out var n))
                    {
                        next = n;
                    }

                    switch (next)
                    {
                        case '*': // ** and **/
                            _ = characters.Dequeue(); // consume the second star
                            if (characters.TryPeek(out var nn) && nn == '/')
                            {
                                // ** followed by /, ignore the /
                                _ = characters.Dequeue();
                                // any other character gets left in the queue and will be dequeued on the next step
                            }

                            result.Append(".*");
                            break;

                        default: // * followed by anything else or nothing
                            // leave the regex, do not consume the next character
                            result.Append("[^/]*");
                            break;
                    }
                    break;
                }
                default:
                    result.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        return new Regex($"^{result}$", RegexOptions.CultureInvariant);
    }
}
