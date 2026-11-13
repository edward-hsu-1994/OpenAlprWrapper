using System.Runtime.InteropServices;
using System.Text.Json;

namespace OpenAlprWrapper;

/// <summary>Loads and invokes the platform-specific OpenALPR C ABI bridge.</summary>
internal sealed class NativeOpenAlprApi : IOpenAlprNativeApi
{
    private const string LibraryName = "openalpr_dotnet";
    private readonly IntPtr handle;
    private readonly string country;
    private readonly int topN;
    private bool disposed;

    /// <summary>Initializes the native OpenALPR API with configuration options.</summary>
    /// <param name="options">OpenALPR options including country, config path, runtime directory, and recognition settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="OpenAlprException">Thrown when native library fails to load or engine initialization fails.</exception>
    public NativeOpenAlprApi(OpenAlprOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        this.country = options.Country;
        handle = Native.Create(this.country, options.ConfigurationFile ?? string.Empty,
            options.RuntimeDirectory ?? string.Empty, this.topN = options.TopN, options.DetectRegion ? 1 : 0);
        
        if (handle == IntPtr.Zero)
            throw new OpenAlprException($"Failed to initialize OpenALPR engine for country '{options.Country}' with TopN={options.TopN}. Ensure native runtime data files are installed.", null);
    }

    public unsafe NativeRecognitionResult Recognize(ReadOnlyMemory<byte> image)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        
        if (image.IsEmpty)
            throw new ArgumentException("Image data must not be empty.", nameof(image));
        
        using var pin = image.Pin();
        var jsonPointer = Native.Recognize(handle, (IntPtr)pin.Pointer, (nuint)image.Length);
        
        if (jsonPointer == IntPtr.Zero)
            throw new OpenAlprException($"Recognition failed for country='{country}' with TopN={topN}. No plate detected or native API returned null result.", null);
        
        try
        {
            using var json = JsonDocument.Parse(Marshal.PtrToStringUTF8(jsonPointer) ?? "{}");
            var root = json.RootElement;
            
            var plates = new List<NativePlateResult>();
            if (root.TryGetProperty("results", out var results))
            {
                foreach (var plate in results.EnumerateArray())
                {
                    var candidates = plate.GetProperty("candidates").EnumerateArray()
                        .Select(c => new NativePlateCandidate(
                            c.GetProperty("plate").GetString() ?? string.Empty, 
                            c.GetProperty("confidence").GetSingle())).ToArray();
                    var corners = plate.GetProperty("coordinates").EnumerateArray()
                        .Select(p => new PlatePoint(
                            p.GetProperty("x").GetInt32(), 
                            p.GetProperty("y").GetInt32())).ToArray();
                    plates.Add(new NativePlateResult(
                        plate.GetProperty("plate").GetString() ?? string.Empty,
                        plate.GetProperty("confidence").GetSingle(), 
                        candidates, corners));
                }
            }
            
            return new NativeRecognitionResult(
                root.GetProperty("img_width").GetInt32(),
                root.GetProperty("img_height").GetInt32(),
                root.GetProperty("processing_time_ms").GetSingle(), 
                plates);
        }
        catch (JsonException ex)
        {
            throw new OpenAlprException($"OpenALPR returned malformed or truncated JSON during recognition for country='{country}'.", ex);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            throw new OpenAlprException($"OpenALPR returned JSON with missing or invalid fields during recognition for country='{country}'.", ex);
        }
        finally
        {
            Native.FreeString(jsonPointer);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        Native.Destroy(handle);
        disposed = true;
    }

    private static class Native
    {
        [DllImport(LibraryName, EntryPoint = "alpr_create", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr Create(string country, string config, string runtime, int topN, int detectRegion);
        
        [DllImport(LibraryName, EntryPoint = "alpr_recognize_json", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Recognize(IntPtr engine, IntPtr image, nuint length);
        
        [DllImport(LibraryName, EntryPoint = "alpr_free_string", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeString(IntPtr value);
        
        [DllImport(LibraryName, EntryPoint = "alpr_destroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Destroy(IntPtr engine);
    }
}
