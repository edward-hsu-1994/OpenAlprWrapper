# OpenALPRWrapper Documentation

技術文檔目錄 - [OpenALPR](https://www.openalpr.com/) .NET Wrapper 完整參考指南

## 📚 文檔索引

| 文件 | 說明 |
|------|------|
| **[API Reference](api/README.md)** | API 介面詳解、範例程式碼、屬性說明 |
| **[Installation Guide](installation.md)** | 詳細安裝步驟、環境要求、Runtime 配置 |
| **[Architecture](architecture.md)** | 架構設計、層級結構、依賴注入模式 |
| **[Troubleshooting](troubleshooting.md)** | 常見問題排除、錯誤訊息解析、診斷技巧 |
| **[Contributing](CONTRIBUTING.md)** | 專案貢獻規範、程式碼風格、測試要求 |

## 🎯 API Reference

### IPlateRecognizer 介面

```csharp
public interface IPlateRecognizer : IDisposable
{
    /// <summary>
    /// Recognizes license plates in encoded image data.
    /// </summary>
    /// <param name="image">Encoded image bytes; must not be empty.</param>
    /// <returns>Image metadata and zero or more recognized plates.</returns>
    /// <exception cref="ArgumentException">The image is empty.</exception>
    /// <exception cref="ObjectDisposedException">The recognizer has been disposed.</exception>
    /// <exception cref="OpenAlprException">OpenALPR failed to recognize the image.</exception>
    PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);
}
```

### OpenAlprOptions 配置選項

| 屬性 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| `Country` | `string` | `"us"` | OpenALPR 國家代碼（如 us, uk, eu） |
| `ConfigurationFile` | `string?` | `null` | 自定義配置文件路徑 |
| `RuntimeDirectory` | `string?` | `null` | 自定義 Runtime Data 目錄 |
| `TopN` | `int` | `10` | 返回候選結果數目 |
| `DetectRegion` | `bool` | `false` | 實驗性區域檢測開關 |

### OpenAlprException 例外類型

```csharp
public sealed class OpenAlprException : Exception
{
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">Optional original exception providing context.</param>
    public OpenAlprException(string message, Exception? innerException = null);
}
```

常見錯誤訊息：

| 錯誤訊息 | 原因 | 解決方案 |
|----------|------|----------|
| "Failed to initialize OpenALPR engine..." | Native library 未安裝或 Runtime Data 缺失 | 安裝 OpenALPR native library 並配置 runtime 路徑 |
| "DllNotFoundException" | Platform-specific .so/.dll 找不到 | 確認平台對應的二進位檔案存在 |
| "OpenALPR returned no recognition result." | Image 中無有效车牌 | 檢查圖片明暗、模糊度、是否包含合法车牌 |

---

**版本：** 0.1.0  
**授權：** AGPL-3.0-only  
**最後更新：** 2026-11-12
