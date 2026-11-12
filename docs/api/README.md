# API Reference

Comprehensive reference guide for the OpenALPRWrapper API.

## 🎯 Core Interface: IPlateRecognizer

The primary entry point for license plate recognition in OpenALPRWrapper is the `IPlateRecognizer` interface, implemented by `OpenAlprRecognizer`.

### Interface Definition

```csharp
namespace OpenAlprWrapper;

public interface IPlateRecognizer : IDisposable
{
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);
    BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images);
    Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default);
    Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken = default);

    event EventHandler<PlateRecognitionEventArgs>? RecognitionProgress;
    event EventHandler<PlateErrorEventArgs>? RecognitionError;
    event EventHandler<PlateCompletedEventArgs>? Completed;
}
```

---

## 💻 Usage Examples

### Single Image Recognition

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions
{
    Country = "us",
    TopN = 10
};

using var recognizer = new OpenAlprRecognizer(options);

byte[] imageBytes = File.ReadAllBytes("car.jpg");
PlateRecognitionResult result = recognizer.Recognize(imageBytes);

Console.WriteLine($"Image Dimensions: {result.ImageWidth}x{result.ImageHeight}");
Console.WriteLine($"Processing Time: {result.ProcessingTimeMs:F2} ms");
Console.WriteLine($"Plates Detected: {result.Plates.Count}");

foreach (var plate in result.Plates)
{
    Console.WriteLine($"Best Match: {plate.BestText} (Confidence: {plate.Confidence:F2}%)");
    
    foreach (var candidate in plate.Candidates)
    {
        Console.WriteLine($"  Candidate: {candidate.Text} ({candidate.Confidence:F2}%)");
    }
}
```

### Batch Recognition

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us", TopN = 5 };
using var recognizer = new OpenAlprRecognizer(options);

var imageBatch = new List<ReadOnlyMemory<byte>>
{
    File.ReadAllBytes("car1.jpg"),
    File.ReadAllBytes("car2.jpg")
};

BatchRecognitionResult batchResult = recognizer.RecognizeBatch(imageBatch);

Console.WriteLine($"Total Images: {batchResult.TotalImages}");
Console.WriteLine($"Successful: {batchResult.SuccessCount}");
Console.WriteLine($"Failures: {batchResult.FailureCount}");
Console.WriteLine($"Average Time: {batchResult.AverageProcessingTimeMs:F2} ms");

foreach (var res in batchResult.Results)
{
    foreach (var plate in res.Plates)
    {
        Console.WriteLine($"Plate: {plate.BestText}");
    }
}

foreach (var error in batchResult.Errors)
{
    Console.Error.WriteLine($"Error at index {error.ImageIndex}: {error.ErrorMessage}");
}
```

### Asynchronous Recognition

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us" };
using var recognizer = new OpenAlprRecognizer(options);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

// Single image async recognition
ReadOnlyMemory<byte> image = File.ReadAllBytes("car.jpg");
PlateRecognitionResult singleResult = await recognizer.RecognizeAsync(image, cts.Token);
Console.WriteLine($"Detected: {singleResult.Plates.FirstOrDefault()?.BestText}");

// Batch async recognition
var batch = new[] { image };
BatchRecognitionResult batchResult = await recognizer.RecognizeBatchAsync(batch, cts.Token);
Console.WriteLine($"Batch Completed. Total: {batchResult.TotalImages}, Succeeded: {batchResult.SuccessCount}");
```

### Event-Driven Progress and Error Monitoring

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us" };
using var recognizer = new OpenAlprRecognizer(options);

recognizer.RecognitionProgress += (sender, e) =>
{
    Console.WriteLine($"Processing {e.Current}/{e.Total} ({(e.Progress * 100):F0}%)");
};

recognizer.RecognitionError += (sender, e) =>
{
    Console.Error.WriteLine($"[Error] Image {e.ImageIndex}: {e.ErrorMessage} (Recoverable: {e.IsRecoverable})");
};

recognizer.Completed += (sender, e) =>
{
    Console.WriteLine($"Batch Finished: {e.Result.SuccessCount}/{e.Result.TotalImages} succeeded.");
};

var images = new List<ReadOnlyMemory<byte>>
{
    File.ReadAllBytes("car1.jpg"),
    File.ReadAllBytes("car2.jpg")
};

recognizer.RecognizeBatch(images);
```

---

## ⚙️ Configuration: OpenAlprOptions

```csharp
public sealed class OpenAlprOptions
{
    public string Country { get; init; } = "us";
    public string? ConfigurationFile { get; init; }
    public string? RuntimeDirectory { get; init; }
    public int TopN { get; init; } = 10;
    public bool DetectRegion { get; init; }

    public void Validate();
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Country` | `string` | `"us"` | Country code for the training data (e.g., `us`, `eu`, `uk`). Must not be blank. |
| `ConfigurationFile` | `string?` | `null` | Optional path to an `openalpr.conf` file. If provided, the file must exist. |
| `RuntimeDirectory` | `string?` | `null` | Optional path to the runtime data directory containing `runtime_data/`. If provided, the directory must exist. |
| `TopN` | `int` | `10` | Maximum number of candidate plate readings to return. Must be greater than 0. |
| `DetectRegion` | `bool` | `false` | Enables experimental region detection in OpenALPR. |

---

## 📊 Data Models

### PlateRecognitionResult

```csharp
public sealed record PlateRecognitionResult(
    int ImageWidth,
    int ImageHeight,
    float ProcessingTimeMs,
    IReadOnlyList<RecognizedPlate> Plates
);
```

| Property | Type | Description |
|----------|------|-------------|
| `ImageWidth` | `int` | Width of the processed image in pixels |
| `ImageHeight` | `int` | Height of the processed image in pixels |
| `ProcessingTimeMs` | `float` | Processing duration in milliseconds |
| `Plates` | `IReadOnlyList<RecognizedPlate>` | List of recognized license plates |

### RecognizedPlate

```csharp
public sealed record RecognizedPlate(
    string BestText,
    float Confidence,
    IReadOnlyList<PlateCandidate> Candidates,
    IReadOnlyList<PlatePoint> Corners
);
```

| Property | Type | Description |
|----------|------|-------------|
| `BestText` | `string` | Highest confidence plate text reading |
| `Confidence` | `float` | Confidence percentage for `BestText` (0.0 to 100.0) |
| `Candidates` | `IReadOnlyList<PlateCandidate>` | All candidate readings meeting the `TopN` threshold |
| `Corners` | `IReadOnlyList<PlatePoint>` | Pixel coordinates of the four corners of the plate |

### PlateCandidate

```csharp
public sealed record PlateCandidate(string Text, float Confidence);
```

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Candidate license plate reading |
| `Confidence` | `float` | Confidence score (0.0 to 100.0) |

### PlatePoint

```csharp
public readonly record struct PlatePoint(int X, int Y);
```

| Property | Type | Description |
|----------|------|-------------|
| `X` | `int` | Horizontal coordinate in image pixels |
| `Y` | `int` | Vertical coordinate in image pixels |

### BatchRecognitionResult

```csharp
public sealed record BatchRecognitionResult(
    int TotalImages,
    int SuccessCount,
    int FailureCount,
    float AverageProcessingTimeMs,
    IReadOnlyList<PlateRecognitionResult> Results,
    IReadOnlyList<BatchError> Errors
);
```

| Property | Type | Description |
|----------|------|-------------|
| `TotalImages` | `int` | Total count of images submitted for batch processing |
| `SuccessCount` | `int` | Count of images successfully recognized |
| `FailureCount` | `int` | Count of images that produced recognition errors |
| `AverageProcessingTimeMs` | `float` | Average processing duration across successful images |
| `Results` | `IReadOnlyList<PlateRecognitionResult>` | List of successful plate recognition results |
| `Errors` | `IReadOnlyList<BatchError>` | List of errors encountered during batch processing |

### BatchError

```csharp
public sealed record BatchError(
    int ImageIndex,
    string ErrorMessage,
    Exception? InnerException,
    bool IsRecoverable
);
```

| Property | Type | Description |
|----------|------|-------------|
| `ImageIndex` | `int` | Zero-based index of the failed image in the batch |
| `ErrorMessage` | `string` | Diagnostic message describing the failure |
| `InnerException` | `Exception?` | Underlying exception if available |
| `IsRecoverable` | `bool` | Indicates whether the error was recoverable or fatal to that item |

---

## 📢 Event Argument Types

### PlateRecognitionEventArgs

```csharp
public sealed record PlateRecognitionEventArgs(
    int Current,
    int Total,
    PlateRecognitionResult? Result,
    float Progress
);
```

| Property | Type | Description |
|----------|------|-------------|
| `Current` | `int` | Number of images processed so far |
| `Total` | `int` | Total number of images in the batch |
| `Result` | `PlateRecognitionResult?` | Recognition result of the current image, or `null` before processing starts |
| `Progress` | `float` | Progress ratio from 0.0 to 1.0 |

### PlateErrorEventArgs

```csharp
public sealed record PlateErrorEventArgs(
    int ImageIndex,
    string ErrorMessage,
    Exception? InnerException,
    bool IsRecoverable
);
```

| Property | Type | Description |
|----------|------|-------------|
| `ImageIndex` | `int` | Zero-based index of the image that produced an error |
| `ErrorMessage` | `string` | Detailed error description |
| `InnerException` | `Exception?` | Underlying exception instance |
| `IsRecoverable` | `bool` | Indicates whether execution could continue past the error |

### PlateCompletedEventArgs

```csharp
public sealed record PlateCompletedEventArgs(BatchRecognitionResult Result);
```

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `BatchRecognitionResult` | Final aggregated batch result |

---

## ⚠️ Exceptions

### OpenAlprException

```csharp
public sealed class OpenAlprException : Exception
{
    public OpenAlprException(string message, Exception? innerException = null);
}
```

Thrown when the OpenALPR native engine fails to initialize, fails to produce a recognition result, or encounters corrupt JSON output. When an `innerException` is provided, the message includes the original exception details.

### Argument Validation Exceptions

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | Thrown if `options` or `nativeApi` is `null` |
| `ArgumentException` | Thrown if `Country` is blank, image data is empty, or image collection is empty |
| `ArgumentOutOfRangeException` | Thrown if `TopN` is less than or equal to 0 |
| `FileNotFoundException` | Thrown by `Validate()` if `ConfigurationFile` is specified but does not exist |
| `DirectoryNotFoundException` | Thrown by `Validate()` if `RuntimeDirectory` is specified but does not exist |
| `ObjectDisposedException` | Thrown when attempting to call `Recognize*` on a disposed recognizer |
| `OperationCanceledException` | Thrown when cancellation is requested on `RecognizeAsync` or `RecognizeBatchAsync` |

---

## 🚧 Roadmap

Future enhancements under consideration:

- [ ] Stream-based recognition: `Recognize(Stream imageStream)` and `RecognizeAsync(Stream imageStream)`
- [ ] Image preprocessing helpers (resizing, contrast enhancement, grayscale conversion)
- [ ] Pre-packaged native bridge binaries for common Linux, Windows, and macOS architectures

---

**API Version:** 0.1.0
**Target Frameworks:** .NET 10.0, .NET 8.0
**License:** AGPL-3.0-only
