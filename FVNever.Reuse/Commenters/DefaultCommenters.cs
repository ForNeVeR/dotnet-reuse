// SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.Reuse.Commenters;

// REUSE-IgnoreStart
internal static class DefaultCommenters
{
    public static ICommenter Guess(LocalPath path)
    {
        return path.GetExtensionWithoutDot() switch
        {
            "conf" => new HashCommenter(),
            "cs" => new CPlusPlusStyleCommenter(),
            "css" => new CStyleCommenter(),
            "dockerfile" => new HashCommenter(),
            "dockerignore" => new HashCommenter(),
            "editorconfig" => new HashCommenter(),
            "env" => new HashCommenter(),
            "gitignore" => new HashCommenter(),
            "html" => new XmlCommenter(),
            "md" => new XmlCommenter(),
            "plugins" => new HashCommenter(), // https://www.playframework.com/documentation/2.3.4/ScalaPlugins
            "properties" => new HashCommenter(),
            "ps1" => new HashCommenter(),
            "sbt" => new CPlusPlusStyleCommenter(),
            "scala" => new CPlusPlusStyleCommenter(),
            "sql" => new DoubleDashCommenter(),
            "svg" => new XmlCommenter(),
            "xml" => new XmlCommenter(),
            "yml" => new HashCommenter(),
            _ => new PlainTextCommenter()
        };
    }
}

internal abstract class CommenterBase : ICommenter
{
    protected virtual string? CommentStartLine => null;
    protected abstract string LinePrefix { get; }
    protected virtual string? CommentEndLine => null;

    public string GenerateHeader(IEnumerable<string> copyrightStatements, IEnumerable<string> licenseIdentifiers)
    {
        return string.Join("\n", GenerateLines()) + "\n";

        IEnumerable<string> GenerateLines()
        {
            var hadAnyLine = false;
            var startLine = CommentStartLine;
            var hadCopyright = false;
            foreach (var copyrightStatement in copyrightStatements)
            {
                hadCopyright = true;
                if (!hadAnyLine && startLine != null) yield return startLine;
                yield return $"{LinePrefix}SPDX-FileCopyrightText: {copyrightStatement}";
                hadAnyLine = true;
            }

            if (hadCopyright) yield return LinePrefix.TrimEnd();

            foreach (var licenseIdentifier in licenseIdentifiers)
            {
                if (!hadAnyLine && startLine != null) yield return startLine;
                yield return $"{LinePrefix}SPDX-License-Identifier: {licenseIdentifier}";
                hadAnyLine = true;
            }

            if (hadAnyLine && CommentEndLine is {} endLine) yield return endLine;
        }
    }

    public string RemoveHeader(string currentContent)
    {
        var lines = currentContent.Split('\n');
        var hadFinalNewLine = currentContent.EndsWith('\n');
        return string.Join("\n", FilterLines()) + (hadFinalNewLine ? "\n" : "");

        IEnumerable<string> FilterLines()
        {
            var startLine = CommentStartLine?.TrimEnd();
            var endLine = CommentEndLine?.TrimEnd();
            var spaceBuffer = new List<string>();

            var blockEnded = false;
            foreach (var line in lines)
            {
                if (!blockEnded)
                {
                    if (ReuseFileEntry.CopyrightPatterns.Any(p => p.IsMatch(line))
                        || line.Contains("SPDX-License-Identifier"))
                    {
                        spaceBuffer.Clear();
                        continue;
                    }

                    var trimmedLine = line.TrimEnd();
                    if (trimmedLine == LinePrefix.TrimEnd())
                    {
                        spaceBuffer.Add(line);
                        continue;
                    }

                    if ((startLine != null && trimmedLine == startLine)
                        || (endLine != null && trimmedLine == endLine)) continue;

                    // Otherwise, we encountered something not corresponding to the patterns, and thus the block ended.
                    blockEnded = true;

                    // Flush the space buffer, mostly to keep the newlines after the comment block:
                    foreach (var space in spaceBuffer)
                    {
                        yield return space;
                    }
                }

                yield return line;
            }
        }
    }
}

internal class PlainTextCommenter : CommenterBase
{
    protected override string LinePrefix => "";
}

internal class CPlusPlusStyleCommenter : CommenterBase
{
    protected override string LinePrefix => "// ";
}

internal class HashCommenter : CommenterBase
{
    protected override string LinePrefix => "# ";
}

internal class DoubleDashCommenter : CommenterBase
{
    protected override string LinePrefix => "-- ";
}

internal class CStyleCommenter : CommenterBase
{
    protected override string CommentStartLine => "/*";
    protected override string LinePrefix => "";
    protected override string CommentEndLine => "*/";
}

internal class XmlCommenter : CommenterBase
{
    protected override string CommentStartLine => "<!--";
    protected override string LinePrefix => "";
    protected override string CommentEndLine => "-->";
}
// REUSE-IgnoreEnd
