namespace TextDiff.Tests.Spec;

public class SpecialContentTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_ContentWithAtAtSymbols_NotConfusedWithHunkHeader()
    {
        string document = "line1\n@@ this is content\nline3";
        string diff = " line1\n @@ this is content\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("@@ this is content", result.Text);
        Assert.Contains("modified3", result.Text);
    }

    [Fact]
    public void Process_ContentWithTripleDash_NotConfusedWithFileHeader()
    {
        string document = "line1\n--- not a header\nline3";
        string diff = " line1\n --- not a header\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("--- not a header", result.Text);
        Assert.Contains("modified3", result.Text);
    }

    [Fact]
    public void Process_ContentWithTriplePlus_NotConfusedWithFileHeader()
    {
        string document = "line1\n+++ not a header\nline3";
        string diff = " line1\n +++ not a header\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("+++ not a header", result.Text);
        Assert.Contains("modified3", result.Text);
    }

    [Fact]
    public void Process_ContentWithDiffGitText_NotConfusedWithDiffHeader()
    {
        string document = "line1\ndiff --git is a command\nline3";
        string diff = " line1\n diff --git is a command\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("diff --git is a command", result.Text);
        Assert.Contains("modified3", result.Text);
    }

    [Fact]
    public void Process_VeryLongLines_HandledCorrectly()
    {
        string longContent = new string('x', 10000);
        string newContent = new string('y', 10000);
        string document = $"short\n{longContent}\nend";
        string diff = $" short\n-{longContent}\n+{newContent}\n end";
        var result = _differ.Process(document, diff);
        Assert.Contains(newContent, result.Text);
        Assert.DoesNotContain(longContent, result.Text);
    }

    [Fact]
    public void Process_MergeConflictMarkersAsContent_TreatedAsRegularText()
    {
        string document = "line1\n<<<<<<< HEAD\n=======\n>>>>>>> branch\nline5";
        string diff = " line1\n <<<<<<< HEAD\n =======\n >>>>>>> branch\n-line5\n+modified5";
        var result = _differ.Process(document, diff);
        Assert.Contains("<<<<<<< HEAD", result.Text);
        Assert.Contains("=======", result.Text);
        Assert.Contains(">>>>>>> branch", result.Text);
        Assert.Contains("modified5", result.Text);
    }

    [Fact]
    public void Process_JsonContent_PreservesStructure()
    {
        string document = "{\n  \"key\": \"old\",\n  \"other\": true\n}";
        string diff = " {\n-  \"key\": \"old\",\n+  \"key\": \"new\",\n   \"other\": true\n }";
        var result = _differ.Process(document, diff);
        Assert.Contains("\"key\": \"new\"", result.Text);
        Assert.Contains("\"other\": true", result.Text);
    }

    [Fact]
    public void Process_AddedLineStartingWithDash_CorrectlyAdded()
    {
        string document = "line1\nline2";
        string diff = " line1\n-line2\n+--- this starts with dashes";
        var result = _differ.Process(document, diff);
        Assert.Contains("--- this starts with dashes", result.Text);
    }

    [Fact]
    public void Process_AddedLineStartingWithPlus_CorrectlyAdded()
    {
        string document = "line1\nline2";
        string diff = " line1\n-line2\n++++ this starts with pluses";
        var result = _differ.Process(document, diff);
        Assert.Contains("+++ this starts with pluses", result.Text);
    }

    [Fact]
    public void Process_ContentWithNullBytes_HandledCorrectly()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n-line2\n+with\0null\0bytes\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("with\0null\0bytes", result.Text);
    }
}
