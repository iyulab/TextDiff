using TextDiff.Exceptions;

namespace TextDiff.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void DiffApplicationException_MessageOnly()
    {
        var ex = new DiffApplicationException("test error");
        Assert.Equal("test error", ex.Message);
        Assert.Null(ex.DocumentPosition);
    }

    [Fact]
    public void DiffApplicationException_WithDocumentPosition()
    {
        var ex = new DiffApplicationException("context error", 42);
        Assert.Equal("context error", ex.Message);
        Assert.Equal(42, ex.DocumentPosition);
    }

    [Fact]
    public void DiffApplicationException_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DiffApplicationException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
        Assert.Null(ex.DocumentPosition);
    }

    [Fact]
    public void InvalidDiffFormatException_MessageOnly()
    {
        var ex = new InvalidDiffFormatException("bad format");
        Assert.Equal("bad format", ex.Message);
        Assert.Null(ex.LineNumber);
    }

    [Fact]
    public void InvalidDiffFormatException_WithLineNumber()
    {
        var ex = new InvalidDiffFormatException("parse error", 10);
        Assert.Equal("parse error", ex.Message);
        Assert.Equal(10, ex.LineNumber);
    }

    [Fact]
    public void InvalidDiffFormatException_WithInnerException()
    {
        var inner = new FormatException("inner");
        var ex = new InvalidDiffFormatException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
        Assert.Null(ex.LineNumber);
    }
}
