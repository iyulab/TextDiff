# TextDiff.Sharp

[![NuGet Version](https://img.shields.io/nuget/v/TextDiff.Sharp.svg)](https://www.nuget.org/packages/TextDiff.Sharp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TextDiff.Sharp.svg)](https://www.nuget.org/packages/TextDiff.Sharp/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/yourusername/TextDiff.Sharp/ci.yml?branch=main)](https://github.com/iyulab/TextDiff.Sharp/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A production-ready C# library for processing unified diff files and applying changes to text documents. TextDiff.Sharp provides multiple API variants optimized for different scenarios, from simple synchronous processing to high-performance streaming for large files.

## Features

- **🔄 Multiple Processing APIs**: Synchronous, asynchronous, streaming, and memory-optimized processing
- **📦 DiffX Format Support**: Native support for the extensible DiffX format with multi-file and metadata handling
- **⚡ High Performance**: Memory-efficient algorithms optimized for large files with 2-5x performance improvements
- **🛡️ Comprehensive Error Handling**: Specific exception types with detailed error information and line numbers
- **📊 Progress Reporting**: Real-time progress updates for long-running operations with cancellation support
- **🔧 Extensible Design**: Dependency injection support for custom parsing, matching, and tracking logic
- **🌐 Cross-Platform**: Supports .NET 8.0, .NET 9.0, and .NET 10.0
- **📚 Complete Documentation**: Comprehensive XML documentation, examples, and guides
- **✅ Production Ready**: 190+ tests with comprehensive validation and monitoring support

## Installation

TextDiff.Sharp can be seamlessly integrated into your C# project. You can include it using source files or via NuGet.

### Using Source Files

1. Clone or download the repository.
2. Add the `TextDiff.Sharp` project or individual source files to your solution.
3. Add a reference to the `TextDiff.Sharp` project in your application.

### Using NuGet

```bash
Install-Package TextDiff.Sharp

# or

dotnet add package TextDiff.Sharp
```

## Getting Started

To start using TextDiff.Sharp for applying diffs to original documents, follow the example below.

### Example: Applying a Diff to a Text Document

```csharp
using TextDiff;

// Original document
string originalText = @"line1
line2
line3";

// Diff to be applied
string diffText = @" line1
- line2
+ line2_modified
 line3";

// Create a TextDiffer instance
var textDiffer = new TextDiffer();

// Process the diff
var result = textDiffer.Process(originalText, diffText);

// Get the updated text
string updatedText = result.Text;

// Output the updated text
Console.WriteLine(updatedText);

/* Output:
line1
line2_modified
line3
*/
```

In this example:

- The original document contains three lines: `line1`, `line2`, and `line3`.
- The diff indicates that `line2` should be replaced with `line2_modified`.
- Using `TextDiffer.Process`, the diff is applied to the original text.
- The resulting `updatedText` reflects the change specified by the diff.

## Exploring the Code

To gain a deeper understanding of how TextDiff.Sharp applies diffs to original documents, you can examine the test files included in the repository:

- **DiffProcessorTests.cs**  
  Located at `/src/TextDiff.Tests/DiffProcessorTests.cs`, this file contains unit tests demonstrating various scenarios of applying diffs to original texts. The tests cover simple replacements, insertions, deletions, and complex changes, showcasing the library's capabilities.

### Sample Test Case from DiffProcessorTests.cs

```csharp
[Fact]
public void TestSimpleDeleteAndInsert()
{
    // Arrange
    var original = @"line1
line2
line3
line4";

    var diff = @" line1
- line2
+ new_line2
 line3
 line4";

    var expected = @"line1
new_line2
line3
line4";

    // Act
    var result = _differ.Process(original, diff);

    // Assert
    Assert.Equal(expected, result.Text);
}
```

In this test case:

- The original document has four lines.
- The diff replaces `line2` with `new_line2`.
- The `Process` method applies the diff to the original text.
- The test asserts that the resulting text matches the expected output.

## How It Works

TextDiff.Sharp processes diffs line by line, matching context lines and applying additions and deletions accordingly:

- **Context Lines**: Lines that start with a space `' '` represent unchanged lines in the diff and are used to align the diff with the original document.
- **Deletion Lines**: Lines that start with a minus `'-'` indicate lines to be removed from the original document.
- **Addition Lines**: Lines that start with a plus `'+'` represent new lines to be inserted into the original document.

The library ensures that the diff is applied accurately, even in cases where the diff contains complex changes, special characters, or whitespace variations.

## Working with DiffPlex

TextDiff.Sharp can be combined with [DiffPlex](https://github.com/mmanela/diffplex) to create a complete diff generation and application workflow:

```csharp
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TextDiff;

// Generate unified diff using DiffPlex
var diffBuilder = new InlineDiffBuilder(new Differ());
var diffResult = diffBuilder.BuildDiffModel(oldText, newText);

// Convert DiffPlex result to unified diff format
var unifiedDiff = ConvertToUnifiedDiff(diffResult);

// Apply the diff using TextDiff.Sharp
var textDiffer = new TextDiffer();
var result = textDiffer.Process(oldText, unifiedDiff);
string patchedText = result.Text;

// Helper method to convert DiffPlex output to unified diff
static string ConvertToUnifiedDiff(DiffPaneModel diffModel)
{
    var lines = new List<string>();

    foreach (var line in diffModel.Lines)
    {
        switch (line.Type)
        {
            case ChangeType.Unchanged:
                lines.Add($" {line.Text}");
                break;
            case ChangeType.Deleted:
                lines.Add($"-{line.Text}");
                break;
            case ChangeType.Inserted:
                lines.Add($"+{line.Text}");
                break;
        }
    }

    return string.Join(Environment.NewLine, lines);
}
```

This combination allows you to:
- Use DiffPlex for generating diffs between documents
- Use TextDiff.Sharp for applying those diffs to target documents
- Build automated code review, patching, and synchronization systems

## DiffX Format Support

TextDiff.Sharp supports the [DiffX](https://diffx.org/) extensible diff format, which provides structured multi-file diffs with metadata:

```csharp
using TextDiff;

// DiffX content with structured sections
var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff:
 def hello():
-    print('Hello')
+    print('Hello, World!')";

var differ = new TextDiffer();

// Auto-detect format (works with both DiffX and unified diff)
var result = differ.ProcessDiffX(document, diffX);

// Or specify target file in multi-file DiffX
var result = differ.ProcessDiffX(document, diffX, "/src/main.py");

// Extract all file entries for batch processing
var entries = differ.ExtractDiffXEntries(diffX);
foreach (var entry in entries)
{
    var doc = LoadDocument(entry.Path);
    var result = differ.Process(doc, entry.DiffContent);
    SaveDocument(entry.Path, result.Text);
}

// Check format before processing
if (TextDiffer.IsDiffX(content))
{
    // Handle as DiffX
}
```

### DiffX Features

- **Auto-detection**: `ProcessDiffX()` automatically detects format and falls back to standard unified diff
- **Multi-file support**: Extract and process multiple file changes from a single DiffX
- **Metadata extraction**: Access file paths, operations (create/modify/delete), and encoding
- **Backward compatible**: Existing `Process()` method continues to work with unified diff

## API Reference

### Processing Methods

| Method | Description |
|--------|-------------|
| `Process()` | Standard synchronous processing for unified diff |
| `ProcessAsync()` | Async processing with cancellation and progress |
| `ProcessStreamsAsync()` | Streaming for very large files |
| `ProcessOptimized()` | Memory-optimized synchronous processing |
| `ProcessDiffX()` | Auto-detect DiffX or unified diff format |

### Change Statistics

```csharp
var result = differ.Process(document, diff);

Console.WriteLine($"Added: {result.Changes.AddedLines}");
Console.WriteLine($"Deleted: {result.Changes.DeletedLines}");
Console.WriteLine($"Changed: {result.Changes.ChangedLines}");
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.