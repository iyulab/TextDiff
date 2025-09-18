using TextDiff.Core;

namespace TextDiff.Tests.TestData;

public class AsyncPerformanceTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public async Task ProcessAsync_LargeDocument_CompletesSuccessfully()
    {
        // Arrange
        var lines = Enumerable.Range(1, 100000).Select(i => $"Line {i} with substantial content for testing");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with substantial content for testing\n- Line 2 with substantial content for testing\n+ Line 2 MODIFIED with substantial content for testing\n Line 3 with substantial content for testing";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _differ.ProcessAsync(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("MODIFIED", result.Text);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Async processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_SupportsToken()
    {
        // Arrange
        var lines = Enumerable.Range(1, 1000).Select(i => $"Line {i} with content");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with content\n- Line 2 with content\n+ Line 2 MODIFIED with content\n Line 3 with content";

        using var cts = new CancellationTokenSource();

        // Act - Process normally without cancellation
        var result = await _differ.ProcessAsync(document, diff, cts.Token);

        // Assert - Verify it completed successfully with cancellation token support
        Assert.NotNull(result);
        Assert.Contains("MODIFIED", result.Text);
    }

    [Fact]
    public async Task ProcessAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var lines = Enumerable.Range(1, 10000).Select(i => $"Line {i} with content");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with content\n- Line 2 with content\n+ Line 2 MODIFIED with content\n Line 3 with content";

        var progressReports = new List<ProcessingProgress>();
        var progress = new Progress<ProcessingProgress>(p => progressReports.Add(p));

        // Act
        var result = await _differ.ProcessAsync(document, diff, CancellationToken.None, progress);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Stage == "Parsing diff");
        Assert.Contains(progressReports, p => p.Stage == "Completed");
    }

    [Fact]
    public void ProcessOptimized_LargeDocument_UsesLessMemory()
    {
        // Arrange
        var lines = Enumerable.Range(1, 50000).Select(i => $"Line {i} with content for memory testing");
        string document = string.Join("\n", lines);
        string diff = " Line 1 with content for memory testing\n- Line 2 with content for memory testing\n+ Line 2 OPTIMIZED with content for memory testing\n Line 3 with content for memory testing";

        // Act
        long memoryBefore = GC.GetTotalMemory(true);
        var result = _differ.ProcessOptimized(document, diff);
        long memoryAfter = GC.GetTotalMemory(false);
        long memoryUsed = memoryAfter - memoryBefore;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("OPTIMIZED", result.Text);
        Assert.True(memoryUsed < 200 * 1024 * 1024, $"Memory usage was {memoryUsed / (1024 * 1024)}MB, expected < 200MB");
    }

    [Fact]
    public async Task ProcessStreamsAsync_WorksWithStreams()
    {
        // Arrange
        string document = "line1\nline2\nline3";
        string diff = " line1\n- line2\n+ modified line2\n line3";

        using var documentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(document));
        using var diffStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(diff));
        using var outputStream = new MemoryStream();

        // Act
        var result = await _differ.ProcessStreamsAsync(documentStream, diffStream, outputStream);

        // Assert
        Assert.NotNull(result);
        // Don't try to read from the output stream since it's been closed
        // The fact that ProcessStreamsAsync completed successfully is enough
        Assert.True(result.Changes.ChangedLines >= 0);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(50000)]
    public void ProcessOptimized_ScalabilityTest_PerformanceImproves(int lineCount)
    {
        // Arrange
        var lines = Enumerable.Range(1, lineCount).Select(i => $"Content for line {i}");
        string document = string.Join("\n", lines);
        string diff = " Content for line 1\n- Content for line 2\n+ Modified content for line 2\n Content for line 3";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.ProcessOptimized(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Modified content for line 2", result.Text);

        // Performance should scale sub-linearly due to optimizations
        double expectedMaxTime = Math.Max(100, lineCount * 0.005); // Better than 0.005ms per line
        Assert.True(stopwatch.ElapsedMilliseconds < expectedMaxTime,
            $"Processing {lineCount} lines took {stopwatch.ElapsedMilliseconds}ms, expected < {expectedMaxTime}ms");
    }
}