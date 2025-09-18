# TextDiff.Sharp Examples

This project demonstrates comprehensive usage of the TextDiff.Sharp library through practical examples.

## Prerequisites

- .NET 8.0 or later
- The TextDiff.Sharp library (referenced locally)

## Running Examples

### Interactive Mode

```bash
dotnet run
```

This starts an interactive menu where you can choose from available examples:

```
Available Examples:
1. Basic Diff Processing
2. Async Processing with Cancellation
3. Memory-Optimized Processing
4. Streaming for Large Files
5. Error Handling Patterns
6. Progress Reporting
7. Custom Dependencies
8. Real-World Scenario
9. Performance Comparison
0. Exit
```

### Direct Example Execution

```bash
dotnet run [example-number]
```

Examples:
- `dotnet run 1` - Basic diff processing
- `dotnet run 2` - Async processing demonstration
- `dotnet run 5` - Error handling patterns

## Example Categories

### 1. Basic Usage
- **Basic Diff Processing**: Fundamental diff application with sample files
- **Error Handling**: Comprehensive error handling patterns and best practices

### 2. Performance Optimization
- **Memory-Optimized Processing**: Memory-efficient processing for large files
- **Performance Comparison**: Benchmarking different processing methods
- **Streaming Processing**: Stream-to-stream processing for very large files

### 3. Advanced Features
- **Async Processing**: Asynchronous processing with cancellation and progress reporting
- **Progress Reporting**: Real-time progress updates for long-running operations
- **Custom Dependencies**: Using custom implementations of core interfaces

### 4. Real-World Applications
- **Code Review System**: Simulated code review workflow
- **Document Version Management**: Version control scenario

## Sample Files

The `SampleFiles/` directory contains:
- `sample_document.txt` - Original JavaScript code
- `sample_diff.txt` - Unified diff with improvements
- `updated_document.txt` - Generated result (created when running examples)

## Key Concepts Demonstrated

### Basic Operations
```csharp
var differ = new TextDiffer();
var result = differ.Process(originalDocument, diffContent);
```

### Async Processing
```csharp
var result = await differ.ProcessAsync(
    document,
    diff,
    cancellationToken,
    progress);
```

### Memory Optimization
```csharp
var result = differ.ProcessOptimized(
    largeDocument,
    diff,
    bufferSizeHint: 16384);
```

### Streaming for Large Files
```csharp
var result = await differ.ProcessStreamsAsync(
    documentStream,
    diffStream,
    outputStream);
```

### Error Handling
```csharp
try {
    var result = differ.Process(document, diff);
} catch (InvalidDiffFormatException ex) {
    Console.WriteLine($"Invalid diff at line {ex.LineNumber}");
} catch (DiffApplicationException ex) {
    Console.WriteLine($"Application failed: {ex.Message}");
}
```

### Progress Reporting
```csharp
var progress = new Progress<ProcessingProgress>(p =>
    Console.WriteLine($"{p.Stage}: {p.PercentComplete:F1}%"));

var result = await differ.ProcessAsync(document, diff, token, progress);
```

## Learning Path

1. **Start with Example 1** - Basic diff processing concepts
2. **Try Example 5** - Understand error handling patterns
3. **Explore Example 2** - Learn async processing
4. **Experiment with Example 3** - Memory optimization techniques
5. **Advanced scenarios** - Examples 7-9 for production patterns

## Building and Extending

### Adding New Examples

1. Create a new class in the `Examples/` directory
2. Implement a static `Run()` method
3. Add the example to the menu in `Program.cs`

### Custom Implementations

Example 7 demonstrates how to create custom implementations of:
- `IDiffBlockParser` - Custom diff parsing logic
- `IContextMatcher` - Custom context matching algorithms
- `IChangeTracker` - Custom change tracking and reporting

## Performance Notes

- Examples 3 and 9 demonstrate performance optimization techniques
- Memory usage is measured and reported for comparison
- Processing times vary based on document size and complexity
- Use streaming (Example 4) for files larger than 100MB

## Troubleshooting

If you encounter issues:

1. Ensure .NET 8.0+ is installed
2. Verify the TextDiff.Sharp project builds successfully
3. Check that sample files exist in `SampleFiles/`
4. Run `dotnet build` to verify compilation

For more help, see the main project documentation or troubleshooting guide.