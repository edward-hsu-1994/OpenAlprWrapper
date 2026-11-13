const labels = {
  copy: 'Copy',
  copied: 'Copied',
  note: 'Note',
  warning: 'Warning',
  themeToggle: 'Toggle dark / light theme',
  footer: 'OpenALPRWrapper is licensed under AGPL-3.0-only. Documentation covers package version 1.0.2.',
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
    kicker: 'Overview',
    title: 'OpenALPRWrapper',
    intro: 'An idiomatic .NET wrapper for the OpenALPR license-plate-recognition engine — a thin, allocation-conscious P/Invoke layer over the native openalpr_dotnet C ABI bridge, with batch processing, async APIs with cancellation, and progress events.',
    sections: [
      {
        t: 'table',
        head: ['Fact', 'Value'],
        rows: [
          ['Package', 'OpenAlprWrapper on NuGet.org (current: 1.0.2)'],
          ['Targets', '.NET 10.0 and .NET 8.0'],
          ['License', 'AGPL-3.0-only'],
          ['Native dependency', 'openalpr_dotnet bridge + OpenALPR runtime data'],
          ['Source', 'github.com/edward-hsu-1994/OpenAlprWrapper'],
        ],
      },
      { t: 'h2', text: 'Feature highlights' },
      {
        t: 'list',
        items: [
          'Single-image and batch recognition with per-image error tracking (`BatchError.IsRecoverable`)',
          'Async APIs honoring `CancellationToken` — cancellation is observed between images',
          '`RecognitionProgress` / `RecognitionError` / `Completed` events for UI and logging pipelines',
          'Structured error model: `OpenAlprException` keeps a clean `Message` and preserves the original error on `InnerException`',
          'Deterministic resource management: `IDisposable` on the public surface, `SafeHandle` on the native engine handle (a missed `Dispose` is still released at finalization)',
          'Full XML documentation shipped in the package — every public member has IntelliSense text',
        ],
      },
      { t: 'h2', text: 'Architecture at a glance' },
      {
        t: 'p',
        text: '`OpenAlprRecognizer` owns a `NativeOpenAlprApi`, which P/Invokes the platform-specific `openalpr_dotnet` library. Recognition results arrive as JSON from the native engine and are parsed into immutable record models. The managed layer adds validation, events, batching, and cancellation on top.',
      },
      {
        t: 'note',
        kind: 'info',
        text: 'The managed package alone is not enough: the native OpenALPR engine and its runtime data files must be installed on the machine. See Getting Started.',
      },
    ],
  },
  {
    slug: 'getting-started',
    kicker: 'Setup',
    title: 'Getting Started',
    intro: 'Install the native engine, add the NuGet package, and recognize your first plate in a few minutes.',
    sections: [
      { t: 'h2', text: '1 · Install the native OpenALPR engine' },
      { t: 'p', text: 'OpenALPRWrapper P/Invokes into the native `openalpr_dotnet` bridge. Install the engine and its runtime data for your platform:' },
      {
        t: 'code',
        lang: 'bash',
        title: 'macOS (Homebrew)',
        code: `brew install openalpr
# runtime data: extract the release archive into the library location
curl -L https://github.com/openalpr/openalpr/releases/download/vX.X.X/openalpr_runtime_X.X.X.tar.gz \\
  | tar -xzf - -C /usr/local/lib/openalpr/runtime`,
      },
      {
        t: 'code',
        lang: 'bash',
        title: 'Linux (Ubuntu/Debian)',
        code: `sudo apt-get update
sudo apt-get install -y build-essential cmake libopenalpr-dev
git clone https://github.com/openalpr/openalpr.git && cd openalpr && make -j$(nproc)
tar -xzf openalpr_runtime.tar.gz -C /usr/local/openalpr/runtime
ldconfig -p | grep libopenalpr   # verify`,
      },
      {
        t: 'code',
        lang: 'powershell',
        title: 'Windows',
        code: `choco install openalpr
# or download a release archive from https://github.com/openalpr/openalpr/releases
# and extract it (runtime data is included)`,
      },
      { t: 'h2', text: '2 · Add the NuGet package' },
      {
        t: 'code',
        lang: 'bash',
        title: 'terminal',
        code: 'dotnet add package OpenAlprWrapper --version 1.0.2',
      },
      { t: 'h2', text: '3 · Recognize your first plate' },
      { t: 'code', lang: 'csharp', title: 'Program.cs', code: quickStart },
      {
        t: 'code',
        lang: 'bash',
        title: 'output',
        code: `Image: 640x480 in 42.3 ms
ABC123 (91.47%)
   alt: A8C123 (12.03%)`,
      },
    ],
  },
  {
    slug: 'configuration',
    kicker: 'Setup',
    title: 'Configuration',
    intro: 'OpenAlprOptions controls the recognition engine: country model, candidate count, and native runtime locations.',
    sections: [
      {
        t: 'table',
        head: ['Property', 'Type', 'Default', 'Description'],
        rows: [
          ['Country', 'string', '"us"', 'Training-data country code (e.g. "us", "eu")'],
          ['TopN', 'int', '10', 'Maximum candidate plates returned per detection'],
          ['ConfigurationFile', 'string?', 'null', 'Optional path to a custom openalpr.conf'],
          ['RuntimeDirectory', 'string?', 'null', 'Optional path to the OpenALPR runtime data directory'],
          ['DetectRegion', 'bool', 'false', 'Experimental region detection'],
        ],
      },
      { t: 'h2', text: 'Validation' },
      { t: 'p', text: '`OpenAlprOptions.Validate()` runs when a recognizer is constructed and fails fast:' },
      {
        t: 'table',
        head: ['Condition', 'Exception'],
        rows: [
          ['Country is blank', 'ArgumentException'],
          ['TopN <= 0', 'ArgumentOutOfRangeException'],
          ['ConfigurationFile set but missing', 'FileNotFoundException'],
          ['RuntimeDirectory set but missing', 'DirectoryNotFoundException'],
        ],
      },
      { t: 'h2', text: 'Custom runtime locations' },
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
        text: 'Standard system installations need no paths at all — leave ConfigurationFile and RuntimeDirectory null and the native loader uses its defaults.',
      },
    ],
  },
  {
    slug: 'recognition',
    kicker: 'Guides',
    title: 'Single-Image Recognition',
    intro: 'Recognize plates in one encoded image with the synchronous Recognize API.',
    sections: [
      { t: 'code', lang: 'csharp', code: quickStart },
      { t: 'h2', text: 'Result model' },
      {
        t: 'table',
        head: ['Member', 'Type', 'Description'],
        rows: [
          ['ImageWidth / ImageHeight', 'int', 'Decoded image dimensions'],
          ['ProcessingTimeMs', 'float', 'Native processing time for this image'],
          ['Plates', 'IReadOnlyList<RecognizedPlate>', 'Detected plates, best candidate first'],
          ['RecognizedPlate.BestText', 'string', 'Highest-confidence plate text'],
          ['RecognizedPlate.Confidence', 'float', 'Confidence of BestText (0–100)'],
          ['RecognizedPlate.Candidates', 'IReadOnlyList<PlateCandidate>', 'Alternative readings (up to TopN)'],
          ['RecognizedPlate.Corners', 'IReadOnlyList<PlatePoint>', 'Plate quadrilateral in pixel coordinates'],
        ],
      },
      {
        t: 'note',
        kind: 'warn',
        text: 'An empty image buffer throws ArgumentException. If the engine finds no plate (or returns a null result), Recognize throws OpenAlprException — treat "no plate" as an exception path in single-image calls.',
      },
      {
        t: 'note',
        kind: 'info',
        text: 'The input is the encoded image (JPEG/PNG/BMP/GIF as supported by your native build), passed as ReadOnlyMemory<byte> — no pre-decoding needed.',
      },
    ],
  },
  {
    slug: 'batch',
    kicker: 'Guides',
    title: 'Batch Recognition & Events',
    intro: 'Process many images in one call with aggregated results, per-image error tracking, and progress events.',
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
        head: ['Member', 'Type', 'Description'],
        rows: [
          ['TotalImages / SuccessCount / FailureCount', 'int', 'Aggregated counters'],
          ['AverageProcessingTimeMs', 'float', 'Mean processing time of successful images'],
          ['Results', 'IReadOnlyList<PlateRecognitionResult>', 'One entry per successful image'],
          ['Errors', 'IReadOnlyList<BatchError>', 'One entry per failed image'],
          ['BatchError.ImageIndex', 'int', '0-based index of the failed image'],
          ['BatchError.InnerException', 'Exception?', 'The original error'],
          ['BatchError.IsRecoverable', 'bool', 'true when the caller can fix this image\'s input and retry it (currently: per-image validation errors such as empty data)'],
        ],
      },
      { t: 'h2', text: 'Semantics worth knowing' },
      {
        t: 'list',
        items: [
          'The input sequence is enumerated exactly once — lazy sources (e.g. `Directory.EnumerateFiles(...).Select(File.ReadAllBytes)`) are safe',
          'A per-image validation error (empty buffer → `ArgumentException`) is recorded in `Errors` with `IsRecoverable = true`; processing continues with the next image',
          'An engine-level `OpenAlprException` (including "no plate detected" from the native engine) is not swallowed: it aborts the batch and propagates',
          'An empty or null image collection throws `ArgumentException` before any native call',
        ],
      },
    ],
  },
  {
    slug: 'async',
    kicker: 'Guides',
    title: 'Async & Cancellation',
    intro: 'Non-blocking recognition with CancellationToken support on both single-image and batch APIs.',
    sections: [
      {
        t: 'code',
        lang: 'csharp',
        code: `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var recognizer = new OpenAlprRecognizer(new OpenAlprOptions());

PlateRecognitionResult one = await recognizer.RecognizeAsync(imageBytes, cts.Token);

BatchRecognitionResult many = await recognizer.RecognizeBatchAsync(images, cts.Token);`,
      },
      { t: 'h2', text: 'Cancellation semantics' },
      {
        t: 'list',
        items: [
          'A token cancelled before the call throws `OperationCanceledException` before any native work starts',
          'Batch cancellation is observed between images: the loop checks the token at the top of every iteration',
          'A native P/Invoke already blocking on the current image cannot be interrupted mid-call; cancellation takes effect at the next iteration',
          'On cancellation the task faults with `OperationCanceledException` carrying your token',
        ],
      },
      {
        t: 'note',
        kind: 'info',
        text: 'The async surface mirrors the sync exception contract: failures surface as the original exception (e.g. OpenAlprException), never wrapped in AggregateException.',
      },
    ],
  },
  {
    slug: 'error-handling',
    kicker: 'Guides',
    title: 'Error Handling',
    intro: 'One exception type with a clean Message and a structured InnerException chain.',
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
    // Message stays clean; the original error (e.g. JsonException from a
    // malformed native payload) is preserved structurally.
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine($"caused by: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
}
catch (ArgumentException) { /* empty image or empty batch */ }
catch (ObjectDisposedException) { /* recognizer already disposed */ }`,
      },
      { t: 'h2', text: 'Failure → exception map' },
      {
        t: 'table',
        head: ['Failure', 'Exception'],
        rows: [
          ['Engine failed to initialize (missing runtime data)', 'OpenAlprException from the constructor'],
          ['No plate detected / native returned null', 'OpenAlprException (single-image); aborts a batch'],
          ['Empty image buffer', 'ArgumentException'],
          ['Malformed or truncated native JSON', 'OpenAlprException with JsonException as InnerException'],
          ['Missing / invalid JSON fields', 'OpenAlprException with KeyNotFoundException or InvalidOperationException'],
          ['Use after Dispose', 'ObjectDisposedException'],
          ['Cancelled token', 'OperationCanceledException'],
        ],
      },
      {
        t: 'note',
        kind: 'info',
        text: 'Since 1.0.2 the constructor message no longer embeds the inner error text ("Original error: …"); the detail lives on InnerException and in ToString().',
      },
    ],
  },
  {
    slug: 'api-reference',
    kicker: 'Reference',
    title: 'API Reference',
    intro: 'The complete public surface of the OpenAlprWrapper package.',
    sections: [
      { t: 'h2', text: 'IPlateRecognizer' },
      {
        t: 'table',
        head: ['Member', 'Signature'],
        rows: [
          ['Recognize', 'PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image)'],
          ['RecognizeBatch', 'BatchRecognitionResult RecognizeBatch(IEnumerable<ReadOnlyMemory<byte>> images)'],
          ['RecognizeAsync', 'Task<PlateRecognitionResult> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken ct = default)'],
          ['RecognizeBatchAsync', 'Task<BatchRecognitionResult> RecognizeBatchAsync(IEnumerable<ReadOnlyMemory<byte>> images, CancellationToken ct = default)'],
          ['RecognitionProgress', 'event EventHandler<PlateRecognitionEventArgs>?'],
          ['RecognitionError', 'event EventHandler<PlateErrorEventArgs>?'],
          ['Completed', 'event EventHandler<PlateCompletedEventArgs>?'],
          ['Dispose', 'void Dispose() — idempotent'],
        ],
      },
      { t: 'h2', text: 'Models' },
      {
        t: 'table',
        head: ['Type', 'Shape'],
        rows: [
          ['OpenAlprOptions', 'Country · TopN · ConfigurationFile? · RuntimeDirectory? · DetectRegion; Validate()'],
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
        text: 'Every public member carries XML documentation — full IntelliSense text ships inside the NuGet package.',
      },
    ],
  },
  {
    slug: 'troubleshooting',
    kicker: 'Reference',
    title: 'Troubleshooting',
    intro: 'Common failure modes and their fixes.',
    sections: [
      { t: 'h2', text: '"Failed to initialize OpenALPR engine…"' },
      { t: 'p', text: 'The constructor throws `OpenAlprException` when the native engine cannot start. Almost always the runtime data files are missing or `RuntimeDirectory` points nowhere:' },
      {
        t: 'code',
        lang: 'bash',
        code: `ls /usr/local/lib/openalpr/runtime/   # must contain the training data
# extract the runtime archive if empty:
curl -L <runtime-archive-url> | tar -xzf - -C /usr/local/lib/openalpr/runtime/`,
      },
      { t: 'h2', text: 'DllNotFoundException: openalpr_dotnet' },
      { t: 'p', text: 'The native bridge library is not on the OS library search path:' },
      {
        t: 'code',
        lang: 'bash',
        code: `# Linux
export LD_LIBRARY_PATH=/usr/local/lib/openalpr:$LD_LIBRARY_PATH
# or install system-wide:
sudo cp /usr/local/lib/openalpr/*.so /usr/lib/ && sudo ldconfig`,
      },
      { t: 'h2', text: 'DirectoryNotFoundException on startup' },
      { t: 'p', text: '`OpenAlprOptions.RuntimeDirectory` is set but does not exist on disk — fix the path (absolute paths recommended) or leave it null for system defaults.' },
      { t: 'h2', text: 'Cancellation does not stop immediately' },
      { t: 'p', text: 'By design: batch cancellation is checked between images. A single image already blocking inside the native engine finishes first; the token takes effect at the next iteration.' },
      { t: 'h2', text: 'One bad image aborts the whole batch' },
      { t: 'p', text: 'Only engine-level failures (`OpenAlprException`, e.g. "no plate detected") abort a batch. Per-image input problems (empty buffers) are collected in `BatchRecognitionResult.Errors` with `IsRecoverable = true` and processing continues.' },
    ],
  },
];

const nav = [
  {
    title: 'Getting Started',
    items: [
      { slug: 'intro', title: 'Introduction' },
      { slug: 'getting-started', title: 'Installation & Quick Start' },
      { slug: 'configuration', title: 'Configuration' },
    ],
  },
  {
    title: 'Guides',
    items: [
      { slug: 'recognition', title: 'Single-Image Recognition' },
      { slug: 'batch', title: 'Batch Recognition & Events' },
      { slug: 'async', title: 'Async & Cancellation' },
      { slug: 'error-handling', title: 'Error Handling' },
    ],
  },
  {
    title: 'Reference',
    items: [
      { slug: 'api-reference', title: 'API Reference' },
      { slug: 'troubleshooting', title: 'Troubleshooting' },
    ],
  },
];

export default { labels, pages, nav };
