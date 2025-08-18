---
_disableBreadcrumb: true
---

<!--
SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

dotnet-reuse
============
dotnet-reuse is a .NET library to interact with license information in [REUSE][reuse]-compliant sources.

Example Usage
-------------
The main entry point of the library is [ReuseDirectory.ReadEntries](xref:FVNever.Reuse.ReuseDirectory.ReadEntries(TruePath.AbsolutePath)):
<!-- REUSE-IgnoreStart -->
```csharp
using TruePath;
using FVNever.Reuse;

var repo = AbsolutePath.CurrentWorkingDirectory / "some-repo";
var entries = await ReuseDirectory.ReadEntries(repo);
foreach (var entry in entries)
{
    Console.WriteLine($"- {entry.Path}");
    foreach (var license in entry.LicenseIdentifiers)
        Console.WriteLine($"  - license found: {license}");
    foreach (var copyright in entry.CopyrightStatements)
        Console.WriteLine($"  - copyright found: {copyright}");
}
```
<!-- REUSE-IgnoreEnd -->

Read more in [the API documentation][api.index].

[api.index]: api/FVNever.Reuse.yml
[reuse]: https://reuse.software/
