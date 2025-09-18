using TextDiff;
using TextDiff.Models;
using TextDiff.Core;

namespace TextDiff.Examples;

public static class ProgressReportingExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Progress Reporting Example ===");
        Console.WriteLine();

        var document = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i} content"));
        var diff = " Line 1 content\n-Line 2 content\n+Line 2 UPDATED content\n Line 3 content";

        var progressHistory = new List<ProcessingProgress>();
        var progress = new Progress<ProcessingProgress>(p =>
        {
            progressHistory.Add(p);
            var bar = CreateProgressBar(p.PercentComplete);
            Console.WriteLine($"[{bar}] {p.Stage}: {p.PercentComplete:F1}% ({p.ProcessedItems}/{p.TotalItems})");
        });

        var differ = new TextDiffer();
        var result = await differ.ProcessAsync(document, diff, CancellationToken.None, progress);

        Console.WriteLine();
        Console.WriteLine($"✓ Processing completed with {progressHistory.Count} progress updates");
        Console.WriteLine($"Stages: {string.Join(" → ", progressHistory.Select(p => p.Stage).Distinct())}");
    }

    private static string CreateProgressBar(double percentage, int width = 20)
    {
        var filled = (int)(percentage / 100.0 * width);
        return new string('█', filled) + new string('░', width - filled);
    }
}