# OpenAlprWrapper

AGPL-3.0 .NET wrapper for the [OpenALPR](https://www.openalpr.com/) license plate recognition library.

## 📋 Status

✅ **Native C ABI bridge** built and tested on Linux ARM64 with Docker using real images  
⚠️ **Managed P/Invoke path** not yet smoke-tested on all OS/architecture combinations  
⚠️ **Native bridge binaries** are not included in the NuGet package; platform-specific support must be limited to verified combinations

## 🎯 Features

- P/Invoke wrapper for OpenALPR native C API
- Full control over configuration and runtime data paths
- Cross-platform compatible (.NET 10.0)
- Comprehensive error handling with detailed exception messages
- Resource-safe memory management via `IDisposable` pattern

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
make nuget          # Build NuGet package to artifacts/packages/
make native-build   # Build native bridge (requires CMake + OpenALPR dev libraries)
```

## 💻 Managed API Usage

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

### API Reference

| Method | Description | Returns |
|--------|-------------|---------|
| `Recognize(ReadOnlyMemory<byte>)` | Recognizes license plates in image data | `PlateRecognitionResult` with width, height, processing time, and detected plates |

### Plate Recognition Result

```csharp
public record PlateRecognitionResult(
    int ImageWidth,
    int ImageHeight,
    float ProcessingTimeMs,
    IReadOnlyList<RecognizedPlate> Plates
);

public record RecognizedPlate(
    string BestText,        // Best plate text (e.g., "ABC123")
    float Confidence,       // Recognition confidence percentage
    IReadOnlyList<PlateCandidate> Candidates,  // Alternative readings
    IReadOnlyList<PlatePoint> Corners          // Plate corner coordinates [x,y]
);

public readonly record struct PlatePoint(int X, int Y);
```

## 📦 NuGet Package Installation

After building:

```bash
make nuget

# Add to your project
dotnet add package OpenAlprWrapper --source ./artifacts/packages
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
| `Country` | `string` | `"us"` | OpenALPR country code for plate recognition model |
| `ConfigurationFile` | `string?` | `null` | Optional custom configuration file path |
| `RuntimeDirectory` | `string?` | `null` | Override default runtime data directory |
| `TopN` | `int` | `10` | Maximum number of candidate plates to return |
| `DetectRegion` | `bool` | `false` | Enable experimental region detection |

## ⚠️ Error Handling

### Common Exceptions

| Exception | Cause | Solution |
|-----------|-------|----------|
| `OpenAlprException: "Failed to initialize OpenALPR engine..."` | Native library missing or runtime data not installed | Install OpenALPR native library and copy runtime files |
| `DllNotFoundException` | Platform-specific .so/.dll not found | Verify platform-specific native binary exists |
| `EntryPointNotFoundException` | Expected C API exports not found | Ensure using correct native build with required symbols |
| `ArgumentException: "Image data must not be empty."` | Passed empty or null image bytes | Validate image data before calling `Recognize()` |
| `OpenAlprException: "Recognition failed for country='us'..."` | No plate detected in image | Image may not contain valid license plate or is too dark/blurry |

### Error Messages Include Context

Exception messages now include:
- Failed country and TopN parameters
- Suggested runtime data installation path (when configured)
- Original native-level errors for debugging

## 🧪 Sample Application

Run the sample app with your images:

```bash
# Place JPEG/PNG/BMP/GIF files in samples/images/
dotnet run --project samples/OpenAlprWrapper.Sample.csproj
```

The sample enumerates all supported image formats and prints detected plates.

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
│   └── OpenAlpr.Net/
│       ├── Models.cs              # Data models (Options, Results)
│       ├── OpenAlprRecognizer.cs  # Main recognizer implementation
│       └── NativeOpenAlprApi.cs   # P/Invoke bridge wrapper
├── tests/OpenAlpr.Net.Tests/      # Unit tests with mocks
├── samples/                       # Example applications
│   └── images/                    # Test images
├── native/openalpr-dotnet-bridge/ # Native C++ bridge source
├── docs/                         # Technical documentation
│   ├── README.md                 # Documentation index
│   ├── api/                      # API reference
│   ├── architecture.md           # Architecture overview
│   ├── installation.md           # Installation guide
│   ├── troubleshooting.md        # Troubleshooting guide
│   └── CONTRIBUTING.md           # Contributing guidelines
├── Makefile                       # Build automation
├── README.md                     # This file
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
