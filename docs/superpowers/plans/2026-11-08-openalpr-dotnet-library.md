# OpenALPR .NET Library Implementation Plan

> **For agentic workers:** Execute the plan inline in this session. Each task uses test-first implementation and verification.

**Goal:** Create a first usable .NET library API for OpenALPR with a managed façade and a native C ABI boundary, targeting Windows, macOS, and Linux.

**Architecture:** A .NET 10.0 library exposes `IPlateRecognizer`, managed result models, typed options, and `OpenAlprRecognizer`. An internal native adapter calls a small C ABI bridge; native assets are RID-specific and are not claimed available until built and smoke-tested.

**Tech Stack:** .NET 10.0, C#, xUnit, C++ C ABI bridge, NuGet runtime assets.

**Spec:** `docs/superpowers/specs/2026-11-08-openalpr-dotnet-library-design.md`

## Global Constraints

- Target Windows, macOS, and Linux; claim only RIDs with working native artifacts and smoke evidence.
- Do not expose OpenALPR C++ ABI types through the public .NET API.
- Keep the native adapter internal and dispose native handles deterministically.
- License this project under AGPL-3.0 and retain upstream notices.
- Do not add async, factory, CLI, hosted service, or speculative public interfaces.

---

### Task 1: Create managed API and behavior tests

**Files:**
- Create: `src/OpenAlpr.Net/OpenAlprWrapper.csproj`
- Create: `src/OpenAlpr.Net/IPlateRecognizer.cs`
- Create: `src/OpenAlpr.Net/OpenAlprOptions.cs`
- Create: `src/OpenAlpr.Net/PlateRecognitionResult.cs`
- Create: `src/OpenAlpr.Net/OpenAlprRecognizer.cs`
- Create: `tests/OpenAlpr.Net.Tests/OpenAlprWrapper.Tests.csproj`
- Create: tests for options validation, empty/multiple result mapping, and disposed-use behavior.

**Interfaces:**
- Public: `IPlateRecognizer : IDisposable` with `PlateRecognitionResult Recognize(ReadOnlyMemory<byte> image)`.
- Internal: `IOpenAlprNativeApi` handles create, recognize, and destroy operations.
- Concrete: `OpenAlprRecognizer` receives typed options and the native adapter.

- [ ] Write tests first for invalid options, zero/multiple candidate result mapping, and calls after disposal.
- [ ] Run the focused tests and confirm expected failures.
- [ ] Implement managed API and result mapping with XML documentation.
- [ ] Run focused tests and verify pass.

### Task 2: Add native C ABI bridge

**Files:**
- Create: `native/openalpr-dotnet-bridge/CMakeLists.txt`
- Create: `native/openalpr-dotnet-bridge/include/openalpr_dotnet.h`
- Create: `native/openalpr-dotnet-bridge/src/openalpr_dotnet.cpp`
- Add native ABI contract tests or a minimal bridge smoke harness.

**Interfaces:**
- Native exports create/destroy, loaded-state query, encoded-image recognition, and result serialization or typed result accessors.
- Managed adapter calls only exported C ABI functions and translates errors into documented .NET exceptions.

- [ ] Define opaque handles and explicit ownership for every returned buffer/result.
- [ ] Write bridge contract tests or a smoke harness before bridge implementation.
- [ ] Implement OpenALPR calls and ensure cleanup on all error paths.
- [ ] Build against installed OpenALPR headers/libraries where available; otherwise report the missing native prerequisite without inventing successful runtime support.

### Task 3: Package metadata, license, and documentation

**Files:**
- Create: `OpenAlprWrapper.sln`
- Create: `LICENSE`
- Create: `README.md`
- Create: package metadata in the library project.

- [ ] Add AGPL-3.0 license text and upstream notices.
- [ ] Document API use, native dependency installation/loading, platform support status, and unsupported RIDs.
- [ ] Keep platform support statements limited to tested combinations.

### Task 4: Verify the first deliverable

- [ ] Run the full managed test suite and build.
- [ ] Exercise a real image recognition smoke scenario if the native OpenALPR runtime is installed.
- [ ] Record exact tested RID and native dependency status; do not report unavailable platform smoke tests as passing.
- [ ] Ask for confirmation before adding a root `.gitignore`; repository currently has none and user-level rules require approval before creating one.
