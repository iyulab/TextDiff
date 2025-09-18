# TextDiff.Sharp Troubleshooting Guide

This guide helps you diagnose and resolve common issues when using TextDiff.Sharp.

## Common Issues and Solutions

### 1. Invalid Diff Format Exceptions

#### Problem: `InvalidDiffFormatException` thrown during processing

**Symptoms:**
```
InvalidDiffFormatException: Invalid diff line format at line 5: 'unexpected content'
```

**Common Causes:**
- Non-unified diff format
- Missing line prefixes (+, -, space)
- Corrupted or incomplete diff content
- Mixed diff formats

**Solutions:**

1. **Validate Diff Format**
```csharp
public bool IsValidUnifiedDiff(string diff)
{
    var lines = diff.Split('\n');

    foreach (var line in lines.Where(l => !string.IsNullOrEmpty(l)))
    {
        char prefix = line[0];

        // Valid prefixes for unified diff
        if (prefix != '+' && prefix != '-' && prefix != ' ' &&
            prefix != '@' && prefix != '\\' &&
            !line.StartsWith("---") && !line.StartsWith("+++") &&
            !line.StartsWith("diff ") && !line.StartsWith("index "))
        {
            return false;
        }
    }

    return true;
}
```

2. **Clean Diff Content**
```csharp
public string CleanDiffContent(string diff)
{
    var lines = diff.Split('\n');
    var cleanedLines = new List<string>();

    foreach (var line in lines)
    {
        // Skip common non-diff lines
        if (line.StartsWith("diff --git") ||
            line.StartsWith("index ") ||
            line.StartsWith("@@") ||
            line.StartsWith("---") ||
            line.StartsWith("+++"))
        {
            continue;
        }

        // Only include lines with valid prefixes
        if (line.Length > 0 && (line[0] == '+' || line[0] == '-' || line[0] == ' '))
        {
            cleanedLines.Add(line);
        }
    }

    return string.Join('\n', cleanedLines);
}
```

3. **Handle Git Diff Format**
```csharp
public string ConvertGitDiffToUnified(string gitDiff)
{
    var lines = gitDiff.Split('\n');
    var unifiedLines = new List<string>();
    bool inHunk = false;

    foreach (var line in lines)
    {
        if (line.StartsWith("@@"))
        {
            inHunk = true;
            continue;
        }

        if (inHunk && line.Length > 0)
        {
            char prefix = line[0];
            if (prefix == '+' || prefix == '-' || prefix == ' ')
            {
                unifiedLines.Add(line);
            }
        }
    }

    return string.Join('\n', unifiedLines);
}
```

### 2. Diff Application Failures

#### Problem: `DiffApplicationException` - Context not found

**Symptoms:**
```
DiffApplicationException: Cannot find matching context for diff block at line 15
```

**Common Causes:**
- Document has been modified since diff was created
- Line ending differences (CRLF vs LF)
- Whitespace variations
- Incorrect context lines in diff

**Solutions:**

1. **Normalize Line Endings**
```csharp
public string NormalizeLineEndings(string text)
{
    return text.Replace("\r\n", "\n").Replace("\r", "\n");
}

public ProcessResult ProcessWithNormalizedLineEndings(string document, string diff)
{
    var normalizedDoc = NormalizeLineEndings(document);
    var normalizedDiff = NormalizeLineEndings(diff);

    var differ = new TextDiffer();
    return differ.Process(normalizedDoc, normalizedDiff);
}
```

2. **Flexible Context Matching**
```csharp
public class LenientContextMatcher : IContextMatcher
{
    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        // Try exact match first
        for (int i = startPosition; i <= documentLines.Length - block.BeforeContext.Count; i++)
        {
            if (MatchesExactly(documentLines, i, block.BeforeContext))
                return i;
        }

        // Try fuzzy match with normalized whitespace
        for (int i = startPosition; i <= documentLines.Length - block.BeforeContext.Count; i++)
        {
            if (MatchesFuzzy(documentLines, i, block.BeforeContext))
                return i;
        }

        return -1; // Not found
    }

    private bool MatchesFuzzy(string[] documentLines, int position, List<string> contextLines)
    {
        for (int i = 0; i < contextLines.Count; i++)
        {
            var docLine = documentLines[position + i].Trim();
            var contextLine = contextLines[i].Trim();

            if (!string.Equals(docLine, contextLine, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
```

3. **Retry with Different Strategies**
```csharp
public async Task<ProcessResult> ProcessWithRetryStrategies(string document, string diff)
{
    var strategies = new List<Func<Task<ProcessResult>>>
    {
        // Strategy 1: Standard processing
        () => Task.FromResult(new TextDiffer().Process(document, diff)),

        // Strategy 2: Normalized line endings
        () => Task.FromResult(ProcessWithNormalizedLineEndings(document, diff)),

        // Strategy 3: Lenient context matching
        () => Task.FromResult(new TextDiffer(contextMatcher: new LenientContextMatcher())
            .Process(document, diff)),

        // Strategy 4: Async with timeout
        () => new TextDiffer().ProcessAsync(document, diff)
    };

    foreach (var strategy in strategies)
    {
        try
        {
            return await strategy();
        }
        catch (DiffApplicationException)
        {
            // Try next strategy
            continue;
        }
    }

    throw new DiffApplicationException("All retry strategies failed");
}
```

### 3. Performance Issues

#### Problem: Slow processing or high memory usage

**Symptoms:**
- Processing takes longer than expected
- High memory consumption
- Application becomes unresponsive

**Diagnosis:**

1. **Measure Performance**
```csharp
public class PerformanceDiagnostics
{
    public ProcessingMetrics DiagnosePerformance(string document, string diff)
    {
        var metrics = new ProcessingMetrics();

        // Measure document characteristics
        metrics.DocumentSize = Encoding.UTF8.GetByteCount(document);
        metrics.DocumentLines = document.Split('\n').Length;
        metrics.DiffSize = Encoding.UTF8.GetByteCount(diff);

        // Measure processing performance
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(true);

        var differ = new TextDiffer();
        var result = differ.Process(document, diff);

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(false);

        metrics.ProcessingTime = stopwatch.ElapsedMilliseconds;
        metrics.MemoryUsed = memoryAfter - memoryBefore;
        metrics.ChangesApplied = result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines;

        return metrics;
    }
}

public class ProcessingMetrics
{
    public long DocumentSize { get; set; }
    public int DocumentLines { get; set; }
    public long DiffSize { get; set; }
    public long ProcessingTime { get; set; }
    public long MemoryUsed { get; set; }
    public int ChangesApplied { get; set; }

    public string GetRecommendation()
    {
        if (DocumentSize > 100 * 1024 * 1024) // > 100MB
            return "Use ProcessStreamsAsync for large files";

        if (ProcessingTime > 5000) // > 5 seconds
            return "Use ProcessOptimized or ProcessAsync";

        if (MemoryUsed > 500 * 1024 * 1024) // > 500MB
            return "Consider streaming approach";

        return "Performance is acceptable";
    }
}
```

**Solutions:**

1. **Choose Optimal Processing Method**
```csharp
public async Task<ProcessResult> ProcessOptimally(string document, string diff)
{
    var diagnostics = new PerformanceDiagnostics();
    var metrics = diagnostics.DiagnosePerformance(document, diff);

    var differ = new TextDiffer();

    return metrics.DocumentSize switch
    {
        < 1_048_576 => differ.Process(document, diff), // < 1MB
        < 10_485_760 => differ.ProcessOptimized(document, diff), // < 10MB
        < 104_857_600 => await differ.ProcessAsync(document, diff), // < 100MB
        _ => await ProcessWithStreaming(document, diff) // >= 100MB
    };
}
```

2. **Memory-Optimized Processing**
```csharp
public ProcessResult ProcessWithMemoryOptimization(string document, string diff)
{
    // Use optimized buffer size
    var optimalBufferSize = CalculateOptimalBufferSize(document.Length);

    var differ = new TextDiffer();
    return differ.ProcessOptimized(document, diff, optimalBufferSize);
}

private int CalculateOptimalBufferSize(int documentLength)
{
    return documentLength switch
    {
        < 1_024 => 512,
        < 10_240 => 2048,
        < 102_400 => 8192,
        _ => 16384
    };
}
```

### 4. Async Operation Issues

#### Problem: Deadlocks or task continuation issues

**Symptoms:**
- Application hangs during async processing
- `ProcessAsync` never completes
- Timeout exceptions

**Solutions:**

1. **Proper Async/Await Usage**
```csharp
// ❌ Incorrect - can cause deadlocks
public ProcessResult ProcessSync(string document, string diff)
{
    var differ = new TextDiffer();
    return differ.ProcessAsync(document, diff).Result; // Don't do this!
}

// ✅ Correct - proper async pattern
public async Task<ProcessResult> ProcessAsync(string document, string diff)
{
    var differ = new TextDiffer();
    return await differ.ProcessAsync(document, diff).ConfigureAwait(false);
}
```

2. **Timeout and Cancellation**
```csharp
public async Task<ProcessResult> ProcessWithTimeout(string document, string diff, TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    var differ = new TextDiffer();

    try
    {
        return await differ.ProcessAsync(document, diff, cts.Token);
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException($"Processing timed out after {timeout.TotalSeconds} seconds");
    }
}
```

### 5. Memory Leaks

#### Problem: Application memory usage grows over time

**Symptoms:**
- Increasing memory usage with multiple operations
- OutOfMemoryException after processing many files
- Garbage collection pressure

**Solutions:**

1. **Proper Disposal**
```csharp
public async Task ProcessMultipleFiles(IEnumerable<string> filePaths)
{
    var differ = new TextDiffer(); // Reuse instance

    foreach (var filePath in filePaths)
    {
        // Process each file
        var document = await File.ReadAllTextAsync(filePath);
        var diffPath = filePath + ".diff";

        if (File.Exists(diffPath))
        {
            var diff = await File.ReadAllTextAsync(diffPath);
            var result = await differ.ProcessAsync(document, diff);

            // Save result
            await File.WriteAllTextAsync(filePath + ".updated", result.Text);
        }

        // Force garbage collection for large operations
        if (document.Length > 10 * 1024 * 1024) // > 10MB
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
```

2. **Streaming for Large Files**
```csharp
public async Task ProcessLargeFilesSequence(IEnumerable<(string docPath, string diffPath, string outputPath)> files)
{
    var differ = new TextDiffer();

    foreach (var (docPath, diffPath, outputPath) in files)
    {
        using var docStream = File.OpenRead(docPath);
        using var diffStream = File.OpenRead(diffPath);
        using var outputStream = File.Create(outputPath);

        await differ.ProcessStreamsAsync(docStream, diffStream, outputStream);

        // Streams are automatically disposed here
    }
}
```

### 6. Thread Safety Issues

#### Problem: Concurrent access errors

**Symptoms:**
- Inconsistent results in multi-threaded scenarios
- Sporadic exceptions in concurrent operations
- Data corruption

**Solutions:**

1. **Use Separate Instances**
```csharp
// ❌ Incorrect - sharing instance across threads
private static readonly TextDiffer _sharedDiffer = new TextDiffer();

public Task<ProcessResult> ProcessConcurrently(string document, string diff)
{
    return Task.Run(() => _sharedDiffer.Process(document, diff)); // Not thread-safe!
}

// ✅ Correct - separate instances per operation
public Task<ProcessResult> ProcessConcurrentlySafe(string document, string diff)
{
    return Task.Run(() =>
    {
        var differ = new TextDiffer();
        return differ.Process(document, diff);
    });
}
```

2. **Thread-Safe Batch Processing**
```csharp
public async Task<IEnumerable<ProcessResult>> ProcessBatchSafely(
    IEnumerable<(string document, string diff)> items,
    int maxConcurrency = Environment.ProcessorCount)
{
    using var semaphore = new SemaphoreSlim(maxConcurrency);

    var tasks = items.Select(async item =>
    {
        await semaphore.WaitAsync();
        try
        {
            var differ = new TextDiffer(); // Instance per task
            return await differ.ProcessAsync(item.document, item.diff);
        }
        finally
        {
            semaphore.Release();
        }
    });

    return await Task.WhenAll(tasks);
}
```

## Debugging Techniques

### 1. Enable Detailed Logging

```csharp
public class DiagnosticDiffProcessor
{
    private readonly ILogger _logger;

    public async Task<ProcessResult> ProcessWithDiagnostics(string document, string diff)
    {
        _logger.LogInformation("Starting diff processing");
        _logger.LogDebug("Document size: {Size} bytes, lines: {Lines}",
            document.Length, document.Split('\n').Length);
        _logger.LogDebug("Diff size: {Size} bytes", diff.Length);

        try
        {
            var differ = new TextDiffer();
            var stopwatch = Stopwatch.StartNew();

            var result = await differ.ProcessAsync(document, diff);

            stopwatch.Stop();
            _logger.LogInformation("Processing completed in {Time}ms with {Changes} changes",
                stopwatch.ElapsedMilliseconds,
                result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed");
            throw;
        }
    }
}
```

### 2. Validate Input Data

```csharp
public class ValidationHelper
{
    public ValidationResult ValidateInputs(string document, string diff)
    {
        var result = new ValidationResult();

        // Document validation
        if (string.IsNullOrEmpty(document))
        {
            result.Errors.Add("Document is null or empty");
        }

        if (document?.Contains('\0') == true)
        {
            result.Errors.Add("Document contains null characters");
        }

        // Diff validation
        if (string.IsNullOrEmpty(diff))
        {
            result.Errors.Add("Diff is null or empty");
        }

        var diffLines = diff?.Split('\n') ?? Array.Empty<string>();
        bool hasValidDiffLines = false;

        for (int i = 0; i < diffLines.Length; i++)
        {
            var line = diffLines[i];
            if (line.Length > 0)
            {
                char prefix = line[0];
                if (prefix == '+' || prefix == '-' || prefix == ' ')
                {
                    hasValidDiffLines = true;
                }
                else if (!IsValidDiffMetadata(line))
                {
                    result.Warnings.Add($"Suspicious diff line at {i + 1}: {line}");
                }
            }
        }

        if (!hasValidDiffLines)
        {
            result.Errors.Add("Diff contains no valid change lines");
        }

        return result;
    }

    private bool IsValidDiffMetadata(string line)
    {
        return line.StartsWith("@@") ||
               line.StartsWith("---") ||
               line.StartsWith("+++") ||
               line.StartsWith("diff ") ||
               line.StartsWith("index ") ||
               line.StartsWith("\\");
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;
}
```

### 3. Create Minimal Reproduction Cases

```csharp
public class ReproductionHelper
{
    public void CreateMinimalReproCase(string document, string diff, Exception exception)
    {
        var testCase = new
        {
            Document = TruncateForDebugging(document),
            Diff = TruncateForDebugging(diff),
            Exception = new
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            },
            Environment = new
            {
                Framework = RuntimeInformation.FrameworkDescription,
                OS = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture
            }
        };

        var json = JsonSerializer.Serialize(testCase, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText($"repro-case-{DateTime.Now:yyyyMMdd-HHmmss}.json", json);
    }

    private string TruncateForDebugging(string content, int maxLength = 1000)
    {
        return content.Length <= maxLength ? content : content.Substring(0, maxLength) + "...";
    }
}
```

## FAQ

### Q: Why is my diff processing slow?

**A:** Common causes include:
- Large document size (>10MB) - use `ProcessOptimized()` or `ProcessAsync()`
- Complex diff with many changes - consider batching
- Memory pressure - use streaming approach for very large files

### Q: How do I handle different line ending formats?

**A:** Normalize line endings before processing:
```csharp
document = document.Replace("\r\n", "\n").Replace("\r", "\n");
diff = diff.Replace("\r\n", "\n").Replace("\r", "\n");
```

### Q: Can I process multiple diffs concurrently?

**A:** Yes, but create separate `TextDiffer` instances for each thread:
```csharp
var tasks = diffs.Select(diff => Task.Run(() => new TextDiffer().Process(doc, diff)));
var results = await Task.WhenAll(tasks);
```

### Q: How do I handle very large files?

**A:** Use streaming processing:
```csharp
await differ.ProcessStreamsAsync(documentStream, diffStream, outputStream);
```

### Q: What if my diff format is not standard unified diff?

**A:** Convert to unified diff format first, or implement a custom `IDiffBlockParser`:
```csharp
var customParser = new MyDiffBlockParser();
var differ = new TextDiffer(blockParser: customParser);
```

## Getting Additional Help

If you're still experiencing issues:

1. **Check Examples**: Review the [examples project](../examples/) for similar scenarios
2. **API Documentation**: Consult the [complete API reference](../api/)
3. **GitHub Issues**: Search existing issues or create a new one
4. **Community Support**: Join community discussions for help from other users

When reporting issues, please include:
- TextDiff.Sharp version
- .NET framework version
- Minimal reproduction case
- Complete error messages and stack traces
- Environmental details (OS, architecture)