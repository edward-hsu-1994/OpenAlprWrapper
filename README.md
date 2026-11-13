# OpenAlprWrapper

[繁體中文](README-ZH.md)
[![CI](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/ci.yml) [![CI Matrix](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/matrix.yml/badge.svg?branch=main)](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/matrix.yml) [![NuGet Version](https://img.shields.io/nuget/v/OpenAlprWrapper.svg)](https://www.nuget.org/packages/OpenAlprWrapper/) [![NuGet Downloads](https://img.shields.io/nuget/dt/OpenAlprWrapper.svg)](https://www.nuget.org/packages/OpenAlprWrapper/) [![License](https://img.shields.io/github/license/edward-hsu-1994/OpenAlprWrapper.svg)](https://github.com/edward-hsu-1994/OpenAlprWrapper/blob/main/LICENSE)

AGPL-3.0 .NET wrapper for the [OpenALPR](https://www.openalpr.com/) license plate recognition library.

## 📋 Status

✅ **Native C ABI bridge** built and tested on Linux ARM64 with Docker using real images  
⚠️ **Managed P/Invoke path** not yet smoke-tested on all OS/architecture combinations  
⚠️ **Native bridge binaries** are not included in the NuGet package; platform-specific support must be limited to verified combinations

## 🎯 Features

- P/Invoke wrapper for OpenALPR native C API
- Full control over configuration and runtime data paths
- Cross-platform compatible (.NET 10.0 / .NET 8.0)
- Comprehensive error handling with detailed exception messages
- Resource-safe memory management via `IDisposable` pattern

### API Capabilities

| Method | Description | Returns |
|--------|-------------|---------|
| `Recognize(ReadOnlyMemory<byte>)` | Single image recognition | `PlateRecognitionResult` |
| `RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>>)` | Batch processing | `BatchRecognitionResult` with aggregated results and errors |
| `RecognizeAsync(..., CancellationToken)` | Async single image recognition | `Task<PlateRecognitionResult>` |
| `RecognizeBatchAsync(..., CancellationToken)` | Async batch processing | `Task<BatchRecognitionResult>` |

### Progress Events

- `RecognitionProgress` - Firing on each image during batch processing (Current/Total/Progress)
- `RecognitionError` - Error reporting with detailed error messages and stack traces
- `Completed` - Final batch result after all images processed

## 🚀 Quick Start

### Prerequisites

1. **Install OpenALPR native library** for your platform:
   - [OpenALPR Installation Guide](https://github.com/OpenALPR/openalpr)
   - Ensure the following libraries are available:
     - `openalpr_dotnet.so` (Linux), `openalpr_dotnet.dll` (Windows/macOS)

2. **Install runtime data files** (required for recognition to work):
   ```bash
   # Linux example (adjust path according to your installation)
   tar -xzf openalpr_runtime.tar.gz -C /usr/local/openalpr/runtime
   ```

### Build & Test

```bash
make test           # Run unit tests
make package        # Build NuGet package to artifacts/packages/
make native-build   # Build native bridge (requires CMake + OpenALPR dev libraries)
```

## 💻 Managed API Usage

### Basic Single Image Recognition

```csharp
using OpenAlprWrapper;

// Configure recognizer with country and paths
var options = new OpenAlprOptions 
{ 
    Country = "us",          // US license plates
    TopN = 10,               // Return top 10 candidates
    RuntimeDirectory = "/path/to/openalpr/runtime" // Optional: override runtime path
};

using IPlateRecognizer recognizer = new OpenAlprRecognizer(options);

// Recognize plates in image bytes
var result = recognizer.Recognize(File.ReadAllBytes("image.jpg"));

foreach (var plate in result.Plates)
{
    Console.WriteLine($"{plate.BestText}: {plate.Confidence:F2}%");
    foreach (var candidate in plate.Candidates)
        Console.WriteLine($"  - {candidate.Text}: {candidate.Confidence:F2}%");
}
```

### Batch Processing

```csharp
using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions 
{ 
    Country = "us", 
    TopN = 10 
});

var images = File.ReadAllBytes("image1.jpg"), 
             File.ReadAllBytes("image2.jpg");

var result = recognizer.RecognizeBatch(images);
Console.WriteLine($"Processed {result.TotalImages} images, found {result.SuccessCount} plates with avg time {(result.AverageProcessingTimeMs ?? 0):F2}ms");
```

### Asynchronous Recognition

```csharp
using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions 
{ 
    Country = "us", 
    TopN = 10 
});

var images = File.ReadAllBytes("image1.jpg"), 
             File.ReadAllBytes("image2.jpg");

// Async single image recognition
var result = await recognizer.RecognizeAsync(imageBytes, CancellationToken.None);
Console.WriteLine($"Detected: {result.Plates[0].BestText} ({result.Plates[0].Confidence:F2}%)");

// Async batch processing
var batchResult = await recognizer.RecognizeBatchAsync(images, CancellationToken.None);
Console.WriteLine($"Completed with {batchResult.SuccessCount}/{batchResult.TotalImages} successful");
```

### Progress Event Handling

```csharp
using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions 
{ 
    Country = "us", 
    TopN = 10 
});

// Subscribe to progress events
recognizer.RecognitionProgress += (sender, e) => 
    Console.WriteLine($"Processing image {e.Current}/{e.Total} [{(e.Progress * 100:F0)}%]");

recognizer.RecognitionError += (sender, e) => 
    Console.WriteLine($"Error at index {e.ImageIndex}: {e.ErrorMessage}");

recognizer.Completed += (sender, e) => 
    Console.WriteLine($"Batch completed: {e.Result.SuccessCount}/{e.Result.TotalImages} success");

var result = recognizer.RecognizeBatch(images);
```

## 📦 NuGet Package Installation

### After building locally
```bash
make nuget
dotnet add package OpenAlprWrapper --source ./artifacts/packages
```

### From NuGet.org (when published)
```bash
dotnet add package OpenAlprWrapper
```

### Native Assets (Optional)

For .NET 8+ projects, you can include pre-built native libraries as NuGet assets:

```bash
# Place pre-built native binaries in artifacts/packages/native/<os>/<arch>/
# e.g., artifacts/packages/native/linux-arm64/openalpr_dotnet.so
```

## 🔧 Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Country` | `string` | `"us"` | OpenALPR country code for plate recognition model (e.g., "us", "eu") |
| `ConfigurationFile` | `string?` | `null` | Optional custom configuration file path |
| `RuntimeDirectory` | `string?` | `null` | Override default runtime data directory |
| `TopN` | `int` | `10` | Maximum number of candidate plates to return |
| `DetectRegion` | `bool` | `false` | Enable experimental region detection |

## ⚠️ Error Handling

### Common Exceptions

| Exception | Cause | Solution |
|-----------|-------|---------|
| `OpenAlprException: "Failed to initialize..."` | Native library missing or runtime data not installed | Install OpenALPR native library and copy runtime files |
| `DllNotFoundException` | Platform-specific .so/.dll not found | Verify platform-specific native binary exists |
| `EntryPointNotFoundException` | Expected C API exports not found | Ensure using correct native build with required symbols |
| `ArgumentException: "Image data must not be empty."` | Passed empty or null image bytes | Validate image data before calling `Recognize()` |
| `OpenAlprException: "Recognition failed..."` | No plate detected in image | Image may not contain valid license plate or is too dark/blurry |

### Error Messages Include Context

Exception messages now include:
- Failed country and TopN parameters
- Suggested runtime data installation path (when configured)
- Original native-level errors for debugging

## 📚 Documentation

Comprehensive documentation available in `docs/` directory:

- **[API Reference](docs/api/README.md)** - Complete API documentation with examples
- **[Architecture](docs/architecture.md)** - System design and implementation patterns  
- **[Installation Guide](docs/installation.md)** - Platform-specific setup instructions
- **[Troubleshooting Guide](docs/troubleshooting.md)** - Common issues and solutions
- **[Contributing](docs/CONTRIBUTING.md)** - Development guidelines and code style

## 📁 Directory Structure

```
OpenALPRWrapper/
├── src/
│   └── OpenAlprWrapper/
│       ├── Models.cs              # Data models (Options, Results)
│       ├── OpenAlprRecognizer.cs  # Main recognizer implementation
│       ├── NativeOpenAlprApi.cs   # P/Invoke bridge wrapper
│       └── Events.cs              # Progress events and delegates
├── tests/OpenAlpr.Net.Tests/      # Unit tests with mocks
├── samples/                       # Example applications
│   └── images/                    # Test images
├── native/openalpr-dotnet-bridge/ # Native C++ bridge source
├── docs/                         # Technical documentation
│   ├── api/                      # API reference
│   ├── architecture.md           # Architecture overview
│   ├── installation.md           # Installation guide
│   ├── troubleshooting.md        # Troubleshooting guide
│   └── CONTRIBUTING.md           # Contributing guidelines
├── Makefile                       # Build automation
├── README.md                     # This file
├── VERSION                       # Semantic versioning file
└── .gitignore                    # Git ignore rules
```

## 📄 License

This project is licensed under the [GNU Affero General Public License version 3](https://www.gnu.org/licenses/agpl-3.0.html).  
OpenALPR is also distributed under AGPL-3.0. When distributing OpenALPR binaries, preserve all upstream copyright and license notices.

## 🔗 Links

- [OpenALPR Official Website](https://www.openalpr.com/)
- [OpenALPR GitHub Repository](https://github.com/OpenALPR/openalpr)
- [.NET 10 Documentation](https://docs.microsoft.com/dotnet)

## 📢 Contributing

This is a community-driven open-source project. Please respect the AGPL-3.0 license terms and preserve upstream attribution.  
See [Contributing Guidelines](docs/CONTRIBUTING.md) for details on how to participate.
