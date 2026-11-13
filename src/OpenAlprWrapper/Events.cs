namespace OpenAlprWrapper;

/// <summary>
/// Event arguments for recognition progress updates in batch processing.
/// </summary>
public sealed record PlateRecognitionEventArgs(
    int Current,              // Current image index being processed (0-based)
    int Total,                // Total number of images in the batch
    PlateRecognitionResult? Result, // Result of the current image if available (null if not yet complete)
    float Progress            // Progress percentage as a decimal (e.g., 0.5 for 50%)
);

/// <summary>
/// Event arguments for recognition errors during batch processing.
/// </summary>
public sealed record PlateErrorEventArgs(
    int ImageIndex,           // Index of the image that failed (0-based)
    string ErrorMessage,      // Human-readable error message
    Exception? InnerException, // Original exception if available
    bool IsRecoverable        // Whether the error can be recovered from
);

/// <summary>
/// Event arguments for recognition completion in batch processing.
/// </summary>
public sealed record PlateCompletedEventArgs(
    BatchRecognitionResult Result // Final batch recognition result
);
