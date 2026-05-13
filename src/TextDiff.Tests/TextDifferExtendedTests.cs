using TextDiff.Exceptions;

namespace TextDiff.Tests;

public class TextDifferExtendedTests
{
    private readonly TextDiffer _differ = new();

    // ProcessOptimized — currently untested
    [Fact]
    public void ProcessOptimized_BasicDiff_Succeeds()
    {
        var document = "line1\nline2\nline3";
        var diff = " line1\n-line2\n+replaced\n line3";
        var result = _differ.ProcessOptimized(document, diff);
        Assert.Contains("replaced", result.Text);
    }

    [Fact]
    public void ProcessOptimized_NullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _differ.ProcessOptimized(null!, "+ line"));
    }

    [Fact]
    public void ProcessOptimized_NullDiff_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _differ.ProcessOptimized("doc", null!));
    }

    [Fact]
    public void ProcessOptimized_WhitespaceDiff_Throws()
    {
        Assert.Throws<ArgumentException>(() => _differ.ProcessOptimized("doc", "   "));
    }

    // ProcessAsync input validation
    [Fact]
    public async Task ProcessAsync_NullDocument_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _differ.ProcessAsync(null!, "+ line"));
    }

    [Fact]
    public async Task ProcessAsync_NullDiff_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _differ.ProcessAsync("doc", null!));
    }

    [Fact]
    public async Task ProcessAsync_WhitespaceDiff_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _differ.ProcessAsync("doc", "   "));
    }

    [Fact]
    public async Task ProcessAsync_BasicDiff_Succeeds()
    {
        var document = "line1\nline2\nline3";
        var diff = " line1\n-line2\n+new\n line3";
        var result = await _differ.ProcessAsync(document, diff);
        Assert.Contains("new", result.Text);
    }

    // ValidateDiffFormat — hunk-header-only is valid (no-op diff)
    [Fact]
    public void Process_HunkHeaderOnly_IsValidNoop()
    {
        // Diff with only @@ header and no +/-/space lines is a valid no-op
        var document = "line1\nline2";
        var diff = "@@ -0,0 +1,0 @@";
        var result = _differ.Process(document, diff);
        Assert.NotNull(result);
    }

    // ValidateDiffFormat — no hunk header AND no valid lines → throws
    [Fact]
    public void Process_OnlyGitHeaders_NoHunkNoValidLines_Throws()
    {
        // diff/index headers are allowed but have no valid diff lines and no @@ header
        var document = "line1\nline2";
        var diff = "diff --git a/file.txt b/file.txt\nindex abc..def 100644";
        Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
    }

    // ValidateDiffFormat — backslash prefix (no newline at end of file) is allowed
    [Fact]
    public void Process_BackslashPrefixLine_IsAllowed()
    {
        var document = "line1\nline2";
        var diff = " line1\n-line2\n+line2new\n\\ No newline at end of file";
        var result = _differ.Process(document, diff);
        Assert.Contains("line2new", result.Text);
    }

    // ProcessDiffX — binary diff throws
    [Fact]
    public void ProcessDiffX_BinaryDiff_Throws()
    {
        var diffX = "#diffx: encoding=utf-8, version=1.0\n" +
                    "#.change:\n" +
                    "#..file:\n" +
                    "#...meta:\n" +
                    "{ \"path\": \"image.png\", \"op\": \"modify\" }\n" +
                    "#...diff: op=binary\n" +
                    "binary data here\n";
        var ex = Assert.Throws<DiffApplicationException>(() =>
            _differ.ProcessDiffX("document", diffX));
        Assert.Contains("binary", ex.Message);
    }

    // ProcessDiffX — filePath not found throws
    [Fact]
    public void ProcessDiffX_FilePathNotFound_Throws()
    {
        var diffX = "#diffx: encoding=utf-8, version=1.0\n" +
                    "#.change:\n" +
                    "#..file:\n" +
                    "#...meta:\n" +
                    "{ \"path\": \"src/foo.cs\", \"op\": \"modify\" }\n" +
                    "#...diff:\n" +
                    " line1\n" +
                    "-line2\n" +
                    "+line2new\n";
        var ex = Assert.Throws<DiffApplicationException>(() =>
            _differ.ProcessDiffX("line1\nline2", diffX, "nonexistent.cs"));
        Assert.Contains("nonexistent.cs", ex.Message);
    }
}
