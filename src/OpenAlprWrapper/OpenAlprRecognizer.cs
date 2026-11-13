namespace OpenAlprWrapper;

/// <summary>Defines the plate recognition operations exposed by <see cref="OpenAlprRecognizer"/>.</summary>
public interface IPlateRecognizer : IDisposable
{
    /// <summary>Recognizes plates in a single image.</summary>
    /// <param name="image">The encoded image data. Must not be empty.</param>
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);

    /// <summary>Recognizes plates across a batch of images sequentially.</summary>
    /// <param name="images">The images to recognize. Must not be empty.</param>
    BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images);

    /// <summary>Recognizes a single image asynchronously, honoring the supplied cancellation token.</summary>
    /// <param name="image">The encoded image data. Must not be empty.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default);

    /// <summary>Recognizes a batch of images asynchronously, honoring the supplied cancellation token.</summary>
    /// <param name="images">The images to recognize. Must not be empty.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken = default);

    /// <summary>Raised before each image is processed and after completion with progress information.</summary>
    event EventHandler<PlateRecognitionEventArgs>? RecognitionProgress;

    /// <summary>Raised when recognition of an image fails with detailed error information.</summary>
    event EventHandler<PlateErrorEventArgs>? RecognitionError;

    /// <summary>Raised when batch processing completes successfully.</summary>
    event EventHandler<PlateCompletedEventArgs>? Completed;
}

internal interface IOpenAlprNativeApi : IDisposable
{
    NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image);
}

internal sealed record NativeRecognitionResult(int ImageWidth, int ImageHeight, float ProcessingTimeMs, IReadOnlyList<NativePlateResult> Plates);
internal sealed record NativePlateResult(string BestText, float Confidence, IReadOnlyList<NativePlateCandidate> Candidates, IReadOnlyList<PlatePoint> Corners);
internal sealed record NativePlateCandidate(string Text, float Confidence);

/// <summary>Managed OpenALPR recognizer with deterministic native-resource ownership.</summary>
public sealed class OpenAlprRecognizer : IPlateRecognizer
{
    private readonly IOpenAlprNativeApi nativeApi;
    private bool disposed;

    /// <summary>Raised before each image is processed and after completion with progress information.</summary>
    public event EventHandler<PlateRecognitionEventArgs>? RecognitionProgress;

    /// <summary>Raised when recognition of an image fails with detailed error information.</summary>
    public event EventHandler<PlateErrorEventArgs>? RecognitionError;

    /// <summary>Raised when batch processing completes successfully.</summary>
    public event EventHandler<PlateCompletedEventArgs>? Completed;

    /// <summary>Creates a recognizer with the given options, initializing the native engine.</summary>
    /// <param name="options">The configuration for recognition and native runtime paths.</param>
    public OpenAlprRecognizer(OpenAlprOptions options) : this(options, CreateNativeApi(options)) { }

    private static IOpenAlprNativeApi CreateNativeApi(OpenAlprOptions options) => new NativeOpenAlprApi(options);

    internal OpenAlprRecognizer(OpenAlprOptions options, IOpenAlprNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(nativeApi);
        options.Validate();
        this.nativeApi = nativeApi;
    }

    /// <summary>Recognizes plates in a single image, returning the detected plate(s).</summary>
    /// <param name="image">The encoded image data. Must not be empty.</param>
    public PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (image.IsEmpty)
            throw new ArgumentException("Encoded image data must not be empty.", nameof(image));

        var result = nativeApi.Recognize(image);
        return new PlateRecognitionResult(
            result.ImageWidth,
            result.ImageHeight,
            result.ProcessingTimeMs,
            result.Plates.Select(p => new RecognizedPlate(
                p.BestText,
                p.Confidence,
                p.Candidates.Select(c => new PlateCandidate(c.Text, c.Confidence)).ToArray(),
                p.Corners.ToArray())).ToArray());
    }

    /// <summary>Recognizes plates in a batch of images sequentially, reporting progress and per-image errors.</summary>
    /// <param name="images">The images to recognize. Must not be empty.</param>
    public BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return RecognizeBatch(images, CancellationToken.None);
    }

    internal BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (images == null || !images.Any())
            throw new ArgumentException("Image collection must not be empty.", nameof(images));

        cancellationToken.ThrowIfCancellationRequested();

        var imageList = images.ToList();

        OnRecognitionProgress(0, imageList.Count, null, 0f);

        var results = new List<PlateRecognitionResult>();
        var errors = new List<BatchError>();
        float totalProcessingTimeMs = 0;
        int successCount = 0;

        for (var i = 0; i < imageList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(disposed, this);

            try
            {
                if (imageList[i].IsEmpty)
                    throw new ArgumentException("Encoded image data must not be empty.", "image");

                var result = nativeApi.Recognize(imageList[i]);

                OnRecognitionProgress(i + 1, imageList.Count, 
                    new PlateRecognitionResult(result.ImageWidth, result.ImageHeight, result.ProcessingTimeMs, []),
                    (float)(i + 1) / imageList.Count);

                results.Add(new PlateRecognitionResult(
                    result.ImageWidth,
                    result.ImageHeight,
                    result.ProcessingTimeMs,
                    result.Plates.Select(p => new RecognizedPlate(
                        p.BestText,
                        p.Confidence,
                        p.Candidates.Select(c => new PlateCandidate(c.Text, c.Confidence)).ToArray(),
                        p.Corners.ToArray())).ToArray()));

                totalProcessingTimeMs += result.ProcessingTimeMs;
                successCount++;
            }
            catch (Exception ex) when (ex is not OpenAlprException)
            {
                var error = new BatchError(i, $"Recognition failed: {ex.Message}", ex, false);
                errors.Add(error);
                OnRecognitionError(i, $"Recognition failed: {ex.Message}", ex, false);
            }
        }

        var averageTime = successCount > 0 ? totalProcessingTimeMs / successCount : 0f;

        OnCompleted(new BatchRecognitionResult(imageList.Count, successCount, errors.Count, averageTime, results, errors));

        return new BatchRecognitionResult(imageList.Count, successCount, errors.Count, averageTime, results, errors);
    }

    /// <summary>Recognizes a single image asynchronously, honoring the supplied cancellation token.</summary>
    /// <param name="image">The encoded image data. Must not be empty.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        if (image.IsEmpty)
            throw new ArgumentException("Encoded image data must not be empty.", nameof(image));

        return await Task.Run(() => Recognize(image), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Recognizes a batch of images asynchronously, honoring the supplied cancellation token.</summary>
    /// <param name="images">The images to recognize. Must not be empty.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (images == null || !images.Any())
            throw new ArgumentException("Image collection must not be empty.", nameof(images));

        BatchRecognitionResult result;
        try
        {
            result = await Task.Run(() => RecognizeBatch(images, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return result;
    }

    private void OnRecognitionProgress(int current, int total, PlateRecognitionResult? result, float progress)
        => RecognitionProgress?.Invoke(this, new PlateRecognitionEventArgs(current, total, result, progress));

    private void OnRecognitionError(int imageIndex, string errorMessage, Exception? innerException, bool isRecoverable)
        => RecognitionError?.Invoke(this, new PlateErrorEventArgs(imageIndex, errorMessage, innerException, isRecoverable));

    private void OnCompleted(BatchRecognitionResult result)
        => Completed?.Invoke(this, new PlateCompletedEventArgs(result));

    /// <summary>Releases the native resources owned by this recognizer. Safe to call multiple times.</summary>
    public void Dispose()
    {
        if (disposed)
            return;
        nativeApi.Dispose();
        disposed = true;
    }
}

/// <summary>Thrown when the native OpenALPR API fails during recognition or engine initialization.</summary>
public sealed class OpenAlprException : Exception
{
    /// <summary>Creates a new exception with an optional wrapped cause.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="innerException">The underlying error, if any.</param>
    public OpenAlprException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
