namespace TextDiff.Tests.Spec;

public class ContextMatchingTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_ContextShiftedByPrependedLines_AppliesWithOffset()
    {
        string document = "extra1\nextra2\nline1\nline2\nline3";
        string diff = " line1\n-line2\n+modified2\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
        Assert.Contains("extra1", result.Text);
        Assert.Contains("extra2", result.Text);
    }

    [Fact]
    public void Process_ContextShiftedByDeletedLinesInPriorHunk_AppliesCorrectly()
    {
        string document = "a\nb\nc\nd\ne\nf\ng";
        string diff = " a\n-b\n c\n d\n e\n-f\n+F\n g";
        var result = _differ.Process(document, diff);
        Assert.DoesNotContain("\nb\n", result.Text);
        Assert.Contains("F", result.Text);
    }

    [Fact]
    public void Process_DuplicateContextLines_MatchesNearestToTarget()
    {
        string document = "header\nrepeat\nrepeat\nrepeat\ntarget\nrepeat\nfooter";
        string diff = " repeat\n-target\n+modified\n repeat";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified", result.Text);
        Assert.DoesNotContain("target", result.Text);
    }

    [Fact]
    public void Process_AllIdenticalLines_ReplacesCorrectPosition()
    {
        string document = "same\nsame\nsame\nsame\nsame";
        string diff = " same\n-same\n+different\n same";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal("same", lines[0]);
        Assert.Equal("different", lines[1]);
        Assert.Equal("same", lines[2]);
    }

    [Fact]
    public void Process_ZeroContextLines_AppliesByRemovalMatch()
    {
        string document = "line1\nline2\nline3\nline4\nline5";
        string diff = "-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified3", result.Text);
        Assert.DoesNotContain("\nline3\n", result.Text);
    }

    [Fact]
    public void Process_LargeContextWindow20Lines_AppliesCorrectly()
    {
        var docLines = Enumerable.Range(1, 20).Select(i => $"line{i}").ToList();
        string document = string.Join("\n", docLines);

        var diffLines = new List<string>();
        for (int i = 1; i <= 9; i++) diffLines.Add($" line{i}");
        diffLines.Add("-line10");
        diffLines.Add("+modified10");
        for (int i = 11; i <= 20; i++) diffLines.Add($" line{i}");

        string diff = string.Join("\n", diffLines);
        var result = _differ.Process(document, diff);
        Assert.Contains("modified10", result.Text);
        Assert.Equal(20, result.Text.Split('\n').Length);
    }

    [Fact]
    public void Process_ContextWithTrailingWhitespaceDiff_FuzzyMatches()
    {
        string document = "line1  \nline2\nline3";
        string diff = " line1\n-line2\n+modified\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified", result.Text);
    }

    [Fact]
    public void Process_ContextAtStartOfFile_FewerContextLinesBefore()
    {
        string document = "first\nsecond\nthird";
        string diff = "-first\n+FIRST\n second";
        var result = _differ.Process(document, diff);
        Assert.StartsWith("FIRST", result.Text);
    }

    [Fact]
    public void Process_ContextAtEndOfFile_FewerContextLinesAfter()
    {
        string document = "first\nsecond\nthird";
        string diff = " second\n-third\n+THIRD";
        var result = _differ.Process(document, diff);
        Assert.EndsWith("THIRD", result.Text);
    }
}
