using Xunit;
using System.Collections.Generic;
using OpenAlprWrapper;

namespace OpenAlprWrapper.Tests;

public sealed class BatchRecognitionTests
{
    [Fact]
    public void RecognizeBatch_EmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var native = new FakeNativeApi { Result = new NativeRecognitionResult(640, 480, 10f, []) };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => recognizer.RecognizeBatch(new List<ReadOnlyMemory<byte>>().AsEnumerable()));
    }

    [Fact]
    public void RecognizeBatch_SingleSuccessfulRecognition_ReturnsCorrectResult()
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
        var result = recognizer.RecognizeBatch([new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 })]);

        // Assert
        Assert.Equal(1, result.TotalImages);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RecognizeBatch_MultipleSuccessfulRecognitions_ReturnsCorrectResult()
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
        var result = recognizer.RecognizeBatch([
            new ReadOnlyMemory<byte>(new byte[] { 1 }),
            new ReadOnlyMemory<byte>(new byte[] { 2 }),
            new ReadOnlyMemory<byte>(new byte[] { 3 })
        ]);

        // Assert
        Assert.Equal(3, result.TotalImages);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RecognizeBatch_AvgProcessingTime_CalculatedCorrectly()
    {
        // Arrange - single image processing time is 12.5ms
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(640, 480, 12.5f, [])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        // Act - 3 images with same processing time
        var result = recognizer.RecognizeBatch([
            new ReadOnlyMemory<byte>(new byte[] { 1 }),
            new ReadOnlyMemory<byte>(new byte[] { 2 }),
            new ReadOnlyMemory<byte>(new byte[] { 3 })
        ]);

        // Assert - average of [12.5, 12.5, 12.5] = 12.5
        Assert.Equal(12.5f, result.AverageProcessingTimeMs);
    }

    [Fact]
    public void RecognizeBatch_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var native = new FakeNativeApi { Result = new NativeRecognitionResult(0, 0, 0, []) };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
        recognizer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => recognizer.RecognizeBatch([new ReadOnlyMemory<byte>(new byte[] { 1 })]));
    }

    [Fact]
    public async Task RecognizeBatchAsync_SuccessfulImages_ReturnsCorrectCounts()
    {
        // Arrange
        var native = new FakeNativeApi { Result = new NativeRecognitionResult(640, 480, 10f, []) };
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
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task RecognizeBatchAsync_CancelledToken_ThrowsOperationCanceled_AndInvokesNoNative()
    {
        // Arrange: cancel before invoking so the per-iteration check short-circuits.
        var cts = new CancellationTokenSource();
        cts.Cancel();

        using var api = new CountingNativeApi();
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), api);

        // Act & Assert: cancellation propagates and no native call happens.
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => recognizer.RecognizeBatchAsync(
                Enumerable.Range(1, 5).Select(_ => new ReadOnlyMemory<byte>(new byte[] { 1 })),
                cts.Token));

        Assert.Equal(0, api.RecognizeCount);
    }

    private sealed class CountingNativeApi : IOpenAlprNativeApi
    {
        private int _recognizeCount;
        public int RecognizeCount => Volatile.Read(ref _recognizeCount);

        public NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image) => Interlocked.Increment(ref _recognizeCount) > 0 ? new NativeRecognitionResult(0, 0, 0f, []) : new NativeRecognitionResult(0, 0, 0f, []);

        public void Dispose() { }
    }

    private sealed class FakeNativeApi : IOpenAlprNativeApi
    {
        public NativeRecognitionResult Result { get; set; } = new(0, 0, 0, []);
        public int DisposeCount { get; private set; }

        public NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image) => Result;
        public void Dispose() => DisposeCount++;
    }
}
