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

    public event EventHandler<PlateRecognitionEventArgs>? RecognitionProgress;
    public event EventHandler<PlateErrorEventArgs>? RecognitionError;
    public event EventHandler<PlateCompletedEventArgs>? Completed;

    public OpenAlprRecognizer(OpenAlprOptions options) : this(options, CreateNativeApi(options)) { }

    private static IOpenAlprNativeApi CreateNativeApi(OpenAlprOptions options) => new NativeOpenAlprApi(options);

    internal OpenAlprRecognizer(OpenAlprOptions options, IOpenAlprNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(nativeApi);
        options.Validate();
        this.nativeApi = nativeApi;
    }

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

        OnCompleted(new BatchRecognitionResult((int)imageList.Count, successCount, errors.Count, averageTime, results, errors));

        return new BatchRecognitionResult((int)imageList.Count, successCount, errors.Count, averageTime, results, errors);
    }

    public Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        
        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        if (image.IsEmpty)
            throw new ArgumentException("Encoded image data must not be empty.", nameof(image));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return Task.Run(() => Recognize(image), cts.Token).ContinueWith(t => {
            if (cts.IsCancellationRequested)
                throw new OperationCanceledException("Recognition was canceled.", cancellationToken);
            return t.Result;
        });
    }

    public async Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (images == null || !images.Any())
            throw new ArgumentException("Image collection must not be empty.", nameof(images));

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        BatchRecognitionResult result;
        try
        {
            result = await Task.Run(() => RecognizeBatch(images, linked.Token), linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested && cancellationToken.IsCancellationRequested)
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

    public void Dispose()
    {
        if (disposed)
            return;
        nativeApi.Dispose();
        disposed = true;
    }
}

public sealed class OpenAlprException : Exception
{
    public OpenAlprException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
