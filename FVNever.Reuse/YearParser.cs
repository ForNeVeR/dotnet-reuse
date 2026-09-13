// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse;

/// <remarks>
/// This follows the specification outlined <a href="https://github.com/fsfe/reuse-website/issues/128">here</a>.
/// </remarks>
internal static class YearParser
{
    internal static (List<CopyrightNotice.YearItem> years, string rest) Parse(string fullText)
    {
        var years = new List<CopyrightNotice.YearItem>();
        var restStartIndex = DoParse();
        return (years, fullText[restStartIndex..]);

        int DoParse()
        {
            var tokens = Tokenize(fullText);
            while (tokens.Count > 0)
            {
                var token = tokens.Dequeue();
                switch (token)
                {
                    case YearToken year:
                        if (tokens.TryDequeue(out var nextToken))
                        {
                            switch (nextToken)
                            {
                                case CommaToken comma:
                                {
                                    years.Add(new CopyrightNotice.YearItem.SingleYear(year.Year));

                                    if (!tokens.TryPeek(out var nextYear) || nextYear is not YearToken)
                                    {
                                        // invalid item after comma, comma itself also gets invalidated
                                        return comma.StartIndex;
                                    }

                                    continue;
                                }
                                case DashToken dashToken:
                                {
                                    if (!tokens.TryDequeue(out var nextYear) || nextYear is not YearToken ny)
                                    {
                                        years.Add(new CopyrightNotice.YearItem.SingleYear(year.Year));
                                        return dashToken.StartIndex;
                                    }

                                    years.Add(new CopyrightNotice.YearItem.YearRange(year.Year, ny.Year));

                                    if (!tokens.TryDequeue(out var nextComma)) return fullText.Length;
                                    if (nextComma is not CommaToken) return nextComma.StartIndex;
                                    if (!tokens.TryPeek(out var evenNextYear) || evenNextYear is not YearToken)
                                    {
                                        // invalid item after next comma, comma itself also gets invalidated
                                        return nextComma.StartIndex;
                                    }
                                    continue;
                                }

                                case RestTextToken restTextToken:
                                    years.Add(new CopyrightNotice.YearItem.SingleYear(year.Year));
                                    return restTextToken.StartIndex;
                                case YearToken yearToken:
                                    years.Add(new CopyrightNotice.YearItem.SingleYear(year.Year));
                                    return yearToken.StartIndex;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(nextToken));
                            }
                        }

                        years.Add(new CopyrightNotice.YearItem.SingleYear(year.Year));
                        continue;
                    case CommaToken or DashToken or RestTextToken: // invalid start of expression
                        return token.StartIndex;
                    default:
                        throw new Exception($"Impossible state: {token} encountered while parsing {fullText}.");
                }
            }

            return fullText.Length;
        }
    }

    private static Queue<Token> Tokenize(string text)
    {
        return new Queue<Token>(Go());

        IEnumerable<Token> Go()
        {
            YearToken? currentYear = null;
            int index;
            for (index = 0; index < text.Length; ++index)
            {
                var c = text[index];
                if (c is >= '0' and <= '9')
                {
                    if (currentYear is null)
                    {
                        currentYear = new YearToken(index, c - '0');
                    }
                    else
                    {
                        currentYear = currentYear with { Year = currentYear.Year * 10 + (c - '0') };
                    }
                }
                else
                {
                    var isSeparator = char.IsWhiteSpace(c) || c is '-' or '–' or '—' or ',';
                    if (currentYear is not null)
                    {
                        if (!isSeparator)
                        {
                            // digits directly followed by text (e.g. "3M") are not a year
                            yield return new RestTextToken(currentYear.StartIndex);
                            yield break;
                        }

                        yield return currentYear;
                        currentYear = null;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                    }
                    else if (c is '-' or '–' or '—')
                    {
                        yield return new DashToken(index);
                    }
                    else if (c is ',')
                    {
                        yield return new CommaToken(index);
                    }
                    else
                    {
                        yield return new RestTextToken(index);
                        yield break;
                    }
                }
            }

            if (currentYear is { } remainingYear)
            {
                yield return remainingYear;
            }
        }
    }

    private abstract record Token(int StartIndex);
    private record YearToken(int StartIndex, int Year) : Token(StartIndex);
    private record DashToken(int StartIndex) : Token(StartIndex);
    private record CommaToken(int StartIndex) : Token(StartIndex);
    private record RestTextToken(int StartIndex) : Token(StartIndex);
}
