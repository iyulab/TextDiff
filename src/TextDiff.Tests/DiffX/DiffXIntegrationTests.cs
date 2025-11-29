using TextDiff.DiffX;
using TextDiff.Exceptions;
using Xunit.Abstractions;

namespace TextDiff.Tests.DiffX;

/// <summary>
/// Integration tests for DiffX support in TextDiffer.
/// Verifies end-to-end DiffX processing functionality.
/// </summary>
public class DiffXIntegrationTests
{
    private readonly TextDiffer _differ = new();
    private readonly ITestOutputHelper _output;

    public DiffXIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region ProcessDiffX Basic Tests

    [Fact]
    public void ProcessDiffX_WithDiffXFormat_AppliesChanges()
    {
        // Arrange
        var document = "line1\nline2\nline3";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/test.txt""}
#...diff:
 line1
-line2
+modified
 line3";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nmodified\nline3", normalizedResult);
        Assert.Equal(1, result.Changes.ChangedLines);
    }

    [Fact]
    public void ProcessDiffX_WithUnifiedDiff_FallsBackToProcess()
    {
        // Arrange
        var document = "line1\nline2\nline3";
        var unifiedDiff = @" line1
-line2
+modified
 line3";

        // Act
        var result = _differ.ProcessDiffX(document, unifiedDiff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nmodified\nline3", normalizedResult);
    }

    [Fact]
    public void ProcessDiffX_WithFilePath_SelectsCorrectFile()
    {
        // Arrange
        var document = "original content";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/file1.txt""}
#...diff:
-original content
+file1 modified
#..file:
#...meta: format=json, length=30
{""path"": ""/src/file2.txt""}
#...diff:
-original content
+file2 modified";

        // Act
        var result = _differ.ProcessDiffX(document, diffX, "/src/file2.txt");

        // Assert
        Assert.Equal("file2 modified", result.Text);
    }

    [Fact]
    public void ProcessDiffX_WithoutFilePath_UsesFirstEntry()
    {
        // Arrange
        var document = "original content";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/first.txt""}
#...diff:
-original content
+first file
#..file:
#...meta: format=json, length=30
{""path"": ""/src/second.txt""}
#...diff:
-original content
+second file";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        Assert.Equal("first file", result.Text);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ProcessDiffX_NullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        string? document = null;
        var diffX = "#diffx: encoding=utf-8\n...";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differ.ProcessDiffX(document!, diffX));
    }

    [Fact]
    public void ProcessDiffX_NullDiff_ThrowsArgumentNullException()
    {
        // Arrange
        var document = "content";
        string? diffX = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _differ.ProcessDiffX(document, diffX!));
    }

    [Fact]
    public void ProcessDiffX_EmptyDiff_ThrowsArgumentException()
    {
        // Arrange
        var document = "content";
        var diffX = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _differ.ProcessDiffX(document, diffX));
    }

    [Fact]
    public void ProcessDiffX_NoDiffsInDiffX_ThrowsDiffApplicationException()
    {
        // Arrange
        var document = "content";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.preamble:
Just a preamble, no actual diffs.";

        // Act & Assert
        var ex = Assert.Throws<DiffApplicationException>(
            () => _differ.ProcessDiffX(document, diffX));
        Assert.Contains("No applicable diff found", ex.Message);
    }

    [Fact]
    public void ProcessDiffX_FilePathNotFound_ThrowsDiffApplicationException()
    {
        // Arrange
        var document = "content";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/exists.txt""}
#...diff:
-content
+modified";

        // Act & Assert
        var ex = Assert.Throws<DiffApplicationException>(
            () => _differ.ProcessDiffX(document, diffX, "/src/not-exists.txt"));
        Assert.Contains("No diff found for file", ex.Message);
        Assert.Contains("/src/exists.txt", ex.Message);
    }

    [Fact]
    public void ProcessDiffX_BinaryDiff_ThrowsDiffApplicationException()
    {
        // Arrange
        var document = "";
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/image.png""}
#...diff: op=binary
GIT binary patch...";

        // Act & Assert
        var ex = Assert.Throws<DiffApplicationException>(
            () => _differ.ProcessDiffX(document, diffX));
        Assert.Contains("Cannot process binary diff", ex.Message);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ProcessDiffX_MultipleHunks_AppliesAllChanges()
    {
        // Arrange
        var document = @"function start() {
    console.log('start');
}

function middle() {
    console.log('middle');
}

function end() {
    console.log('end');
}";

        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/app.js""}
#...diff:
 function start() {
-    console.log('start');
+    console.info('Starting...');
 }

 function middle() {
-    console.log('middle');
+    console.info('Processing...');
 }

 function end() {
-    console.log('end');
+    console.info('Done!');
 }";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        Assert.Contains("console.info('Starting...')", result.Text);
        Assert.Contains("console.info('Processing...')", result.Text);
        Assert.Contains("console.info('Done!')", result.Text);
        Assert.DoesNotContain("console.log", result.Text);
    }

    [Fact]
    public void ProcessDiffX_AddNewLines_InsertsCorrectly()
    {
        // Arrange
        var document = @"class MyClass {
    def existing(self):
        pass
}";

        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=40
{""path"": ""/myclass.py"", ""op"": ""modify""}
#...diff:
 class MyClass {
     def existing(self):
         pass
+
+    def new_method(self):
+        return True
 }";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        Assert.Contains("def new_method(self):", result.Text);
        Assert.Contains("return True", result.Text);
    }

    [Fact]
    public void ProcessDiffX_DeleteLines_RemovesCorrectly()
    {
        // Arrange
        var document = @"line1
line2
line3
line4
line5";

        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...diff:
 line1
-line2
-line3
-line4
 line5";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nline5", normalizedResult);
        Assert.Equal(3, result.Changes.DeletedLines);
    }

    [Fact]
    public void ProcessDiffX_RealWorldRefactoring_AppliesCorrectly()
    {
        // Arrange - Python code refactoring scenario
        var document = @"import os
import sys

def get_config():
    # Load configuration
    config = load_from_file()
    return config

def process():
    cfg = get_config()
    do_work(cfg)";

        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.preamble: mimetype=text/markdown
    Refactoring to use dependency injection
#.change:
#..preamble:
    Convert to class-based design with DI
#..file:
#...meta: format=json, length=40
{""path"": ""/app.py"", ""op"": ""modify""}
#...diff:
 import os
 import sys
+from dataclasses import dataclass

-def get_config():
-    # Load configuration
-    config = load_from_file()
-    return config
+@dataclass
+class Config:
+    path: str
+    debug: bool = False

-def process():
-    cfg = get_config()
-    do_work(cfg)
+class Processor:
+    def __init__(self, config: Config):
+        self.config = config
+
+    def process(self):
+        do_work(self.config)";

        // Act
        var result = _differ.ProcessDiffX(document, diffX);

        // Assert
        Assert.Contains("from dataclasses import dataclass", result.Text);
        Assert.Contains("@dataclass", result.Text);
        Assert.Contains("class Config:", result.Text);
        Assert.Contains("class Processor:", result.Text);
        Assert.DoesNotContain("def get_config():", result.Text);
    }

    #endregion

    #region Static Helper Tests

    [Fact]
    public void IsDiffX_Static_DetectsFormat()
    {
        // Arrange
        var diffX = "#diffx: encoding=utf-8\n...";
        var unified = "--- a/file\n+++ b/file\n...";

        // Act & Assert
        Assert.True(TextDiffer.IsDiffX(diffX));
        Assert.False(TextDiffer.IsDiffX(unified));
    }

    [Fact]
    public void ExtractDiffXEntries_ReturnsAllEntries()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/file1.txt""}
#...diff:
 content1
#..file:
#...meta: format=json, length=30
{""path"": ""/file2.txt""}
#...diff:
 content2
#..file:
#...meta: format=json, length=30
{""path"": ""/file3.txt""}
#...diff:
 content3";

        // Act
        var entries = _differ.ExtractDiffXEntries(diffX).ToList();

        // Assert
        Assert.Equal(3, entries.Count);
        Assert.Equal("/file1.txt", entries[0].Path);
        Assert.Equal("/file2.txt", entries[1].Path);
        Assert.Equal("/file3.txt", entries[2].Path);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void ProcessDiffX_StandardUnifiedDiff_WorksIdenticallyToProcess()
    {
        // Arrange
        var document = "line1\nline2\nline3";
        var unifiedDiff = @" line1
-line2
+modified
 line3";

        // Act
        var resultDiffX = _differ.ProcessDiffX(document, unifiedDiff);
        var resultProcess = _differ.Process(document, unifiedDiff);

        // Assert
        Assert.Equal(resultProcess.Text, resultDiffX.Text);
        Assert.Equal(resultProcess.Changes.AddedLines, resultDiffX.Changes.AddedLines);
        Assert.Equal(resultProcess.Changes.DeletedLines, resultDiffX.Changes.DeletedLines);
        Assert.Equal(resultProcess.Changes.ChangedLines, resultDiffX.Changes.ChangedLines);
    }

    [Fact]
    public void ProcessDiffX_GitDiffWithHeaders_WorksAsUnifiedDiff()
    {
        // Arrange
        var document = "original line";
        var gitDiff = @"diff --git a/test.txt b/test.txt
index abc1234..def5678 100644
--- a/test.txt
+++ b/test.txt
@@ -1 +1 @@
-original line
+modified line";

        // Act
        var result = _differ.ProcessDiffX(document, gitDiff);

        // Assert
        Assert.Equal("modified line", result.Text);
    }

    #endregion
}
