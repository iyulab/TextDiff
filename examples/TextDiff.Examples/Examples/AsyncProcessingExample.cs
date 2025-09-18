using TextDiff;
using TextDiff.Models;
using TextDiff.Core;

namespace TextDiff.Examples;

public static class AsyncProcessingExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Async Processing with Cancellation Example ===");
        Console.WriteLine();

        // Create a large document for demonstration
        var largeDocument = GenerateLargeDocument(1000);
        var diff = GenerateSampleDiff();

        Console.WriteLine($"Processing document with {largeDocument.Split('\n').Length} lines...");

        var differ = new TextDiffer();

        // Set up cancellation token with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Create progress reporter
        var progressReports = new List<ProcessingProgress>();
        var progress = new Progress<ProcessingProgress>(p =>
        {
            progressReports.Add(p);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {p.Stage}: {p.PercentComplete:F1}% ({p.ProcessedItems}/{p.TotalItems})");
        });

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Process asynchronously
            var result = await differ.ProcessAsync(
                largeDocument,
                diff,
                cts.Token,
                progress
            );

            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine("✓ Async processing completed successfully!");
            Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Progress reports: {progressReports.Count}");
            Console.WriteLine($"Changes applied: {result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines}");

            // Show progress stages
            var stages = progressReports.Select(p => p.Stage).Distinct().ToList();
            Console.WriteLine($"Processing stages: {string.Join(" → ", stages)}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ Processing was cancelled due to timeout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Processing failed: {ex.Message}");
        }
    }

    private static string GenerateLargeDocument(int lines)
    {
        var content = new List<string>();
        for (int i = 1; i <= lines; i++)
        {
            content.Add($"// Line {i}: This is sample content for testing large document processing");
            if (i % 10 == 0)
            {
                content.Add($"function sampleFunction{i}() {{");
                content.Add("    return 'sample code';");
                content.Add("}");
                content.Add("");
            }
        }
        return string.Join("\n", content);
    }

    private static string GenerateSampleDiff()
    {
        return @" // Line 1: This is sample content for testing large document processing
 // Line 2: This is sample content for testing large document processing
 // Line 3: This is sample content for testing large document processing
-// Line 4: This is sample content for testing large document processing
+// Line 4: This is UPDATED content for testing large document processing
 // Line 5: This is sample content for testing large document processing";
    }
}