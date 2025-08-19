// SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.Reuse.Commenters;

/// <summary>Entity that manages metadata comments in a text file.</summary>
public interface ICommenter
{
    /// <summary>
    /// Generate a string with metadata information that will be inserted at the beginning of the file.
    /// </summary>
    /// <param name="copyrightStatements">The copyright statement collection.</param>
    /// <param name="licenseIdentifiers">The license identifier collection.</param>
    /// <returns>Resulting string, ending with a newline.</returns>
    string GenerateHeader(IEnumerable<string> copyrightStatements, IEnumerable<string> licenseIdentifiers);

    /// <summary>Remove metadata information from the file contents.</summary>
    /// <param name="currentContent">Current file contents.</param>
    /// <returns>File contents without metadata information.</returns>
    string RemoveHeader(string currentContent);
}
