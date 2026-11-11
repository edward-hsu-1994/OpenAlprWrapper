# API Reference

OpenALPRWrapper API 完整參考指南

## 🎯 Core Interface: IPlateRecognizer

### 介面定義

```csharp
public interface IPlateRecognizer : IDisposable
{
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);
}
```

### 使用範例

#### Basic Usage

```csharp
using OpenAlprWrapper;

// Create recognizer with default US settings
var options = new OpenAlprOptions { Country = "us", TopN = 10 };

using var recognizer = new OpenAlprRecognizer(options);

// Recognize plates in image bytes
var result = recognizer.Recognize(File.ReadAllBytes("image.jpg"));

foreach (var plate in result.Plates)
{
    Console.WriteLine($"Detected: {plate.BestText}");
    Console.WriteLine($"Confidence: {plate.Confidence:F2}%");
    
    // Show alternative readings
    foreach (var candidate in plate.Candidates)
        Console.WriteLine($"  Alternative: {candidate.Text} ({candidate.Confidence:F2}%)");
}
```

#### Advanced Configuration

```csharp
// Configure with custom runtime paths
var options = new OpenAlprOptions 
{ 
    Country = "eu",          // European license plates
    TopN = 5,                // Return top 5 candidates only
    RuntimeDirectory = "/path/to/openalpr/runtime",
    ConfigurationFile = "/path/to/custom_config.ini"
};

using var recognizer = new OpenAlprRecognizer(options);

// Batch processing with memory pool
var images = GetImages(); // IEnumerable<byte[]>
foreach (var imageBytes in images)
{
    using var recognizer = new OpenAlprRecognizer(options);
    try
    {
        var result = recognizer.Recognize(imageBytes);
        ProcessResult(result);
    }
    catch (OpenAlprException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}
```

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

| 屬性 | 類型 | 說明 |
|------|------|------|
| `ImageWidth` | `int` | 原圖寬度（像素） |
| `ImageHeight` | `int` | 原圖高度（像素） |
| `ProcessingTimeMs` | `float` | 處理時間（毫秒） |
| `Plates` | `IReadOnlyList<RecognizedPlate>` | 檢測到的车牌列表 |

#### Example: Detailed Processing Time Analysis

```csharp
var result = recognizer.Recognize(imageBytes);
Console.WriteLine($"Image: {result.ImageWidth}x{result.ImageHeight}");
Console.WriteLine($"Processing time: {result.ProcessingTimeMs:F2}ms");
Console.WriteLine($"Plates found: {result.Plates.Count}");

if (result.Plates.Count > 0)
{
    // Calculate average processing time per plate
    var avgPerPlate = result.Plates.Count > 0 
        ? result.ProcessingTimeMs / result.Plates.Count 
        : 0;
    
    Console.WriteLine($"Average time per plate: {avgPerPlate:F2}ms");
}
```

### RecognizedPlate

```csharp
public sealed record RecognizedPlate(
    string BestText,
    float Confidence,
    IReadOnlyList<PlateCandidate> Candidates,
    IReadOnlyList<PlatePoint> Corners
);
```

| 屬性 | 類型 | 說明 |
|------|------|------|
| `BestText` | `string` | 最可能的车牌文字（如 "ABC123"） |
| `Confidence` | `float` | 置信度百分比（0-100） |
| `Candidates` | `IReadOnlyList<PlateCandidate>` | 候選結果列表 |
| `Corners` | `IReadOnlyList<PlatePoint>` | 车牌角点坐标 [x,y] |

#### Example: Geolocation with Corners

```csharp
using OpenAlprWrapper;

var result = recognizer.Recognize(imageBytes);
if (result.Plates.Count > 0)
{
    var plate = result.Plates[0];
    
    Console.WriteLine($"Plate: {plate.BestText}");
    Console.WriteLine($"Confidence: {plate.Confidence:F2}%");
    Console.WriteLine($"Number of candidates: {plate.Candidates.Count}");
    
    // Calculate bounding box dimensions (in pixels)
    var corners = plate.Corners.ToArray();
    var minX = corners.Select(c => c.X).Min();
    var maxX = corners.Select(c => c.X).Max();
    var minY = corners.Select(c => c.Y).Min();
    var maxY = corners.Select(c => c.Y).Max();
    
    Console.WriteLine($"Bounding box: ({minX},{minY}) to ({maxX},{maxY})");
    Console.WriteLine($"Width: {maxX - minX} pixels, Height: {maxY - minY} pixels");
}
```

### PlateCandidate

```csharp
public sealed record PlateCandidate(string Text, float Confidence);
```

| 屬性 | 說明 |
|------|------|
| `Text` | 车牌文字候選 |
| `Confidence` | 置信度百分比 |

#### Example: Top Candidates Display

```csharp
var result = recognizer.Recognize(imageBytes);
if (result.Plates.Count > 0)
{
    var plate = result.Plates[0];
    
    Console.WriteLine($"Best: {plate.BestText} ({plate.Confidence:F2}%)");
    
    // Show top 3 candidates if available
    var topCandidates = plate.Candidates.Take(3).ToList();
    foreach (var candidate in topCandidates)
        Console.WriteLine($"  - {candidate.Text}: {candidate.Confidence:F2}%");
}
```

### PlatePoint

```csharp
public readonly record struct PlatePoint(int X, int Y);
```

| 屬性 | 說明 |
|------|------|
| `X` | 水平坐标（像素） |
| `Y` | 垂直坐标（像素） |

---

## 🚧 Work In Progress

### Planned Features (v0.2.0+)

- [ ] Batch recognition: `BatchRecognize(IEnumerable<byte> images)`
- [ ] Progress callbacks: `IProgress<float>` support for long-running operations
- [ ] Async API: `RecognizeAsync(ReadOnlyMemory<byte>, CancellationToken)`
- [ ] Event-driven error reporting: `EventPattern<PlateErrorEventArgs>`

---

**API Version:** 0.1.0  
**Target Framework:** .NET 10.0  
**License:** AGPL-3.0-only
