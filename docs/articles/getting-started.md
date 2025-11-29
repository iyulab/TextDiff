# Getting Started with TextDiff.Sharp

TextDiff.Sharp is a production-ready C# library for processing unified diff files and applying changes to text documents. This guide will help you get started quickly.

## Installation

### NuGet Package

Install TextDiff.Sharp via NuGet Package Manager:

```
Install-Package TextDiff.Sharp
```

Or via .NET CLI:

```
dotnet add package TextDiff.Sharp
```

### Manual Installation

If building from source:

```bash
git clone https://github.com/iyulab/TextDiff.Sharp.git
cd TextDiff.Sharp
dotnet build
```

## Quick Start

### Basic Diff Processing

Here's the simplest way to apply a diff to a document:

```csharp
using TextDiff;

// Your original document
string originalDocument = @"line 1
line 2
line 3";

// Your unified diff
string diffContent = @" line 1
-line 2
+line 2 modified
 line 3";

// Create differ and process
var differ = new TextDiffer();
var result = differ.Process(originalDocument, diffContent);

// Access the results
Console.WriteLine("Updated document:");
Console.WriteLine(result.Text);

Console.WriteLine($"Changes: +{result.Changes.AddedLines} -{result.Changes.DeletedLines}");
```

### Working with Files

Processing diff files from disk:

```csharp
using TextDiff;

// Read files
string document = File.ReadAllText("original.txt");
string diff = File.ReadAllText("changes.diff");

// Process diff
var differ = new TextDiffer();
var result = differ.Process(document, diff);

// Save result
File.WriteAllText("updated.txt", result.Text);

Console.WriteLine($"Applied {result.Changes.ChangedLines} changes");
```

## Core Concepts

### Unified Diff Format

TextDiff.Sharp processes standard unified diff format:

```
 context line (unchanged)
-removed line
+added line
 another context line
```

Key elements:
- Lines starting with ` ` (space) are context lines
- Lines starting with `-` are removals
- Lines starting with `+` are additions
- Header lines starting with `@@` define line ranges (optional)

### ProcessResult

Every operation returns a `ProcessResult` containing:

```csharp
public class ProcessResult
{
    public string Text { get; }        // Updated document text
    public ChangeStats Changes { get; } // Detailed change statistics
}

public class ChangeStats
{
    public int AddedLines { get; }     // Lines added
    public int DeletedLines { get; }   // Lines deleted
    public int ChangedLines { get; }   // Lines modified
}
```

## Processing Options

### 1. Standard Processing

For most use cases:

```csharp
var differ = new TextDiffer();
var result = differ.Process(document, diff);
```

### 2. Asynchronous Processing

For large files or responsive applications:

```csharp
var differ = new TextDiffer();

// With cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
var result = await differ.ProcessAsync(document, diff, cts.Token);
```

### 3. Memory-Optimized Processing

For performance-critical scenarios:

```csharp
var differ = new TextDiffer();
var result = differ.ProcessOptimized(document, diff, bufferSizeHint: 8192);
```

### 4. Streaming Processing

For very large files (>100MB):

```csharp
var differ = new TextDiffer();

using var docStream = new FileStream("large_file.txt", FileMode.Open);
using var diffStream = new FileStream("changes.diff", FileMode.Open);
using var outputStream = new FileStream("updated_file.txt", FileMode.Create);

var result = await differ.ProcessStreamsAsync(docStream, diffStream, outputStream);
```

## Error Handling

TextDiff.Sharp provides specific exception types for different error scenarios:

```csharp
using TextDiff;
using TextDiff.Exceptions;

try
{
    var result = differ.Process(document, diff);
}
catch (ArgumentNullException ex)
{
    // Null input parameters
    Console.WriteLine($"Invalid input: {ex.ParamName}");
}
catch (ArgumentException ex)
{
    // Empty or whitespace-only input
    Console.WriteLine($"Input validation failed: {ex.Message}");
}
catch (InvalidDiffFormatException ex)
{
    // Malformed diff content
    Console.WriteLine($"Invalid diff format at line {ex.LineNumber}: {ex.Message}");
}
catch (DiffApplicationException ex)
{
    // Diff cannot be applied (context mismatch, etc.)
    Console.WriteLine($"Failed to apply diff: {ex.Message}");
}
catch (TextDiffException ex)
{
    // Base exception for all TextDiff-specific errors
    Console.WriteLine($"TextDiff error: {ex.Message}");
}
```

## Progress Reporting

For long-running operations, monitor progress:

```csharp
using TextDiff.Core;

var progress = new Progress<ProcessingProgress>(progressInfo =>
{
    Console.WriteLine($"{progressInfo.Stage}: {progressInfo.PercentComplete:F1}%");
});

var result = await differ.ProcessAsync(document, diff, CancellationToken.None, progress);
```

## Framework Support

TextDiff.Sharp supports multiple .NET frameworks:

- **.NET Standard 2.1** - Maximum compatibility
- **.NET 8.0** - Modern performance optimizations
- **.NET 9.0** - Latest features and improvements

Choose based on your application's requirements:

```xml
<!-- For maximum compatibility -->
<TargetFramework>netstandard2.1</TargetFramework>

<!-- For modern applications -->
<TargetFramework>net8.0</TargetFramework>

<!-- For latest features -->
<TargetFramework>net9.0</TargetFramework>
```

## Common Patterns

### Validation Before Processing

```csharp
public ProcessResult SafeProcess(string document, string diff)
{
    // Input validation
    if (string.IsNullOrWhiteSpace(document))
        throw new ArgumentException("Document cannot be empty", nameof(document));

    if (string.IsNullOrWhiteSpace(diff))
        throw new ArgumentException("Diff cannot be empty", nameof(diff));

    // Process with error handling
    try
    {
        var differ = new TextDiffer();
        return differ.Process(document, diff);
    }
    catch (InvalidDiffFormatException ex)
    {
        // Log and rethrow or handle appropriately
        LogError($"Invalid diff format: {ex.Message}");
        throw;
    }
}
```

### Retry Logic

```csharp
public async Task<ProcessResult> ProcessWithRetry(string document, string diff, int maxRetries = 3)
{
    var differ = new TextDiffer();

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (attempt == 1)
            {
                // Try optimized processing first
                return differ.ProcessOptimized(document, diff);
            }
            else
            {
                // Fallback to standard processing
                return await differ.ProcessAsync(document, diff);
            }
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(attempt)); // Exponential backoff
        }
    }

    throw new Exception($"Processing failed after {maxRetries} attempts");
}
```

## Performance Considerations

### Choose the Right Method

| Document Size | Recommended Method |
|---------------|-------------------|
| < 1MB | `Process()` |
| 1-10MB | `ProcessOptimized()` |
| 10-100MB | `ProcessAsync()` |
| > 100MB | `ProcessStreamsAsync()` |

### Memory Management

```csharp
// For memory-constrained environments
using TextDiff.Helpers;

// Estimate memory usage before processing
var documentLines = MemoryEfficientTextUtils.SplitLinesEfficient(document);
var estimatedMemory = MemoryEfficientTextUtils.EstimateMemoryUsage(
    documentLines.Length,
    averageLineLength,
    estimatedBlocks: 10
);

if (estimatedMemory > availableMemory)
{
    // Use streaming approach
    await ProcessWithStreaming();
}
```

## Next Steps

1. **Explore Examples**: Check out the [Examples](examples.md) for comprehensive usage patterns
2. **Review Best Practices**: Read [Best Practices](best-practices.md) for production optimization
3. **Handle Edge Cases**: See [Troubleshooting](troubleshooting.md) for common issues
4. **API Reference**: Browse the complete [API Documentation](../api/index.html)

## Getting Help

- **Examples Project**: Run `dotnet run` in the examples directory for interactive demos
- **API Documentation**: Complete reference with code examples
- **GitHub Issues**: Report bugs or request features
- **Community**: Join discussions and get help from other users

## What's Next?

Now that you understand the basics, explore these advanced topics:

- **Custom Dependencies**: Implement custom parsing or matching logic
- **Batch Processing**: Handle multiple diffs efficiently
- **Integration Patterns**: Integrate with version control systems
- **Performance Optimization**: Fine-tune for your specific use case

Ready to dive deeper? Check out our [comprehensive examples](examples.md) and [best practices guide](best-practices.md)!