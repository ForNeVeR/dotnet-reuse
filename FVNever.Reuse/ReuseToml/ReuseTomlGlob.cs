// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.RegularExpressions;

namespace FVNever.Reuse.ReuseToml;

/// <summary>
/// <para>Translates the <c>path</c> globs of <c>REUSE.toml</c> annotations into regular expressions.</para>
/// </summary>
/// <remarks>
/// The specification rules are: <c>*</c> matches everything except forward slashes, <c>**</c> matches everything
/// including forward slashes, <c>\*</c> is a verbatim asterisk, <c>\\</c> is a verbatim backslash, and a backslash
/// followed by any other character is equal to just that character (the backslashes here are already unescaped from
/// TOML). Matching is case-sensitive.
/// </remarks>
internal static class ReuseTomlGlob
{
    /// <summary>Translates the glob into an anchored regular expression.</summary>
    /// <param name="glob">The glob from the <c>path</c> key, with forward slashes as separators.</param>
    public static Regex Translate(string glob)
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
                    {
                        characters.Enqueue(c);
                        break;
                    }

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
