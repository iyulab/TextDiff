# Changelog

All notable changes to TextDiff.Sharp are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.4.0] — 2026-05-14

### Added
- `StreamProcessResult` record type — returned by `ProcessStreamsAsync` and `StreamingDiffProcessor.ProcessStreamingAsync`. Provides `Changes` statistics without a misleading `.Text` field that was always empty.

### Changed
- `TextDiffer.ProcessStreamsAsync` now returns `Task<StreamProcessResult>` instead of `Task<ProcessResult>`. The old return type violated the type contract because `.Text` was always `string.Empty` for stream processing.
- `TextDiffer.IsDiffX(string)` now delegates to `DiffXReader.IsDiffX(string)` directly without allocating a `DiffXReader` instance.
- `TextDiffer.ProcessDiffX(...)` no longer allocates a `DiffXReader` instance solely for the `IsDiffX` check.

### Deprecated
- `TextDiffer.ProcessAsync` — marked `[Obsolete]`. This method wraps a CPU-bound operation with `Task.Run`, which violates Microsoft library design guidelines. Prefer `Process()` and offload to `Task.Run` at the call site. A true async pipeline will be introduced in a future major version.

### Removed
- `IDiffXReader.IsDiffX(string)` — removed from the interface. `IsDiffX` is a stateless format check (pure function with no instance state) and does not belong on an instance interface. Use `DiffXReader.IsDiffX(string)` directly.

### Fixed
- `DiffXReader.IsDiffX` is now `static`, eliminating unnecessary instance allocation at every call site.

---

## [1.3.0] — 2026-03

Unified diff comprehensive testing (TDD), Git Extended Headers, API Parity, CRLF/LF preservation, LLM diff support, DiffX Phase 1.

## [1.1.1] — 2025-11

.NET 8/9/10 multi-targeting, thread safety, streaming API, code quality improvements.
