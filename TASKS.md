# TextDiff.Sharp Improvement Roadmap

## 🔍 Current State (v1.1.1 → v2.0.0 준비중)

### ✅ Completed (2025-11-29)

#### Framework Modernization
- [x] **Target Framework Update**: netstandard2.1 제거 → net8.0;net9.0;net10.0
- [x] **Dependencies Update**: 모든 NuGet 패키지 최신화
  - Microsoft.NET.Test.Sdk: 17.11.1 → 18.0.1
  - xunit: 2.9.2 → 2.9.3
  - xunit.runner.visualstudio: 2.8.2 → 3.1.5
  - coverlet.collector: 6.0.2 → 6.0.4
  - BenchmarkDotNet: 0.14.0 → 0.15.6

#### Critical Fixes
- [x] **Thread Safety**: ContextMatcher에 Reset() 메서드 추가, 매 처리 시 호출
- [x] **Namespace Fix**: DocumentProcessor에 namespace TextDiff.Core 추가
- [x] **Streaming Return Value**: ProcessStreamingAsync 반환값 수정 (string.Empty)
- [x] **Conditional Compilation**: netstandard2.1 조건부 컴파일 코드 제거

#### Code Quality
- [x] **Immutability**: ProcessResult 속성을 { get; } 으로 변경
- [x] **ChangeStats Enhancement**: TotalAffectedLines, NetLineChange 편의 속성 추가
- [x] **Comments Unification**: 모든 한국어 주석 → 영어로 통일
- [x] **IContextMatcher Enhancement**: Reset() 인터페이스 메서드 추가

### 📊 Technical Metrics
- **Source Files**: 19 C# files
- **Public API Surface**: 12 public types
- **Build Status**: ✅ 성공 (0 warnings, 0 errors)
- **Test Status**: ✅ 77/77 tests pass
- **Target Frameworks**: net8.0, net9.0, net10.0

---

## 📋 Remaining Roadmap

### 🔥 Phase 1: Quality & Reliability ✅ COMPLETED
- [x] Fix Nullable Reference Warnings
- [x] API Design Improvements (ProcessResult initialization)
- [x] Thread Safety (IContextMatcher.Reset())
- [x] Code Quality (주석 통일, 불변성)

### ⚡ Phase 2: Performance & Efficiency
**Status**: Partially Complete

- [x] Async Support (ProcessAsync, ProcessStreamsAsync)
- [x] CancellationToken support
- [x] Progress Reporting (IProgress<T>)
- [ ] Memory Profiling and optimization
- [ ] Performance regression testing

### 📚 Phase 3: Documentation & Developer Experience
**Status**: In Progress

- [x] Complete XML docs for public members
- [x] Code examples in README
- [ ] Auto-generated API documentation
- [ ] Migration guide for v2.0.0

### 🔬 Phase 4: Advanced Features
**Status**: Not Started

- [ ] Context Size Configuration
- [ ] Binary Detection
- [ ] Encoding Support
- [ ] Fuzzy Matching

### 🚀 Phase 5: Production Deployment
**Status**: Not Started

- [ ] GitHub Actions CI/CD
- [ ] Automated NuGet publishing
- [ ] Code coverage >95%

---

## 🎯 Next Release: v2.0.0

### Breaking Changes
- Removed .NET Standard 2.1 support
- IContextMatcher now requires Reset() method implementation
- ProcessResult.Text is now read-only (no setter)

### New Features
- .NET 10.0 support
- Thread-safe diff processing
- Enhanced ChangeStats with convenience properties

### Upgrade Guide
1. Update target framework to net8.0 or higher
2. If using custom IContextMatcher, implement Reset() method
3. Remove any code relying on ProcessResult.Text setter
