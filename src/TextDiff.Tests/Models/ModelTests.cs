using TextDiff.Core;
using TextDiff.Models;

namespace TextDiff.Tests.Models;

public class ChangeStatsTests
{
    [Fact]
    public void TotalAffectedLines_SumsAllCategories()
    {
        var stats = new ChangeStats { ChangedLines = 2, AddedLines = 3, DeletedLines = 1 };
        Assert.Equal(6, stats.TotalAffectedLines);
    }

    [Fact]
    public void TotalAffectedLines_AllZero_ReturnsZero()
    {
        var stats = new ChangeStats();
        Assert.Equal(0, stats.TotalAffectedLines);
    }

    [Fact]
    public void NetLineChange_Positive_WhenMoreAdded()
    {
        var stats = new ChangeStats { AddedLines = 5, DeletedLines = 2 };
        Assert.Equal(3, stats.NetLineChange);
    }

    [Fact]
    public void NetLineChange_Negative_WhenMoreDeleted()
    {
        var stats = new ChangeStats { AddedLines = 1, DeletedLines = 4 };
        Assert.Equal(-3, stats.NetLineChange);
    }

    [Fact]
    public void NetLineChange_Zero_WhenBalanced()
    {
        var stats = new ChangeStats { AddedLines = 3, DeletedLines = 3 };
        Assert.Equal(0, stats.NetLineChange);
    }
}

public class OptimizedLineBufferTests
{
    [Fact]
    public void AddLine_Single_ProducesCorrectOutput()
    {
        var buf = new OptimizedLineBuffer();
        buf.AddLine("hello");
        Assert.Equal("hello", buf.ToString());
    }

    [Fact]
    public void AddLine_Multiple_JoinedWithSeparator()
    {
        var buf = new OptimizedLineBuffer(lineSeparator: "\n");
        buf.AddLine("a");
        buf.AddLine("b");
        buf.AddLine("c");
        Assert.Equal("a\nb\nc", buf.ToString());
    }

    [Fact]
    public void AddLines_AddsAllLines()
    {
        var buf = new OptimizedLineBuffer(lineSeparator: "\n");
        buf.AddLines(new[] { "x", "y", "z" });
        Assert.Equal("x\ny\nz", buf.ToString());
    }

    [Fact]
    public void EstimatedLength_ReflectsBufferSize()
    {
        var buf = new OptimizedLineBuffer();
        buf.AddLine("hello");
        Assert.True(buf.EstimatedLength > 0);
    }

    [Fact]
    public void Clear_ResetsBuffer()
    {
        var buf = new OptimizedLineBuffer(lineSeparator: "\n");
        buf.AddLine("data");
        buf.Clear();
        Assert.Equal(string.Empty, buf.ToString());
        Assert.Equal(0, buf.EstimatedLength);
        // Adding after clear works correctly
        buf.AddLine("new");
        Assert.Equal("new", buf.ToString());
    }
}

public class ProcessingProgressTests
{
    [Fact]
    public void PercentComplete_WithItems_CalculatesCorrectly()
    {
        var progress = new ProcessingProgress("stage", 50, 200);
        Assert.Equal(25.0, progress.PercentComplete);
    }

    [Fact]
    public void PercentComplete_WhenTotalIsZero_ReturnsZero()
    {
        var progress = new ProcessingProgress("stage", 0, 0);
        Assert.Equal(0.0, progress.PercentComplete);
    }

    [Fact]
    public void Properties_StoredCorrectly()
    {
        var progress = new ProcessingProgress("Processing blocks", 10, 100);
        Assert.Equal("Processing blocks", progress.Stage);
        Assert.Equal(10, progress.ProcessedItems);
        Assert.Equal(100, progress.TotalItems);
    }
}
