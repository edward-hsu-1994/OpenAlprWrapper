# Changelog

All notable changes to OpenAlprWrapper will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Batch Recognition API**: `RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>>)` method for processing multiple images in a single call with aggregated results and error tracking
- **Async API**: Non-blocking recognition methods with `CancellationToken` support:
  - `RecognizeAsync(ReadOnlyMemory<byte>, CancellationToken)`
  - `RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>>, CancellationToken)`
- **Progress Events**: Event-driven notifications for batch processing:
  - `RecognitionProgress`: Fired before each image and after completion (Current/Total/Progress%)
  - `RecognitionError`: Fired when recognition fails with detailed error information  
  - `Completed`: Fired when batch processing completes successfully
- **Multi-target framework support**: Cross-platform compilation targeting .NET 10.0 and .NET 8.0
- **Event argument models**: `PlateRecognitionEventArgs`, `PlateErrorEventArgs`, `PlateCompletedEventArgs` with full documentation
- **Integration tests**: Real-world image validation tests (skipped when native library unavailable)
- **NuGet package documentation**: Comprehensive installation and usage guide in package
- **Semantic versioning**: Version file-based management for automated release workflow
- **Cross-platform docs**: API Reference, Architecture, Installation, and Troubleshooting guides

### Changed  
- `OpenAlprException`: `Message` no longer embeds the inner exception text as "(Original error: …)"; the original error is now preserved structurally on `InnerException` (and in `ToString()`). Callers logging only `ex.Message` will see cleaner text
- Dropped the .NET Framework 4.8 target: it was silently removed from every build by a csproj condition and could not compile anyway (C# 7.3 default language level, .NET 7+ only APIs). Supported targets are .NET 10.0 and .NET 8.0; README/README-ZH claims updated accordingly
- `OpenAlprRecognizer.cs`: Added batch processing logic with progress event firing on each image (Current/Total/Result/Progress%)
- `OpenAlprRecognizer.cs`: Wrapped blocking native API calls in `Task.Run()` with cancellation token support
- `OpenAlprRecognizer.cs`: Added error handling with detailed BatchError tracking including IsRecoverable flag
- `README.md`: Expanded with complete API documentation and usage examples for batch/async APIs
- `.gitignore`: Fixed to exclude only .snupkg (symbols), not .nupkg (NuGet packages)

### Fixed
- Memory leak prevention: Progress event handlers use copy-to-array pattern to avoid invocation issues
- Batch error tracking: Original exceptions preserved in InnerException field for debugging
- Type safety: All new record types sealed with explicit parameter documentation
- `RecognizeAsync` surfaces failures as the original exception (e.g. `OpenAlprException`) instead of an `AggregateException` wrapper, matching the synchronous API
- Native JSON errors are consistently wrapped in `OpenAlprException` instead of relying on stale exception-message prefix matching

### Removed
- Unused public delegates `PlateRecognitionEventHandler`, `PlateErrorEventHandler`, `PlateCompletedEventHandler`; events use `EventHandler<T>` directly

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
