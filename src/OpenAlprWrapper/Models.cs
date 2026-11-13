using System.IO;

namespace OpenAlprWrapper;

/// <summary>Configuration for OpenALPR recognition and native runtime paths.</summary>
public sealed class OpenAlprOptions
{
    /// <summary>Gets or sets the training-data country code. Defaults to "us".</summary>
    public string Country { get; init; } = "us";
    
    /// <summary>Gets or sets an optional OpenALPR configuration file path.</summary>
    public string? ConfigurationFile { get; init; }
    
    /// <summary>Gets or sets an optional OpenALPR runtime data directory.</summary>
    public string? RuntimeDirectory { get; init; }
    
    /// <summary>Gets or sets the maximum candidate count. Defaults to 10.</summary>
    public int TopN { get; init; } = 10;
    
    /// <summary>Gets or sets whether experimental region detection is enabled.</summary>
    public bool DetectRegion { get; init; }

    /// <summary>Validates settings before native initialization.</summary>
    /// <remarks>
    /// Validates required fields and checks file system paths if provided.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when Country is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when TopN is not positive.</exception>
    /// <exception cref="FileNotFoundException">Thrown when ConfigurationFile does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when RuntimeDirectory does not exist.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Country))
            throw new ArgumentException("Country must not be blank.", nameof(Country));
        
        if (TopN <= 0)
            throw new ArgumentOutOfRangeException(nameof(TopN), "TopN must be positive.");
        
        // Validate optional paths when explicitly provided
        if (!string.IsNullOrEmpty(ConfigurationFile) && File.Exists(ConfigurationFile) == false)
            throw new FileNotFoundException("Configuration file does not exist.", nameof(ConfigurationFile));
        
        if (!string.IsNullOrEmpty(RuntimeDirectory))
        {
            if (Directory.Exists(RuntimeDirectory) == false)
                throw new DirectoryNotFoundException($"Runtime directory does not exist: '{RuntimeDirectory}'.", null);
        }
    }
}

/// <summary>Recognition metadata and the detected plates for one image.</summary>
public sealed record PlateRecognitionResult(int ImageWidth, int ImageHeight, float ProcessingTimeMs, IReadOnlyList<RecognizedPlate> Plates);

/// <summary>A detected plate and its ordered alternatives.</summary>
public sealed record RecognizedPlate(string BestText, float Confidence, IReadOnlyList<PlateCandidate> Candidates, IReadOnlyList<PlatePoint> Corners);

/// <summary>A candidate plate string and its confidence score.</summary>
public sealed record PlateCandidate(string Text, float Confidence);

/// <summary>A point in image pixel coordinates.</summary>
public readonly record struct PlatePoint(int X, int Y);

/// <summary>
/// Collection of recognition results from a single batch processing call.
/// </summary>
public sealed record BatchRecognitionResult(
    int TotalImages,           // Total number of images processed in this batch
    int SuccessCount,          // Number of successfully recognized images
    int FailureCount,          // Number of images that failed recognition
    float AverageProcessingTimeMs, // Average processing time across all successful recognitions
    IReadOnlyList<PlateRecognitionResult> Results, // All plate recognition results (may contain empty plates for failures)
    IReadOnlyList<BatchError> Errors // List of errors that occurred during batch processing
);

/// <summary>
/// Error information for a failed image in batch processing.
/// </summary>
public sealed record BatchError(
    int ImageIndex,            // Index of the failed image (0-based)
    string ErrorMessage,       // Human-readable error message
    Exception? InnerException, // Original exception if available
    bool IsRecoverable         // Whether the error can be recovered from (e.g., file not found vs. no plate detected)
);
