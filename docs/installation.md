# Installation Guide

Comprehensive installation steps and runtime configuration guide for OpenALPRWrapper.

## 📋 Prerequisites

### Required Components

| Component | Description | Version Requirement |
|-----------|-------------|---------------------|
| **.NET Runtime** | .NET runtime environment | .NET 10.0 or .NET 8.0 |
| **OpenALPR Native Library** | OpenALPR C ABI bridge (`openalpr_dotnet`) | Built against OpenALPR v2.3+ / v3.x |
| **Runtime Data Files** | OpenALPR training data and configuration files | Required for plate detection and recognition |

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

1. Visit [OpenALPR GitHub Releases](https://github.com/OpenALPR/openalpr/releases).
2. Download the platform-specific package:
   - Linux: `openalpr_linux_x64.tar.gz` or `openalpr_linux_arm64.tar.gz`
   - Windows: `openalpr_windows_x64.zip`
   - macOS: `openalpr_osx_universal.zip`

3. Extract to a known directory, for example:
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

### From a Local Package

Build the package from the repository root, then add it from the local package
directory. Replace the example version with the version you built:

```bash
make package PACKAGE_OUTPUT_DIR=./artifacts/packages
dotnet add package OpenAlprWrapper --source ./artifacts/packages --version 0.1.0
```

### Publish to NuGet.org (Optional)

The tag-triggered GitHub Actions workflow publishes the package to NuGet.org
using Trusted Publishing and uploads it as a downloadable artifact. It obtains
a short-lived publishing credential through GitHub OIDC; no long-lived NuGet
API key is stored in GitHub. Before the first release, configure a NuGet.org
Trusted Publishing policy for GitHub owner `edward-hsu-1994`, repository
`OpenALPRWrapper`, and workflow filename `publish.yml`. Set the repository
Actions secret `NUGET_USER` to your NuGet.org profile name, not your email.
For local manual publishing, a NuGet API key is still required; set it in
`NUGET_API_KEY` before running the command below:

```bash
PACKAGE_VERSION=0.1.0
dotnet nuget push \
    "./artifacts/packages/OpenAlprWrapper.${PACKAGE_VERSION}.nupkg" \
    --api-key "$NUGET_API_KEY" \
    --source "https://api.nuget.org/v3/index.json" \
    --skip-duplicate
```

### Install from NuGet.org

After publishing, add the package to your project:

```bash
dotnet add package OpenAlprWrapper
```

---

## 🔧 Configure Runtime Paths

### Using Default Paths

Standard system installations work without explicit path configuration:

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us", TopN = 10 };

using var recognizer = new OpenAlprRecognizer(options);

// Works with standard OpenALPR system installations
var result = recognizer.Recognize(imageBytes);
```

### Custom Runtime Directory

To specify a non-standard runtime data directory:

```csharp
var options = new OpenAlprOptions 
{ 
    Country = "us",
    TopN = 10,
    RuntimeDirectory = "/usr/local/lib/openalpr/runtime" // Custom runtime path
};

using var recognizer = new OpenAlprRecognizer(options);
```

### Custom Configuration File (Optional)

To specify a custom `openalpr.conf` file:

```csharp
var options = new OpenAlprOptions 
{ 
    Country = "eu",                                   // European plates
    ConfigurationFile = "/path/to/custom_config.ini", // Optional custom configuration
    RuntimeDirectory = "/usr/local/lib/openalpr/runtime"
};

using var recognizer = new OpenAlprRecognizer(options);
```

---

## 🧪 Verify Installation

### Quick Test

```bash
# Run these commands from the repository root.

# Run unit tests (with mocks, does not require native library)
dotnet test --configuration Release --filter "FullyQualifiedName~OpenAlprRecognizerTests"

# Run sample application with test images (requires compiled native bridge and runtime data)
dotnet run --project samples/OpenAlprWrapper.Sample.csproj
```

### Manual Verification Test

Create a simple verification script:

```csharp
using System;
using System.IO;
using OpenAlprWrapper;

var options = new OpenAlprOptions 
{ 
    Country = "us", 
    TopN = 5,
    RuntimeDirectory = "/path/to/openalpr/runtime" // Adjust to your installation path
};

try 
{
    using var recognizer = new OpenAlprRecognizer(options);
    
    // Load a test image (e.g., from samples/images/)
    var imageData = File.ReadAllBytes("samples/images/wikipedia-155-JVM.jpg");
    var result = recognizer.Recognize(imageData);
    
    Console.WriteLine("Processing complete!");
    Console.WriteLine($"Image: {result.ImageWidth}x{result.ImageHeight}");
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

**Cause:** Native runtime data files were not found or could not be loaded.

**Resolution:**

```bash
# Check if runtime directory exists and contains data
ls -la /usr/local/lib/openalpr/runtime/

# If empty or missing, download and extract runtime data:
curl -L https://github.com/OpenALPR/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz | \
  tar -xzf - -C /usr/local/lib/openalpr/runtime/
```

### Error: "DllNotFoundException: openalpr_dotnet" (Windows/macOS) or "openalpr.so" (Linux)

**Cause:** The native library was not found in the operating system library search path.

**Resolution:**

```bash
# Linux - add to LD_LIBRARY_PATH
export LD_LIBRARY_PATH=/usr/local/lib:/usr/local/lib/openalpr:$LD_LIBRARY_PATH

# Or copy native library to system library directory
sudo cp /usr/local/lib/openalpr/*.so /usr/lib/
sudo ldconfig
```

### Error: "DirectoryNotFoundException" or path-related errors

**Cause:** The specified `RuntimeDirectory` does not exist on disk.

**Resolution:**

1. Verify that runtime data files are extracted correctly.
2. Provide an absolute path in `OpenAlprOptions.RuntimeDirectory`.
3. On Windows, use valid path formatting (either forward slashes or escaped backslashes).

---

## 🐳 Docker Deployment (Optional)

Run in a pre-built Docker container environment:

```bash
# Build development environment
docker compose up -d --build

# Or use a specific compose file
docker compose -f docker-compose.dev.yml up -d
```

---

## 📚 Additional Resources

- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [OpenALPR GitHub Repository](https://github.com/OpenALPR/openalpr)
- [.NET P/Invoke Best Practices](https://learn.microsoft.com/dotnet/standard/native-interop/best-practices)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**License:** AGPL-3.0-only
