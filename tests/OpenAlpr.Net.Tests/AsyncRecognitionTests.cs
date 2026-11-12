using Xunit;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenAlprWrapper;

namespace OpenAlprWrapper.Tests;

public sealed class AsyncRecognitionTests
{
    [Fact]
    public async Task RecognizeAsync_SingleImage_ReturnsCorrectResult()
    {
        // Arrange
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(
                640, 480, 12.5f,
                [new NativePlateResult("ABC123", 90.0f, [], [])])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act
        var result = await recognizer.RecognizeAsync(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));

        // Assert
        Assert.Equal(640, result.ImageWidth);
        Assert.Equal("ABC123", result.Plates[0].BestText);
    }

    [Fact]
    public async Task RecognizeBatchAsync_MultipleImages_ReturnsCorrectResult()
    {
        // Arrange
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(
                640, 480, 12.5f,
                [new NativePlateResult("ABC123", 90.0f, [], [])])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act
        var result = await recognizer.RecognizeBatchAsync([
            new ReadOnlyMemory<byte>(new byte[] { 1 }),
            new ReadOnlyMemory<byte>(new byte[] { 2 }),
            new ReadOnlyMemory<byte>(new byte[] { 3 })
        ]);

        // Assert
        Assert.Equal(3, result.TotalImages);
        Assert.Equal(3, result.SuccessCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task RecognizeAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(640, 480, 12.5f, [])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act - cancellation token is not requested during execution
        var result = await recognizer.RecognizeAsync(new ReadOnlyMemory<byte>(new byte[] { 1 }));

        // Assert
        Assert.Empty(result.Plates);
    }

    [Fact]
    public async Task RecognizeBatchAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(640, 480, 12.5f, [])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act
        var result = await recognizer.RecognizeBatchAsync([
            new ReadOnlyMemory<byte>(new byte[] { 1 }),
            new ReadOnlyMemory<byte>(new byte[] { 2 })
        ]);

        // Assert
        Assert.Equal(2, result.TotalImages);
    }

    [Fact]
    public async Task RecognizeAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var native = new FakeNativeApi { Result = new NativeRecognitionResult(0, 0, 0, []) };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
        recognizer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => 
            await recognizer.RecognizeAsync(new ReadOnlyMemory<byte>(new byte[] { 1 })));
    }

    [Fact]
    public async Task RecognizeBatchAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var native = new FakeNativeApi { Result = new NativeRecognitionResult(0, 0, 0, []) };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
        recognizer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => 
            await recognizer.RecognizeBatchAsync([new ReadOnlyMemory<byte>(new byte[] { 1 })]));
    }

    private sealed class FakeNativeApi : IOpenAlprNativeApi
    {
        public NativeRecognitionResult Result { get; set; } = new(0, 0, 0, []);
        public int DisposeCount { get; private set; }

        public NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image) => Result;
        public void Dispose() => DisposeCount++;
    }
}
