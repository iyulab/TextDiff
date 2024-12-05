# TextDiff.Sharp

**TextDiff.Sharp** is a powerful and efficient C# library specifically designed for applying diffs to original text documents. If you have a diff file and an original document, TextDiff.Sharp allows you to seamlessly patch the diff onto the original, producing the updated document. This makes it an ideal tool for applications that need to update documents based on diff files, such as version control systems, code editors, or any text manipulation utilities that rely on diff operations.

## Key Features

- **Apply Diffs to Originals**: Precisely patch diff files onto original documents to generate updated versions.
- **Robust Parsing**: Accurately parse diff files, handling various diff formats and edge cases.
- **High Performance**: Optimized for efficiency, suitable for large documents and complex diffs.
- **Easy Integration**: Simple API that can be easily integrated into your C# projects.

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