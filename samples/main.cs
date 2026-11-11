using OpenAlprWrapper;

var imagesDirectory = Path.Combine(AppContext.BaseDirectory, "images");
if (!Directory.Exists(imagesDirectory))
{
    Console.Error.WriteLine($"Images directory not found: {imagesDirectory}");
    return 1;
}

var images = Directory.EnumerateFiles(imagesDirectory)
    .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif")
    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (images.Length == 0)
{
    Console.WriteLine($"No supported images found in {imagesDirectory}");
    return 0;
}

using IPlateRecognizer recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });
foreach (var imagePath in images)
{
    Console.WriteLine($"Image: {Path.GetFileName(imagePath)}");
    try
    {
        var result = recognizer.Recognize(File.ReadAllBytes(imagePath));
        if (result.Plates.Count == 0)
        {
            Console.WriteLine("  No plate detected.");
            continue;
        }

        foreach (var plate in result.Plates)
            Console.WriteLine($"  {plate.BestText} ({plate.Confidence:F2}%)");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OpenAlprException)
    {
        Console.Error.WriteLine($"  Failed: {exception.Message}");
    }
}

return 0;
