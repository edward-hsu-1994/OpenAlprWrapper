using Xunit;
using OpenAlprWrapper;

namespace OpenAlprWrapper.Tests;

public sealed class OpenAlprRecognizerTests
{
    [Fact]
    public void Recognize_MapsMultipleCandidatesAndEmptyResults()
    {
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(
                640,
                480,
                12.5f,
                [new NativePlateResult("ABC123", 90.0f, 
                    [new NativePlateCandidate("ABC123", 90.0f), new NativePlateCandidate("ABC12B", 71.0f)], [])])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        var populated = recognizer.Recognize(new byte[] { 1, 2, 3 });
        Assert.Equal(640, populated.ImageWidth);
        Assert.Equal("ABC123", Assert.Single(populated.Plates).BestText);
        Assert.Equal(2, populated.Plates[0].Candidates.Count);

        native.Result = new NativeRecognitionResult(0, 0, 0, []);
        Assert.Empty(recognizer.Recognize(new byte[] { 1 }).Plates);
    }

    [Fact]
    public void Recognize_AfterDispose_ThrowsObjectDisposedException()
    {
        var native = new FakeNativeApi();
        var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
        recognizer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => recognizer.Recognize(new byte[] { 1 }));
        Assert.Equal(1, native.DisposeCount);
    }

    [Fact]
    public void Options_RejectNonPositiveTopN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpenAlprOptions { TopN = 0 }.Validate());
    }

    /// <summary>Verifies exception is thrown when options are null.</summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenAlprRecognizer(null!, new FakeNativeApi()));
    }

    /// <summary>Verifies exception is thrown with null native API.</summary>
    [Fact]
    public void Constructor_NullNativeApi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenAlprRecognizer(new OpenAlprOptions(), null!));
    }

    /// <summary>Tests that empty results are handled correctly.</summary>
    [Fact]
    public void Recognize_EmptyResults_ReturnsEmptyPlates()
    {
        var native = new FakeNativeApi
        {
            Result = new NativeRecognitionResult(640, 480, 5.0f, [])
        };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        var result = recognizer.Recognize(new byte[10]);
        Assert.Empty(result.Plates);
        Assert.Equal(640, result.ImageWidth);
        Assert.Equal(480, result.ImageHeight);
    }

    /// <summary>Tests recognition with multiple plates.</summary>
    [Fact]
    public void Recognize_MultiplePlates_ReturnsCorrectStructure()
    {
        var plates = new[]
        {
            new NativePlateResult("ABC123", 95.5f, 
                [new NativePlateCandidate("ABC123", 95.5f), new NativePlateCandidate("ABC12B", 71.0f)],
                new PlatePoint[] { new PlatePoint(100, 100), new PlatePoint(140, 100), new PlatePoint(140, 130), new PlatePoint(100, 130) }),
            new NativePlateResult("XYZ789", 88.2f, 
                [new NativePlateCandidate("XYZ789", 88.2f), new NativePlateCandidate("XYZT9", 65.0f)],
                new PlatePoint[] { new PlatePoint(200, 100), new PlatePoint(240, 100), new PlatePoint(240, 130), new PlatePoint(200, 130) })
        };

        var native = new FakeNativeApi { Result = new NativeRecognitionResult(640, 480, 15.3f, plates) };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);

        var result = recognizer.Recognize(new byte[10]);
        Assert.Equal(2, result.Plates.Count);
        Assert.Equal("ABC123", result.Plates[0].BestText);
        Assert.Equal(95.5f, result.Plates[0].Confidence);
        Assert.Equal(2, result.Plates[0].Candidates.Count);
        Assert.Equal(4, result.Plates[0].Corners.Count);
    }

    /// <summary>Stress test: multiple Recognize/Dispose cycles should not leak resources.</summary>
    [Fact]
    public void StressTest_MultipleRecognizeDisposeCycles_DoesNotLeak()
    {
        var iterations = 100;
        for (var i = 0; i < iterations; i++)
        {
            var native = new FakeNativeApi
            {
                Result = new NativeRecognitionResult(640, 480, 10.0f, [])
            };
            using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
            try
            {
                recognizer.Recognize(new byte[10]);
            }
            finally
            {
                // Recognizer will be disposed via 'using'
            }
        }
    }

    private sealed class FakeNativeApi : IOpenAlprNativeApi
    {
        public NativeRecognitionResult Result { get; set; } = new(0, 0, 0, []);
        public int DisposeCount { get; private set; }
        public NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image) => Result;
        public void Dispose() => DisposeCount++;
    }
}
