namespace OpenAlprWrapper;

/// <summary>Recognizes license plates in encoded image data.</summary>
public interface IPlateRecognizer : IDisposable
{
    /// <summary>Recognizes plates in encoded BMP, PNG, JPEG, or GIF image data.</summary>
    /// <param name="image">Encoded image bytes; must not be empty.</param>
    /// <returns>Image metadata and zero or more recognized plates.</returns>
    /// <exception cref="ArgumentException">Thrown when the image is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the recognizer has been disposed.</exception>
    /// <exception cref="OpenAlprException">Thrown when OpenALPR failed to recognize the image.</exception>
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);
}

/// <summary>Internal adapter boundary for native recognition implementations.</summary>
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

    /// <summary>Creates a recognizer using the installed OpenALPR native runtime.</summary>
    /// <param name="options">Country, runtime paths, and recognition settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="OpenAlprException">Thrown when the native OpenALPR runtime could not be loaded.</exception>
    public OpenAlprRecognizer(OpenAlprOptions options) : this(options, CreateNativeApi(options)) { }

    private static IOpenAlprNativeApi CreateNativeApi(OpenAlprOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new NativeOpenAlprApi(options);
    }

    internal OpenAlprRecognizer(OpenAlprOptions options, IOpenAlprNativeApi nativeApi)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(nativeApi);
        options.Validate();
        this.nativeApi = nativeApi;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
            return;
        nativeApi.Dispose();
        disposed = true;
    }
}

/// <summary>Signals failures loading or invoking the OpenALPR runtime.</summary>
public sealed class OpenAlprException : Exception
{
    /// <summary>Creates an exception with a diagnostic message and optional cause for debugging.</summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">Optional original exception providing context. Includes message in outer message for clarity.</param>
    /// <exception cref="OpenAlprException">Thrown when OpenALPR runtime fails to load or execute.</exception>
    public OpenAlprException(string message, Exception? innerException = null) 
        : base($"{message}{(innerException != null ? $" (Original error: {innerException.Message})" : "")}") { }
}
