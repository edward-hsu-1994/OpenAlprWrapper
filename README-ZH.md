# OpenAlprWrapper

[English](README.md) | 繁體中文
[![CI 狀態](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/ci.yml) [![跨平台測試](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/matrix.yml/badge.svg?branch=main)](https://github.com/edward-hsu-1994/OpenAlprWrapper/actions/workflows/matrix.yml) [![NuGet 版本](https://img.shields.io/nuget/v/OpenAlprWrapper.svg)](https://www.nuget.org/packages/OpenAlprWrapper/) [![NuGet 下載量](https://img.shields.io/nuget/dt/OpenAlprWrapper.svg)](https://www.nuget.org/packages/OpenAlprWrapper/) [![授權](https://img.shields.io/github/license/edward-hsu-1994/OpenAlprWrapper.svg)](https://github.com/edward-hsu-1994/OpenAlprWrapper/blob/main/LICENSE)

OpenAlprWrapper 是 OpenALPR 車牌辨識函式庫的 .NET 包裝程式，透過 P/Invoke 呼叫原生 OpenALPR C API，授權方式為 AGPL-3.0。

## 專案狀態

- ✅ 原生 C ABI bridge 已在 Linux ARM64 Docker 環境以真實影像建置並測試。
- ⚠️ Managed P/Invoke 呼叫路徑尚未在所有作業系統與架構組合完成 smoke test。
- ⚠️ NuGet 套件不包含原生 bridge 執行檔；使用時需自行安裝相符平台的原生函式庫與 OpenALPR runtime data。

## 功能

- 封裝 OpenALPR 原生 C API。
- 可設定 country、設定檔與 runtime data 路徑。
- 支援 .NET 8 與 .NET 10（跨平台）。
- 提供批次、非同步辨識及進度／錯誤事件。
- 透過 `IDisposable` 管理原生資源。

### API 能力

| 方法或事件 | 說明 |
|---|---|
| `Recognize(ReadOnlyMemory<byte>)` | 辨識單張影像。 |
| `RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>>)` | 批次辨識並彙整結果與錯誤。 |
| `RecognizeAsync(..., CancellationToken)` | 非同步辨識單張影像。 |
| `RecognizeBatchAsync(..., CancellationToken)` | 非同步批次辨識。 |
| `RecognitionProgress` | 回報批次處理進度。 |
| `RecognitionError` | 回報批次中的單張影像錯誤。 |
| `Completed` | 回報批次辨識完成。 |

## 快速開始

### 必要條件

1. 安裝適用平台的 OpenALPR 原生函式庫與 bridge；相關原生二進位檔不包含在 NuGet 套件中。請參考 [OpenALPR 安裝說明](https://github.com/OpenALPR/openalpr)。
2. 安裝 OpenALPR runtime data，辨識時需要訓練資料及設定檔。
3. 安裝 .NET 8 或 .NET 10 SDK／runtime。

### 建置與測試

在 repository 根目錄執行：

```bash
make test           # 執行測試
make package        # 建置 NuGet 套件至 artifacts/packages/
make native-build   # 建置原生 bridge（需要 CMake 與 OpenALPR 開發函式庫）
```

### 單張影像辨識

```csharp
using OpenAlprWrapper;

var options = new OpenAlprOptions
{
    Country = "us",
    TopN = 10,
    RuntimeDirectory = "/path/to/openalpr/runtime"
};

using IPlateRecognizer recognizer = new OpenAlprRecognizer(options);
byte[] imageBytes = File.ReadAllBytes("image.jpg");
var result = recognizer.Recognize(imageBytes);

foreach (var plate in result.Plates)
{
    Console.WriteLine($"{plate.BestText}: {plate.Confidence:F2}%");
}
```

### 批次辨識

```csharp
using OpenAlprWrapper;

using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions
{
    Country = "us",
    TopN = 10
});

var images = new ReadOnlyMemory<byte>[]
{
    File.ReadAllBytes("image1.jpg"),
    File.ReadAllBytes("image2.jpg")
};

var result = recognizer.RecognizeBatch(images);
Console.WriteLine($"處理 {result.TotalImages} 張，成功 {result.SuccessCount} 張，失敗 {result.FailureCount} 張");
```

### 非同步辨識與進度事件

```csharp
using OpenAlprWrapper;

using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions { Country = "us" });

recognizer.RecognitionProgress += (_, e) =>
    Console.WriteLine($"處理中：{e.Current}/{e.Total}（{e.Progress:P0}）");

recognizer.RecognitionError += (_, e) =>
    Console.Error.WriteLine($"影像 {e.ImageIndex} 發生錯誤：{e.ErrorMessage}");

recognizer.Completed += (_, e) =>
    Console.WriteLine($"批次完成：{e.Result.SuccessCount}/{e.Result.TotalImages} 張成功");

var image = File.ReadAllBytes("image.jpg");
var singleResult = await recognizer.RecognizeAsync(image, CancellationToken.None);

var images = new ReadOnlyMemory<byte>[]
{
    image,
    File.ReadAllBytes("image2.jpg")
};
var batchResult = await recognizer.RecognizeBatchAsync(images, CancellationToken.None);
```

## NuGet 套件

### 從本機建置

```bash
make package
# 在你的 .NET 專案目錄執行
dotnet add package OpenAlprWrapper --source /path/to/OpenALPRWrapper/artifacts/packages
```

### 從 NuGet.org 安裝

套件發布後可執行：

```bash
dotnet add package OpenAlprWrapper
```

版本 tag（例如 `v1.0.0`）會觸發 GitHub Actions 測試、建置及 NuGet.org 發布流程。自動發布使用 NuGet Trusted Publishing 與 GitHub OIDC，不需在 GitHub 儲存長期 API key；首次發布前需設定 NuGet.org Trusted Publishing policy 與 GitHub Actions 的 `NUGET_USER` secret。

## 設定選項

| 屬性 | 型別 | 預設值 | 說明 |
|---|---|---|---|
| `Country` | `string` | `"us"` | OpenALPR 車牌資料集的國家代碼，例如 `us`、`eu`。 |
| `ConfigurationFile` | `string?` | `null` | 自訂 OpenALPR 設定檔路徑。 |
| `RuntimeDirectory` | `string?` | `null` | OpenALPR runtime data 目錄路徑。 |
| `TopN` | `int` | `10` | 回傳的候選車牌數量上限。 |
| `DetectRegion` | `bool` | `false` | 啟用實驗性區域辨識。 |

## 常見錯誤

| 錯誤 | 常見原因 | 建議處理方式 |
|---|---|---|
| `OpenAlprException: Failed to initialize...` | 原生函式庫或 runtime data 缺失。 | 安裝相符平台的 OpenALPR 原生函式庫並確認 runtime 路徑。 |
| `DllNotFoundException` | 作業系統找不到 `.so`、`.dll` 或 `.dylib`。 | 確認原生檔案及系統 library search path。 |
| `EntryPointNotFoundException` | 原生函式庫缺少預期的 C API 匯出項目。 | 使用包含所需 C ABI bridge symbols 的原生建置版本。 |
| `ArgumentException` | 傳入空影像資料。 | 呼叫辨識前確認影像 bytes 不為空。 |

## 文件

- [API 參考](docs/api/README.md)
- [架構說明](docs/architecture.md)
- [安裝指南](docs/installation.md)
- [疑難排解](docs/troubleshooting.md)
- [貢獻指南](docs/CONTRIBUTING.md)

## 專案結構

```text
OpenALPRWrapper/
├── src/OpenAlprWrapper/       # .NET library
├── tests/OpenAlpr.Net.Tests/  # 單元與整合測試
├── samples/                   # 範例程式與影像
├── native/                    # 原生 C++ bridge
├── docs/                      # 英文技術文件
├── README.md                  # 英文說明
└── README-ZH.md               # 繁體中文說明
```

## 授權

本專案採用 [GNU Affero General Public License v3](https://www.gnu.org/licenses/agpl-3.0.html)。OpenALPR 亦採 AGPL-3.0；散布 OpenALPR 二進位檔時，請保留上游著作權與授權聲明。

## 連結

- [OpenALPR 官方網站](https://www.openalpr.com/)
- [OpenALPR GitHub](https://github.com/OpenALPR/openalpr)
- [.NET 文件](https://learn.microsoft.com/dotnet)
- [本專案 GitHub](https://github.com/edward-hsu-1994/OpenAlprWrapper)
