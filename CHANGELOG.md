# Changelog

All notable changes to OpenALPRWrapper will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive exception messages with native error context
- Unit test coverage for null options, empty image data, disposal scenarios
- Stress testing for memory leak prevention (100+ recognize/dispose cycles)
- Multi-plate recognition result verification tests

### Changed
- Improved `OpenAlprException` to include original exception message in outer exception
- Enhanced `NativeOpenAlprApi` with field storage for country and TopN in error messages
- Added image empty validation before native API call

### Fixed
- Removed unused field warnings from `NativeOpenAlprApi` by proper field initialization
- Corrected P/Invoke error handling to preserve native exception chain
- Fixed JSON parsing edge cases (truncated/malformed JSON)

## [0.1.0] - 2026-11-xx

### Added
- Initial release of .NET wrapper for OpenALPR native C API
- P/Invoke bridge implementation with full resource management
- Basic configuration via `OpenAlprOptions` (country, TopN, runtime paths)
- Sample application for quick testing
- Cross-platform compatible code structure (.NET 10.0 target)

### Known Limitations
- Native library binaries not vendored; users must install OpenALPR separately
- Platform support limited to verified combinations (Linux ARM64 tested)
- Managed P/Invoke path not yet smoke-tested on all architectures

## [v0.2.0] - Planned

### Planned Features
- Pre-built native asset bundling for Linux/Windows/macOS
- NuGet `nativeAssets.json` support for auto-download of platform binaries
- Docker Compose file for development with pre-installed OpenALPR
- Batch recognition API (`BatchRecognize(IEnumerable<byte>)`)
- Progress callback for long-running recognition jobs

---

*See GitHub releases page for official version tags.*
