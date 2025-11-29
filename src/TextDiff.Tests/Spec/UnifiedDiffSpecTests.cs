using TextDiff.Exceptions;
using Xunit.Abstractions;

namespace TextDiff.Tests.Spec;

/// <summary>
/// Tests based on GNU Unified Diff Format Specification.
/// Reference: https://www.gnu.org/software/diffutils/manual/html_node/Unified-Format.html
///
/// The unified output format starts with a two-line header:
/// --- from-file from-file-modification-time
/// +++ to-file to-file-modification-time
///
/// Next come one or more hunks of differences. Each hunk shows one area
/// where the files differ. Unified format hunks look like this:
/// @@ from-file-line-numbers to-file-line-numbers @@
///  line-from-either-file
///  line-from-either-file
/// ...
///
/// Lines beginning with:
/// - space ' ' are common to both files
/// - minus '-' appear only in the first file
/// - plus '+' appear only in the second file
/// </summary>
public class UnifiedDiffSpecTests
{
    private readonly TextDiffer _differ = new();
    private readonly ITestOutputHelper _output;

    public UnifiedDiffSpecTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region File Header Tests

    [Fact]
    public void Process_WithStandardGitHeaders_ShouldParseCorrectly()
    {
        // Arrange - Standard git diff header format
        var document = "line1\nline2\nline3";
        var diff = @"diff --git a/file.txt b/file.txt
index 1234567..abcdefg 100644
--- a/file.txt
+++ b/file.txt
@@ -1,3 +1,3 @@
 line1
-line2
+modified_line2
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
        Assert.DoesNotContain("line2\n", result.Text.Replace("modified_line2", ""));
    }

    [Fact]
    public void Process_WithTimestampHeaders_ShouldParseCorrectly()
    {
        // Arrange - Traditional diff with timestamps
        var document = "line1\nline2\nline3";
        var diff = @"--- lao	2002-02-21 23:30:39.942229878 -0800
+++ tzu	2002-02-21 23:30:50.442260588 -0800
@@ -1,3 +1,3 @@
 line1
-line2
+modified_line2
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
    }

    [Fact]
    public void Process_WithMinimalHeaders_ShouldParseCorrectly()
    {
        // Arrange - Minimal headers without timestamps
        var document = "line1\nline2\nline3";
        var diff = @"--- a/file.txt
+++ b/file.txt
 line1
-line2
+modified_line2
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
    }

    [Fact]
    public void Process_WithoutHeaders_ShouldParseCorrectly()
    {
        // Arrange - No file headers, just diff content
        var document = "line1\nline2\nline3";
        var diff = @" line1
-line2
+modified_line2
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
    }

    #endregion

    #region Line Prefix Tests

    [Theory]
    [InlineData(' ', "context line")]
    [InlineData('-', "removed line")]
    [InlineData('+', "added line")]
    public void Process_LinePrefixes_ShouldBeRecognized(char prefix, string description)
    {
        // Arrange
        _output.WriteLine($"Testing prefix '{prefix}' for {description}");

        var document = "line1\nline2\nline3";
        string diff = prefix switch
        {
            ' ' => " line1\n line2\n line3",
            '-' => " line1\n-line2\n line3",
            '+' => " line1\n+inserted\n line2\n line3",
            _ => throw new ArgumentException("Invalid prefix")
        };

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Text);
    }

    [Fact]
    public void Process_ContextLines_ShouldRemainUnchanged()
    {
        // Arrange - All context lines (prefixed with space)
        var document = "line1\nline2\nline3";
        var diff = " line1\n line2\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var expectedText = document.Replace("\r\n", "\n");
        var actualText = result.Text.Replace("\r\n", "\n");
        Assert.Equal(expectedText, actualText);
        Assert.Equal(0, result.Changes.ChangedLines);
        Assert.Equal(0, result.Changes.AddedLines);
        Assert.Equal(0, result.Changes.DeletedLines);
    }

    [Fact]
    public void Process_RemovedLines_ShouldBeDeleted()
    {
        // Arrange
        var document = "line1\nline2\nline3";
        var diff = " line1\n-line2\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nline3", normalizedResult);
        Assert.Equal(1, result.Changes.DeletedLines);
    }

    [Fact]
    public void Process_AddedLines_ShouldBeInserted()
    {
        // Arrange
        var document = "line1\nline3";
        var diff = " line1\n+line2\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nline2\nline3", normalizedResult);
        Assert.Equal(1, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_ModifiedLines_ShouldBeReplacedAsDeleteAndAdd()
    {
        // Arrange - In unified diff, modification is represented as delete + add
        var document = "line1\noriginal\nline3";
        var diff = " line1\n-original\n+modified\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nmodified\nline3", normalizedResult);
        Assert.Equal(1, result.Changes.ChangedLines);
    }

    #endregion

    #region Invalid Format Tests

    [Theory]
    [InlineData("invalid line without prefix")]
    [InlineData("*starred line")]
    [InlineData("!exclaimed line")]
    [InlineData(">quoted line")]
    [InlineData("<less than line")]
    public void Process_InvalidLinePrefix_ShouldThrowException(string invalidLine)
    {
        // Arrange
        var document = "line1\nline2";
        var diff = $" line1\n{invalidLine}";

        // Act & Assert
        Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
    }

    #endregion

    #region Complex Change Patterns

    [Fact]
    public void Process_MultipleConsecutiveAdditions_ShouldInsertAll()
    {
        // Arrange
        var document = "start\nend";
        var diff = " start\n+line1\n+line2\n+line3\n end";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("start\nline1\nline2\nline3\nend", normalizedResult);
        Assert.Equal(3, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_MultipleConsecutiveDeletions_ShouldRemoveAll()
    {
        // Arrange
        var document = "start\nline1\nline2\nline3\nend";
        var diff = " start\n-line1\n-line2\n-line3\n end";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("start\nend", normalizedResult);
        Assert.Equal(3, result.Changes.DeletedLines);
    }

    [Fact]
    public void Process_InterleavedAdditionsAndDeletions_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "line1\nold1\nline2\nold2\nline3";
        var diff = " line1\n-old1\n+new1\n line2\n-old2\n+new2\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nnew1\nline2\nnew2\nline3", normalizedResult);
        Assert.Equal(2, result.Changes.ChangedLines);
    }

    [Fact]
    public void Process_ReplaceMoreLinesThanRemoved_ShouldExpandContent()
    {
        // Arrange
        var document = "start\nold\nend";
        var diff = " start\n-old\n+new1\n+new2\n+new3\n end";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("start\nnew1\nnew2\nnew3\nend", normalizedResult);
        Assert.Equal(1, result.Changes.ChangedLines);
        Assert.Equal(2, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_ReplaceFewerLinesThanRemoved_ShouldShrinkContent()
    {
        // Arrange
        var document = "start\nold1\nold2\nold3\nend";
        var diff = " start\n-old1\n-old2\n-old3\n+new\n end";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("start\nnew\nend", normalizedResult);
        Assert.Equal(1, result.Changes.ChangedLines);
        Assert.Equal(2, result.Changes.DeletedLines);
    }

    #endregion

    #region Boundary Conditions

    [Fact]
    public void Process_ChangeAtFileStart_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "first\nmiddle\nlast";
        var diff = "-first\n+new_first\n middle\n last";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.StartsWith("new_first", result.Text);
    }

    [Fact]
    public void Process_ChangeAtFileEnd_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "first\nmiddle\nlast";
        var diff = " first\n middle\n-last\n+new_last";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.EndsWith("new_last", result.Text);
    }

    [Fact]
    public void Process_DeleteFirstLine_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "first\nsecond\nthird";
        var diff = "-first\n second\n third";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("second\nthird", normalizedResult);
    }

    [Fact]
    public void Process_DeleteLastLine_ShouldApplyCorrectly()
    {
        // Arrange
        var document = "first\nsecond\nthird";
        var diff = " first\n second\n-third";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("first\nsecond", normalizedResult);
    }

    [Fact]
    public void Process_AddAtFileStart_ShouldPrependLine()
    {
        // Arrange
        var document = "first\nsecond";
        var diff = "+new_first\n first\n second";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.StartsWith("new_first", result.Text);
    }

    [Fact]
    public void Process_AddAtFileEnd_ShouldAppendLine()
    {
        // Arrange
        var document = "first\nsecond";
        var diff = " first\n second\n+new_last";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.EndsWith("new_last", result.Text);
    }

    #endregion

    #region Empty Document Tests

    [Fact]
    public void Process_EmptyDocumentWithAdditions_ShouldCreateContent()
    {
        // Arrange
        var document = "";
        var diff = "+line1\n+line2\n+line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedResult = result.Text.Replace("\r\n", "\n");
        Assert.Equal("line1\nline2\nline3", normalizedResult);
        Assert.Equal(3, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_SingleLineDocumentToEmpty_ShouldDeleteAllContent()
    {
        // Arrange
        var document = "only_line";
        var diff = "-only_line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Equal("", result.Text);
        Assert.Equal(1, result.Changes.DeletedLines);
    }

    #endregion

    #region Whitespace Handling

    [Fact]
    public void Process_LineWithOnlySpaces_ShouldPreserve()
    {
        // Arrange
        var document = "line1\n   \nline3";
        var diff = " line1\n    \n-line3\n+modified";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("   ", result.Text); // Preserves whitespace-only line
        Assert.Contains("modified", result.Text);
    }

    [Fact]
    public void Process_TrailingWhitespace_ShouldPreserve()
    {
        // Arrange
        var document = "line1   \nline2";
        var diff = " line1   \n-line2\n+modified";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line1   ", result.Text);
    }

    [Fact]
    public void Process_LeadingWhitespace_ShouldPreserve()
    {
        // Arrange
        var document = "   line1\nline2";
        var diff = "    line1\n-line2\n+modified";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("   line1", result.Text);
    }

    [Fact]
    public void Process_TabIndentation_ShouldPreserve()
    {
        // Arrange
        var document = "\tindented\n\t\tdouble";
        var diff = " \tindented\n-\t\tdouble\n+\t\tmodified";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("\t\tmodified", result.Text);
    }

    #endregion
}
