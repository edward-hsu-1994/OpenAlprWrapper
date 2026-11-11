# Installation Guide

OpenALPRWrapper 完整安裝步驟與配置指南

## 📋 Prerequisites

### Required Components

| 組件 | 說明 | 版本要求 |
|------|------|----------|
| **.NET Runtime** | .NET 10.0 或更高版本 | 必須安裝對應平台運行庫 |
| **OpenALPR Native Library** | OpenALPR C ABI bridge | v3.x+ 推薦 |
| **Runtime Data Files** | OpenALPR training data | 必須安裝才能識別车牌 |

### Platform-Specific Installation

#### Linux (Ubuntu/Debian)

```bash
# 1. Install build dependencies
sudo apt-get update
sudo apt-get install -y build-essential cmake libopenalpr-dev

# 2. Clone and build OpenALPR native library
git clone https://github.com/OpenALPR/openalpr.git
cd openalpr
make -j$(nproc)

# 3. Install runtime data files
tar -xzf openalpr_runtime.tar.gz -C /usr/local/openalpr/runtime

# 4. Verify installation
ldconfig -p | grep libopenalpr
```

#### Windows (MSVC)

```powershell
# Install via chocolatey (optional, or download directly)
choco install openalpr

# Or download from GitHub releases:
# https://github.com/OpenALPR/openalpr/releases

# Extract to a known location
exp -o C:\OpenALPR\openalpr.exe openalpr_windows_x64.tar.gz

# Install runtime data files (included in release package)
tar -xf openalpr_windows_x64.tar.gz
```

#### macOS (Homebrew)

```bash
# Install OpenALPR native library
brew install openalpr

# Download runtime data files from GitHub releases
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz \
     | tar -xzf - -C /usr/local/lib/openalpr/runtime
```

### Manual Installation (All Platforms)

#### Download OpenALPR Native Library

1. Visit [OpenALPR GitHub Releases](https://github.com/OpenALPR/openalpr/releases)
2. Download platform-specific package:
   - Linux: `openalpr_linux_x64.tar.gz` or `openalpr_linux_arm64.tar.gz`
   - Windows: `openalpr_windows_x64.zip`
   - macOS: `openalpr_osx_universal.zip`

3. Extract to a known location, e.g.:
   - Linux: `/usr/local/lib/openalpr/`
   - Windows: `C:\OpenALPR\`
   - macOS: `/usr/local/lib/openalpr/`

#### Install Runtime Data Files

```bash
# Linux/macOS (extract to native library location)
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz | \
  tar -xzf - -C /usr/local/lib/openalpr/runtime/

# Windows (use PowerShell)
Expand-Archive -Path openalpr_runtime_*.zip -DestinationPath C:\OpenALPR\runtime -Force
```

---

## 🚀 Install NuGet Package

### From Local Package

After building:

```bash
cd /Users/edward_hsu/SourceCodes/OpenALPRWrapper/artifacts/packages

# Add to your project
dotnet add package OpenAlprWrapper --source ./OpenAlprWrapper.0.1.0.nupkg

# Or use with custom version
dotnet add package OpenAlprWrapper --version 0.1.0
```

### From NuGet.org (After Publishing)

First, publish to NuGet.org:

```bash
dotnet nuget push \
    artifacts/packages/OpenAlprWrapper.0.1.0.nupkg \
    -k "YOUR_NUGET_API_KEY" \
    -s "https://api.nuget.org/v3/index.json" \
    --skip-duplicate
```

Then add to your project:

```bash
dotnet add package OpenAlprWrapper --version 0.1.0
```

---

## 🔧 Configure Runtime Paths

### Using Default Paths

Most installations work with default paths:

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us", TopN = 10 };

using var recognizer = new OpenAlprRecognizer(options);

// This should work with standard OpenALPR installation
var result = recognizer.Recognize(imageBytes);
```

### Custom Runtime Directory

If you need to specify a custom runtime location:

```csharp
var options = new OpenAlprOptions 
{ 
    Country = "us",
    TopN = 10,
    RuntimeDirectory = "/usr/local/lib/openalpr/runtime" // or your custom path
};

using var recognizer = new OpenAlprRecognizer(options);
```

### Custom Configuration File (Optional)

If you have a custom OpenALPR configuration:

```csharp
var options = new OpenAlprOptions 
{ 
    Country = "eu",           // European plates
    ConfigurationFile = "/path/to/custom_config.ini",  // Optional
    RuntimeDirectory = "/usr/local/lib/openalpr/runtime"
};

using var recognizer = new OpenAlprRecognizer(options);
```

---

## 🧪 Verify Installation

### Quick Test

```bash
cd /Users/edward_hsu/SourceCodes/OpenALPRWrapper

# Run unit tests (requires native library)
dotnet test --configuration Release --filter "FullyQualifiedName~OpenAlprRecognizerTests"

# Or run sample application with test images
dotnet run --project samples/OpenAlprWrapper.Sample.csproj
```

### Manual Verification Test

Create a simple test:

```csharp
using System;
using OpenAlprWrapper;

var options = new OpenAlprOptions 
{ 
    Country = "us", 
    TopN = 5,
    RuntimeDirectory = "/path/to/openalpr/runtime" // Adjust to your installation
};

try 
{
    using var recognizer = new OpenAlprRecognizer(options);
    
    // Use a test image (e.g., from samples/images/)
    var imageData = File.ReadAllBytes("samples/images/test.jpg");
    var result = recognizer.Recognize(imageData);
    
    Console.WriteLine($"Processing complete!");
    Console.WriteLine($"Images: {result.ImageWidth}x{result.ImageHeight}");
    Console.WriteLine($"Plates found: {result.Plates.Count}");
    
    if (result.Plates.Count > 0)
    {
        foreach (var plate in result.Plates)
            Console.WriteLine($"- {plate.BestText}: {plate.Confidence:F2}%");
    }
    else
    {
        Console.WriteLine("No license plates detected in this image.");
    }
}
catch (OpenAlprException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
        Console.Error.WriteLine($"Inner: {ex.InnerException.Message}");
}
```

---

## ❌ Common Installation Errors

### Error: "Failed to initialize OpenALPR engine..."

**Cause:** Native runtime data files not found.

**Solution:**

```bash
# Check if runtime directory exists
ls -la /usr/local/lib/openalpr/runtime/

# If empty or missing, download runtime data:
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz | \
  tar -xzf - -C /usr/local/lib/openalpr/runtime/
```

### Error: "DllNotFoundException: openalpr_dotnet" (Windows/macOS) or "openalpr.so" (Linux)

**Cause:** Native library not found in system PATH.

**Solution:**

```bash
# Linux - add to LD_LIBRARY_PATH
export LD_LIBRARY_PATH=/usr/local/lib:/usr/local/lib/openalpr:$LD_LIBRARY_PATH

# Or move native library to /usr/lib
sudo cp /usr/local/lib/openalpr/*.so /usr/lib/
sudo ldconfig
```

### Error: "DirectoryNotFoundException" or path-related errors

**Cause:** Runtime directory doesn't exist.

**Solution:**

1. Verify runtime files are extracted correctly
2. Use absolute path in `OpenAlprOptions.RuntimeDirectory`
3. For Windows, use forward slashes instead of backslashes

---

## 🐳 Docker Deployment (Optional)

Use pre-built Docker image:

```bash
# Build development environment
docker compose up -d --build

# Or use provided docker-compose.yml (if created in future releases)
docker compose -f docker-compose.dev.yml up -d
```

---

## 📚 Additional Resources

- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [OpenALPR GitHub Repository](https://github.com/OpenALPR/openalpr)
- [.NET P/Invoke Best Practices](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**License:** AGPL-3.0-only
