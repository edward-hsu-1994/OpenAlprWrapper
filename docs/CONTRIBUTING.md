# Contributing Guidelines

感謝你想為 OpenALPRWrapper 專案貢獻！本文檔說明如何參與專案開發。

## 📋 Code of Conduct

本專案遵循開放原始碼社區協作原則：

- **尊重他人**：對待所有參與者都保持尊重和包容
- **建設性反饋**：提供具體、有幫助的意見，避免人身攻擊
- **歡迎新手**：鼓勵新人參與和提問

---

## 🏗️ Architecture & Design Principles

### 核心設計原則

1. **KISS (Keep It Simple, Stupid)**
   - 保持代碼簡單直觀，避免過度抽象
   - 優先使用直截了當的解決方案而非複雜的設計模式

2. **依賴倒轉 (Dependency Inversion)**
   - Managed 層不依賴 native 實現細節
   - 通過介面隔離 P/Invoke 橋接邏輯

3. **資源確定性釋放**
   - 所有 native 資源通過 `IDisposable` 模式管理
   - 強制使用 `using` 語句確保正確釋放

4. **錯誤處理最佳化**
   - 保留原始例外訊息用於調試
   - 提供明確的情境資訊（如 failed country、runtime path）

### 程式碼風格規範

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

// ❌ DON'T: Scatter P/Invoke calls in business logic
```

---

## 🧪 Testing Requirements

### 必須覆蓋的測試場景

| 類別 | 測試要求 |
|------|----------|
| **Constructor Validation** | Null checks, validation exceptions |
| **Empty Results** | No plates detected edge case |
| **Multiple Plates** | Batch recognition with multiple detections |
| **Resource Management** | Dispose pattern, no memory leaks |
| **Exception Scenarios** | Native errors, JSON parsing failures |
| **Stress Testing** | 100+ Recognize/Dispose cycles |

### 測試結構範例

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
cd /Users/edward_hsu/SourceCodes/OpenALPRWrapper

# Build solution
dotnet build OpenAlprWrapper.sln --configuration Release

# Run tests
dotnet test --configuration Release

# Build NuGet package
make nuget PACKAGE_OUTPUT_DIR=./artifacts/packages
```

### Code Review Process

1. **本地審查：**
   - 檢查是否遵循程式碼風格規範
   - 驗證 XML documentation 完整性
   - 確保無編譯警告

2. **提交前檢查：**
   ```bash
   # Clean build
   dotnet clean --configuration Release
   dotnet build --configuration Release
   
   # Verify tests pass
   dotnet test --configuration Release
   
   # Check for warnings
   dotnet build -warnaserror
   ```

3. **Commit Message 格式：**
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

- **描述變更：** 說明為什麼需要這個變更
- **引用相關問題：** GitHub issue 編號或 feature request
- **測試覆蓋率：** 確保新增功能有對應測試
- **文件更新：** API 變更需更新 README.md 和 docs/

---

## 📦 Release Process

### Versioning Strategy

採用 [Semantic Versioning](https://semver.org/)：

- `MAJOR`: Breaking API changes (e.g., remove deprecated methods)
- `MINOR`: New features, backward compatible
- `PATCH`: Bug fixes, documentation updates

### Pre-Release Checklist

- [ ] All unit tests pass
- [ ] No compiler warnings/errors
- [ ] XML documentation complete on public APIs
- [ ] README.md updated with new features
- [ ] CHANGELOG.md updated
- [ ] NuGet package builds successfully
- [ ] Sample application tested with images

---

## 🚀 Future Enhancements

### Planned Features (v0.2.0+)

| 功能 | 優先級 | 說明 |
|------|--------|------|
| **批次處理 API** | High | `BatchRecognize(IEnumerable<byte>)` |
| **Async 支援** | Medium | `RecognizeAsync(..., CancellationToken)` |
| **進度回調** | Medium | `IProgress<float>` for long operations |
| **事件驅動錯誤** | Low | `EventPattern<PlateErrorEventArgs>` |

### Performance Optimizations

- [ ] JSON parsing pre-warming / caching
- [ ] Image resizing utilities (optional)
- [ ] Memory pool for large batch processing

---

## 📝 Documentation Standards

### API Documentation

所有公開類型必須包含完整 XML 文件：

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
    
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">Optional original exception providing context.</param>
    /// <exception cref="OpenAlprException">Thrown when OpenALPR runtime fails to load.</exception>
    public OpenAlprException(string message, Exception? innerException = null);
}
```

### Documentation Updates

| 文件類型 | 更新時機 |
|----------|----------|
| **README.md** | New feature added / Breaking change |
| **CHANGELOG.md** | Every release (see versioning section) |
| **docs/api/README.md** | API surface changes |
| **docs/architecture.md** | Architecture refactoring |

---

## 🌐 Community & Support

### Reporting Issues

使用 GitHub Issues 報告問題時，請提供：

- **平台與版本：** Linux/Windows/macOS, .NET version
- **OpenALPR 版本：** Installed native library version
- **完整錯誤訊息：** Including inner exception messages
- **最小復現步驟：** Minimal code to reproduce the issue
- **配置參數：** `OpenAlprOptions` settings used

### Security Vulnerabilities

發現安全問題請直接聯繫維護者，不要公開披露：

📧 security@[your-email].example.com

---

## 📜 License

本專案採用 AGPL-3.0 授權。貢獻代碼表示你同意在 AGPL-3.0 下分發你的變更。

```
By contributing to this project, you agree that your contributions will be licensed under the same AGPL-3.0 license.
```

---

## 🎓 Learning Resources

- [OpenALPR Official Documentation](https://docs.openalpr.com/)
- [.NET P/Invoke Best Practices](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute)
- [Dependency Injection Patterns](https://learn.microsoft.com/dotnet/fundamentals/design-patterns/inversion-of-control)

---

**Version:** 0.1.0  
**Last Updated:** 2026-11-12  
**Maintained by:** OpenALPRWrapper Contributors
