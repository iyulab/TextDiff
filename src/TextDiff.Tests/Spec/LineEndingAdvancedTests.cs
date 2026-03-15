namespace TextDiff.Tests.Spec;

public class LineEndingAdvancedTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_PureCrlfDocument_LfDiff_AppliesCorrectly()
    {
        string document = "line1\r\nline2\r\nline3\r\nline4";
        string diff = " line1\n line2\n-line3\n+modified3\n line4";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified3", result.Text);
        Assert.DoesNotContain("\nline3", result.Text.Replace("modified3", ""));
    }

    [Fact]
    public void Process_SingleLineNoNewline_ReplaceCorrectly()
    {
        string document = "single line";
        string diff = "-single line\n+replaced line";
        var result = _differ.Process(document, diff);
        Assert.Equal("replaced line", result.Text);
    }

    [Fact]
    public void Process_DocumentOfOnlyNewlines_AppliesCorrectly()
    {
        string document = "\n\n\n";
        string diff = " \n-\n+content\n ";
        var result = _differ.Process(document, diff);
        Assert.Contains("content", result.Text);
    }

    [Fact]
    public void Process_DocumentWithTrailingNewline_HandlesCorrectly()
    {
        string document = "line1\nline2\n";
        string diff = " line1\n-line2\n+modified2";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }

    [Fact]
    public void Process_LfDocument_CrlfDiff_AppliesCorrectly()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\r\n-line2\r\n+modified2\r\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }

    [Fact]
    public void Process_MultiLineChangeWithMixedEndings_AppliesAll()
    {
        string document = "a\r\nb\r\nc\r\nd";
        string diff = " a\n-b\n-c\n+B\n+C\n d";
        var result = _differ.Process(document, diff);
        Assert.Contains("B", result.Text);
        Assert.Contains("C", result.Text);
        Assert.Contains("a", result.Text);
        Assert.Contains("d", result.Text);
    }
}
