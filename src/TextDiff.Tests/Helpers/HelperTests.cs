using TextDiff.Helpers;

namespace TextDiff.Tests.Helpers;

public class TextUtilsTests
{
    [Fact]
    public void RemoveIndentation_EmptyString_ReturnsSame()
    {
        Assert.Equal("", TextUtils.RemoveIndentation(""));
    }

    [Fact]
    public void RemoveIndentation_AllWhitespace_ReturnsSame()
    {
        // All-whitespace line: loop exhausts i, returns original
        Assert.Equal("   ", TextUtils.RemoveIndentation("   "));
    }

    [Fact]
    public void ExtractIndentation_NoLeadingWhitespace_ReturnsEmpty()
    {
        Assert.Equal("", TextUtils.ExtractIndentation("noindent"));
    }

    [Fact]
    public void LinesMatch_DifferentContent_ReturnsFalse()
    {
        Assert.False(TextUtils.LinesMatch("foo", "bar"));
    }

    [Fact]
    public void LinesMatch_SameContent_ReturnsTrue()
    {
        Assert.True(TextUtils.LinesMatch("hello world", "hello world"));
    }

    [Fact]
    public void LinesMatch_ExtraWhitespace_ReturnsTrue()
    {
        Assert.True(TextUtils.LinesMatch("hello  world", "hello world"));
    }

    [Fact]
    public void HasTextSimilarity_BothEmpty_ReturnsTrue()
    {
        Assert.True(TextUtils.HasTextSimilarity("", "   "));
    }

    [Fact]
    public void HasTextSimilarity_ExactMatch_ReturnsTrue()
    {
        Assert.True(TextUtils.HasTextSimilarity("foo", "foo"));
    }

    [Fact]
    public void HasTextSimilarity_DifferentLeadingWhitespace_ReturnsTrue()
    {
        Assert.True(TextUtils.HasTextSimilarity("  foo", "    foo"));
    }

    [Fact]
    public void HasTextSimilarity_TrimmedMatch_ReturnsTrue()
    {
        Assert.True(TextUtils.HasTextSimilarity("  foo  ", "foo"));
    }

    [Fact]
    public void HasTextSimilarity_DifferentContent_ReturnsFalse()
    {
        Assert.False(TextUtils.HasTextSimilarity("foo", "bar"));
    }

    [Fact]
    public void ApplyIndentationPreservation_WhitespaceOnlyOriginal_ReturnsAddedLine()
    {
        var result = TextUtils.ApplyIndentationPreservation("   ", "  -", "  +new");
        Assert.Equal("  +new", result);
    }

    [Fact]
    public void ApplyIndentationPreservation_DifferentIndentation_ReturnsAddedLine()
    {
        // removal has 2 spaces, addition has 4 spaces → diff explicitly changes indent
        var result = TextUtils.ApplyIndentationPreservation("  original", "  removal", "    addition");
        Assert.Equal("    addition", result);
    }

    [Fact]
    public void ApplyIndentationPreservation_SameIndentation_PreservesOriginal()
    {
        // removal and addition both have same indent → preserve original's indent
        var result = TextUtils.ApplyIndentationPreservation("    original", "    removal", "    addition");
        Assert.Equal("    addition", result);
    }

    [Fact]
    public void DetectLineSeparator_NullWhenNoNewline()
    {
        Assert.Null(TextUtils.DetectLineSeparator("single line"));
    }

    [Fact]
    public void DetectLineSeparator_DetectsCRLF()
    {
        Assert.Equal("\r\n", TextUtils.DetectLineSeparator("line1\r\nline2"));
    }

    [Fact]
    public void DetectLineSeparator_DetectsLF()
    {
        Assert.Equal("\n", TextUtils.DetectLineSeparator("line1\nline2"));
    }
}

public class MemoryEfficientTextUtilsTests
{
    [Fact]
    public void SplitLinesEfficient_Empty_ReturnsEmpty()
    {
        var result = MemoryEfficientTextUtils.SplitLinesEfficient("");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitLinesEfficient_SingleLine_ReturnsSingle()
    {
        var result = MemoryEfficientTextUtils.SplitLinesEfficient("hello");
        Assert.Single(result);
        Assert.Equal("hello", result[0]);
    }

    [Fact]
    public void SplitLinesEfficient_MultiLine_LF()
    {
        var result = MemoryEfficientTextUtils.SplitLinesEfficient("a\nb\nc");
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void SplitLinesEfficient_MultiLine_CRLF()
    {
        var result = MemoryEfficientTextUtils.SplitLinesEfficient("a\r\nb\r\nc");
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void EstimateMemoryUsage_ReturnsPositiveValue()
    {
        long estimate = MemoryEfficientTextUtils.EstimateMemoryUsage(1000, 80, 50);
        Assert.True(estimate > 0);
    }

    [Fact]
    public void EstimateMemoryUsage_ScalesWithInputs()
    {
        long small = MemoryEfficientTextUtils.EstimateMemoryUsage(100, 40, 5);
        long large = MemoryEfficientTextUtils.EstimateMemoryUsage(10000, 80, 100);
        Assert.True(large > small);
    }
}
