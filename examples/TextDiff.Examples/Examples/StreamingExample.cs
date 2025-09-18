using TextDiff;

namespace TextDiff.Examples;

public static class StreamingExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Streaming Processing Example ===");
        Console.WriteLine("Creating large test files for streaming demonstration...");

        // Create temporary large files for demonstration
        var docPath = Path.GetTempFileName();
        var diffPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        try
        {
            // Generate large test files
            await CreateLargeTestFile(docPath, 10000);
            await CreateSampleDiffFile(diffPath);

            Console.WriteLine($"Created test document: {new FileInfo(docPath).Length / 1024}KB");
            Console.WriteLine($"Processing with streaming...");

            var differ = new TextDiffer();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Stream processing
            using var documentStream = new FileStream(docPath, FileMode.Open, FileAccess.Read);
            using var diffStream = new FileStream(diffPath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            var result = await differ.ProcessStreamsAsync(documentStream, diffStream, outputStream);

            stopwatch.Stop();

            Console.WriteLine($"✓ Streaming completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Output file size: {new FileInfo(outputPath).Length / 1024}KB");
            Console.WriteLine($"Changes applied: {result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines}");
        }
        finally
        {
            // Cleanup
            File.Delete(docPath);
            File.Delete(diffPath);
            File.Delete(outputPath);
        }
    }

    private static async Task CreateLargeTestFile(string path, int lines)
    {
        using var writer = new StreamWriter(path);
        for (int i = 1; i <= lines; i++)
        {
            await writer.WriteLineAsync($"Line {i}: Sample content for streaming test with some additional text to make it larger");
        }
    }

    private static async Task CreateSampleDiffFile(string path)
    {
        var diff = @" Line 1: Sample content for streaming test with some additional text to make it larger
-Line 2: Sample content for streaming test with some additional text to make it larger
+Line 2: MODIFIED content for streaming test with some additional text to make it larger
 Line 3: Sample content for streaming test with some additional text to make it larger";

        await File.WriteAllTextAsync(path, diff);
    }
}