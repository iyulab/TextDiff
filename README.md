# TextDiff.Sharp

**TextDiff.Sharp** is a robust and efficient C# library designed for parsing, analyzing, and applying textual diffs to documents. Whether you're building a version control system, a text editor with diff capabilities, or any application that requires detailed text comparison and manipulation, TextDiff.Sharp provides the tools you need to handle complex diff operations seamlessly.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
  - [Basic Usage](#basic-usage)
  - [Advanced Usage](#advanced-usage)
- [API Reference](#api-reference)
- [Examples](#examples)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Diff Parsing:** Efficiently parses diff texts into manageable blocks.
- **Change Tracking:** Accurately tracks added, deleted, and changed lines.
- **Line Matching:** Intelligent line matching with similarity scoring to handle ambiguous cases.
- **Whitespace Handling:** Robust handling of whitespace differences to ensure meaningful comparisons.
- **String Similarity:** Utilizes Levenshtein Distance for precise string similarity calculations.
- **Comprehensive Testing:** Includes a suite of unit tests to ensure reliability and correctness.
- **Extensible Architecture:** Designed with extensibility in mind, allowing for easy integration and customization.

## Installation

TextDiff.Sharp can be easily integrated into your C# project. You can add the source files directly or include it as a project reference.

### Using Source Files

1. Clone or download the repository.
2. Add the `TextDiff.Sharp` project or individual source files to your solution.
3. Add a reference to the `TextDiff.Sharp` project in your application.

### Using NuGet

```bash
Install-Package TextDiff.Sharp
```

## Getting Started

### Basic Usage

Here's a simple example of how to use TextDiff.Sharp to apply a diff to a document:

```csharp
using TextDiff;
using System;

class Program
{
    static void Main(string[] args)
    {
        var document = @"line1
line2
line3
line4";

        var diff = @" line1
- line2
- line3
+ new_line2
+ new_line3
 line4";

        var processor = new DiffProcessor();
        var result = processor.Process(document, diff);

        Console.WriteLine("Updated Document:");
        Console.WriteLine(result.Text);
        Console.WriteLine("\nChange Summary:");
        Console.WriteLine($"Added Lines: {result.Changes.AddedLines}");
        Console.WriteLine($"Deleted Lines: {result.Changes.DeletedLines}");
        Console.WriteLine($"Changed Lines: {result.Changes.ChangedLines}");
    }
}
```

**Output:**
```
Updated Document:
line1
new_line2
new_line3
line4

Change Summary:
Added Lines: 2
Deleted Lines: 2
Changed Lines: 0
```

### Advanced Usage

For more complex scenarios involving multiple diff blocks, whitespace handling, and similarity scoring, refer to the [Examples](#examples) section below.

## API Reference

### `DiffProcessor`

**Namespace:** `TextDiff`

The main class responsible for processing diffs and applying them to documents.

- **Methods:**
  - `Process(string documentText, string diffText)`: Applies the given diff to the document and returns the result.

### `DiffParser`

**Namespace:** `TextDiff`

Parses diff texts into structured `DiffBlock` objects.

- **Methods:**
  - `Parse(string diffText)`: Parses the diff text and returns a list of `DiffBlock` instances.

### `DiffBlock`

**Namespace:** `TextDiff`

Represents a block of differences, including leading, trailing, target, and insert lines.

- **Properties:**
  - `IReadOnlyList<string> TargetLines`
  - `IReadOnlyList<string> LeadingLines`
  - `IReadOnlyList<string> TrailingLines`
  - `IReadOnlyList<string> InsertLines`

- **Methods:**
  - `AddTargetLine(DiffLine line)`
  - `AddLeadingLine(DiffLine line)`
  - `AddTrailingLine(DiffLine line)`
  - `AddInsertLine(DiffLine line)`
  - `GetCommonIndentation()`

### `ChangeStats`

**Namespace:** `TextDiff`

Tracks statistics about changes, including added, deleted, and changed lines.

- **Methods:**
  - `UpdateStats(DiffBlock block)`
  - `ToResult()`: Converts the statistics to a `DocumentChangeResult`.

### `DocumentChange`

**Namespace:** `TextDiff`

Represents a single change to be applied to the document.

- **Properties:**
  - `int LineNumber`
  - `IReadOnlyList<string> LinesToRemove`
  - `IReadOnlyList<string> LinesToInsert`

### `DocumentChangeResult`

**Namespace:** `TextDiff`

Holds the summary of changes after processing a diff.

- **Properties:**
  - `int DeletedLines`
  - `int ChangedLines`
  - `int AddedLines`

### `StringSimilarityCalculator`

**Namespace:** `TextDiff`

Provides functionality to calculate the similarity between two strings using Levenshtein Distance.

- **Methods:**
  - `Calculate(string str1, string str2)`: Returns a similarity score between 0.0 and 1.0.

## Examples

### Applying a Simple Diff

```csharp
var document = @"line1
line2
line3
line4";

var diff = @" line1
- line2
- line3
+ new_line2
+ new_line3
 line4";

var processor = new DiffProcessor();
var result = processor.Process(document, diff);

Console.WriteLine(result.Text);
```

**Output:**
```
line1
new_line2
new_line3
line4
```

### Handling Additions Only

```csharp
var document = @"line1
    line2
    line3";

var diff = @" line1
+ line1.5
 line2
 line3";

var result = processor.Process(document, diff);

Console.WriteLine(result.Text);
```

**Output:**
```
line1
    line1.5
    line2
    line3
```

### Handling Deletions Only

```csharp
var document = @"line1
    line2
    line3";

var diff = @" line1
- line2
 line3";

var result = processor.Process(document, diff);

Console.WriteLine(result.Text);
```

**Output:**
```
line1
    line3
```

### Complex Changes with Multiple Diff Blocks

```csharp
var document = @"header1
    line1
    line2
    line3
    line4
    footer1
    footer2";

var diff = @" header1
- line1
+ new_line1
 line2
- line3
+ new_line3
+ added_line3.1
 line4
 footer1
- footer2
+ footer2_modified";

var result = processor.Process(document, diff);

Console.WriteLine(result.Text);
```

**Output:**
```
header1
    new_line1
    line2
    new_line3
    added_line3.1
    line4
    footer1
    footer2_modified
```

## Testing

TextDiff.Sharp includes a comprehensive suite of unit tests to ensure functionality and reliability. The tests are located in the `TextDiff.Tests` project and cover various scenarios, including simple additions and deletions, complex multi-line changes, whitespace handling, and more.

To run the tests:

1. Open the solution in your preferred IDE (e.g., Visual Studio).
2. Build the solution to restore all dependencies.
3. Navigate to the Test Explorer.
4. Run all tests to ensure everything is working as expected.

## Contributing

Contributions are welcome! If you'd like to improve TextDiff.Sharp, please follow these guidelines:

1. **Fork the Repository:** Click the "Fork" button at the top-right corner of the repository page.
2. **Clone the Fork:** Clone your forked repository to your local machine.
3. **Create a Branch:** Create a new branch for your feature or bugfix.
   ```bash
   git checkout -b feature/YourFeatureName
   ```
4. **Make Changes:** Implement your changes and ensure they align with the existing code style.
5. **Run Tests:** Ensure all tests pass and add new tests for your changes if necessary.
6. **Commit and Push:** Commit your changes and push them to your fork.
   ```bash
   git commit -m "Add feature XYZ"
   git push origin feature/YourFeatureName
   ```
7. **Create a Pull Request:** Navigate to the original repository and create a pull request from your fork.

Please ensure your code adheres to the project's coding standards and that all tests pass before submitting a pull request.

## License

This project is licensed under the [MIT License](LICENSE).
