using Xunit.Abstractions;

namespace TextDiff.Tests.Spec;

/// <summary>
/// Tests for hunk header parsing according to GNU unified diff specification.
///
/// Hunk header format: @@ -l,s +l,s @@ [optional section heading]
///
/// Where:
/// - l = starting line number
/// - s = number of lines (can be omitted if s=1)
/// - The range for the original file is preceded by minus (-)
/// - The range for the new file is preceded by plus (+)
///
/// Examples:
/// @@ -1,7 +1,6 @@           - Standard format
/// @@ -1 +1 @@               - Single line change (s=1 omitted)
/// @@ -1,7 +1,6 @@ function  - With section heading
/// </summary>
public class HunkHeaderTests
{
    private readonly TextDiffer _differ = new();
    private readonly ITestOutputHelper _output;

    public HunkHeaderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Standard Hunk Header Format

    [Fact]
    public void Process_StandardHunkHeader_ShouldParseCorrectly()
    {
        // Arrange - Standard @@ -l,s +l,s @@ format
        var document = "line1\nline2\nline3\nline4\nline5";
        var diff = @"@@ -1,5 +1,5 @@
 line1
-line2
+modified_line2
 line3
 line4
 line5";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
        Assert.Equal(1, result.Changes.ChangedLines);
    }

    [Fact]
    public void Process_HunkHeaderWithAddedLines_ShouldReflectCorrectCounts()
    {
        // Arrange - Adding lines changes the +s count
        var document = "line1\nline2\nline3";
        var diff = @"@@ -1,3 +1,5 @@
 line1
 line2
+added1
+added2
 line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("added1", result.Text);
        Assert.Contains("added2", result.Text);
        Assert.Equal(2, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_HunkHeaderWithDeletedLines_ShouldReflectCorrectCounts()
    {
        // Arrange - Deleting lines changes the +s count
        var document = "line1\nline2\nline3\nline4\nline5";
        var diff = @"@@ -1,5 +1,3 @@
 line1
-line2
-line3
 line4
 line5";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.DoesNotContain("line2", result.Text);
        Assert.DoesNotContain("line3", result.Text);
        Assert.Equal(2, result.Changes.DeletedLines);
    }

    #endregion

    #region Single Line Hunk Headers

    [Fact]
    public void Process_SingleLineCountOmitted_ShouldDefaultToOne()
    {
        // Arrange - When s=1, the count can be omitted
        var document = "single_line";
        var diff = @"@@ -1 +1 @@
-single_line
+modified_single_line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Equal("modified_single_line", result.Text);
    }

    [Fact]
    public void Process_MixedOmittedCounts_ShouldParseCorrectly()
    {
        // Arrange - One side omits count, other specifies
        var document = "line1";
        var diff = @"@@ -1 +1,2 @@
-line1
+line1a
+line1b";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line1a", result.Text);
        Assert.Contains("line1b", result.Text);
    }

    #endregion

    #region Hunk Header with Section Heading

    [Fact]
    public void Process_HunkWithSectionHeading_ShouldIgnoreHeading()
    {
        // Arrange - Optional section/function heading after @@
        var document = "function test() {\n    old_code();\n}";
        var diff = @"@@ -1,3 +1,3 @@ function test()
 function test() {
-    old_code();
+    new_code();
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("new_code();", result.Text);
    }

    [Fact]
    public void Process_HunkWithLongSectionHeading_ShouldIgnoreHeading()
    {
        // Arrange - Long function signature as heading
        var document = "public void ProcessData(string input, int count) {\n    DoSomething();\n}";
        var diff = @"@@ -1,3 +1,3 @@ public void ProcessData(string input, int count)
 public void ProcessData(string input, int count) {
-    DoSomething();
+    DoSomethingElse();
 }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("DoSomethingElse();", result.Text);
    }

    #endregion

    #region Multiple Hunks

    [Fact]
    public void Process_TwoSeparateHunks_ShouldApplyBothChanges()
    {
        // Arrange - Two distinct hunks in one diff
        var document = "line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\nline9\nline10";
        var diff = @"@@ -1,3 +1,3 @@
 line1
-line2
+modified_line2
 line3
@@ -8,3 +8,3 @@
 line8
-line9
+modified_line9
 line10";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
        Assert.Contains("modified_line9", result.Text);
        Assert.Equal(2, result.Changes.ChangedLines);
    }

    [Fact]
    public void Process_AdjacentHunks_ShouldApplyBothChanges()
    {
        // Arrange - Hunks that are adjacent (no gap between them)
        var document = "line1\nline2\nline3\nline4";
        var diff = @"@@ -1,2 +1,2 @@
 line1
-line2
+modified_line2
@@ -3,2 +3,2 @@
 line3
-line4
+modified_line4";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified_line2", result.Text);
        Assert.Contains("modified_line4", result.Text);
    }

    [Fact]
    public void Process_ThreeHunks_ShouldApplyAllChanges()
    {
        // Arrange
        var document = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line{i}"));
        var diff = @"@@ -1,3 +1,3 @@
 line1
-line2
+modified2
 line3
@@ -9,3 +9,3 @@
 line9
-line10
+modified10
 line11
@@ -18,3 +18,3 @@
 line18
-line19
+modified19
 line20";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified2", result.Text);
        Assert.Contains("modified10", result.Text);
        Assert.Contains("modified19", result.Text);
        Assert.Equal(3, result.Changes.ChangedLines);
    }

    #endregion

    #region Hunk Starting Line Tests

    [Fact]
    public void Process_HunkStartingAtMiddle_ShouldApplyAtCorrectPosition()
    {
        // Arrange - Hunk starts at line 5, not at beginning
        var document = "line1\nline2\nline3\nline4\nline5\nline6\nline7";
        var diff = @"@@ -4,4 +4,4 @@
 line4
 line5
-line6
+modified_line6
 line7";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var lines = result.Text.Replace("\r\n", "\n").Split('\n');
        Assert.Equal("line1", lines[0]);
        Assert.Equal("line2", lines[1]);
        Assert.Equal("line3", lines[2]);
        Assert.Equal("line4", lines[3]);
        Assert.Equal("line5", lines[4]);
        Assert.Equal("modified_line6", lines[5]);
        Assert.Equal("line7", lines[6]);
    }

    [Fact]
    public void Process_HunkAtVeryEnd_ShouldApplyCorrectly()
    {
        // Arrange - Hunk at the last lines of file
        var document = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"line{i}"));
        var diff = @"@@ -98,3 +98,3 @@
 line98
 line99
-line100
+modified_line100";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.EndsWith("modified_line100", result.Text);
    }

    #endregion

    #region Zero Line Count Edge Cases

    [Fact]
    public void Process_AddToEmptyRange_ShouldInsertLines()
    {
        // Arrange - @@ -5,0 +5,2 @@ means adding lines where there were none
        var document = "line1\nline2\nline3\nline4\nline5";
        var diff = @"@@ -3,3 +3,5 @@
 line3
 line4
+inserted1
+inserted2
 line5";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("inserted1", result.Text);
        Assert.Contains("inserted2", result.Text);
    }

    [Fact]
    public void Process_DeleteToEmptyRange_ShouldRemoveLines()
    {
        // Arrange
        var document = "line1\nline2\nline3\nline4\nline5";
        var diff = @"@@ -1,5 +1,3 @@
 line1
 line2
-line3
-line4
 line5";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.DoesNotContain("line3", result.Text);
        Assert.DoesNotContain("line4", result.Text);
        var lines = result.Text.Replace("\r\n", "\n").Split('\n');
        Assert.Equal(3, lines.Length);
    }

    #endregion

    #region Complex Hunk Scenarios

    [Fact]
    public void Process_LargeHunkWithMixedChanges_ShouldApplyCorrectly()
    {
        // Arrange
        var document = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line{i}"));
        var diff = @"@@ -5,10 +5,12 @@
 line5
-line6
-line7
+modified6
+modified7
+new_line_a
 line8
 line9
-line10
+modified10
 line11
 line12
+new_line_b
 line13
 line14";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified6", result.Text);
        Assert.Contains("modified7", result.Text);
        Assert.Contains("new_line_a", result.Text);
        Assert.Contains("modified10", result.Text);
        Assert.Contains("new_line_b", result.Text);
    }

    [Fact]
    public void Process_OverlappingContextInHunks_ShouldHandleCorrectly()
    {
        // Arrange - When context lines are shared conceptually between hunks
        var document = "a\nb\nc\nd\ne\nf";
        var diff = @"@@ -1,3 +1,3 @@
 a
-b
+B
 c
@@ -4,3 +4,3 @@
 d
-e
+E
 f";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var lines = result.Text.Replace("\r\n", "\n").Split('\n');
        Assert.Equal("a", lines[0]);
        Assert.Equal("B", lines[1]);
        Assert.Equal("c", lines[2]);
        Assert.Equal("d", lines[3]);
        Assert.Equal("E", lines[4]);
        Assert.Equal("f", lines[5]);
    }

    #endregion
}
