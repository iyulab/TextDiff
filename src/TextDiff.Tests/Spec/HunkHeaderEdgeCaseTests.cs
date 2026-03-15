namespace TextDiff.Tests.Spec;

public class HunkHeaderEdgeCaseTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_HunkWithZeroOldCount_PureInsertion()
    {
        string document = "line1\nline2\nline3";
        string diff = "@@ -1,0 +2,2 @@\n+inserted1\n+inserted2";
        var result = _differ.Process(document, diff);
        var text = result.Text;
        Assert.Contains("inserted1", text);
        Assert.Contains("inserted2", text);
        Assert.Contains("line1", text);
        Assert.Contains("line2", text);
        Assert.Contains("line3", text);
    }

    [Fact]
    public void Process_HunkWithZeroNewCount_PureDeletion()
    {
        string document = "line1\nline2\nline3\nline4";
        string diff = "@@ -2,2 +2,0 @@\n-line2\n-line3";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("line4", lines[1]);
    }

    [Fact]
    public void Process_NewFileHunkHeader_CreatesFromEmpty()
    {
        string document = "";
        string diff = "@@ -0,0 +1,2 @@\n+new line 1\n+new line 2";
        var result = _differ.Process(document, diff);
        Assert.Equal("new line 1\nnew line 2", result.Text);
    }

    [Fact]
    public void Process_SectionNameContainingAtSymbols_ParsesCorrectly()
    {
        string document = "line1\nline2\nline3";
        string diff = "@@ -1,3 +1,3 @@ something @@ with @@ symbols\n line1\n-line2\n+modified\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified", result.Text);
    }

    [Fact]
    public void Process_HunkCountMismatch_MoreLinesThanDeclared_StillApplies()
    {
        string document = "line1\nline2\nline3";
        string diff = "@@ -1,2 +1,2 @@\n line1\n-line2\n+modified2\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }

    [Fact]
    public void Process_VeryLargeLineNumbers_ParsesCorrectly()
    {
        var lines = Enumerable.Range(1, 1000).Select(i => $"line{i}");
        string document = string.Join("\n", lines);
        string diff = "@@ -998,3 +998,3 @@\n line998\n-line999\n+modified999\n line1000";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified999", result.Text);
        Assert.Contains("line998", result.Text);
        Assert.Contains("line1000", result.Text);
    }

    [Fact]
    public void Process_ConsecutiveHunkHeaders_SkipsEmptyHunks()
    {
        string document = "line1\nline2\nline3";
        string diff = "@@ -1,1 +1,1 @@\n@@ -1,3 +1,3 @@\n line1\n-line2\n+modified2\n line3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }
}
