namespace TextDiff.Tests.TestData;

public class PerformanceTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_SmallDocument_CompletesQuickly()
    {
        // Arrange
        string document = "line1\nline2\nline3";
        string diff = " line1\n- line2\n+ modified line2\n line3";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 10, $"Small document processing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Process_MediumDocument_CompletesWithinReasonableTime()
    {
        // Arrange
        var lines = Enumerable.Range(1, 1000).Select(i => $"Line {i} with some content");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with some content\n- Line 2 with some content\n+ Line 2 modified with some content\n Line 3 with some content";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Medium document processing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Process_LargeDocument_CompletesWithinAcceptableTime()
    {
        // Arrange
        var lines = Enumerable.Range(1, 50000).Select(i => $"This is line number {i} with some additional content to make it more realistic");
        string document = string.Join("\n", lines);
        string diff = " This is line number 1 with some additional content to make it more realistic\n- This is line number 2 with some additional content to make it more realistic\n+ This is line number 2 MODIFIED with some additional content to make it more realistic\n This is line number 3 with some additional content to make it more realistic";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("MODIFIED", result.Text);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Large document processing took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
    }

    [Fact]
    public void Process_ComplexDiff_HandlesMultipleBlocks()
    {
        // Arrange
        var lines = Enumerable.Range(1, 1000).Select(i => $"Line {i}");
        string document = string.Join("\n", lines);

        string diff = @" Line 1
- Line 2
+ Modified Line 2
 Line 3
 Line 4
 Line 5
- Line 6
+ Modified Line 6
 Line 7
 Line 8
 Line 9
- Line 10
+ Modified Line 10
 Line 11";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Modified Line 2", result.Text);
        Assert.Contains("Modified Line 6", result.Text);
        Assert.Contains("Modified Line 10", result.Text);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Complex diff processing took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Process_MemoryUsage_RemainsReasonable()
    {
        // Arrange
        var lines = Enumerable.Range(1, 10000).Select(i => $"Line {i} with content");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with content\n- Line 2 with content\n+ Modified Line 2 with content\n Line 3 with content";

        // Act
        long memoryBefore = GC.GetTotalMemory(true);
        var result = _differ.Process(document, diff);
        long memoryAfter = GC.GetTotalMemory(false);
        long memoryUsed = memoryAfter - memoryBefore;

        // Assert
        Assert.NotNull(result);
        // Memory usage should be reasonable (less than 50MB for this test)
        Assert.True(memoryUsed < 50 * 1024 * 1024, $"Memory usage was {memoryUsed / (1024 * 1024)}MB, expected < 50MB");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Process_ScalabilityTest_PerformanceScalesLinearly(int lineCount)
    {
        // Arrange
        var lines = Enumerable.Range(1, lineCount).Select(i => $"Content for line {i}");
        string document = string.Join("\n", lines);
        string diff = lineCount > 1 ?
            " Content for line 1\n- Content for line 2\n+ Modified content for line 2\n Content for line 3" :
            "+ Modified content for line 1";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);

        // Performance should scale roughly linearly with document size
        // Allow some flexibility for smaller documents due to overhead
        double expectedMaxTime = Math.Max(50, lineCount * 0.01); // 0.01ms per line minimum 50ms
        Assert.True(stopwatch.ElapsedMilliseconds < expectedMaxTime,
            $"Processing {lineCount} lines took {stopwatch.ElapsedMilliseconds}ms, expected < {expectedMaxTime}ms");
    }
}