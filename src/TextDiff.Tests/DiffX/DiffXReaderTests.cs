using TextDiff.DiffX;
using Xunit.Abstractions;

namespace TextDiff.Tests.DiffX;

/// <summary>
/// Tests for <see cref="DiffXReader"/> implementation.
/// Verifies DiffX format detection and diff section extraction.
/// </summary>
public class DiffXReaderTests
{
    private readonly DiffXReader _reader = new();
    private readonly ITestOutputHelper _output;

    public DiffXReaderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region IsDiffX Detection Tests

    [Fact]
    public void IsDiffX_WithDiffXHeader_ReturnsTrue()
    {
        // Arrange
        var content = "#diffx: encoding=utf-8, version=1.0\n#.change:\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDiffX_WithMinimalHeader_ReturnsTrue()
    {
        // Arrange
        var content = "#diffx:\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDiffX_WithUnifiedDiff_ReturnsFalse()
    {
        // Arrange
        var content = "--- a/file.txt\n+++ b/file.txt\n@@ -1,3 +1,3 @@\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDiffX_WithGitDiff_ReturnsFalse()
    {
        // Arrange
        var content = "diff --git a/file.txt b/file.txt\nindex abc..def 100644\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDiffX_WithEmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = "";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDiffX_WithNullContent_ReturnsFalse()
    {
        // Arrange
        string? content = null;

        // Act
        var result = _reader.IsDiffX(content!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDiffX_WithSimilarButInvalidHeader_ReturnsFalse()
    {
        // Arrange - Similar but not exactly "#diffx:"
        var content = "#diff: encoding=utf-8\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDiffX_WithWindowsLineEndings_ReturnsTrue()
    {
        // Arrange
        var content = "#diffx: encoding=utf-8\r\n#.change:\r\n...";

        // Act
        var result = _reader.IsDiffX(content);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ExtractFileDiffs Basic Tests

    [Fact]
    public void ExtractFileDiffs_SingleFile_ExtractsCorrectly()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff: length=50
 line1
-old line
+new line
 line3";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("/src/main.py", entries[0].Path);
        Assert.Contains("-old line", entries[0].DiffContent);
        Assert.Contains("+new line", entries[0].DiffContent);
    }

    [Fact]
    public void ExtractFileDiffs_MultipleFiles_ExtractsAll()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/file1.py""}
#...diff: length=30
 line1
-old
+new
#..file:
#...meta: format=json, length=30
{""path"": ""/src/file2.py""}
#...diff: length=30
 other
-remove
+add";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("/src/file1.py", entries[0].Path);
        Assert.Equal("/src/file2.py", entries[1].Path);
    }

    [Fact]
    public void ExtractFileDiffs_MultipleChanges_ExtractsAll()
    {
        // Arrange - Multiple commits/changes
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..meta: format=json, length=40
{""id"": ""commit1""}
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff:
 first
-old1
+new1
#.change:
#..meta: format=json, length=40
{""id"": ""commit2""}
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff:
 second
-old2
+new2";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains("-old1", entries[0].DiffContent);
        Assert.Contains("-old2", entries[1].DiffContent);
    }

    [Fact]
    public void ExtractFileDiffs_WithOperation_ExtractsOperation()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=50
{""path"": ""/src/new.py"", ""op"": ""create""}
#...diff:
+new file content
+line 2";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("/src/new.py", entries[0].Path);
        Assert.Equal("create", entries[0].Operation);
    }

    #endregion

    #region Encoding Tests

    [Fact]
    public void ExtractFileDiffs_WithEncoding_InheritsFromHeader()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-16, version=1.0
#.change:
#..file:
#...diff:
 line";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("utf-16", entries[0].Encoding);
    }

    [Fact]
    public void ExtractFileDiffs_DefaultEncoding_IsUtf8()
    {
        // Arrange
        var diffX = @"#diffx: version=1.0
#.change:
#..file:
#...diff:
 line";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("utf-8", entries[0].Encoding);
    }

    #endregion

    #region Binary Diff Tests

    [Fact]
    public void ExtractFileDiffs_BinaryDiff_MarkedAsBinary()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=40
{""path"": ""/image.png"", ""op"": ""modify""}
#...diff: op=binary, binary-format=git-literal
literal 1234
abc...";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.True(entries[0].IsBinary);
    }

    [Fact]
    public void ExtractFileDiffs_TextDiff_NotMarkedAsBinary()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...diff:
 line1
-old
+new";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.False(entries[0].IsBinary);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ExtractFileDiffs_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        string? content = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _reader.ExtractFileDiffs(content!).ToList());
    }

    [Fact]
    public void ExtractFileDiffs_NotDiffX_ThrowsArgumentException()
    {
        // Arrange
        var content = "--- a/file.txt\n+++ b/file.txt\n...";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _reader.ExtractFileDiffs(content).ToList());
    }

    [Fact]
    public void ExtractFileDiffs_EmptyDiffX_ReturnsEmpty()
    {
        // Arrange - Valid DiffX header but no diffs
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.preamble:
This is just a preamble with no actual diffs.";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Empty(entries);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ExtractFileDiffs_WithPreambleAndMeta_SkipsNonDiffSections()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.preamble: indent=2
  This is a description of the changes.
  It can span multiple lines.
#.meta: format=json, length=100
{
  ""stats"": {
    ""files_changed"": 1,
    ""insertions"": 5,
    ""deletions"": 3
  }
}
#.change:
#..preamble:
  Commit message here
#..meta: format=json, length=50
{""author"": ""dev@example.com""}
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff:
 context
-removed
+added";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("/src/main.py", entries[0].Path);
        Assert.Contains("-removed", entries[0].DiffContent);
        Assert.Contains("+added", entries[0].DiffContent);
    }

    [Fact]
    public void ExtractFileDiffs_WithHunkHeaders_PreservesFormat()
    {
        // Arrange
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.change:
#..file:
#...meta: format=json, length=30
{""path"": ""/src/main.py""}
#...diff:
--- /src/main.py
+++ /src/main.py
@@ -10,5 +10,6 @@
 context line
-old line
+new line
+another new line
 trailing context";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        var diff = entries[0].DiffContent;
        Assert.Contains("--- /src/main.py", diff);
        Assert.Contains("@@ -10,5 +10,6 @@", diff);
    }

    [Fact]
    public void ExtractFileDiffs_RealWorldExample_ProcessesCorrectly()
    {
        // Arrange - Example from DiffX documentation
        var diffX = @"#diffx: encoding=utf-8, version=1.0
#.preamble: mimetype=text/markdown, indent=4
    # Release Notes v2.0

    Major refactoring release.
#.change:
#..preamble: mimetype=text/plain
    Refactored authentication module for better security
#..meta: format=json, length=80
{
    ""author"": ""security-team@company.com"",
    ""date"": ""2024-01-15T10:30:00Z""
}
#..file:
#...meta: format=json, length=60
{""path"": ""/src/auth/login.py"", ""op"": ""modify""}
#...diff:
--- /src/auth/login.py
+++ /src/auth/login.py
@@ -25,8 +25,10 @@
 def authenticate(username, password):
-    # Old insecure check
-    return check_password(username, password)
+    # New secure authentication
+    hashed = hash_password(password)
+    return verify_hash(username, hashed)

 def logout():";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("/src/auth/login.py", entries[0].Path);
        Assert.Equal("modify", entries[0].Operation);
        Assert.Contains("New secure authentication", entries[0].DiffContent);
        Assert.DoesNotContain("Release Notes", entries[0].DiffContent);
        Assert.DoesNotContain("security-team@company.com", entries[0].DiffContent);
    }

    [Fact]
    public void ExtractFileDiffs_WindowsLineEndings_HandlesCorrectly()
    {
        // Arrange
        var diffX = "#diffx: encoding=utf-8\r\n#.change:\r\n#..file:\r\n#...diff:\r\n line1\r\n-old\r\n+new\r\n line3";

        // Act
        var entries = _reader.ExtractFileDiffs(diffX).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Contains("-old", entries[0].DiffContent);
        Assert.Contains("+new", entries[0].DiffContent);
    }

    #endregion
}
