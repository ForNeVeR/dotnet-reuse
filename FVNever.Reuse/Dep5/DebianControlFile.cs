// SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse.Dep5;

/// <summary>
/// <para>Represents a Debian control-format file (DEP-5) consisting of a sequence of stanzas (sections).</para>
/// <para>See more in <a href="https://www.debian.org/doc/debian-policy/ch-controlfields.html">the relevant documentation</a>.</para>
/// </summary>
/// <remarks>
/// This parser is intentionally minimal and focuses on the subset of the Debian copyright
/// format required by the REUSE workflows. Comment lines starting with <c>#</c> are ignored, empty
/// lines separate stanzas, and lines starting with a space are treated as continuations of the previous
/// field value as per the Debian control-file conventions.
/// </remarks>
public class DebianControlFile(List<Stanza> stanzas)
{
    private static readonly char[] Separator = [':'];

    /// <summary>
    /// Reads and parses a Debian control-format file (DEP-5) from the provided text stream.
    /// </summary>
    /// <param name="stream">A <see cref="StreamReader"/> positioned at the beginning of a DEP-5 file.</param>
    /// <returns>A task that produces a <see cref="DebianControlFile"/> instance with parsed stanzas.</returns>
    public static async Task<DebianControlFile> Read(StreamReader stream)
    {
        var text = await stream.ReadToEndAsync().ConfigureAwait(false);
        var lines = text.ReplaceLineEndings("\n").Split("\n");

        var stanzas = new List<Stanza>();
        Stanza? currentStanza = null;
        void EndStanza()
        {
            if (currentStanza != null)
                stanzas.Add(currentStanza);
            currentStanza = null;
        }
        void AppendContinuationValue(string line)
        {
            line = line.Trim();
            if (currentStanza == null)
                throw new Exception($"No stanza to append value to: \"{line}\".");
            if (currentStanza.Fields.Count == 0)
                throw new Exception($"No stanza field to append value to: \"{line}\".");
            var (key, value) = currentStanza.Fields[^1];
            currentStanza.Fields[^1] = (key, value + "\n" + line);
        }

        void AppendNewValue(string key, string value)
        {
            currentStanza ??= new Stanza();
            currentStanza.Fields.Add((key, value));
        }

        foreach (var line in lines)
        {
            if (line.StartsWith('#')) continue;
            if (line.Trim().Length == 0)
            {
                EndStanza();
                continue;
            }

            if (line.StartsWith(' '))
            {
                AppendContinuationValue(line);
                continue;
            }

            var components = line.Split(Separator, 2);
            if (components.Length != 2)
                throw new Exception($"Format error: line doesn't have separator: \"{line}\".");
            var key = components[0];
            var value = components[1].Trim();
            AppendNewValue(key, value);
        }

        EndStanza();

        return new DebianControlFile(stanzas);
    }

    /// <summary>
    /// The list of parsed stanzas in the order they appeared in the file.
    /// </summary>
    public List<Stanza> Stanzas = stanzas;
}
