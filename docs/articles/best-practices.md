# TextDiff.Sharp Best Practices

This guide provides production-ready patterns and optimization strategies for using TextDiff.Sharp effectively in your applications.

## Performance Optimization

### 1. Choose the Right Processing Method

Select processing methods based on document characteristics:

```csharp
public async Task<ProcessResult> ProcessOptimally(string document, string diff)
{
    var documentSize = Encoding.UTF8.GetByteCount(document);
    var differ = new TextDiffer();

    switch (documentSize)
    {
        case < 1_024_000: // < 1MB
            return differ.Process(document, diff);

        case < 10_485_760: // < 10MB
            return differ.ProcessOptimized(document, diff);

        case < 104_857_600: // < 100MB
            return await differ.ProcessAsync(document, diff);

        default: // > 100MB
            return await ProcessWithStreaming(document, diff);
    }
}
```

### 2. Memory Management

#### Estimate Memory Requirements

```csharp
using TextDiff.Helpers;

public bool CanProcessInMemory(string document, int availableMemoryMB)
{
    var lines = MemoryEfficientTextUtils.SplitLinesEfficient(document);
    var avgLineLength = document.Length / Math.Max(lines.Length, 1);

    var estimatedMemory = MemoryEfficientTextUtils.EstimateMemoryUsage(
        lines.Length,
        avgLineLength,
        estimatedBlocks: 20
    );

    return estimatedMemory < (availableMemoryMB * 1024 * 1024 * 0.8); // 80% threshold
}
```

#### Use Streaming for Large Files

```csharp
public async Task<ProcessResult> ProcessLargeFile(
    string documentPath,
    string diffPath,
    string outputPath)
{
    var differ = new TextDiffer();

    using var docStream = File.OpenRead(documentPath);
    using var diffStream = File.OpenRead(diffPath);
    using var outputStream = File.Create(outputPath);

    return await differ.ProcessStreamsAsync(docStream, diffStream, outputStream);
}
```

### 3. Asynchronous Processing Patterns

#### Responsive UI Applications

```csharp
public async Task ProcessWithProgress(string document, string diff, IProgress<string> uiProgress)
{
    var differ = new TextDiffer();

    var progress = new Progress<ProcessingProgress>(p =>
    {
        uiProgress?.Report($"{p.Stage}: {p.PercentComplete:F0}%");
    });

    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

    try
    {
        var result = await differ.ProcessAsync(document, diff, cts.Token, progress);
        uiProgress?.Report("✓ Completed successfully");
    }
    catch (OperationCanceledException)
    {
        uiProgress?.Report("❌ Operation cancelled");
    }
}
```

#### Batch Processing

```csharp
public async Task<IEnumerable<ProcessResult>> ProcessBatch(
    IEnumerable<(string Document, string Diff)> items,
    int maxConcurrency = 4)
{
    var semaphore = new SemaphoreSlim(maxConcurrency);
    var differ = new TextDiffer();

    var tasks = items.Select(async item =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await differ.ProcessAsync(item.Document, item.Diff);
        }
        finally
        {
            semaphore.Release();
        }
    });

    return await Task.WhenAll(tasks);
}
```

## Error Handling Strategies

### 1. Defensive Processing

```csharp
public class RobustDiffProcessor
{
    private readonly TextDiffer _differ = new();
    private readonly ILogger _logger;

    public async Task<ProcessResult?> ProcessSafely(string document, string diff)
    {
        // Input validation
        if (!ValidateInputs(document, diff))
            return null;

        try
        {
            return await _differ.ProcessAsync(document, diff);
        }
        catch (InvalidDiffFormatException ex)
        {
            _logger.LogError("Invalid diff format at line {LineNumber}: {Message}",
                ex.LineNumber, ex.Message);
            return null;
        }
        catch (DiffApplicationException ex)
        {
            _logger.LogError("Failed to apply diff: {Message}", ex.Message);

            // Attempt recovery with relaxed matching
            return await AttemptRecovery(document, diff);
        }
    }

    private bool ValidateInputs(string document, string diff)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            _logger.LogWarning("Document is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(diff))
        {
            _logger.LogWarning("Diff is null or empty");
            return false;
        }

        return true;
    }
}
```

### 2. Graceful Degradation

```csharp
public async Task<ProcessResult> ProcessWithFallback(string document, string diff)
{
    var differ = new TextDiffer();

    // Strategy 1: Try optimized processing
    try
    {
        return differ.ProcessOptimized(document, diff);
    }
    catch (Exception ex) when (!(ex is ArgumentNullException))
    {
        _logger.LogWarning("Optimized processing failed, trying standard: {Error}", ex.Message);
    }

    // Strategy 2: Try standard processing
    try
    {
        return differ.Process(document, diff);
    }
    catch (Exception ex) when (!(ex is ArgumentNullException))
    {
        _logger.LogWarning("Standard processing failed, trying async: {Error}", ex.Message);
    }

    // Strategy 3: Try async processing (last resort)
    return await differ.ProcessAsync(document, diff);
}
```

## Production Deployment

### 1. Configuration Management

```csharp
public class DiffProcessorConfig
{
    public int MaxDocumentSizeMB { get; set; } = 100;
    public int ProcessingTimeoutMinutes { get; set; } = 10;
    public int MaxConcurrentOperations { get; set; } = 4;
    public bool EnableProgressReporting { get; set; } = true;
    public string TempDirectory { get; set; } = Path.GetTempPath();
}

public class ConfiguredDiffProcessor
{
    private readonly DiffProcessorConfig _config;
    private readonly TextDiffer _differ = new();

    public ConfiguredDiffProcessor(DiffProcessorConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<ProcessResult> Process(string document, string diff)
    {
        // Validate size limits
        var sizeBytes = Encoding.UTF8.GetByteCount(document);
        if (sizeBytes > _config.MaxDocumentSizeMB * 1024 * 1024)
        {
            throw new ArgumentException($"Document size ({sizeBytes / 1024 / 1024}MB) exceeds limit ({_config.MaxDocumentSizeMB}MB)");
        }

        // Configure timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_config.ProcessingTimeoutMinutes));

        // Configure progress reporting
        IProgress<ProcessingProgress>? progress = null;
        if (_config.EnableProgressReporting)
        {
            progress = new Progress<ProcessingProgress>(LogProgress);
        }

        return await _differ.ProcessAsync(document, diff, cts.Token, progress);
    }
}
```

### 2. Monitoring and Telemetry

```csharp
public class InstrumentedDiffProcessor
{
    private readonly TextDiffer _differ = new();
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;

    public async Task<ProcessResult> ProcessWithTelemetry(string document, string diff)
    {
        var stopwatch = Stopwatch.StartNew();
        var documentSize = Encoding.UTF8.GetByteCount(document);

        _metrics.Increment("diff_processing.started");
        _metrics.Histogram("diff_processing.document_size", documentSize);

        try
        {
            var result = await _differ.ProcessAsync(document, diff);

            stopwatch.Stop();
            _metrics.Histogram("diff_processing.duration_ms", stopwatch.ElapsedMilliseconds);
            _metrics.Histogram("diff_processing.changes",
                result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines);
            _metrics.Increment("diff_processing.success");

            _logger.LogInformation("Processed diff: {DocumentSize} bytes, {Changes} changes, {Duration}ms",
                documentSize,
                result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _metrics.Increment("diff_processing.error", ("error_type", ex.GetType().Name));
            _logger.LogError(ex, "Diff processing failed for document of size {DocumentSize}", documentSize);
            throw;
        }
    }
}
```

## Security Considerations

### 1. Input Validation

```csharp
public class SecureDiffProcessor
{
    private const int MaxDocumentSize = 50 * 1024 * 1024; // 50MB
    private const int MaxDiffSize = 10 * 1024 * 1024;     // 10MB
    private const int MaxLineLength = 10000;              // 10K chars per line

    public ProcessResult ProcessSecurely(string document, string diff)
    {
        ValidateInput(nameof(document), document, MaxDocumentSize);
        ValidateInput(nameof(diff), diff, MaxDiffSize);
        ValidateLineLength(document);

        var differ = new TextDiffer();
        return differ.Process(document, diff);
    }

    private void ValidateInput(string paramName, string content, int maxSize)
    {
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content cannot be null or empty", paramName);

        var size = Encoding.UTF8.GetByteCount(content);
        if (size > maxSize)
            throw new ArgumentException($"Content size ({size}) exceeds maximum ({maxSize})", paramName);
    }

    private void ValidateLineLength(string content)
    {
        var lines = content.Split('\n');
        var longLine = lines.FirstOrDefault(line => line.Length > MaxLineLength);
        if (longLine != null)
        {
            throw new ArgumentException($"Line length ({longLine.Length}) exceeds maximum ({MaxLineLength})");
        }
    }
}
```

### 2. Sandboxed Processing

```csharp
public class SandboxedProcessor
{
    public async Task<ProcessResult> ProcessInSandbox(string document, string diff)
    {
        // Use temporary directory for any file operations
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Process with limited resources
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            var differ = new TextDiffer();
            return await differ.ProcessAsync(document, diff, cts.Token);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
```

## Testing Strategies

### 1. Unit Testing Patterns

```csharp
[TestClass]
public class DiffProcessorTests
{
    private TextDiffer _differ;

    [TestInitialize]
    public void Setup()
    {
        _differ = new TextDiffer();
    }

    [TestMethod]
    public void Process_SimpleAddition_AppliesCorrectly()
    {
        // Arrange
        var document = "line 1\nline 2\nline 3";
        var diff = " line 1\n line 2\n+line 2.5\n line 3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.AreEqual(1, result.Changes.AddedLines);
        Assert.AreEqual(0, result.Changes.DeletedLines);
        Assert.IsTrue(result.Text.Contains("line 2.5"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDiffFormatException))]
    public void Process_InvalidDiff_ThrowsCorrectException()
    {
        // Arrange
        var document = "content";
        var diff = "invalid diff format";

        // Act
        _differ.Process(document, diff);
    }
}
```

### 2. Integration Testing

```csharp
[TestClass]
public class DiffProcessorIntegrationTests
{
    [TestMethod]
    public async Task ProcessAsync_LargeDocument_CompletesWithinTimeout()
    {
        // Arrange
        var largeDocument = GenerateLargeDocument(10000);
        var diff = GenerateTestDiff();
        var differ = new TextDiffer();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        var result = await differ.ProcessAsync(largeDocument, diff, cts.Token);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Changes.AddedLines > 0 || result.Changes.DeletedLines > 0);
    }

    private string GenerateLargeDocument(int lines)
    {
        return string.Join("\n",
            Enumerable.Range(1, lines).Select(i => $"Line {i} content"));
    }
}
```

## Troubleshooting Guidelines

### 1. Performance Issues

**Symptom**: Slow processing
```csharp
// Diagnosis
var stopwatch = Stopwatch.StartNew();
var result = differ.Process(document, diff);
stopwatch.Stop();

if (stopwatch.ElapsedMilliseconds > 5000) // > 5 seconds
{
    // Consider optimization strategies
    Console.WriteLine($"Slow processing detected: {stopwatch.ElapsedMilliseconds}ms");

    // Try optimized processing
    var optimizedResult = differ.ProcessOptimized(document, diff);
}
```

**Solutions**:
- Use `ProcessOptimized()` for large documents
- Switch to `ProcessAsync()` for better responsiveness
- Consider `ProcessStreamsAsync()` for very large files

### 2. Memory Issues

**Symptom**: High memory usage
```csharp
// Monitor memory usage
var memoryBefore = GC.GetTotalMemory(true);
var result = differ.Process(document, diff);
var memoryAfter = GC.GetTotalMemory(false);
var memoryUsed = memoryAfter - memoryBefore;

if (memoryUsed > 100 * 1024 * 1024) // > 100MB
{
    Console.WriteLine($"High memory usage: {memoryUsed / 1024 / 1024}MB");
    // Switch to streaming approach
}
```

### 3. Error Recovery

```csharp
public async Task<ProcessResult?> ProcessWithRecovery(string document, string diff)
{
    try
    {
        return await _differ.ProcessAsync(document, diff);
    }
    catch (DiffApplicationException)
    {
        // Try with more lenient context matching
        var customMatcher = new LenientContextMatcher();
        var customDiffer = new TextDiffer(contextMatcher: customMatcher);

        try
        {
            return customDiffer.Process(document, diff);
        }
        catch
        {
            // Log and return null or throw
            return null;
        }
    }
}
```

## Deployment Checklist

### Before Production

- [ ] **Performance Testing**: Validate with expected document sizes
- [ ] **Memory Testing**: Check memory usage under load
- [ ] **Error Handling**: Test all exception scenarios
- [ ] **Security Review**: Validate input sanitization
- [ ] **Monitoring**: Implement telemetry and logging
- [ ] **Configuration**: Externalize all limits and timeouts
- [ ] **Documentation**: Update API documentation
- [ ] **Testing**: Achieve >90% test coverage

### Production Monitoring

- [ ] **Performance Metrics**: Track processing times and throughput
- [ ] **Error Rates**: Monitor exception frequencies
- [ ] **Resource Usage**: Track memory and CPU utilization
- [ ] **Success Rates**: Monitor processing success/failure ratios
- [ ] **User Experience**: Track timeout and cancellation rates

### Scaling Considerations

- [ ] **Horizontal Scaling**: Design for stateless processing
- [ ] **Load Balancing**: Distribute processing across instances
- [ ] **Caching**: Cache frequently processed documents
- [ ] **Queue Management**: Handle processing queues efficiently
- [ ] **Circuit Breakers**: Implement failure isolation

Following these best practices will help you build robust, high-performance applications with TextDiff.Sharp that scale effectively in production environments.