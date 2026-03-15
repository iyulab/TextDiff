namespace TextDiff.Tests.Spec;

public class FileCreationDeletionTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_NewFileWithDevNullHeader_CreatesMultiLineContent()
    {
        string document = "";
        string diff = "--- /dev/null\n+++ b/newfile.txt\n@@ -0,0 +1,3 @@\n+line1\n+line2\n+line3";
        var result = _differ.Process(document, diff);
        Assert.Equal("line1\nline2\nline3", result.Text);
        Assert.Equal(3, result.Changes.AddedLines);
    }

    [Fact]
    public void Process_DeleteFileWithDevNullHeader_ProducesEmpty()
    {
        string document = "line1\nline2";
        string diff = "--- a/file.txt\n+++ /dev/null\n@@ -1,2 +0,0 @@\n-line1\n-line2";
        var result = _differ.Process(document, diff);
        Assert.Equal("", result.Text);
        Assert.Equal(2, result.Changes.DeletedLines);
    }

    [Fact]
    public void Process_RemoveAllMultipleLines_ProducesEmpty()
    {
        string document = "a\nb\nc\nd\ne";
        string diff = "-a\n-b\n-c\n-d\n-e";
        var result = _differ.Process(document, diff);
        Assert.Equal("", result.Text);
        Assert.Equal(5, result.Changes.DeletedLines);
    }

    [Fact]
    public void Process_ReplaceEntireDocument_DifferentLineCounts()
    {
        string document = "old1\nold2\nold3";
        string diff = "-old1\n-old2\n-old3\n+new1\n+new2";
        var result = _differ.Process(document, diff);
        Assert.Equal("new1\nnew2", result.Text);
    }

    [Fact]
    public void Process_ReplaceEntireDocument_MoreLinesThanOriginal()
    {
        string document = "old1";
        string diff = "-old1\n+new1\n+new2\n+new3";
        var result = _differ.Process(document, diff);
        Assert.Equal("new1\nnew2\nnew3", result.Text);
    }

    [Fact]
    public void Process_EmptyDocumentWithEmptyHunk_RemainsEmpty()
    {
        string document = "";
        string diff = "@@ -0,0 +0,0 @@";
        var result = _differ.Process(document, diff);
        Assert.Equal("", result.Text);
    }

    [Fact]
    public void Process_CreateLargeFile_AllAdditions()
    {
        string document = "";
        var addLines = Enumerable.Range(1, 100).Select(i => $"+line{i}");
        string diff = string.Join("\n", addLines);
        var result = _differ.Process(document, diff);
        var lines = result.Text.Split('\n');
        Assert.Equal(100, lines.Length);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("line100", lines[99]);
    }
}
