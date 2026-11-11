# OpenALPR .NET Library Design

## Goal

Create a .NET library that lets .NET applications recognize license plates using OpenALPR. The project will target Windows, macOS, and Linux and use the AGPL-3.0 license to align with OpenALPR's upstream license.

## Scope

The first implementation provides a small, stable .NET façade over OpenALPR's native C++ API. It supports recognizing an encoded image supplied as bytes and returns managed result models. Native binary packaging and dependencies must be designed for all three operating systems; support claims must be backed by builds and smoke tests for each supported runtime identifier (RID).

Video stream processing, remote service clients, a CLI wrapper, ASP.NET integrations, caching, and speculative extension points are out of scope for the initial design.

## Public API

Expose a caller-facing `IPlateRecognizer : IDisposable` with a synchronous method:

```csharp
PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image);
```

`OpenAlprRecognizer` is the concrete implementation. Keep options as a typed configuration object for OpenALPR settings needed by the initial use case, such as country, configuration path, runtime data directory, top-N result count, and region detection. Exact defaults and validation belong to the implementation plan and must follow upstream behavior where possible.

Return managed DTOs, not native C++ objects. The result model should preserve useful upstream recognition data: image dimensions and processing time; recognized plates; candidate text and confidence; plate coordinates; and optional region information. Use immutable or init-only result data where practical.

Do not expose a factory, separate configuration service, asynchronous API, or additional interfaces until a concrete caller workflow requires them. The native recognition call is synchronous, and wrapping it in `Task` would not itself make the native work asynchronous.

## Native Boundary

The public .NET API must not expose C++ ABI details. OpenALPR's API uses C++ types such as `std::string` and `std::vector`, which are unsuitable as a stable direct P/Invoke boundary. Use a small C ABI native bridge to own OpenALPR objects, translate inputs and results, and provide deterministic destruction.

Keep the bridge behind an internal `IOpenAlprNativeApi` abstraction. This boundary enables focused tests of managed mapping and native resource lifecycle without making implementation-specific interop part of the public package contract.

## Platform and Packaging

The intended platforms are Windows, macOS, and Linux. The native bridge and required OpenALPR dependencies must be built and tested per supported RID and architecture. The implementation must not claim general cross-platform support based solely on upstream documentation; list only combinations that have working native artifacts and smoke-test evidence.

Determine during implementation planning whether native assets are built and distributed by this project or provided by the consumer. Whichever delivery model is chosen must document native dependency installation, loading behavior, and unsupported RIDs. Do not silently fall back to a different engine when a native library is unavailable.

## Licensing

License this project under AGPL-3.0. Include the license text and clearly identify bundled upstream components and their notices. The project license does not replace upstream notices or alter the licenses of dependencies.

## Error and Resource Behavior

The concrete recognizer owns its native recognizer handle and implements deterministic disposal. Calls after disposal and native initialization/recognition failures must produce documented .NET exceptions. Native handles and temporary buffers must be released on both success and failure. The initial API is synchronous; cancellation is not promised while an upstream native call is executing.

## Verification

The implementation must verify:

- Managed API result mapping, including zero detections and multiple candidates.
- Initialization failures when native assets or required configuration are unavailable.
- Disposal and rejection of calls after disposal.
- A real recognition smoke scenario using a known image and the native OpenALPR runtime.
- Build and smoke coverage for each claimed Windows, macOS, and Linux RID.

## Upstream Evidence

- OpenALPR C++ API: <https://github.com/openalpr/openalpr/blob/master/src/openalpr/alpr.h>
- OpenALPR README, build platforms, dependencies, and license: <https://github.com/openalpr/openalpr/blob/master/README.md>
