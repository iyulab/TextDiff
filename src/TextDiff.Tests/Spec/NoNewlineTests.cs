using Xunit.Abstractions;

namespace TextDiff.Tests.Spec;

/// <summary>
/// Tests for "No newline at end of file" marker handling.
///
/// According to GNU unified diff specification:
/// If a file does not end with a newline, the diff will include a line:
/// \ No newline at end of file
///
/// This marker appears after the last line of the affected file portion.
/// </summary>
public class NoNewlineTests
{
    private readonly TextDiffer _differ = new();
    private readonly ITestOutputHelper _output;

    public NoNewlineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region No Newline Marker Parsing

    [Fact]
    public void Process_NoNewlineMarkerAfterRemoval_ShouldIgnoreMarker()
    {
        // Arrange - Removed line had no trailing newline
        var document = "line1\nline2";
        var diff = @" line1
-line2
\ No newline at end of file
+line2_modified";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line2_modified", result.Text);
        Assert.DoesNotContain("No newline", result.Text);
    }

    [Fact]
    public void Process_NoNewlineMarkerAfterAddition_ShouldIgnoreMarker()
    {
        // Arrange - Added line has no trailing newline
        var document = "line1\nline2";
        var diff = @" line1
-line2
+line2_modified
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line2_modified", result.Text);
        Assert.DoesNotContain("No newline", result.Text);
    }

    [Fact]
    public void Process_BothFilesNoNewline_ShouldHandleCorrectly()
    {
        // Arrange - Both old and new files have no trailing newline
        var document = "line1\nline2";
        var diff = @" line1
-line2
\ No newline at end of file
+modified
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified", result.Text);
        Assert.DoesNotContain("No newline", result.Text);
    }

    [Fact]
    public void Process_AddingNewlineToFileEnd_ShouldHandleCorrectly()
    {
        // Arrange - File originally had no newline, now it does
        var document = "line1\nline2";
        var diff = @" line1
-line2
\ No newline at end of file
+line2
+";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.DoesNotContain("No newline", result.Text);
    }

    [Fact]
    public void Process_RemovingNewlineFromFileEnd_ShouldHandleCorrectly()
    {
        // Arrange - File originally had newline, now it doesn't
        var document = "line1\nline2\n";
        var diff = @" line1
-line2
+line2
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line2", result.Text);
        Assert.DoesNotContain("No newline", result.Text);
    }

    #endregion

    #region Context Around No Newline Marker

    [Fact]
    public void Process_NoNewlineWithMultipleChanges_ShouldApplyAllChanges()
    {
        // Arrange
        var document = "first\nmiddle\nlast";
        var diff = @" first
-middle
+MIDDLE
 last
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("MIDDLE", result.Text);
        Assert.Contains("last", result.Text);
    }

    [Fact]
    public void Process_NoNewlineOnlyAtNewFile_ShouldHandle()
    {
        // Arrange - Original had newline, new doesn't
        // Note: The library uses "prefix + space + content" format
        var document = "line1\nline2";
        var diff = @"- line1
- line2
+ single_line
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        // The "No newline" marker is skipped (not used to modify behavior)
        // The library joins lines with newlines by default
        Assert.Contains("single_line", result.Text);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Process_BackslashLineNotNoNewline_ShouldTreatAsContent()
    {
        // Arrange - Line starts with backslash but isn't the special marker
        var document = "line1\n\\other content\nline3";
        var diff = @" line1
-\other content
+\modified content
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("\\modified content", result.Text);
    }

    [Fact]
    public void Process_EmptyFileWithNoNewlineMarker_ShouldHandleCorrectly()
    {
        // Arrange
        var document = "";
        var diff = @"+single_line
\ No newline at end of file";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Equal("single_line", result.Text);
    }

    #endregion
}
