# Troubleshooting Guide

Comprehensive guide to diagnosing and resolving common issues with OpenALPRWrapper.

## 🔍 Quick Error Reference

| Error Message | Possible Cause | Resolution |
|---------------|----------------|------------|
| "Failed to initialize OpenALPR engine..." | Runtime data files missing or invalid path | Install OpenALPR runtime data files (see [Installation Guide](installation.md)) |
| "DllNotFoundException" / "openalpr.so not found" | Native library not installed or not in library path | Download and install native binary for target platform |
| "EntryPointNotFoundException" | Expected C ABI exports not found in library | Use correct OpenALPR bridge build and verify exported symbols |
| "DirectoryNotFoundException" | Runtime directory path does not exist | Verify directory exists or use absolute path |
| "FileNotFoundException" (ConfigurationFile) | Custom configuration file missing | Verify file exists or omit configuration file path |
| "Recognition failed for country=..." | No plate in image or unsupported format | Check image brightness, contrast, blurriness, and orientation |

---

## 🛠️ Error Category: Initialization Errors

### ❌ Error: "Failed to initialize OpenALPR engine for country 'us'..."

**Cause:** The native library failed to initialize the OpenALPR engine, typically because the runtime data directory was missing, empty, or inaccessible.

**Resolution Steps:**

1. **Verify Runtime Directory Existence:**
   ```bash
   # Linux/macOS
   ls -la /path/to/openalpr/runtime/
   
   # Windows (PowerShell)
   Get-ChildItem C:\OpenALPR\runtime\
   ```

2. **If the directory is empty or missing, download and extract Runtime Data:**
   ```bash
   curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz | \
     tar -xzf - -C /path/to/openalpr/runtime/
   ```

3. **Verify files are installed properly:**
   ```bash
   ls -la runtime/*.dat  # Should list .dat files
   ls -la runtime/*.xml  # Should list XML files
   ```

### ❌ Error: "DllNotFoundException: openalpr_dotnet" (Windows/macOS) or "libopenalpr.so" (Linux)

**Cause:** Platform-specific native library (`.dll`, `.so`, or `.dylib`) could not be located by the dynamic linker.

**Resolution (by platform):**

#### Windows:

```powershell
# Method 1: Download from GitHub Releases and extract to application folder or PATH
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_windows_x64.zip `
     | Expand-Archive -DestinationPath C:\OpenALPR -Force

# Add to system PATH if not copying to the application directory
$env:PATH += ";C:\OpenALPR"
```

#### Linux:

```bash
# Extract native library
tar -xzf openalpr_linux_x64.tar.gz

# Install to system library path
sudo cp openalpr/libopenalpr.so* /usr/lib/
sudo ldconfig

# Verify installation
ldconfig -p | grep libopenalpr
```

#### macOS:

```bash
tar -xzf openalpr_osx_universal.zip
cp -R openalpr/* /usr/local/lib/openalpr/
export DYLD_LIBRARY_PATH=/usr/local/lib/openalpr:$DYLD_LIBRARY_PATH
```

**Verify Installation:**
```bash
# Linux
ldconfig -p | grep openalpr
# Should display: libopenalpr.so.X -> /usr/lib/openalpr.so.X

# macOS
ls -la /usr/local/lib/openalpr/*.dylib
# Should list .dylib files

# Windows (PowerShell)
Get-Command openalpr_dotnet.dll -ErrorAction SilentlyContinue
```

---

## 🔍 Error Category: API Usage Errors

### ❌ Error: "Recognition failed for country='us' with TopN=10. No plate detected or native API returned null result."

**Cause:** The image does not contain a recognizable license plate, the license plate is obscured or poorly lit, or the image format is incompatible.

**Debugging Steps:**

1. **Inspect Image Content and Properties:**
   ```csharp
   var result = recognizer.Recognize(imageBytes);
   Console.WriteLine($"Image size: {result.ImageWidth}x{result.ImageHeight}");
   Console.WriteLine($"Plates found: {result.Plates.Count}");
   
   if (result.Plates.Count == 0)
   {
       Console.WriteLine("No plates detected. Possible causes:");
       Console.WriteLine("- Image may not contain a license plate");
       Console.WriteLine("- License plate may be too dark, blurry, or angled");
       Console.WriteLine("- Configured country code does not match the plate format");
   }
   ```

2. **Verify with a Known Test Image:**
   ```csharp
   // Test with known working image from sample repository
   var testData = File.ReadAllBytes("samples/images/google-thumbnail-1.jpg");
   var testResult = recognizer.Recognize(testData);
   
   Console.WriteLine($"Test result: {testResult.Plates.Count} plates found");
   foreach (var plate in testResult.Plates)
       Console.WriteLine($"  - {plate.BestText}: {plate.Confidence:F2}%");
   ```

### ❌ Error: "ArgumentException: Encoded image data must not be empty."

**Cause:** Empty or zero-byte array passed to `Recognize()`.

**Resolution:**

```csharp
// ✅ Correct usage: Ensure byte array contains valid image data
byte[] imageData = File.ReadAllBytes("image.jpg");
if (imageData.Length > 0)
{
    var result = recognizer.Recognize(imageData);
}

// ❌ Incorrect: Passing empty buffer throws ArgumentException
var result = recognizer.Recognize(Array.Empty<byte>());
```

---

## 🔍 Error Category: Configuration Errors

### ❌ Error: "DirectoryNotFoundException: Runtime directory does not exist"

**Cause:** The path provided in `OpenAlprOptions.RuntimeDirectory` does not exist on disk.

**Resolution:**

```csharp
// ✅ Use absolute path and verify existence before initializing
var runtimePath = Path.GetFullPath("/usr/local/lib/openalpr/runtime");
if (!Directory.Exists(runtimePath))
{
    throw new DirectoryNotFoundException($"Runtime directory missing: {runtimePath}");
}

var options = new OpenAlprOptions 
{ 
    Country = "us",
    RuntimeDirectory = runtimePath
};

using var recognizer = new OpenAlprRecognizer(options);
```

### ❌ Error: "FileNotFoundException: Configuration file does not exist"

**Cause:** The path provided in `OpenAlprOptions.ConfigurationFile` does not point to an existing file.

**Resolution:**

**Option 1: Use default configuration (recommended for standard setups)**
```csharp
var options = new OpenAlprOptions 
{ 
    Country = "us"
    // Leave ConfigurationFile null to use built-in defaults
};
```

**Option 2: Provide valid path to custom configuration**
```csharp
string configFile = "/path/to/custom_config.ini";
if (File.Exists(configFile))
{
    var options = new OpenAlprOptions 
    { 
        Country = "eu",
        ConfigurationFile = configFile,
        RuntimeDirectory = "/usr/local/lib/openalpr/runtime"
    };
}
else
{
    throw new FileNotFoundException($"Configuration file not found: {configFile}", nameof(configFile));
}
```

---

## 🔍 Error Category: Performance & Resource Issues

### ❌ Warning/Issue: "High memory usage during batch processing"

**Cause:** Instantiating a new `OpenAlprRecognizer` per image causes repeated engine allocations and overhead.

**Resolution:**

```csharp
// ✅ RECOMMENDED: Use RecognizeBatch for multi-image processing
var options = new OpenAlprOptions { Country = "us", TopN = 10 };
using var recognizer = new OpenAlprRecognizer(options);

var batchResult = recognizer.RecognizeBatch(imageDataList);

// Or reuse a single recognizer instance across sequential calls
foreach (var imageData in imageDataList)
{
    var result = recognizer.Recognize(imageData);
}

// ⚠️ AVOID: Creating a new recognizer instance per image
foreach (var imageData in imageDataList)
{
    using var recognizer = new OpenAlprRecognizer(options); // Inefficient: repeatedly reinitializes native engine
    var result = recognizer.Recognize(imageData);
}
```

### ❌ Issue: "Slow recognition performance"

**Cause:** High image resolution (e.g., 4K images) or single-threaded synchronous processing of large batches.

**Resolution:**

1. **Resize images to standard recognition resolution (~640x480 to 1280x720):**
   ```csharp
   // Resize high-resolution images before recognition
   var resized = ResizeImage(inputImage, targetWidth: 640, targetHeight: 480);
   var result = recognizer.Recognize(resized);
   ```

2. **Reuse recognizer instance and leverage async methods:**
   ```csharp
   using var recognizer = new OpenAlprRecognizer(options);
   
   // Async batch recognition
   var batchResult = await recognizer.RecognizeBatchAsync(images, cancellationToken);
   ```

---

## 🔍 Error Category: Platform-Specific Issues

### Linux: "libopenalpr.so.X: cannot open shared object file"

**Resolution:**

```bash
# Locate where the .so file is installed
find /usr -name "libopenalpr*" 2>/dev/null

# Add directory to LD_LIBRARY_PATH
export LD_LIBRARY_PATH=/usr/lib:$LD_LIBRARY_PATH

# Or symlink to standard system library directory
sudo ln -s /usr/local/lib/libopenalpr.so /usr/lib/x86_64-linux-gnu/libopenalpr.so
```

### Windows: Target Framework Compatibility

**Resolution:**

```powershell
# Ensure you are running on supported .NET runtime (.NET 8.0 or .NET 10.0)
dotnet --list-runtimes

# In your project file:
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework> <!-- or net8.0 -->
  </PropertyGroup>
</Project>
```

---

## 🧪 Debug Mode: Enable Verbose Logging

To inspect detailed error information and inner exceptions:

```csharp
try 
{
    var recognizer = new OpenAlprRecognizer(options);
    var result = recognizer.Recognize(imageData);
}
catch (Exception ex)
{
    Console.WriteLine("=== Exception Chain ===");
    Console.WriteLine($"Outer: {ex.GetType().Name}: {ex.Message}");
    
    // Print all inner exceptions
    var current = ex;
    int depth = 0;
    while (current.InnerException != null && depth < 5)
    {
        Console.WriteLine($"Inner [{++depth}]: {current.InnerException.GetType().Name}: {current.InnerException.Message}");
        current = current.InnerException;
    }
    
    // Print stack trace for debugging
    Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
}
```

---

## 📊 Diagnostic Checklist

Before opening an issue, verify:

- [ ] OpenALPR native library is compiled and installed for your platform architecture
- [ ] Runtime data files are extracted into the target runtime directory
- [ ] `RuntimeDirectory` and `ConfigurationFile` paths are valid absolute paths
- [ ] Images contain visible, unoccluded license plates
- [ ] Country code parameter (`Country`) matches the plate standard (e.g., `us`, `eu`, `uk`)
- [ ] Application runs on a supported .NET version (.NET 10.0 or .NET 8.0)

---

## 📞 Getting Help

If the troubleshooting steps above do not resolve your issue, please submit an issue with:

1. **Platform and Environment:** OS (Linux/Windows/macOS), architecture (x64/arm64), .NET version
2. **OpenALPR Version:** Native library version and build source
3. **Full Error Details:** Complete exception messages, inner exceptions, and stack traces
4. **Configuration Options:** Configured `OpenAlprOptions` values
5. **Sample Image:** (If applicable) Sample image that triggers the failure

---

## 🔗 Useful Links

- [OpenALPR GitHub Issues](https://github.com/OpenALPR/openalpr/issues)
- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [.NET Interop Troubleshooting](https://learn.microsoft.com/dotnet/standard/native-interop/best-practices)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**License:** AGPL-3.0-only
