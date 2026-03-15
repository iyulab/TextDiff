namespace TextDiff.Tests.Spec;

public class WhitespaceAdvancedTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_BareEmptyLineAsContext_TreatedAsContextLine()
    {
        string document = "line1\n\nline3";
        string diff = " line1\n\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified3", result.Text);
        Assert.Contains("line1", result.Text);
    }

    [Fact]
    public void Process_AddMultipleEmptyLines_InsertsCorrectly()
    {
        string document = "line1\nline2";
        string diff = " line1\n+\n+\n+\n line2";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(5, lines.Length);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("", lines[2]);
        Assert.Equal("", lines[3]);
        Assert.Equal("line2", lines[4]);
    }

    [Fact]
    public void Process_RemoveConsecutiveEmptyLines_DeletesCorrectly()
    {
        string document = "line1\n\n\n\nline2";
        string diff = " line1\n-\n-\n-\n line2";
        var result = _differ.Process(document, diff);
        Assert.Equal("line1\nline2", result.Text);
    }

    [Fact]
    public void Process_TabToSpaceConversion_InChange()
    {
        string document = "\tindented";
        string diff = "-\tindented\n+    indented";
        var result = _differ.Process(document, diff);
        Assert.Equal("    indented", result.Text);
    }

    [Fact]
    public void Process_SpaceToTabConversion_InChange()
    {
        string document = "    indented";
        string diff = "-    indented\n+\tindented";
        var result = _differ.Process(document, diff);
        Assert.Equal("\tindented", result.Text);
    }

    [Fact]
    public void Process_AddWhitespaceOnlyLine_InsertsCorrectly()
    {
        string document = "line1\nline2";
        string diff = " line1\n+   \n line2";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("   ", lines[1]);
    }

    [Fact]
    public void Process_DeepIndentationChange_PreservesDepth()
    {
        string document = "            deeply indented code";
        string diff = "-            deeply indented code\n+            modified deep code";
        var result = _differ.Process(document, diff);
        Assert.Equal("            modified deep code", result.Text);
    }

    [Fact]
    public void Process_DeleteWhitespaceOnlyLine_PureDeletion()
    {
        string document = "line1\n   \nline3";
        string diff = " line1\n-   \n line3";
        var result = _differ.Process(document, diff);
        Assert.Equal("line1\nline3", result.Text);
    }
}
