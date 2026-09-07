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
            CopyrightNotice.YearItem? lastYearElement = null;
            YearToken? lastYear = null;
            CommaToken? lastComma = null;
            DashToken? lastDash = null;

            foreach (var token in Tokenize(fullText))
            {
                switch (token)
                {
                    case YearToken year when lastYear is null && lastComma is not null && lastDash is null:
                        lastComma = null;
                        lastYear = year;
                        break;
                    case YearToken when lastYear is null && lastComma is null && lastDash is not null:
                        // orphan dash
                        return lastDash.StartIndex;
                    case YearToken nextYear when lastYear is not null && lastDash is null:
                        // two years one after another
                        years.Add(new CopyrightNotice.YearItem.SingleYear(lastYear.Year));
                        return nextYear.StartIndex;
                    case YearToken nextYear when lastYear is not null && lastComma is null && lastDash is not null:
                        years.Add(lastYearElement = new CopyrightNotice.YearItem.YearRange(lastYear.Year, nextYear.Year));
                        lastYear = null;
                        lastDash = null;
                        break;

                    case CommaToken comma when lastYearElement is null && lastYear is null:
                        // comma without preceding year item
                        return comma.StartIndex;
                    case CommaToken comma when lastYear is not null && lastComma is null && lastDash is null:
                        lastYearElement = null;
                        years.Add(new CopyrightNotice.YearItem.SingleYear(lastYear.Year));
                        lastYear = null;
                        lastComma = comma;
                        break;
                    case CommaToken comma when lastComma is not null && lastDash is not null:
                        // comma after comma or dash
                        return comma.StartIndex;
                    case CommaToken comma:
                        lastYearElement = null;
                        lastComma = comma;
                        break;

                    case DashToken dash when lastDash is null && lastComma is null:
                        lastYearElement = null;
                        lastDash = dash;
                        break;
                    case DashToken dash: // dash after dash or comma
                        return dash.StartIndex;

                    case RestTextToken restTextToken:
                        return restTextToken.StartIndex;
                    default:
                        throw new Exception($"Impossible state: {token} encountered while parsing {fullText}.");
                }
            }

            if (lastYear is not null)
                years.Add(new CopyrightNotice.YearItem.SingleYear(lastYear.Year));

            if (lastComma is not null)
                return lastComma.StartIndex;

            if (lastDash is not null)
                return lastDash.StartIndex;

            return fullText.Length;
        }
    }

    private static IEnumerable<Token> Tokenize(string text)
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
                if (currentYear is { } cy)
                {
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

    private abstract record Token(int StartIndex);
    private record YearToken(int StartIndex, int Year) : Token(StartIndex);
    private record DashToken(int StartIndex) : Token(StartIndex);
    private record CommaToken(int StartIndex) : Token(StartIndex);
    private record RestTextToken(int StartIndex) : Token(StartIndex);
}
