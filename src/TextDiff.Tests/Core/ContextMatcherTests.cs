using TextDiff.Core;
using TextDiff.Models;

namespace TextDiff.Tests.Core;

public class ContextMatcherTests
{
    private readonly ContextMatcher _matcher = new();

    [Fact]
    public void FindPosition_ExactMatch_ReturnsCorrectPosition()
    {
        var lines = new[] { "alpha", "beta", "gamma", "delta" };
        var block = new DiffBlock();
        block.BeforeContext.Add("beta");
        block.Removals.Add("gamma");

        int position = _matcher.FindPosition(lines, 0, block);

        Assert.Equal(1, position);
    }

    [Fact]
    public void FindPosition_WithLeadingWhitespace_MatchesTrimmed()
    {
        var lines = new[] { "  alpha", "  beta", "  gamma" };
        var block = new DiffBlock();
        block.BeforeContext.Add("  alpha");
        block.Removals.Add("  beta");

        int position = _matcher.FindPosition(lines, 0, block);

        Assert.Equal(0, position);
    }

    [Fact]
    public void FindPosition_ThrowsWhenNoMatch()
    {
        var lines = new[] { "alpha", "beta", "gamma" };
        var block = new DiffBlock();
        block.BeforeContext.Add("notexist");
        block.Removals.Add("alsonotexist");

        Assert.Throws<InvalidOperationException>(() =>
            _matcher.FindPosition(lines, 0, block));
    }

    [Fact]
    public void FindPosition_ThrowsOnNullDocumentLines()
    {
        var block = new DiffBlock();
        Assert.Throws<ArgumentNullException>(() =>
            _matcher.FindPosition(null!, 0, block));
    }

    [Fact]
    public void FindPosition_ThrowsOnNullBlock()
    {
        var lines = new[] { "alpha" };
        Assert.Throws<ArgumentNullException>(() =>
            _matcher.FindPosition(lines, 0, null!));
    }

    [Fact]
    public void FindPosition_ThrowsOnNegativeStartPosition()
    {
        var lines = new[] { "alpha" };
        var block = new DiffBlock();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _matcher.FindPosition(lines, -1, block));
    }

    [Fact]
    public void Reset_ClearsPreviousMatchState()
    {
        var lines = new[] { "a", "b", "c", "d", "e" };

        var block1 = new DiffBlock();
        block1.BeforeContext.Add("a");
        block1.Removals.Add("b");
        _matcher.FindPosition(lines, 0, block1);

        _matcher.Reset();

        var block2 = new DiffBlock();
        block2.BeforeContext.Add("a");
        block2.Removals.Add("b");
        int position = _matcher.FindPosition(lines, 0, block2);
        Assert.Equal(0, position);
    }

    [Fact]
    public void FindPosition_NoContextNoRemovals_ReturnsStartPosition()
    {
        var lines = new[] { "alpha", "beta" };
        var block = new DiffBlock();
        block.Additions.Add("new line");

        int position = _matcher.FindPosition(lines, 0, block);

        Assert.Equal(0, position);
    }

    [Fact]
    public void FindPosition_StartPositionBeyondLength_ThrowsArgumentOutOfRange()
    {
        var lines = new[] { "alpha" };
        var block = new DiffBlock();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _matcher.FindPosition(lines, 5, block));
    }
}
