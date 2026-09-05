// SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FVNever.Reuse.Commenters;
using JetBrains.Annotations;
using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse;

/// <summary>
/// Represents REUSE metadata collected for a single file, including license identifiers and copyright notices.
/// </summary>
/// <param name="Path">The absolute path to the file the entry refers to.</param>
/// <param name="LicenseIdentifiers">A list of SPDX license identifiers associated with the file.</param>
/// <param name="CopyrightNotices">A list of copyright notices associated with the file.</param>
public record ReuseFileEntry(
    AbsolutePath Path,
    ImmutableArray<string> LicenseIdentifiers,
    ImmutableArray<CopyrightNotice> CopyrightNotices)
{
    /// <summary>Reads the REUSE information exclusively from the provided file.</summary>
    /// <remarks>Note it doesn't look into DEP5 or <c>.license</c> file.</remarks>
    internal static async Task<ReuseFileEntry?> ReadFromFile(AbsolutePath file)
    {
        if (!File.Exists(file.Value))
            return null;

        using var stream = File.OpenText(file.Value);

        // TODO[#22]: Support snippets as well
        // TODO[#23]: Support SPDX contributor info

        var text = await stream.ReadToEndAsync();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = FilterIgnoredBlocks(lines);

        var (licenseIdentifiers, copyrightNotices) = CollectStatements(filteredLines);
        if (licenseIdentifiers.Count == 0 && copyrightNotices.Count == 0)
            return null;

        return new ReuseFileEntry(
            file,
            [..licenseIdentifiers],
            [..copyrightNotices]);
    }

    private static IEnumerable<string> FilterIgnoredBlocks(IEnumerable<string> input)
    {
        var ignoring = false;  // TODO[#24]: Should we support nested ignore/unignore blocks?
        foreach (var line in input)
        {
            if (line.Contains("REUSE-IgnoreStart"))
            {
                ignoring = true;
                continue;
            }

            if (line.Contains("REUSE-IgnoreEnd"))
            {
                ignoring = false;
                continue;
            }

            if (!ignoring)
            {
                yield return line;
            }
        }
    }

    // REUSE-IgnoreStart

    internal static readonly Regex[] CopyrightPatterns = [
        new(@"SPDX-(?:File|Snippet)CopyrightText:\s*(.*)"),
        new(@"Copyright\s?(?:\([Cc]\))\s+(.*)"),
        new(@"©\s+(.*)")
    ];

    private static (List<string> Licenses, List<CopyrightNotice> CopyrightNotices) CollectStatements(
        IEnumerable<string> lines)
    {
        // TODO[#25]: Support inverted comment markers, see https://github.com/fsfe/reuse-tool/issues/343
        var licenses = new List<string>();
        var copyrights = new List<CopyrightNotice>();
        foreach (var line in lines)
        {
            if (line.Contains("SPDX-License-Identifier:"))
            {
                licenses.Add(line.Split("SPDX-License-Identifier:", 2)[1].Trim());
                continue;
            }

            foreach (var pattern in CopyrightPatterns)
            {
                var match = pattern.Match(line);
                if (!match.Success) continue;

                copyrights.Add(new CopyrightNotice(match.Groups[1].Value));
            }
        }

        return (licenses, copyrights);
    }

    /// <summary>
    /// <para>Update the file REUSE headers with the data from this object, replacing the existing headers.</para>
    /// <para>
    ///     This method will try to detect binary files, and automatically put the metadata for them into the
    ///     corresponding <c>.license</c> files.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Note that this method guarantees correct updates only for quite strict formats of metadata — mostly for the
    ///     data it has saved itself.
    /// </para>
    /// <para>
    ///     For any data format that is not strict (e.g., additional comments, additional letters, additional empty
    ///     lines, metadata not at the very beginning of the file, etc.), it does the best effort of preserving the
    ///     existing data in the file, but will require manual review of the changes applied.
    /// </para>
    /// </remarks>
    public async Task UpdateFileContents(ICommenter? commenter = null)
    {
        if (await IsBinaryFile())
        {
            if (Path.GetExtensionWithoutDot() == "license")
            {
                throw new Exception(
                    $"Invalid request: file \"{Path}\" is detected as binary. Cannot update the metadata.");
            }

            var licenseFile = Path.WithExtension($"{Path.GetExtensionWithDot()}.license");
            await UpdateFileContents(
                licenseFile,
                LicenseIdentifiers,
                CopyrightNotices,
                new PlainTextCommenter());
            return;
        }

        await UpdateFileContents(
            Path,
            LicenseIdentifiers,
            CopyrightNotices,
            commenter ?? DefaultCommenters.Guess(Path));
    }

    /// <summary>
    /// Combines multiple <see cref="ReuseFileEntry"/> values into a single set by preserving relative order and removing duplicates.
    /// </summary>
    /// <param name="baseDirectory">The directory to calculate relative ordering of files for deterministic output.</param>
    /// <param name="entries">A sequence of entries to combine.</param>
    /// <returns>A combined entry with deduplicated license identifiers and copyright notices.</returns>
    [PublicAPI]
    public static ReuseCombinedEntry CombineEntries(AbsolutePath baseDirectory, IEnumerable<ReuseFileEntry> entries)
    {
        var licenses = new List<string>();
        var copyrights = new List<CopyrightNotice>();
        var licenseHash = new HashSet<string>();
        var copyrightHash = new HashSet<CopyrightNotice>();
        foreach (var entry in entries.OrderBy(x => ((LocalPath)x.Path).RelativeTo(baseDirectory).Value))
        {
            licenses.AddRange(entry.LicenseIdentifiers.Where(license => licenseHash.Add(license)));
            copyrights.AddRange(entry.CopyrightNotices.Where(statement => copyrightHash.Add(statement)));
        }

        return new ReuseCombinedEntry([..licenses], [..copyrights]);
    }

    private async Task<bool> IsBinaryFile()
    {
        // TODO: Improve this detection: read only first several kibibytes of the file.
        var data = await Path.ReadAllBytesAsync();
        return data.Any(x => x == 0);
    }

    private static async Task UpdateFileContents(
        AbsolutePath path,
        IEnumerable<string> licenseIdentifiers,
        IEnumerable<CopyrightNotice> copyrightNotices,
        ICommenter commenter)
    {
        var newContent = await GenerateContent(path, licenseIdentifiers, copyrightNotices, commenter);
        await path.WriteAllTextAsync(newContent);
    }

    private static async Task<string> GenerateContent(
        AbsolutePath path,
        IEnumerable<string> licenseIdentifiers,
        IEnumerable<CopyrightNotice> copyrightNotices,
        ICommenter commenter)
    {
        var header = commenter.GenerateHeader(copyrightNotices, licenseIdentifiers);
        if (!path.Exists())
        {
            return header;
        }
        var currentContent = await path.ReadAllTextAsync();
        return header + commenter.RemoveHeader(currentContent);
    }

    // REUSE-IgnoreEnd
}

/// <summary>
/// Represents a combined view of REUSE metadata after merging multiple file entries.
/// </summary>
/// <param name="LicenseIdentifiers">Deduplicated SPDX license identifiers in their combined order.</param>
/// <param name="CopyrightNotices">Deduplicated copyright notices in their combined order.</param>
[PublicAPI]
public record ReuseCombinedEntry(
    ImmutableArray<string> LicenseIdentifiers,
    ImmutableArray<CopyrightNotice> CopyrightNotices
);
