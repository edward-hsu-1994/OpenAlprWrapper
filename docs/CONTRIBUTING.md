# Contributing Guidelines

Thank you for your interest in contributing to OpenALPRWrapper! This document outlines how to participate in project development, adhere to our standards, and submit contributions.

## 📋 Code of Conduct

This project follows open-source community collaboration principles:

- **Respect Others**: Treat all participants with respect, courtesy, and inclusion.
- **Constructive Feedback**: Provide actionable, helpful feedback and avoid personal remarks.
- **Welcome Beginners**: Encourage newcomers to ask questions and get involved.

---

## 🏗️ Architecture & Design Principles

### Core Design Principles

1. **KISS (Keep It Simple, Stupid)**
   - Keep code simple, readable, and intuitive; avoid unnecessary abstractions.
   - Favor straightforward solutions over complex design patterns.

2. **Dependency Inversion**
   - The managed layer does not depend on concrete native implementation details.
   - P/Invoke bridging logic is isolated behind interfaces (`IOpenAlprNativeApi`).

3. **Deterministic Resource Disposal**
   - All native resources are managed via the standard `IDisposable` pattern.
   - Enforce the use of `using` declarations/statements to ensure timely resource cleanup.

4. **Context-Rich Error Handling**
   - Preserve original exception details to facilitate debugging.
   - Include explicit contextual details in error messages (such as country code, TopN, or runtime paths).

### Code Style Guidelines

```csharp
// ✅ DO: Clear, descriptive XML documentation
public class OpenAlprOptions
{
    /// <summary>
    /// Gets or sets the training-data country code.
    /// </summary>
    public string Country { get; init; } = "us";
}

// ✅ DO: Validate inputs early and throw with context
public void Recognize(ReadOnlyMemory<byte> image)
{
    if (image.IsEmpty)
        throw new ArgumentException("Image data must not be empty.", nameof(image));
    
    var result = nativeApi.Recognize(image);
    // ... rest of logic
}

// ✅ DO: Use dependency injection pattern for testability
internal OpenAlprRecognizer(OpenAlprOptions options, IOpenAlprNativeApi nativeApi)
{
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(nativeApi);
    this.nativeApi = nativeApi;
}

// ❌ DON'T: Scatter P/Invoke calls across business logic
```

---

## 🧪 Testing Requirements

### Required Test Scenarios

| Category | Testing Requirement |
|----------|---------------------|
| **Constructor Validation** | Null checks and argument validation exceptions |
| **Empty Results** | No plates detected edge case |
| **Multiple Plates** | Recognition with multiple detections |
| **Resource Management** | `IDisposable` pattern and no native memory leaks |
| **Exception Scenarios** | Native errors and JSON parsing failures |
| **Stress Testing** | 100+ Recognize/Dispose cycles |

### Test Structure Example

```csharp
[Fact]
public void Constructor_NullOptions_ThrowsArgumentNullException()
{
    // Arrange - create mock native API
    var fakeNativeApi = new FakeNativeApi();
    
    // Act & Assert - verify null check
    Assert.Throws<ArgumentNullException>(() => 
        new OpenAlprRecognizer(null!, fakeNativeApi));
}

[Fact]
public void Recognize_MultiplePlates_ReturnsCorrectStructure()
{
    // Arrange - setup multi-plate mock result
    var plates = new[] { ... };
    var native = new FakeNativeApi { Result = new NativeRecognitionResult(640, 480, 15.3f, plates) };
    
    // Act
    using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), native);
    var result = recognizer.Recognize(new byte[10]);
    
    // Assert - verify structure
    Assert.Equal(2, result.Plates.Count);
    Assert.Equal("ABC123", result.Plates[0].BestText);
}

[Fact]
public void StressTest_MultipleRecognizeDisposeCycles_DoesNotLeak()
{
    // Arrange
    var iterations = 100;
    
    // Act - multiple cycles
    for (var i = 0; i < iterations; i++)
    {
        using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions(), new FakeNativeApi());
        recognizer.Recognize(new byte[10]);
    }
    
    // Assert - no memory leak detected
}

private sealed class FakeNativeApi : IOpenAlprNativeApi
{
    public NativeRecognitionResult Result { get; set; } = new(0, 0, 0, []);
    public int DisposeCount { get; private set; }
    
    public NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image) => Result;
    public void Dispose() => DisposeCount++;
}
```

---

## 🔧 Development Workflow

### Building from Source

```bash
# Run these commands from the repository root.

# Build solution
dotnet build OpenAlprWrapper.sln --configuration Release

# Run tests
dotnet test --configuration Release

# Build NuGet package
make nuget PACKAGE_OUTPUT_DIR=./artifacts/packages
```

### Code Review Process

1. **Local Review:**
   - Verify compliance with code style guidelines.
   - Ensure XML documentation is complete on public types and members.
   - Verify that the build succeeds without warnings.

2. **Pre-Commit Checks:**
   ```bash
   # Clean build
   dotnet clean --configuration Release
   dotnet build --configuration Release
   
   # Verify tests pass
   dotnet test --configuration Release
   
   # Check for warnings as errors
   dotnet build -warnaserror
   ```

3. **Commit Message Format:**
   ```text
   type(scope): subject

   body (wrapped at 72 chars)

   footer (optional: issue references, BREAKING changes)
   ```

   Examples:
   - `fix(api): improve error message for missing runtime`
   - `feat(test): add stress test for resource disposal`
   - `docs(readme): add installation troubleshooting section`

### Pull Request Guidelines

- **Describe Changes:** Explain the problem being solved and why this change is necessary.
- **Reference Issues:** Link related GitHub issues or feature requests.
- **Test Coverage:** Ensure new functionality or bug fixes include corresponding tests.
- **Documentation Updates:** When changing public APIs, update `README.md` and the documents under `docs/`.

---

## 📦 Release Process

### Versioning Strategy

This project follows [Semantic Versioning](https://semver.org/) (SemVer):

- `MAJOR`: Breaking API changes (e.g., removing or altering existing public methods).
- `MINOR`: Backward-compatible new features (e.g., adding new methods or options).
- `PATCH`: Backward-compatible bug fixes and documentation updates.

### NuGet Package Builds

GitHub Actions builds and uploads NuGet packages only when a semantic version
tag is pushed. Use the `vMAJOR.MINOR.PATCH` format, for example:

```bash
git tag v1.2.3
git push origin v1.2.3
```

The workflow removes the leading `v` and uses the remaining tag value as the
NuGet package version. SemVer prerelease tags such as `v1.2.3-rc.1` are also
supported. The package is uploaded as a workflow artifact; it is not
automatically published to NuGet.org.

### Pre-Release Checklist

- [ ] All unit tests pass across target frameworks (.NET 10.0 / .NET 8.0)
- [ ] No compiler warnings or build errors
- [ ] XML documentation is complete on all public APIs
- [ ] Root `README.md` and `docs/` updated with changes
- [ ] `CHANGELOG.md` updated with release notes
- [ ] NuGet package builds and packages successfully
- [ ] Sample application tested with images

---

## 🚀 Future Enhancements

### Planned Features (Roadmap)

| Feature | Priority | Description |
|---------|----------|-------------|
| **Stream Recognition** | Medium | `Recognize(Stream imageStream)` / `RecognizeAsync(Stream imageStream)` |
| **Pre-built Native Bundling** | Medium | Cross-platform native binary packaging within NuGet packages |
| **Region Tuning & Masks** | Low | Fine-grained detection masks and country-specific tuning parameters |
| **Memory Pool Optimization** | Low | Shared buffer pooling for high-throughput batch recognition |

### Performance Optimizations

- [ ] Pre-allocated buffers for native string marshalling
- [ ] Built-in image resizing and normalization utilities
- [ ] High-throughput memory pool integration for large batch jobs

---

## 📝 Documentation Standards

### API Documentation

All public types and members must include complete XML documentation:

```csharp
/// <summary>
/// Configuration for OpenALPR recognition and native runtime paths.
/// </summary>
public sealed class OpenAlprOptions
{
    /// <summary>
    /// Gets or sets the training-data country code. Defaults to "us".
    /// </summary>
    public string Country { get; init; } = "us";
}

/// <summary>
/// Managed OpenALPR recognizer with deterministic native-resource ownership.
/// </summary>
public sealed class OpenAlprRecognizer : IPlateRecognizer
{
    // ...
}
```

### Documentation Maintenance

| Document | Update Trigger |
|----------|----------------|
| **README.md** | New feature added or breaking changes introduced |
| **CHANGELOG.md** | Every release (following SemVer) |
| **docs/api/README.md** | Public API surface changes or model updates |
| **docs/architecture.md** | Architectural refactoring or component changes |
| **docs/installation.md** | Changes to build steps, dependencies, or packaging |
| **docs/troubleshooting.md** | New known issues, workarounds, or diagnostic tools |

---

## 🌐 Community & Support

### Reporting Issues

When reporting issues via GitHub Issues, please include:

- **Platform & Environment:** Operating system (Linux/Windows/macOS), architecture, .NET version
- **OpenALPR Version:** Native library version and build source
- **Full Error Messages:** Complete exception message, including inner exceptions and stack traces
- **Minimal Reproduction:** Minimal code sample demonstrating the issue
- **Configuration:** `OpenAlprOptions` values used

### Security Vulnerabilities

If you discover a security vulnerability, please report it privately to the maintainers rather than opening a public issue:

📧 security@[your-email].example.com

---

## 📜 License

This project is licensed under the AGPL-3.0 license. By contributing code, you agree that your contributions will be licensed under the same AGPL-3.0 license terms:

```
By contributing to this project, you agree that your contributions will be licensed under the same AGPL-3.0 license.
```

---

## 🎓 Learning Resources

- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [.NET P/Invoke Best Practices](https://learn.microsoft.com/dotnet/standard/native-interop/best-practices)
- [Dependency Injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**Maintained by:** OpenALPRWrapper Contributors
