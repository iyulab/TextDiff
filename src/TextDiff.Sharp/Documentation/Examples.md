# TextDiff.Sharp - Comprehensive Usage Examples

This document provides detailed examples for using TextDiff.Sharp in various scenarios.

## Table of Contents

1. [Basic Usage](#basic-usage)
2. [Asynchronous Processing](#asynchronous-processing)
3. [Memory-Optimized Processing](#memory-optimized-processing)
4. [Streaming for Large Files](#streaming-for-large-files)
5. [Custom Dependencies](#custom-dependencies)
6. [Error Handling](#error-handling)
7. [Progress Reporting](#progress-reporting)
8. [Real-World Scenarios](#real-world-scenarios)

## Basic Usage

### Simple Diff Application

```csharp
using TextDiff;
using TextDiff.Models;

// Original document content
string originalDocument = @"line 1
line 2
line 3
line 4";

// Unified diff content
string diffContent = @" line 1
-line 2
+line 2 modified
 line 3
 line 4";

// Create differ and process
var differ = new TextDiffer();
ProcessResult result = differ.Process(originalDocument, diffContent);

// Access results
Console.WriteLine("Updated document:");
Console.WriteLine(result.Text);

Console.WriteLine($"\nChanges applied:");
Console.WriteLine($"  Modified: {result.Changes.ChangedLines} lines");
Console.WriteLine($"  Added: {result.Changes.AddedLines} lines");
Console.WriteLine($"  Deleted: {result.Changes.DeletedLines} lines");
```

### Working with File Content

```csharp
// Read original file
string originalContent = File.ReadAllText("document.txt");

// Read diff file
string diffContent = File.ReadAllText("changes.diff");

// Process diff
var differ = new TextDiffer();
var result = differ.Process(originalContent, diffContent);

// Write result to new file
File.WriteAllText("updated_document.txt", result.Text);

Console.WriteLine($"Applied {result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines} total changes");
```

## Asynchronous Processing

### Basic Async Processing

```csharp
using TextDiff;
using TextDiff.Models;

var differ = new TextDiffer();

// Async processing with cancellation support
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var result = await differ.ProcessAsync(
        originalDocument,
        diffContent,
        cts.Token
    );

    Console.WriteLine($"Processing completed. Result has {result.Text.Length} characters.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Processing was cancelled due to timeout.");
}
```

### Progress Reporting

```csharp
var differ = new TextDiffer();

// Create progress reporter
var progress = new Progress<ProcessingProgress>(progressInfo =>
{
    Console.WriteLine($"[{progressInfo.Stage}] {progressInfo.PercentComplete:F1}% " +
                     $"({progressInfo.ProcessedItems}/{progressInfo.TotalItems})");
});

// Process with progress reporting
var result = await differ.ProcessAsync(
    largeDocument,
    complexDiff,
    CancellationToken.None,
    progress
);

Console.WriteLine("Processing completed!");
```

### Advanced Progress Handling

```csharp
var progressReports = new List<ProcessingProgress>();
var progress = new Progress<ProcessingProgress>(p =>
{
    progressReports.Add(p);

    // Update UI progress bar
    UpdateProgressBar(p.PercentComplete);

    // Log detailed progress
    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {p.Stage}: {p.PercentComplete:F1}%");
});

var result = await differ.ProcessAsync(document, diff, CancellationToken.None, progress);

// Analyze progress patterns
Console.WriteLine($"Processing took {progressReports.Count} progress updates");
var stages = progressReports.Select(p => p.Stage).Distinct();
Console.WriteLine($"Stages: {string.Join(" → ", stages)}");
```

## Memory-Optimized Processing

### High-Performance Processing

```csharp
var differ = new TextDiffer();

// Process with memory optimizations
var result = differ.ProcessOptimized(
    largeDocument,
    diffContent,
    bufferSizeHint: 16384  // 16KB buffer hint
);

Console.WriteLine($"Optimized processing completed with {result.Changes.ChangedLines} changes");
```

### Memory Usage Estimation

```csharp
using TextDiff.Helpers;

// Estimate memory requirements before processing
string[] documentLines = MemoryEfficientTextUtils.SplitLinesEfficient(document);
int averageLineLength = documentLines.Length > 0
    ? (int)(document.Length / documentLines.Length)
    : 0;

long estimatedMemory = MemoryEfficientTextUtils.EstimateMemoryUsage(
    documentLines.Length,
    averageLineLength,
    estimatedDiffBlocks: 10
);

Console.WriteLine($"Estimated memory usage: {estimatedMemory / 1024 / 1024} MB");

// Process if memory requirements are acceptable
if (estimatedMemory < 500 * 1024 * 1024) // Less than 500MB
{
    var result = differ.ProcessOptimized(document, diffContent);
}
else
{
    Console.WriteLine("Document too large, consider streaming approach");
}
```

## Streaming for Large Files

### Stream-to-Stream Processing

```csharp
var differ = new TextDiffer();

using var documentStream = new FileStream("large_document.txt", FileMode.Open, FileAccess.Read);
using var diffStream = new FileStream("changes.diff", FileMode.Open, FileAccess.Read);
using var outputStream = new FileStream("updated_document.txt", FileMode.Create, FileAccess.Write);

var result = await differ.ProcessStreamsAsync(
    documentStream,
    diffStream,
    outputStream
);

Console.WriteLine($"Streaming processing completed. Changes: {result.Changes.ChangedLines}");
```

### Streaming with Progress and Cancellation

```csharp
var progress = new Progress<ProcessingProgress>(p =>
    Console.WriteLine($"Streaming: {p.Stage} - {p.PercentComplete:F1}%"));

using var cts = new CancellationTokenSource();

try
{
    var result = await differ.ProcessStreamsAsync(
        documentStream,
        diffStream,
        outputStream,
        cts.Token,
        progress
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Streaming operation was cancelled");
    // Cleanup partial output if needed
    if (File.Exists("updated_document.txt"))
    {
        File.Delete("updated_document.txt");
    }
}
```

## Custom Dependencies

### Custom Diff Block Parser

```csharp
public class EnhancedDiffBlockParser : IDiffBlockParser
{
    public IEnumerable<DiffBlock> Parse(string[] diffLines)
    {
        // Custom parsing logic for enhanced diff formats
        // Implementation details...
        yield return new DiffBlock();
    }
}

// Use custom parser
var customParser = new EnhancedDiffBlockParser();
var differ = new TextDiffer(blockParser: customParser);
```

### Custom Context Matcher

```csharp
public class FuzzyContextMatcher : IContextMatcher
{
    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        // Implement fuzzy matching logic
        // Handle minor variations in context
        return -1; // Return position or -1 if not found
    }
}

// Use fuzzy matching
var fuzzyMatcher = new FuzzyContextMatcher();
var differ = new TextDiffer(contextMatcher: fuzzyMatcher);
```

### Custom Change Tracker

```csharp
public class DetailedChangeTracker : IChangeTracker
{
    private readonly List<string> _changeLog = new();

    public void TrackChanges(DiffBlock block, ChangeStats stats)
    {
        // Standard tracking
        stats.AddedLines += block.Additions.Count;
        stats.DeletedLines += block.Removals.Count;

        // Enhanced tracking with logging
        _changeLog.Add($"Block: +{block.Additions.Count} -{block.Removals.Count}");
    }

    public IReadOnlyList<string> GetChangeLog() => _changeLog.AsReadOnly();
}

var detailedTracker = new DetailedChangeTracker();
var differ = new TextDiffer(changeTracker: detailedTracker);
```

## Error Handling

### Comprehensive Error Handling

```csharp
using TextDiff.Exceptions;

var differ = new TextDiffer();

try
{
    var result = differ.Process(originalDocument, diffContent);
    Console.WriteLine("Processing successful!");
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Input validation failed: {ex.Message}");
}
catch (InvalidDiffFormatException ex)
{
    Console.WriteLine($"Diff format error at line {ex.LineNumber}: {ex.Message}");
}
catch (DiffApplicationException ex)
{
    Console.WriteLine($"Failed to apply diff: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Underlying cause: {ex.InnerException.Message}");
    }
}
catch (TextDiffException ex)
{
    Console.WriteLine($"TextDiff error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Async Error Handling

```csharp
try
{
    var result = await differ.ProcessAsync(document, diff, cancellationToken);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
catch (InvalidDiffFormatException ex)
{
    Console.WriteLine($"Invalid diff format: {ex.Message}");
    // Handle specific diff format issues
}
catch (Exception ex)
{
    Console.WriteLine($"Processing failed: {ex.Message}");
    // Log for debugging
    LogError(ex);
}
```

## Real-World Scenarios

### Code Review Application

```csharp
public class CodeReviewProcessor
{
    private readonly TextDiffer _differ = new();

    public async Task<ReviewResult> ProcessCodeReview(
        string originalCode,
        string reviewDiff,
        CancellationToken cancellationToken)
    {
        var progress = new Progress<ProcessingProgress>(LogProgress);

        try
        {
            var result = await _differ.ProcessAsync(
                originalCode,
                reviewDiff,
                cancellationToken,
                progress
            );

            return new ReviewResult
            {
                UpdatedCode = result.Text,
                Statistics = result.Changes,
                Status = ReviewStatus.Applied
            };
        }
        catch (InvalidDiffFormatException)
        {
            return new ReviewResult { Status = ReviewStatus.InvalidDiff };
        }
        catch (DiffApplicationException)
        {
            return new ReviewResult { Status = ReviewStatus.ConflictDetected };
        }
    }

    private void LogProgress(ProcessingProgress progress)
    {
        // Log to application logging system
    }
}
```

### Document Version Management

```csharp
public class DocumentVersionManager
{
    private readonly TextDiffer _differ = new();

    public async Task<string> ApplyVersion(
        string baseDocument,
        IEnumerable<string> versionDiffs)
    {
        string currentDocument = baseDocument;

        foreach (string diff in versionDiffs)
        {
            var result = await _differ.ProcessAsync(currentDocument, diff);
            currentDocument = result.Text;

            Console.WriteLine($"Applied version: {result.Changes.ChangedLines} changes");
        }

        return currentDocument;
    }

    public ProcessResult GetVersionStatistics(string baseDocument, string versionDiff)
    {
        return _differ.Process(baseDocument, versionDiff);
    }
}
```

### Batch Processing

```csharp
public class BatchDiffProcessor
{
    private readonly TextDiffer _differ = new();

    public async Task ProcessBatch(IEnumerable<(string Document, string Diff)> items)
    {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await _differ.ProcessAsync(item.Document, item.Diff);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        Console.WriteLine($"Batch completed: {results.Length} items processed");
        var totalChanges = results.Sum(r => r.Changes.AddedLines + r.Changes.DeletedLines + r.Changes.ChangedLines);
        Console.WriteLine($"Total changes applied: {totalChanges}");
    }
}
```

### Performance Monitoring

```csharp
public class PerformanceMonitor
{
    public async Task<PerformanceMetrics> MeasureProcessing(
        string document,
        string diff)
    {
        var stopwatch = Stopwatch.StartNew();
        long memoryBefore = GC.GetTotalMemory(true);

        var differ = new TextDiffer();
        var result = await differ.ProcessAsync(document, diff);

        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);

        return new PerformanceMetrics
        {
            ProcessingTime = stopwatch.Elapsed,
            MemoryUsed = memoryAfter - memoryBefore,
            LinesProcessed = document.Split('\n').Length,
            ChangesApplied = result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines
        };
    }
}

public class PerformanceMetrics
{
    public TimeSpan ProcessingTime { get; set; }
    public long MemoryUsed { get; set; }
    public int LinesProcessed { get; set; }
    public int ChangesApplied { get; set; }

    public double LinesPerSecond => LinesProcessed / ProcessingTime.TotalSeconds;
    public string MemoryUsedMB => $"{MemoryUsed / 1024.0 / 1024.0:F2} MB";
}
```

## Best Practices

### 1. Choose the Right Processing Method

```csharp
// For small to medium files (< 10MB)
var result = differ.Process(document, diff);

// For large files with time constraints
var result = await differ.ProcessAsync(document, diff, cancellationToken, progress);

// For memory-constrained environments
var result = differ.ProcessOptimized(document, diff, bufferSize: 8192);

// For very large files (> 100MB)
var result = await differ.ProcessStreamsAsync(docStream, diffStream, outStream);
```

### 2. Memory Management

```csharp
// Estimate memory before processing
long estimatedMemory = MemoryEfficientTextUtils.EstimateMemoryUsage(
    lineCount, averageLineLength, blockCount);

if (estimatedMemory > availableMemory)
{
    // Use streaming approach
    await ProcessWithStreaming();
}
```

### 3. Error Recovery

```csharp
public async Task<ProcessResult?> TryProcessWithFallback(string doc, string diff)
{
    try
    {
        // Try optimized processing first
        return differ.ProcessOptimized(doc, diff);
    }
    catch (Exception)
    {
        try
        {
            // Fallback to standard processing
            return differ.Process(doc, diff);
        }
        catch (Exception)
        {
            // Log error and return null
            return null;
        }
    }
}
```

This comprehensive examples documentation demonstrates the full capabilities of TextDiff.Sharp across various use cases and scenarios.