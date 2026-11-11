# Troubleshooting Guide

常見問題與解決方法完整指南

## 🔍 錯誤訊息快速參考

| 錯誤訊息 | 可能原因 | 解決方案 |
|----------|----------|----------|
| "Failed to initialize OpenALPR engine..." | Runtime Data files missing | 安裝 OpenALPR runtime data (見 Installation) |
| "DllNotFoundException" / "openalpr.so not found" | Native library not installed | 下載並安裝對應平台的 native binary |
| "EntryPointNotFoundException" | Expected C API exports not found | 使用正確的 OpenALPR build，驗證 exported symbols |
| "DirectoryNotFoundException" | Runtime directory doesn't exist | 檢查路径是否存在或使用絕對路徑 |
| "FileNotFoundException" (ConfigurationFile) | Custom config file missing | 提供存在的路徑或省略此選項 |
| "OpenALPR returned no recognition result." | No plate in image or invalid data | 檢查圖片明暗、模糊度、是否包含合法车牌 |

---

## 🛠️ Error Category: Initialization Errors

### ❌ Error: "Failed to initialize OpenALPR engine for country 'us'..."

**原因：** Native library failed to load due to missing runtime files.

**解決方案步驟：**

1. **檢查 Runtime Directory 存在性：**
   ```bash
   # Linux/macOS
   ls -la /path/to/openalpr/runtime/
   
   # Windows (PowerShell)
   Get-ChildItem C:\OpenALPR\runtime\
   ```

2. **如果目錄為空或不存在，下載 Runtime Data：**
   ```bash
   curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz | \
     tar -xzf - -C /path/to/openalpr/runtime/
   ```

3. **驗證文件已正確安裝：**
   ```bash
   ls -la runtime/*.dat  # Should list .dat files
   ls -la runtime/*.xml  # Should list XML files
   ```

### ❌ Error: "DllNotFoundException: openalpr_dotnet" (Windows/macOS)

**原因：** Platform-specific native library (.dll or .so) not installed.

**解決方案（按平台）：**

#### Windows:

```powershell
# Method 1: Download from GitHub Releases
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_windows_x64.zip \
     | Expand-Archive -DestinationPath C:\OpenALPR -Force

# Method 2: Copy to GAC (Global Assembly Cache)
Copy-Item "C:\OpenALPR\openalpr_dotnet.dll" [System.Runtime.InteropServices.MSBuildCoreAssembly]
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
```

**驗證安裝：**
```bash
# Linux
ldconfig -p | grep openalpr
# Should show: libopenalpr.so.X -> /usr/lib/openalpr.so.X

# macOS
ls -la /usr/local/lib/openalpr/*.dylib
# Should show .dylib files

# Windows (PowerShell)
Get-Module openalpr_dotnet | Select-Object Name, Path
```

---

## 🔍 Error Category: API Usage Errors

### ❌ Error: "OpenALPR returned no recognition result."

**原因：** Image does not contain a valid license plate.

**調試步驟：**

1. **檢查圖片內容：**
   ```csharp
   var result = recognizer.Recognize(imageBytes);
   Console.WriteLine($"Image size: {result.ImageWidth}x{result.ImageHeight}");
   Console.WriteLine($"Plates found: {result.Plates.Count}");
   
   if (result.Plates.Count == 0)
   {
       // Try with different image or adjust parameters
       Console.WriteLine("No plates detected. Possible causes:");
       Console.WriteLine("- Image may not contain a license plate");
       Console.WriteLine("- License plate may be too dark/blurry/angled");
       Console.WriteLine("- Country code may not match the plate style");
   }
   ```

2. **使用測試圖片驗證：**
   ```csharp
   // Test with known good image
   var testData = File.ReadAllBytes("samples/images/test.jpg");
   var testResult = recognizer.Recognize(testData);
   
   Console.WriteLine($"Test result: {testResult.Plates.Count} plates found");
   foreach (var plate in testResult.Plates)
       Console.WriteLine($"  - {plate.BestText}: {plate.Confidence:F2}%");
   ```

### ❌ Error: "ArgumentException: Encoded image data must not be empty."

**原因：** Passed empty or null image bytes.

**解決方案：**

```csharp
// ✅ Correct usage
byte[] imageData = File.ReadAllBytes("image.jpg");  // Not empty
var result = recognizer.Recognize(imageData);

// ❌ Incorrect - avoid this
var result = recognizer.Recognize(new byte[0]);  // Throws!
```

---

## 🔍 Error Category: Configuration Errors

### ❌ Error: "DirectoryNotFoundException: Runtime directory does not exist"

**原因：** Specified runtime directory path doesn't exist.

**解決方案：**

```csharp
// ✅ Use absolute path and verify it exists first
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

**原因：** Custom configuration file path is invalid.

**解決方案：**

**Option 1: Use default configuration (recommended for most cases)**
```csharp
var options = new OpenAlprOptions 
{ 
    Country = "us",
    // Don't specify ConfigurationFile to use default
};
```

**Option 2: Provide valid path to custom config**
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
    throw new FileNotFoundException($"Configuration file not found: {configFile}", nameof(ConfigurationFile));
}
```

---

## 🔍 Error Category: Performance & Resource Issues

### ❌ Warning/Issue: "High memory usage during batch processing"

**原因：** Creating recognizer per image causes unnecessary allocations.

**解決方案：**

```csharp
// ✅ RECOMMENDED: Create once, reuse multiple times
var options = new OpenAlprOptions { Country = "us", TopN = 10 };
using var recognizer = new OpenAlprRecognizer(options);

foreach (var imageData in imageDataList)
{
    var result = recognizer.Recognize(imageData);
}

// Or use 'using' pattern for automatic disposal
foreach (var imageData in imageDataList)
{
    using var recognizer = new OpenAlprRecognizer(options);
    var result = recognizer.Recognize(imageData);
}
```

### ❌ Issue: "Slow recognition performance"

**原因：** Image resolution too high or many concurrent requests.

**解決方案：**

1. **Resize images before processing (optional):**
   ```csharp
   // Resize to optimal size (~640x480) before recognition
   var resized = ResizeImage(inputImage, targetWidth: 640, targetHeight: 480);
   var result = recognizer.Recognize(resized);
   ```

2. **Reuse same recognizer instance:**
   ```csharp
   // ✅ Better performance
   using var recognizer = new OpenAlprRecognizer(options);
   
   // Reuse across multiple images
   foreach (var image in images)
       ProcessImage(recognizer.Recognize(image));
   ```

---

## 🔍 Error Category: Platform-Specific Issues

### Linux: "libopenalpr.so.X: cannot open shared object file"

**解決方案：**

```bash
# Find where .so file is located
find /usr -name "libopenalpr*" 2>/dev/null

# Add to LD_LIBRARY_PATH
export LD_LIBRARY_PATH=/usr/lib:$LD_LIBRARY_PATH

# Or symlink to standard location
sudo ln -s /path/to/libopenalpr.so.* /usr/lib/x86_64-linux-gnu/libopenalpr.so
```

### Windows: ".NET Core vs .NET Framework compatibility"

**解決方案：**

```powershell
# Ensure you're using .NET Core 3.1+ or .NET 5+
dotnet --list-runtimes

# Project file must target modern framework
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework> <!-- or net8.0, net9.0, etc. -->
  </PropertyGroup>
</Project>
```

---

## 🧪 Debug Mode: Enable Verbose Logging

To see more detailed error information during development:

```csharp
// Wrap in try-catch to see full exception chain
try 
{
    var recognizer = new OpenAlprRecognizer(options);
    var result = recognizer.Recognize(imageData);
}
catch (Exception ex)
{
    Console.WriteLine($"=== Exception Chain ===");
    Console.WriteLine($"Outer: {ex.GetType().Name}: {ex.Message}");
    
    // Print inner exceptions
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

Before reporting an issue, verify:

- [ ] Native library is installed for your platform
- [ ] Runtime data files are extracted to correct directory
- [ ] Configuration paths (RuntimeDirectory) point to valid directories
- [ ] Image being processed contains a valid license plate
- [ ] Using absolute paths (not relative)
- [ ] Running on compatible .NET version (.NET 10.0+)

---

## 📞 Getting Help

如果上述解決方案無效，請提供以下資訊：

1. **平台與版本：** Linux/Windows/macOS, .NET version
2. **OpenALPR 版本：** 檢查 GitHub releases
3. **完整錯誤訊息：** 包含 inner exception 訊息
4. **配置參數：** `OpenAlprOptions` 的設定值
5. **圖片樣本：** (if applicable) Test image for debugging

---

## 🔗 Useful Links

- [OpenALPR GitHub Issues](https://github.com/OpenALPR/openalpr/issues)
- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [.NET Interop Troubleshooting](https://learn.microsoft.com/dotnet/standard/native-interop/best-practices)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**License:** AGPL-3.0-only
