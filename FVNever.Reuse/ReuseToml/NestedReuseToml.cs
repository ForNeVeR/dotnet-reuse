// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.Reuse.ReuseToml;

/// <summary>
/// A set of <c>REUSE.toml</c> files in a project (possibly in nested directories), resolving the licensing information
/// for files according to the <c>precedence</c> rules of
/// <a href="https://reuse.software/spec-3.3/#reusetoml">the specification</a>.
/// </summary>
internal class NestedReuseToml(IEnumerable<ReuseTomlFile> files)
{
    /// <summary>The files, ordered from the project root to the deepest directory.</summary>
    private readonly IReadOnlyList<ReuseTomlFile> _files = [..files.OrderBy(file => file.Directory.Value.Length)];

    /// <summary>Whether there are no <c>REUSE.toml</c> files in the set.</summary>
    public bool IsEmpty => _files.Count == 0;

    /// <summary>Resolves the licensing information for the file.</summary>
    /// <param name="file">Absolute path to the file.</param>
    /// <param name="localEntry">
    /// The licensing information found in the file itself or in its <c>.license</c> file, if any.
    /// </param>
    /// <returns>The resulting entry, or <c>null</c> if no licensing information is associated with the file.</returns>
    /// <remarks>
    /// <para>The <c>REUSE.toml</c> files are walked from the project root to the deepest directory:</para>
    /// <list type="bullet">
    ///     <item>
    ///         <c>aggregate</c> tables always contribute their information;
    ///     </item>
    ///     <item>
    ///         the first <c>override</c> table stops the walk, and its information is used instead of the local
    ///         information and any deeper tables (the <c>aggregate</c> tables closer to the root still contribute);
    ///     </item>
    ///     <item>
    ///         otherwise, the local information is used if present; if not, the closest <c>closest</c> tables are used,
    ///         considering the copyright notices and license identifiers separately.
    ///     </item>
    /// </list>
    /// <para>The aggregated information goes first in the resulting entry.</para>
    /// </remarks>
    public ReuseFileEntry? Resolve(AbsolutePath file, ReuseFileEntry? localEntry)
    {
        var aggregate = new List<ReuseTomlAnnotation>();
        var closest = new List<ReuseTomlAnnotation>();
        ReuseTomlAnnotation? overriding = null;
        foreach (var annotation in _files.Select(toml => toml.FindAnnotation(file)).Where(x => x != null))
        {
            switch (annotation!.Precedence)
            {
                case ReuseTomlPrecedence.Aggregate:
                    aggregate.Add(annotation);
                    break;
                case ReuseTomlPrecedence.Closest:
                    closest.Add(annotation);
                    break;
                case ReuseTomlPrecedence.Override:
                    overriding = annotation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(annotation.Precedence),
                        annotation.Precedence,
                        "Unknown precedence.");
            }

            if (overriding != null)
                break;
        }

        var licenses = new List<string>();
        var copyrights = new List<CopyrightNotice>();
        var licenseHash = new HashSet<string>();
        var copyrightHash = new HashSet<CopyrightNotice>();
        void Add(IEnumerable<string> newLicenses, IEnumerable<CopyrightNotice> newCopyrights)
        {
            licenses.AddRange(newLicenses.Where(licenseHash.Add));
            copyrights.AddRange(newCopyrights.Where(copyrightHash.Add));
        }

        foreach (var annotation in aggregate)
            Add(annotation.LicenseIdentifiers, annotation.CopyrightNotices);

        if (overriding != null)
        {
            Add(overriding.LicenseIdentifiers, overriding.CopyrightNotices);
        }
        else if (localEntry != null)
        {
            Add(localEntry.LicenseIdentifiers, localEntry.CopyrightNotices);
        }
        else
        {
            var closestLicenses = closest.LastOrDefault(x => !x.LicenseIdentifiers.IsEmpty)?.LicenseIdentifiers;
            var closestCopyrights = closest.LastOrDefault(x => !x.CopyrightNotices.IsEmpty)?.CopyrightNotices;
            Add(closestLicenses ?? [], closestCopyrights ?? []);
        }

        if (licenses.Count == 0 && copyrights.Count == 0)
            return null;

        return new ReuseFileEntry(file, [..licenses], [..copyrights]);
    }
}
