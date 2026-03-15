namespace TextDiff.Tests.Spec;

public class MultiHunkTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_SecondHunkAdjustsForFirstInsertion()
    {
        string document = "a\nb\nc\nd\ne";
        string diff = "@@ -1,2 +1,3 @@\n a\n+inserted\n b\n@@ -4,2 +5,2 @@\n d\n-e\n+E";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(6, lines.Length);
        Assert.Equal("a", lines[0]);
        Assert.Equal("inserted", lines[1]);
        Assert.Equal("b", lines[2]);
        Assert.Equal("c", lines[3]);
        Assert.Equal("d", lines[4]);
        Assert.Equal("E", lines[5]);
    }

    [Fact]
    public void Process_SecondHunkAdjustsForFirstDeletion()
    {
        string document = "a\nb\nc\nd\ne";
        string diff = "@@ -1,3 +1,2 @@\n a\n-b\n c\n@@ -4,2 +3,2 @@\n d\n-e\n+E";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("a", lines[0]);
        Assert.Equal("c", lines[1]);
        Assert.Equal("d", lines[2]);
        Assert.Equal("E", lines[3]);
    }

    [Fact]
    public void Process_SingleHunkWithDeleteInsertAndChange()
    {
        string document = "keep1\ndelete1\ndelete2\nkeep2\nchange1\nkeep3";
        string diff = " keep1\n-delete1\n-delete2\n keep2\n-change1\n+CHANGED1\n+added1\n keep3";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(5, lines.Length);
        Assert.Equal("keep1", lines[0]);
        Assert.Equal("keep2", lines[1]);
        Assert.Equal("CHANGED1", lines[2]);
        Assert.Equal("added1", lines[3]);
        Assert.Equal("keep3", lines[4]);
    }

    [Fact]
    public void Process_TenScatteredHunks_AppliesAll()
    {
        var docLines = Enumerable.Range(1, 100).Select(i => $"line{i}");
        string document = string.Join("\n", docLines);

        var diffParts = new List<string>();
        for (int h = 0; h < 10; h++)
        {
            int target = (h * 10) + 5;
            diffParts.Add($" line{target - 1}");
            diffParts.Add($"-line{target}");
            diffParts.Add($"+MODIFIED{target}");
            diffParts.Add($" line{target + 1}");
        }
        string diff = string.Join("\n", diffParts);

        var result = _differ.Process(document, diff);
        for (int h = 0; h < 10; h++)
        {
            int target = (h * 10) + 5;
            Assert.Contains($"MODIFIED{target}", result.Text);
        }
    }

    [Fact]
    public void Process_HeadersOnlyNoHunks_ReturnsOriginalDocument()
    {
        string document = "unchanged content";
        string diff = "--- a/file.txt\n+++ b/file.txt";
        var result = _differ.Process(document, diff);
        Assert.Equal("unchanged content", result.Text);
    }

    [Fact]
    public void Process_MultipleChangeBlocksInSingleContextStream()
    {
        string document = "a\nb\nc\nd\ne\nf";
        string diff = " a\n-b\n+B\n c\n d\n-e\n+E\n f";
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal("a", lines[0]);
        Assert.Equal("B", lines[1]);
        Assert.Equal("c", lines[2]);
        Assert.Equal("d", lines[3]);
        Assert.Equal("E", lines[4]);
        Assert.Equal("f", lines[5]);
    }
}
