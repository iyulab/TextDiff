# TextDiff.Sharp Documentation

TextDiff.Sharp is a production-ready C# library for processing unified diff files and applying changes to text documents.

## Features

- **Unified Diff Processing**: Full support for standard unified diff format
- **Multiple APIs**: Synchronous, asynchronous, streaming, and memory-optimized processing
- **High Performance**: Memory-efficient algorithms optimized for large files
- **Comprehensive Error Handling**: Specific exception types for different failure scenarios
- **Progress Reporting**: Real-time progress updates for long-running operations
- **Cross-Platform**: Supports .NET Standard 2.1, .NET 8.0, and .NET 9.0
- **Extensible**: Dependency injection support for custom implementations

## Quick Start

```csharp
using TextDiff;

var differ = new TextDiffer();
var result = differ.Process(originalDocument, diffContent);

Console.WriteLine($"Applied {result.Changes.AddedLines} additions and {result.Changes.DeletedLines} deletions");
Console.WriteLine("Updated document:");
Console.WriteLine(result.Text);
```

## Installation

Install the NuGet package:

```
dotnet add package TextDiff.Sharp
```

## Documentation Sections

- [API Reference](api/index.html) - Complete API documentation
- [Examples](articles/examples.html) - Comprehensive usage examples
- [Best Practices](articles/best-practices.html) - Performance and usage guidelines
- [Troubleshooting](articles/troubleshooting.html) - Common issues and solutions

## Performance

TextDiff.Sharp is designed for production use with:

- Memory-efficient processing for large files (>100MB)
- Streaming support for minimal memory footprint
- Async processing with cancellation and progress reporting
- Multi-target framework support for maximum compatibility

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.