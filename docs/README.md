# OpenALPRWrapper Documentation

Technical Documentation Index — Complete reference guide for the [OpenALPR](https://www.openalpr.com/) .NET wrapper.

## 📚 Documentation Index

| Document | Description |
|----------|-------------|
| **[API Reference](api/README.md)** | Detailed API interfaces, code examples, and model specifications |
| **[Installation Guide](installation.md)** | Step-by-step installation instructions, environment prerequisites, and runtime setup |
| **[Architecture](architecture.md)** | Architectural design, system layering, and dependency inversion patterns |
| **[Troubleshooting](troubleshooting.md)** | Common issue troubleshooting, error message explanations, and diagnostic techniques |
| **[Contributing](CONTRIBUTING.md)** | Project contribution guidelines, code style standards, and testing requirements |

## 🎯 API Reference

### IPlateRecognizer Interface

```csharp
public interface IPlateRecognizer : IDisposable
{
    /// <summary>
    /// Recognizes license plates in encoded image data.
    /// </summary>
    /// <param name="image">Encoded image bytes; must not be empty.</param>
    /// <returns>Image metadata and zero or more recognized plates.</returns>
    /// <exception cref="ArgumentException">The image is empty.</exception>
    /// <exception cref="ObjectDisposedException">The recognizer has been disposed.</exception>
    /// <exception cref="OpenAlprException">OpenALPR failed to recognize the image.</exception>
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);

    /// <summary>
    /// Recognizes license plates across a batch of encoded images.
    /// </summary>
    BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images);

    /// <summary>
    /// Asynchronously recognizes license plates in encoded image data.
    /// </summary>
    Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously recognizes license plates across a batch of encoded images.
    /// </summary>
    Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken = default);

    /// <summary>Occurs when progress is made during batch recognition.</summary>
    event EventHandler<PlateRecognitionEventArgs>? RecognitionProgress;

    /// <summary>Occurs when an error is encountered during batch recognition.</summary>
    event EventHandler<PlateErrorEventArgs>? RecognitionError;

    /// <summary>Occurs when batch recognition has completed.</summary>
    event EventHandler<PlateCompletedEventArgs>? Completed;
}
```

### OpenAlprOptions Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Country` | `string` | `"us"` | OpenALPR country code (e.g., `us`, `uk`, `eu`) |
| `ConfigurationFile` | `string?` | `null` | Optional path to custom OpenALPR configuration file |
| `RuntimeDirectory` | `string?` | `null` | Optional path to custom OpenALPR runtime data directory |
| `TopN` | `int` | `10` | Maximum number of candidate plate readings to return |
| `DetectRegion` | `bool` | `false` | Experimental region detection flag |

### OpenAlprException Exception Type

```csharp
public sealed class OpenAlprException : Exception
{
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">Optional original exception providing context.</param>
    public OpenAlprException(string message, Exception? innerException = null);
}
```

Common Error Messages:

| Error Message | Cause | Resolution |
|---------------|-------|------------|
| "Failed to initialize OpenALPR engine..." | Native library not found or runtime data files missing | Install the OpenALPR native library and verify the runtime data directory path |
| "DllNotFoundException" | Platform-specific native binary (`.so`, `.dll`, or `.dylib`) not found | Verify the platform-specific native binary exists in the library path |
| "Recognition failed for country=..." | No plate detected or native API returned a null pointer | Verify image exposure, focus/blurriness, resolution, and plate visibility |

---

**Version:** 0.1.0
**License:** AGPL-3.0-only
**Last Updated:** 2026-11-12
