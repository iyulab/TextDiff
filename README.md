# TextDiff

**TextDiff** is a robust and efficient C# library designed for parsing, analyzing, and applying text differences. It supports the Unified Diff format, making it an essential tool for developers and applications that require precise text comparison and modification capabilities.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Supported Formats](#supported-formats)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Unified Diff Format Support**: Fully supports parsing and processing Unified Diff formats, enabling seamless integration with tools and workflows that utilize this standard.
- **Accurate Change Detection**: Identifies and categorizes changes including additions, deletions, and modifications with high precision.
- **Line Matching and Scoring**: Utilizes advanced algorithms to match lines between documents, providing similarity scores to assess the extent of changes.
- **Change Statistics**: Generates detailed statistics on the number of added, deleted, and changed lines, facilitating insightful analysis.
- **Indentation Handling**: Preserves and manages indentation levels to maintain code or text structure integrity during modifications.
- **Unicode Support**: Handles Unicode characters, including non-English languages like Korean, ensuring broad compatibility.
- **Flexible Integration**: Easily integrates into existing C# projects, offering a straightforward API for processing and applying diffs.

## Installation

You can install **TextDiff** via NuGet. Run the following command in the Package Manager Console:

```bash
Install-Package TextDiff
```

Alternatively, you can add it to your project using the .NET CLI:

```bash
dotnet add package TextDiff
```

## Usage

Below is a basic example of how to use **TextDiff** to parse a Unified Diff, calculate changes, and apply them to a document.

### Parsing and Processing a Diff

```csharp
using TextDiff;
using System.IO;
using System.Text;

// Initialize the DiffProcessor
var processor = new DiffProcessor();

// Original document text
string originalText = File.ReadAllText("path/to/original.txt", Encoding.UTF8);

// Unified Diff text
string diffText = File.ReadAllText("path/to/diff.unified", Encoding.UTF8);

// Process the diff
ProcessResult result = processor.Process(originalText, diffText);

// Access the modified text
string modifiedText = result.Text;

// Access change statistics
DocumentChangeResult changes = result.Changes;
Console.WriteLine($"Added Lines: {changes.AddedLines}");
Console.WriteLine($"Deleted Lines: {changes.DeletedLines}");
Console.WriteLine($"Changed Lines: {changes.ChangedLines}");

// Save the modified text
File.WriteAllText("path/to/modified.txt", modifiedText, Encoding.UTF8);
```

### Handling Changes Programmatically

You can also work directly with the `DocumentChange` objects to handle changes more granularly.

```csharp
using TextDiff;
using System;
using System.Linq;
using System.Collections.Generic;

// Assume 'changes' is a List<DocumentChange> obtained from processing
List<DocumentChange> changes = GetDocumentChanges(); // Replace with actual method to get changes

foreach (var change in changes)
{
    Console.WriteLine($"Change at line {change.LineNumber}:");
    if (change.LinesToRemove.Any())
    {
        Console.WriteLine("  Lines to remove:");
        foreach (var line in change.LinesToRemove)
        {
            Console.WriteLine($"    - {line}");
        }
    }
    if (change.LinesToInsert.Any())
    {
        Console.WriteLine("  Lines to insert:");
        foreach (var line in change.LinesToInsert)
        {
            Console.WriteLine($"    + {line}");
        }
    }
}
```

## Supported Formats

- **Unified Diff**: **TextDiff** natively supports the Unified Diff format, which is widely used for representing changes between two versions of a text file. This makes it compatible with popular version control systems like Git.

## Examples

### Example 1: Simple Text Replacement

**Original Text (`original.txt`):**
```
Hello World
This is a sample document.
Goodbye World
```

**Unified Diff (`diff.unified`):**
```diff
--- original.txt
+++ modified.txt
@@ -1,3 +1,3 @@
 Hello World
-This is a sample document.
+This is an updated document.
 Goodbye World
```

**Resulting Modified Text (`modified.txt`):**
```
Hello World
This is an updated document.
Goodbye World
```

### Example 2: Adding and Removing Lines

**Original Text (`original.txt`):**
```
Line 1
Line 2
Line 3
```

**Unified Diff (`diff.unified`):**
```diff
--- original.txt
+++ modified.txt
@@ -1,3 +1,4 @@
 Line 1
+Line 1.5
 Line 2
-Line 3
+Line Three
```

**Resulting Modified Text (`modified.txt`):**
```
Line 1
Line 1.5
Line 2
Line Three
```

### Example 3: Handling Non-English Text (Korean)

**Original Text (`original.txt`):**
```
안녕하세요
이것은 샘플 문서입니다.
안녕히 가세요
```

**Unified Diff (`diff.unified`):**
```diff
--- original.txt
+++ modified.txt
@@ -1,3 +1,3 @@
 안녕하세요
-이것은 샘플 문서입니다.
+이것은 업데이트된 문서입니다.
 안녕히 가세요
```

**Resulting Modified Text (`modified.txt`):**
```
안녕하세요
이것은 업데이트된 문서입니다.
안녕히 가세요
```

## Contributing

Contributions are welcome! If you'd like to contribute to **TextDiff**, please follow these steps:

1. **Fork the Repository**: Click the "Fork" button at the top right of the repository page to create your own fork.

2. **Create a New Branch**: It's best to create a new branch for each feature or bugfix.
    ```bash
    git checkout -b feature/your-feature-name
    ```

3. **Commit Your Changes**: Make sure your commits are clear and descriptive.
    ```bash
    git commit -m "Add feature XYZ"
    ```

4. **Push to Your Fork**:
    ```bash
    git push origin feature/your-feature-name
    ```

5. **Open a Pull Request**: Navigate to the original repository and open a pull request from your fork's branch.

### Guidelines

- **Code Quality**: Ensure your code adheres to the project's coding standards and includes appropriate comments.

- **Testing**: Include unit tests for new features or bugfixes to maintain the library's reliability.

- **Documentation**: Update the documentation as necessary to reflect your changes.

- **Issue Tracking**: Before starting on a new feature or bugfix, please check if an issue already exists. If not, feel free to open a new one.

## License

This project is licensed under the [MIT License](LICENSE).

---

For any questions or support, please open an issue on the [GitHub repository](https://github.com/iyulab-rnd/TextDiff).

---

**Additional Tips:**

- **Testing with Multiple Languages**: It's a good practice to test the library with various languages and character sets to ensure broad compatibility.

- **Stay Updated**: Keep an eye on the [GitHub repository](https://github.com/iyulab-rnd/TextDiff) for updates, new releases, and important announcements.

Feel free to reach out or contribute to enhance the capabilities of **TextDiff**!