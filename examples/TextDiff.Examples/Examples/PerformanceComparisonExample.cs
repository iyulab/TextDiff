using TextDiff;

namespace TextDiff.Examples;

public static class PerformanceComparisonExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Performance Comparison Example ===");
        Console.WriteLine();

        var testSizes = new[] { 100, 500, 1000, 2000 };
        var differ = new TextDiffer();

        Console.WriteLine("| Lines | Standard | Optimized | Async | Memory (KB) |");
        Console.WriteLine("|-------|----------|-----------|--------|-------------|");

        foreach (var size in testSizes)
        {
            var document = GenerateTestDocument(size);
            var diff = GenerateTestDiff(size);

            var standardTime = await MeasureTime(() => differ.Process(document, diff));
            var optimizedTime = await MeasureTime(() => differ.ProcessOptimized(document, diff));
            var asyncTime = await MeasureTime(async () => await differ.ProcessAsync(document, diff));

            var memoryUsage = MeasureMemory(() => differ.ProcessOptimized(document, diff));

            Console.WriteLine($"| {size,5} | {standardTime,8}ms | {optimizedTime,9}ms | {asyncTime,6}ms | {memoryUsage / 1024,9:F1} |");
        }

        Console.WriteLine();
        Console.WriteLine("✓ Performance comparison completed");
    }

    private static async Task<long> MeasureTime(Func<object> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    private static async Task<long> MeasureTime(Func<Task<object>> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    private static long MeasureMemory(Func<object> action)
    {
        var before = GC.GetTotalMemory(true);
        action();
        var after = GC.GetTotalMemory(false);
        return after - before;
    }

    private static string GenerateTestDocument(int lines)
    {
        return string.Join("\n", Enumerable.Range(1, lines).Select(i => $"Line {i}: Test content"));
    }

    private static string GenerateTestDiff(int baseLines)
    {
        var line = Math.Min(baseLines / 2, 10);
        return $" Line {line}: Test content\n-Line {line + 1}: Test content\n+Line {line + 1}: Modified test content\n Line {line + 2}: Test content";
    }
}