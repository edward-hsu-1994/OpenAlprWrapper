# OpenALprWrapper NuGet Package

## Installation

### Add from local artifacts (after building)
```bash
dotnet add package OpenAlprWrapper --source ./artifacts/packages
```

### From NuGet.org (when published)
```bash
dotnet add package OpenAlprWrapper
```

## Prerequisites

Before using the package, you must install the OpenALPR native runtime:

### Linux
```bash
# Install system dependencies
apt-get update && apt-get install -y build-essential cmake

# Download and extract runtime data
curl -L https://github.com/OpenALPR/openalpr/releases/download/2.10.0/openalpr_runtime_ubuntu2004_x64.tar.gz | tar -xzvC /usr/local/openalpr/runtime

# Or use your own installation path
export OPENALPR_RUNTIME_DIR="/path/to/openalpr/runtime"
```

### macOS
```bash
brew install openalpr
# Runtime is typically at: /opt/homebrew/lib/openalpr/
```

### Windows
Download from: https://github.com/OpenALPR/openalpr/releases

## Usage

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions 
{ 
    Country = "us",          // US license plates
    TopN = 10,               // Return top 10 candidates
    RuntimeDirectory = "/usr/local/openalpr/runtime" // Optional: override runtime path
};

using var recognizer = new OpenAlprRecognizer(options);

// Single image recognition
var result = recognizer.Recognize(File.ReadAllBytes("image.jpg"));
foreach (var plate in result.Plates)
{
    Console.WriteLine($"{plate.BestText}: {plate.Confidence:F2}%");
}

// Batch processing
var images = File.ReadAllBytes("image1.jpg"), 
                 File.ReadAllBytes("image2.jpg");

var batchResult = recognizer.RecognizeBatch(images);
Console.WriteLine($"Processed {batchResult.TotalImages} images, found {batchResult.SuccessCount} plates");
```

## API Features

- ✅ **Single image recognition**: `Recognize(ReadOnlyMemory<byte>)`
- ✅ **Batch processing**: `RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>>)`
- ✅ **Async support**: `RecognizeAsync(CancellationToken)`, `RecognizeBatchAsync(CancellationToken)`
- ✅ **Progress events**: `RecognitionProgress`, `RecognitionError`, `Completed`

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Country` | `string` | `"us"` | OpenALPR country code (e.g., "us", "eu") |
| `TopN` | `int` | `10` | Maximum candidates to return |
| `RuntimeDirectory` | `string?` | `null` | Custom runtime data directory path |
| `ConfigurationFile` | `string?` | `null` | Custom configuration file path |
| `DetectRegion` | `bool` | `false` | Enable experimental region detection |

## Error Handling

Common exceptions:
- `OpenAlprException`: Native API initialization or recognition failures
- `DllNotFoundException`: Platform-specific native library not found
- `ArgumentException`: Empty image data provided
- `DirectoryNotFoundException`: Runtime directory doesn't exist (when configured)

Example error handling:
```csharp
try
{
    var result = recognizer.Recognize(imageBytes);
}
catch (OpenAlprException ex)
{
    Console.WriteLine($"Recognition failed: {ex.Message}");
}
```

## License

This package is licensed under the AGPL-3.0 license. When distributing OpenALPR binaries, preserve all upstream copyright and license notices.

## Support

- [OpenALPR Official Website](https://www.openalpr.com/)
- [GitHub Repository](https://github.com/edward-hsu-1994/OpenAlprWrapper)
- [Installation Guide](../../docs/installation.md)
