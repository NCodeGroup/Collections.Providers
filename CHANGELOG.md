# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Public API tracking with `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
- `CONTRIBUTING.md`, `SECURITY.md`, `docs/decisions/` and the `build/dod.ps1` Definition of Done gate.

### Changed

- Updated from project-template v0.41.0 to v0.61.2: build settings, analyzers, CI, Dependabot configuration and agent workflow files. The public API is unchanged and is now tracked in `PublicAPI.Shipped.txt`.
- `AssemblyVersion` now moves only with the major version (`<major>.0.0.0`) instead of following the package version, so a 1.x release keeps the assembly identity 1.0.0.0.

## [1.0.0] - 2026-01-19

Initial release.

[Unreleased]: https://github.com/NCodeGroup/Collections.Providers/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/NCodeGroup/Collections.Providers/releases/tag/v1.0.0
