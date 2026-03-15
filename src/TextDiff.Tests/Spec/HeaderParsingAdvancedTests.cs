namespace TextDiff.Tests.Spec;

public class HeaderParsingAdvancedTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_GitModeChangeHeaders_SkippedCorrectly()
    {
        string document = "content";
        string diff = "diff --git a/script.sh b/script.sh\nold mode 100644\nnew mode 100755\n--- a/script.sh\n+++ b/script.sh\n@@ -1 +1 @@\n-content\n+modified";
        var result = _differ.Process(document, diff);
        Assert.Equal("modified", result.Text);
    }

    [Fact]
    public void Process_GitRenameHeaders_SkippedCorrectly()
    {
        string document = "content";
        string diff = "diff --git a/old.txt b/new.txt\nsimilarity index 95%\nrename from old.txt\nrename to new.txt\n--- a/old.txt\n+++ b/new.txt\n@@ -1 +1 @@\n-content\n+modified";
        var result = _differ.Process(document, diff);
        Assert.Equal("modified", result.Text);
    }

    [Fact]
    public void Process_GitCopyHeaders_SkippedCorrectly()
    {
        string document = "content";
        string diff = "diff --git a/src.txt b/dst.txt\nsimilarity index 100%\ncopy from src.txt\ncopy to dst.txt\n--- a/src.txt\n+++ b/dst.txt\n-content\n+modified";
        var result = _differ.Process(document, diff);
        Assert.Equal("modified", result.Text);
    }

    [Fact]
    public void Process_GitNewFileModeHeader_SkippedCorrectly()
    {
        string document = "";
        string diff = "diff --git a/newfile.txt b/newfile.txt\nnew file mode 100644\n--- /dev/null\n+++ b/newfile.txt\n@@ -0,0 +1,1 @@\n+new content";
        var result = _differ.Process(document, diff);
        Assert.Equal("new content", result.Text);
    }

    [Fact]
    public void Process_GitDeletedFileModeHeader_SkippedCorrectly()
    {
        string document = "to delete";
        string diff = "diff --git a/file.txt b/file.txt\ndeleted file mode 100644\n--- a/file.txt\n+++ /dev/null\n@@ -1,1 +0,0 @@\n-to delete";
        var result = _differ.Process(document, diff);
        Assert.Equal("", result.Text);
    }

    [Fact]
    public void Process_IndexLineWithoutMode_SkippedCorrectly()
    {
        string document = "old";
        string diff = "diff --git a/f.txt b/f.txt\nindex abc1234..def5678\n--- a/f.txt\n+++ b/f.txt\n-old\n+new";
        var result = _differ.Process(document, diff);
        Assert.Equal("new", result.Text);
    }

    [Fact]
    public void Process_MultipleDiffGitBlocks_AppliesFirstFileOnly()
    {
        string document = "content A";
        string diff = "diff --git a/fileA.txt b/fileA.txt\n--- a/fileA.txt\n+++ b/fileA.txt\n@@ -1 +1 @@\n-content A\n+modified A\ndiff --git a/fileB.txt b/fileB.txt\n--- a/fileB.txt\n+++ b/fileB.txt\n@@ -1 +1 @@\n-content B\n+modified B";
        var result = _differ.Process(document, diff);
        Assert.Equal("modified A", result.Text);
    }

    [Fact]
    public void Process_DissimilarityIndexHeader_SkippedCorrectly()
    {
        string document = "old";
        string diff = "diff --git a/f.txt b/f.txt\ndissimilarity index 90%\n--- a/f.txt\n+++ b/f.txt\n-old\n+new";
        var result = _differ.Process(document, diff);
        Assert.Equal("new", result.Text);
    }
}
