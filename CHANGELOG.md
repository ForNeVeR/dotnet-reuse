<!--
SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] (1.1.0)
### Added
- New method `ReuseFileEntry.UpdateFileContents` to apply changes to REUSE metadata kept in a file.

## [1.0.0] - 2025-08-19
### Added
- New classes:
  - `ReuseDirectory` as an entry point to collecting the license data on a repository,
  - `ReuseFileEntry` representing one licensing entry,
  - `ReuseCombinedEntry` representing data merged from different sources.

## [0.0.0] - 2025-08-14
This is the first published version of the package. It doesn't contain any features, serves the purpose of kickstarting the publication system, and to be an anchor for further additions to the package.

[0.0.0]: https://github.com/ForNeVeR/dotnet-reuse/releases/tag/v0.0.0
[1.0.0]: https://github.com/ForNeVeR/dotnet-reuse/compare/v0.0.0...v1.0.0
[Unreleased]: https://github.com/ForNeVeR/dotnet-reuse/compare/v1.0.0...HEAD
