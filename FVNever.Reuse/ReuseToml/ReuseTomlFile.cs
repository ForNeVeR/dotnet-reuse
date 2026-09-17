// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Collections.Immutable;
using Tomlyn;
using Tomlyn.Model;
using TruePath;
using TruePath.SystemIo;

namespace FVNever.Reuse.ReuseToml;

/// <summary>
/// <para>Represents a parsed <c>REUSE.toml</c> file.</para>
/// <para>See more in <a href="https://reuse.software/spec-3.3/#reusetoml">the specification</a>.</para>
/// </summary>
/// <param name="Directory">The directory containing the file; all the annotation paths are relative to it.</param>
/// <param name="Annotations">The <c>[[annotations]]</c> tables in the order they appear in the file.</param>
/// <remarks>
/// Only the keys defined by the specification are validated; any other keys (e.g., <c>SPDX-PackageName</c>) are
/// ignored.
/// </remarks>
internal record ReuseTomlFile(AbsolutePath Directory, ImmutableArray<ReuseTomlAnnotation> Annotations)
{
    /// <summary>The name of the file, as defined by the specification.</summary>
    public const string FileName = "REUSE.toml";

    private const long SupportedVersion = 1;

    /// <summary>Reads and parses a <c>REUSE.toml</c> file.</summary>
    /// <param name="path">Absolute path to the file.</param>
    public static async Task<ReuseTomlFile> ReadFile(AbsolutePath path)
    {
        var text = await path.ReadAllTextAsync().ConfigureAwait(false);
        return Read(text, path.Parent!.Value, path);
    }

    /// <summary>Parses the <c>REUSE.toml</c> file contents.</summary>
    /// <param name="text">The file contents.</param>
    /// <param name="directory">The directory the file is located in.</param>
    /// <param name="fileSource">The file name to mention in error messages.</param>
    /// <exception cref="Exception">The text is not valid TOML, or it doesn't follow the specification.</exception>
    public static ReuseTomlFile Read(string text, AbsolutePath directory, AbsolutePath? fileSource = null)
    {
        var source = fileSource ?? directory / FileName;

        TomlTable document;
        try
        {
            document = TomlSerializer.Deserialize<TomlTable>(text)
                       ?? throw new Exception($"Cannot parse \"{source.Value}\": empty document.");
        }
        catch (TomlException e)
        {
            throw new Exception($"Cannot parse \"{source.Value}\": {e.Message}", e);
        }

        if (!document.TryGetValue("version", out var version))
            throw new Exception($"Format error in \"{source.Value}\": the \"version\" key is required.");
        if (version is not long versionNumber)
            throw new Exception($"Format error in \"{source.Value}\": the \"version\" key should be an integer.");
        if (versionNumber != SupportedVersion)
            throw new Exception(
                $"Format error in \"{source.Value}\": unsupported version {versionNumber}, only {SupportedVersion} is supported.");

        if (!document.TryGetValue("annotations", out var annotationsValue))
            return new ReuseTomlFile(directory, []);
        if (annotationsValue is not IEnumerable annotationTables || annotationsValue is string)
            throw new Exception($"Format error in \"{source.Value}\": \"annotations\" should be an array of tables.");

        var annotations = annotationTables.Cast<object?>()
            .Select(table => table as TomlTable
                             ?? throw new Exception(
                                 $"Format error in \"{source.Value}\": \"annotations\" should be an array of tables."))
            .Select(table => ReadAnnotation(table, source));
        return new ReuseTomlFile(directory, [..annotations]);
    }

    /// <summary>
    /// Finds the table covering the file. If several tables cover the file, then, according to the specification, the
    /// last one is used.
    /// </summary>
    /// <param name="file">Absolute path to the file.</param>
    /// <returns>The table covering the file, or <c>null</c> if there's none.</returns>
    public ReuseTomlAnnotation? FindAnnotation(AbsolutePath file)
    {
        // TODO: Switch back to AbsolutePath.IsPrefixOf once
        // https://github.com/ForNeVeR/TruePath/issues/217 is fixed: it currently compares raw strings, so a "sub"
        // directory would cover a sibling "subx" one.
        if (!new LocalPath(Directory).IsPrefixOf(file))
            return null;

        var relativePath = file.RelativeTo(Directory);
        for (var i = Annotations.Length - 1; i >= 0; --i)
        {
            if (Annotations[i].Matches(relativePath))
                return Annotations[i];
        }

        return null;
    }

    private static ReuseTomlAnnotation ReadAnnotation(TomlTable table, AbsolutePath source)
    {
        var paths = ReadStringOrList(table, "path", source);
        if (paths.IsEmpty)
            throw new Exception($"Format error in \"{source.Value}\": the \"path\" key of an annotation is required.");

        return new ReuseTomlAnnotation(
            [..paths.Select(path => ReuseTomlGlob.Translate(path, source))],
            ReadPrecedence(table, source),
            [..ReadStringOrList(table, "SPDX-FileCopyrightText", source)
                .Select(line => CopyrightNotice.ParseNoMandatoryPrefix(line.Trim()))],
            ReadStringOrList(table, "SPDX-License-Identifier", source));
    }

    private static ReuseTomlPrecedence ReadPrecedence(TomlTable table, AbsolutePath source)
    {
        if (!table.TryGetValue("precedence", out var value))
            return ReuseTomlPrecedence.Closest;

        return value switch
        {
            "closest" => ReuseTomlPrecedence.Closest,
            "aggregate" => ReuseTomlPrecedence.Aggregate,
            "override" => ReuseTomlPrecedence.Override,
            _ => throw new Exception(
                $"Format error in \"{source.Value}\": invalid \"precedence\" value \"{value}\", expected one of " +
                "\"closest\", \"aggregate\", or \"override\".")
        };
    }

    private static ImmutableArray<string> ReadStringOrList(TomlTable table, string key, AbsolutePath source)
    {
        if (!table.TryGetValue(key, out var value))
            return [];

        Exception FormatError() =>
            new($"Format error in \"{source.Value}\": the \"{key}\" key should be a string or an array of strings.");

        return value switch
        {
            string item => [item],
            TomlArray array => [..array.Select(item => item as string ?? throw FormatError())],
            _ => throw FormatError()
        };
    }
}
