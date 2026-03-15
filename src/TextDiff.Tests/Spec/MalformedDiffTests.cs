using TextDiff.Exceptions;

namespace TextDiff.Tests.Spec;

public class MalformedDiffTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_MissingHeadersButValidContent_AppliesSuccessfully()
    {
        string document = "old line";
        string diff = "-old line\n+new line";
        var result = _differ.Process(document, diff);
        Assert.Equal("new line", result.Text);
    }

    [Fact]
    public void Process_IncorrectHunkCountsButValidContent_StillApplies()
    {
        string document = "line1\nline2\nline3\nline4";
        string diff = "@@ -1,1 +1,1 @@\n line1\n-line2\n+modified2\n line3\n line4";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }

    [Fact]
    public void Process_NoValidDiffLines_ThrowsInvalidDiffFormatException()
    {
        string document = "content";
        string diff = "this is not a diff\njust plain text";
        Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
    }

    [Theory]
    [InlineData("?invalid prefix")]
    [InlineData("#comment like")]
    [InlineData("=equals line")]
    public void Process_UnrecognizedLinePrefix_ThrowsInvalidDiffFormatException(string badLine)
    {
        string document = "content";
        string diff = $" content\n{badLine}";
        Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
    }

    [Fact]
    public void Process_ContextDoesNotMatchDocument_ThrowsException()
    {
        string document = "actual content\nsecond line";
        string diff = " wrong context\n-second line\n+new content";
        Assert.ThrowsAny<Exception>(() => _differ.Process(document, diff));
    }

    [Fact]
    public void Process_DuplicateHeaders_IgnoredGracefully()
    {
        string document = "line1\nline2";
        string diff = "--- a/file.txt\n+++ b/file.txt\n--- a/file.txt\n+++ b/file.txt\n-line1\n+modified1\n line2";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified1", result.Text);
    }

    [Fact]
    public void Process_EllipsisComment_SkippedCorrectly()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n...\n-line3\n+modified3";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified3", result.Text);
    }

    [Fact]
    public void Process_OnlyEmptyLinesInDiff_HandlesGracefully()
    {
        string document = "content";
        var ex = Record.Exception(() => _differ.Process(document, "\n\n\n"));
        if (ex != null)
        {
            Assert.True(ex is ArgumentException, $"Expected ArgumentException but got {ex.GetType().Name}");
        }
    }

    [Fact]
    public void Process_TruncatedDiff_AppliesWhatIsAvailable()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n-line2\n+modified2";
        var result = _differ.Process(document, diff);
        Assert.Contains("modified2", result.Text);
    }

    [Fact]
    public void Process_RemovalLineDoesNotMatchDocument_ThrowsOrSkips()
    {
        string document = "actual line\nsecond";
        string diff = "-completely different\n+replacement\n second";
        Assert.ThrowsAny<Exception>(() => _differ.Process(document, diff));
    }
}
