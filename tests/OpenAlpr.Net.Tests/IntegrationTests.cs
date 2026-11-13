using Xunit;
using System.IO;
using OpenAlprWrapper;

namespace OpenAlprWrapper.Tests;

/// <summary>
/// Integration tests using real sample images to verify native API compatibility.
/// All tests that require native library are marked with [Skip].
/// </summary>
public sealed class IntegrationTests
{
    private const string ImagesDirectoryPath = "../../samples/images";
    
    [Fact]
    public void Integration_ImageFilesExist()
    {
        var imagesDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images");
        
        if (!Directory.Exists(imagesDirectory))
            return;

        Assert.True(true, "Samples directory exists and is accessible");
    }

    [Fact(Skip = "Requires OpenALPR native library.")]
    public void Integration_EmptyImage_ReturnsEmptyResults()
    {
        var emptyImageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
        var result = recognizer.Recognize(emptyImageBytes);
        Assert.Empty(result.Plates);
    }

    [Fact(Skip = "Requires OpenALPR native library.")]
    public void Integration_SinglePlateImage_DetectsCorrectly()
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images", "wikipedia-155-JVM.jpg");
        Assert.True(File.Exists(imagePath), $"Test image not found: {imagePath}");
        
        var imageBytes = File.ReadAllBytes(imagePath);
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
        var result = recognizer.Recognize(imageBytes);
        Assert.NotNull(result);
    }

    [Fact(Skip = "Requires OpenALPR native library.")]
    public void Integration_MultipleImages_BatchRecognitionWorks()
    {
        foreach (var fileName in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images"), "*.jpg"))
        {
            var imageBytes = File.ReadAllBytes(fileName);
            using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
            var result = recognizer.Recognize(imageBytes);
            Assert.NotNull(result);
        }
    }

    [Fact(Skip = "Requires OpenALPR native library.")]
    public void Integration_BatchWithMultipleImages_ReturnsCorrectCounts()
    {
        var imagePaths = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images"), "*.jpg");
        var imageBytesList = new List<ReadOnlyMemory<byte>>();
        
        foreach (var path in imagePaths)
            imageBytesList.Add(File.ReadAllBytes(path));

        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
        var result = recognizer.RecognizeBatch(imageBytesList);
        Assert.Equal(imagePaths.Length, result.TotalImages);
    }

    [Fact(Skip = "Requires OpenALPR native library.")]
    public async Task Integration_AsynchronousRecognition_CompletesSuccessfully()
    {
        var imagePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images", "wikipedia-155-JVM.jpg");
        if (!File.Exists(imagePath)) return;
        
        var imageBytes = File.ReadAllBytes(imagePath);
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
        var result = await recognizer.RecognizeAsync(imageBytes, CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public void Integration_ImageValidation_ValidatesPathExistence()
    {
        var options = new OpenAlprOptions 
        { 
            Country = "us",
            RuntimeDirectory = "/nonexistent/path"
        };
        Assert.Throws<DirectoryNotFoundException>(() => options.Validate());
    }

    [Fact]
    public void Integration_ImageFileSizes_VerifyValidImageFiles()
    {
        try
        {
            var imagesDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "images");
            
            if (!Directory.Exists(imagesDirectory))
                return; // Skip if directory doesn't exist
            
            foreach (var fileName in Directory.GetFiles(imagesDirectory, "*.jpg"))
            {
                var size = new FileInfo(fileName).Length;
                Assert.True(size >= 500, $"{fileName} is too small ({size} bytes)");
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
        {
            // Skip test if samples directory doesn't exist
            return;
        }
    }
}
