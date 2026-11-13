const labels = {
  copy: '複製',
  copied: '已複製',
  note: '說明',
  warning: '注意',
  footer: 'OpenALPRWrapper 以 AGPL-3.0-only 授權。本文件對應套件版本 1.0.2。',
};

const quickStart = `using OpenAlprWrapper;

var options = new OpenAlprOptions { Country = "us", TopN = 10 };
using var recognizer = new OpenAlprRecognizer(options);

var imageBytes = File.ReadAllBytes("car.jpg");
var result = recognizer.Recognize(imageBytes);

Console.WriteLine($"Image: {result.ImageWidth}x{result.ImageHeight} in {result.ProcessingTimeMs} ms");
foreach (var plate in result.Plates)
{
    Console.WriteLine($"{plate.BestText} ({plate.Confidence:F2}%)");
    foreach (var alt in plate.Candidates)
        Console.WriteLine($"   alt: {alt.Text} ({alt.Confidence:F2}%)");
}`;

const pages = [
  {
    slug: 'intro',
    kicker: '總覽',
    title: 'OpenALPRWrapper',
    intro: 'OpenALPR 車牌辨識引擎的 .NET 封裝庫 — 對原生 openalpr_dotnet C ABI bridge 的輕量 P/Invoke 層,內建批次處理、支援取消的非同步 API 與進度事件。',
    sections: [
      {
        t: 'table',
        head: ['項目', '內容'],
        rows: [
          ['套件', 'NuGet.org 上的 OpenAlprWrapper(目前版本 1.0.2)'],
          ['目標框架', '.NET 10.0 與 .NET 8.0'],
          ['授權', 'AGPL-3.0-only'],
          ['原生相依', 'openalpr_dotnet bridge + OpenALPR runtime data'],
          ['原始碼', 'github.com/edward-hsu-1994/OpenAlprWrapper'],
        ],
      },
      { t: 'h2', text: '功能重點' },
      {
        t: 'list',
        items: [
          '單張與批次辨識,批次具備逐張錯誤追蹤(`BatchError.IsRecoverable`)',
          '非同步 API 支援 `CancellationToken` — 取消會在「每張圖片之間」生效',
          '`RecognitionProgress` / `RecognitionError` / `Completed` 事件,方便接 UI 與記錄管線',
          '結構化錯誤模型:`OpenAlprException` 保持乾淨的 `Message`,原始錯誤完整保留在 `InnerException`',
          '確定性的資源管理:公開面為 `IDisposable`,原生 engine handle 由 `SafeHandle` 持有(漏呼叫 `Dispose` 也會在 finalization 時釋放)',
          '套件內附完整 XML 文件 — 所有公開成員都有 IntelliSense 說明',
        ],
      },
      { t: 'h2', text: '架構速覽' },
      {
        t: 'p',
        text: '`OpenAlprRecognizer` 持有 `NativeOpenAlprApi`,後者以 P/Invoke 呼叫各平台的 `openalpr_dotnet` 程式庫。辨識結果由原生引擎以 JSON 回傳,再解析為不可變的 record 模型;受控層在其上疊加驗證、事件、批次與取消。',
      },
      {
        t: 'note',
        kind: 'info',
        text: '只有受控套件是不夠的:機器上必須安裝原生 OpenALPR 引擎與其 runtime data 檔案。請見「安裝與快速上手」。',
      },
    ],
  },
  {
    slug: 'getting-started',
    kicker: '安裝設定',
    title: '安裝與快速上手',
    intro: '安裝原生引擎、加入 NuGet 套件,幾分鐘內辨識第一張車牌。',
    sections: [
      { t: 'h2', text: '1 · 安裝原生 OpenALPR 引擎' },
      { t: 'p', text: 'OpenALPRWrapper 以 P/Invoke 呼叫原生 `openalpr_dotnet` bridge。請依平台安裝引擎與 runtime data:' },
      {
        t: 'code',
        lang: 'bash',
        title: 'macOS(Homebrew)',
        code: `brew install openalpr
# runtime data:將 release 壓縮檔解到程式庫位置
curl -L https://github.com/openalpr/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz \\
  | tar -xzf - -C /usr/local/lib/openalpr/runtime`,
      },
      {
        t: 'code',
        lang: 'bash',
        title: 'Linux(Ubuntu/Debian)',
        code: `sudo apt-get update
sudo apt-get install -y build-essential cmake libopenalpr-dev
git clone https://github.com/openalpr/openalpr.git && cd openalpr && make -j$(nproc)
tar -xzf openalpr_runtime.tar.gz -C /usr/local/openalpr/runtime
ldconfig -p | grep libopenalpr   # 驗證`,
      },
      {
        t: 'code',
        lang: 'powershell',
        title: 'Windows',
        code: `choco install openalpr
# 或從 https://github.com/openalpr/openalpr/releases 下載壓縮檔
# 並解壓(runtime data 已含在內)`,
      },
      { t: 'h2', text: '2 · 加入 NuGet 套件' },
      {
        t: 'code',
        lang: 'bash',
        title: '終端機',
        code: 'dotnet add package OpenAlprWrapper --version 1.0.2',
      },
      { t: 'h2', text: '3 · 辨識第一張車牌' },
      { t: 'code', lang: 'csharp', title: 'Program.cs', code: quickStart },
      {
        t: 'code',
        lang: 'bash',
        title: '輸出',
        code: `Image: 640x480 in 42.3 ms
ABC123 (91.47%)
   alt: A8C123 (12.03%)`,
      },
    ],
  },
  {
    slug: 'configuration',
    kicker: '安裝設定',
    title: '組態設定',
    intro: 'OpenAlprOptions 控制辨識引擎:國別模型、候選數量與原生 runtime 位置。',
    sections: [
      {
        t: 'table',
        head: ['屬性', '型別', '預設值', '說明'],
        rows: [
          ['Country', 'string', '"us"', '訓練資料國別代碼(如 "us"、"eu")'],
          ['TopN', 'int', '10', '每次偵測回傳的最大候選車牌數'],
          ['ConfigurationFile', 'string?', 'null', '自訂 openalpr.conf 路徑(選用)'],
          ['RuntimeDirectory', 'string?', 'null', 'OpenALPR runtime data 目錄(選用)'],
          ['DetectRegion', 'bool', 'false', '實驗性區域偵測'],
        ],
      },
      { t: 'h2', text: '驗證' },
      { t: 'p', text: '建立 recognizer 時會執行 `OpenAlprOptions.Validate()`,提早失敗:' },
      {
        t: 'table',
        head: ['條件', '例外'],
        rows: [
          ['Country 為空白', 'ArgumentException'],
          ['TopN <= 0', 'ArgumentOutOfRangeException'],
          ['ConfigurationFile 有設定但檔案不存在', 'FileNotFoundException'],
          ['RuntimeDirectory 有設定但目錄不存在', 'DirectoryNotFoundException'],
        ],
      },
      { t: 'h2', text: '自訂 runtime 位置' },
      {
        t: 'code',
        lang: 'csharp',
        code: `var options = new OpenAlprOptions
{
    Country = "eu",
    TopN = 5,
    ConfigurationFile = "/opt/openalpr/custom.conf",
    RuntimeDirectory = "/opt/openalpr/runtime",
};
using var recognizer = new OpenAlprRecognizer(options);`,
      },
      {
        t: 'note',
        kind: 'info',
        text: '標準系統安裝不需要任何路徑 — ConfigurationFile 與 RuntimeDirectory 保持 null,原生載入器會使用預設位置。',
      },
    ],
  },
  {
    slug: 'recognition',
    kicker: '使用指南',
    title: '單張影像辨識',
    intro: '以同步 Recognize API 辨識單張已編碼影像中的車牌。',
    sections: [
      { t: 'code', lang: 'csharp', code: quickStart },
      { t: 'h2', text: '結果模型' },
      {
        t: 'table',
        head: ['成員', '型別', '說明'],
        rows: [
          ['ImageWidth / ImageHeight', 'int', '解碼後影像尺寸'],
          ['ProcessingTimeMs', 'float', '此影像的原生處理時間'],
          ['Plates', 'IReadOnlyList<RecognizedPlate>', '偵測到的車牌,最佳候選在前'],
          ['RecognizedPlate.BestText', 'string', '信心值最高的車牌文字'],
          ['RecognizedPlate.Confidence', 'float', 'BestText 的信心值(0–100)'],
          ['RecognizedPlate.Candidates', 'IReadOnlyList<PlateCandidate>', '其他候選讀法(至多 TopN 個)'],
          ['RecognizedPlate.Corners', 'IReadOnlyList<PlatePoint>', '車牌四邊形的像素座標'],
        ],
      },
      {
        t: 'note',
        kind: 'warn',
        text: '空的影像緩衝會拋出 ArgumentException。若引擎找不到車牌(或回傳 null 結果),Recognize 會拋出 OpenAlprException — 單張呼叫請把「找不到車牌」視為例外路徑處理。',
      },
      {
        t: 'note',
        kind: 'info',
        text: '輸入為已編碼影像(依原生建置支援 JPEG/PNG/BMP/GIF),以 ReadOnlyMemory<byte> 傳入 — 不需要自行解碼。',
      },
    ],
  },
  {
    slug: 'batch',
    kicker: '使用指南',
    title: '批次辨識與事件',
    intro: '一次呼叫處理多張影像,具備彙總結果、逐張錯誤追蹤與進度事件。',
    sections: [
      {
        t: 'code',
        lang: 'csharp',
        code: `var paths = Directory.EnumerateFiles("images", "*.jpg");
var images = paths.Select(File.ReadAllBytes);

using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions());
recognizer.RecognitionProgress += (_, e) =>
    Console.WriteLine($"{e.Current}/{e.Total} ({e.Progress:P0})");
recognizer.RecognitionError += (_, e) =>
    Console.Error.WriteLine($"image {e.ImageIndex}: {e.ErrorMessage} (recoverable: {e.IsRecoverable})");
recognizer.Completed += (_, e) =>
    Console.WriteLine($"done: {e.Result.SuccessCount}/{e.Result.TotalImages}, avg {e.Result.AverageProcessingTimeMs:F1} ms");

BatchRecognitionResult batch = recognizer.RecognizeBatch(images);`,
      },
      { t: 'h2', text: 'BatchRecognitionResult' },
      {
        t: 'table',
        head: ['成員', '型別', '說明'],
        rows: [
          ['TotalImages / SuccessCount / FailureCount', 'int', '彙總計數'],
          ['AverageProcessingTimeMs', 'float', '成功影像的平均處理時間'],
          ['Results', 'IReadOnlyList<PlateRecognitionResult>', '每張成功影像一筆'],
          ['Errors', 'IReadOnlyList<BatchError>', '每張失敗影像一筆'],
          ['BatchError.ImageIndex', 'int', '失敗影像的索引(從 0 起)'],
          ['BatchError.InnerException', 'Exception?', '原始錯誤'],
          ['BatchError.IsRecoverable', 'bool', '為 true 表示呼叫端可修正該影像的輸入後重試(目前僅逐張驗證錯誤,例如空資料)'],
        ],
      },
      { t: 'h2', text: '值得了解的語意' },
      {
        t: 'list',
        items: [
          '輸入序列只會被列舉一次 — 惰性來源(例如 `Directory.EnumerateFiles(...).Select(File.ReadAllBytes)`)可安全使用',
          '逐張驗證錯誤(空緩衝 → `ArgumentException`)會記入 `Errors` 且 `IsRecoverable = true`,處理繼續前進到下一張',
          '引擎層級的 `OpenAlprException`(包含原生引擎回報「找不到車牌」)不會被吞掉:批次中止並向外傳播',
          '空或 null 的影像集合在任何原生呼叫前就拋出 `ArgumentException`',
        ],
      },
    ],
  },
  {
    slug: 'async',
    kicker: '使用指南',
    title: '非同步與取消',
    intro: '單張與批次 API 皆支援 CancellationToken 的非阻塞辨識。',
    sections: [
      {
        t: 'code',
        lang: 'csharp',
        code: `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions());

PlateRecognitionResult one = await recognizer.RecognizeAsync(imageBytes, cts.Token);

BatchRecognitionResult many = await recognizer.RecognizeBatchAsync(images, cts.Token);`,
      },
      { t: 'h2', text: '取消語意' },
      {
        t: 'list',
        items: [
          '呼叫前已取消的 token 會在任何原生工作開始前拋出 `OperationCanceledException`',
          '批次取消在「每張圖片之間」生效:迴圈於每次迭代開頭檢查 token',
          '已在原生 P/Invoke 中阻塞的當前圖片無法中途打斷;取消會在下一次迭代生效',
          '取消時 task 以攜帶該 token 的 `OperationCanceledException` 結束',
        ],
      },
      {
        t: 'note',
        kind: 'info',
        text: '非同步面與同步面遵守相同的例外契約:失敗以原始例外型別浮現(例如 OpenAlprException),絕不包進 AggregateException。',
      },
    ],
  },
  {
    slug: 'error-handling',
    kicker: '使用指南',
    title: '錯誤處理',
    intro: '單一例外型別,Message 保持乾淨,InnerException 結構化保留錯誤鏈。',
    sections: [
      {
        t: 'code',
        lang: 'csharp',
        code: `try
{
    var result = recognizer.Recognize(imageBytes);
}
catch (OpenAlprException ex) when (ex.InnerException is not null)
{
    // Message 保持乾淨;原始錯誤(例如原生回傳爛 JSON 時的
    // JsonException)以結構化方式保留。
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine($"caused by: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
}
catch (ArgumentException) { /* 空影像或空批次 */ }
catch (ObjectDisposedException) { /* recognizer 已 Dispose */ }`,
      },
      { t: 'h2', text: '失敗 → 例外對照' },
      {
        t: 'table',
        head: ['失敗情況', '例外'],
        rows: [
          ['引擎初始化失敗(缺 runtime data)', '建構子拋出 OpenAlprException'],
          ['找不到車牌 / 原生回傳 null', 'OpenAlprException(單張呼叫);批次則中止'],
          ['空影像緩衝', 'ArgumentException'],
          ['原生 JSON 格式錯誤或截斷', 'OpenAlprException,InnerException 為 JsonException'],
          ['JSON 缺欄位 / 欄位無效', 'OpenAlprException,InnerException 為 KeyNotFoundException 或 InvalidOperationException'],
          ['Dispose 後使用', 'ObjectDisposedException'],
          ['token 已取消', 'OperationCanceledException'],
        ],
      },
      {
        t: 'note',
        kind: 'info',
        text: '自 1.0.2 起,建構訊息不再內嵌內部錯誤文字(「Original error: …」);細節改由 InnerException 與 ToString() 承載。',
      },
    ],
  },
  {
    slug: 'api-reference',
    kicker: '參考資料',
    title: 'API 參考',
    intro: 'OpenALPRWrapper 套件的完整公開介面。',
    sections: [
      { t: 'h2', text: 'IPlateRecognizer' },
      {
        t: 'table',
        head: ['成員', '簽章'],
        rows: [
          ['Recognize', 'PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image)'],
          ['RecognizeBatch', 'BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images)'],
          ['RecognizeAsync', 'Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken ct = default)'],
          ['RecognizeBatchAsync', 'Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken ct = default)'],
          ['RecognitionProgress', 'event EventHandler<PlateRecognitionEventArgs>?'],
          ['RecognitionError', 'event EventHandler<PlateErrorEventArgs>?'],
          ['Completed', 'event EventHandler<PlateCompletedEventArgs>?'],
          ['Dispose', 'void Dispose() — 可重複呼叫'],
        ],
      },
      { t: 'h2', text: '模型' },
      {
        t: 'table',
        head: ['型別', '結構'],
        rows: [
          ['OpenAlprOptions', 'Country · TopN · ConfigurationFile? · RuntimeDirectory? · DetectRegion;Validate()'],
          ['PlateRecognitionResult', '(ImageWidth, ImageHeight, ProcessingTimeMs, Plates)'],
          ['RecognizedPlate', '(BestText, Confidence, Candidates, Corners)'],
          ['PlateCandidate', '(Text, Confidence)'],
          ['PlatePoint', '(X, Y) — readonly record struct'],
          ['BatchRecognitionResult', '(TotalImages, SuccessCount, FailureCount, AverageProcessingTimeMs, Results, Errors)'],
          ['BatchError', '(ImageIndex, ErrorMessage, InnerException?, IsRecoverable)'],
          ['PlateRecognitionEventArgs', '(Current, Total, Result?, Progress)'],
          ['PlateErrorEventArgs', '(ImageIndex, ErrorMessage, InnerException?, IsRecoverable)'],
          ['PlateCompletedEventArgs', '(Result)'],
          ['OpenAlprException', '(string message, Exception? innerException = null)'],
        ],
      },
      {
        t: 'note',
        kind: 'info',
        text: '所有公開成員都附 XML 文件 — NuGet 套件內建完整的 IntelliSense 說明。',
      },
    ],
  },
  {
    slug: 'troubleshooting',
    kicker: '參考資料',
    title: '疑難排解',
    intro: '常見失敗模式與對應解法。',
    sections: [
      { t: 'h2', text: '「Failed to initialize OpenALPR engine…」' },
      { t: 'p', text: '原生引擎無法啟動時,建構子會拋出 `OpenAlprException`。幾乎都是 runtime data 檔案缺失,或 `RuntimeDirectory` 指到不存在的位置:' },
      {
        t: 'code',
        lang: 'bash',
        code: `ls /usr/local/lib/openalpr/runtime/   # 必須含有訓練資料
# 若為空,解壓 runtime 壓縮檔:
curl -L <runtime-archive-url> | tar -xzf - -C /usr/local/lib/openalpr/runtime/`,
      },
      { t: 'h2', text: 'DllNotFoundException: openalpr_dotnet' },
      { t: 'p', text: '原生 bridge 程式庫不在作業系統的程式庫搜尋路徑上:' },
      {
        t: 'code',
        lang: 'bash',
        code: `# Linux
export LD_LIBRARY_PATH=/usr/local/lib/openalpr:$LD_LIBRARY_PATH
# 或安裝到系統路徑:
sudo cp /usr/local/lib/openalpr/*.so /usr/lib/ && sudo ldconfig`,
      },
      { t: 'h2', text: '啟動時 DirectoryNotFoundException' },
      { t: 'p', text: '`OpenAlprOptions.RuntimeDirectory` 有設定但磁碟上不存在 — 修正路徑(建議用絕對路徑),或保持 null 使用系統預設。' },
      { t: 'h2', text: '取消沒有立即停止' },
      { t: 'p', text: '這是設計使然:批次取消在「每張圖片之間」檢查。已在原生引擎中阻塞的單張影像會先跑完,token 在下一次迭代生效。' },
      { t: 'h2', text: '一張壞圖導致整批中止' },
      { t: 'p', text: '只有引擎層級失敗(`OpenAlprException`,例如「找不到車牌」)會中止批次。逐張輸入問題(空緩衝)會收進 `BatchRecognitionResult.Errors` 且 `IsRecoverable = true`,處理繼續。' },
    ],
  },
];

const nav = [
  {
    title: '入門',
    items: [
      { slug: 'intro', title: '簡介' },
      { slug: 'getting-started', title: '安裝與快速上手' },
      { slug: 'configuration', title: '組態設定' },
    ],
  },
  {
    title: '使用指南',
    items: [
      { slug: 'recognition', title: '單張影像辨識' },
      { slug: 'batch', title: '批次辨識與事件' },
      { slug: 'async', title: '非同步與取消' },
      { slug: 'error-handling', title: '錯誤處理' },
    ],
  },
  {
    title: '參考資料',
    items: [
      { slug: 'api-reference', title: 'API 參考' },
      { slug: 'troubleshooting', title: '疑難排解' },
    ],
  },
];

export default { labels, pages, nav };
