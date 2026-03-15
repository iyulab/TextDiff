namespace TextDiff.Tests.Spec;

public class NoNewlineAdvancedTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_MultipleChangesWithFinalNoNewlineMarker_AppliesAll()
    {
        string document = "a\nb\nc";
        string diff = "-a\n+A\n b\n-c\n+C\n\\ No newline at end of file";
        var result = _differ.Process(document, diff);
        Assert.Contains("A", result.Text);
        Assert.Contains("C", result.Text);
        Assert.Contains("b", result.Text);
    }

    [Fact]
    public void Process_NoNewlineMarkerBetweenRemovalAndAddition_Ignored()
    {
        string document = "last line";
        string diff = "-last line\n\\ No newline at end of file\n+new last line\n\\ No newline at end of file";
        var result = _differ.Process(document, diff);
        Assert.Equal("new last line", result.Text);
    }

    [Fact]
    public void Process_NoNewlineMarkerInSecondHunk_IgnoredCorrectly()
    {
        string document = "a\nb\nc\nd";
        string diff = "@@ -1,2 +1,2 @@\n-a\n+A\n b\n@@ -3,2 +3,2 @@\n c\n-d\n+D\n\\ No newline at end of file";
        var result = _differ.Process(document, diff);
        Assert.Contains("A", result.Text);
        Assert.Contains("D", result.Text);
    }

    [Fact]
    public void Process_BackslashPathInContent_NotConfusedWithMarker()
    {
        string document = "line1\nline2";
        string diff = " line1\n-line2\n+\\server\\share\\path";
        var result = _differ.Process(document, diff);
        Assert.Contains("\\server\\share\\path", result.Text);
    }

    [Fact]
    public void Process_BackslashWithDifferentText_SkippedAsMarker()
    {
        string document = "line1\nline2";
        string diff = "-line1\n+modified1\n\\ Some other backslash line\n line2";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified1", result.Text);
        Assert.DoesNotContain("Some other backslash line", result.Text);
    }

    [Fact]
    public void Process_NoNewlineAfterContextLine_IgnoredCorrectly()
    {
        string document = "a\nb";
        string diff = " a\n-b\n+B\n\\ No newline at end of file";
        var result = _differ.Process(document, diff);
        Assert.Equal("a\nB", result.Text);
    }
}
