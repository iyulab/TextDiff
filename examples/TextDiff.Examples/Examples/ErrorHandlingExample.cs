using TextDiff;
using TextDiff.Exceptions;
using TextDiff.Models;

namespace TextDiff.Examples;

public static class ErrorHandlingExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Error Handling Patterns Example ===");
        Console.WriteLine();

        var differ = new TextDiffer();

        // Example 1: Null input handling
        Console.WriteLine("1. Testing null input handling...");
        try
        {
            var result = differ.Process(null!, "some diff");
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"   ✓ Caught ArgumentNullException: {ex.ParamName}");
        }

        // Example 2: Empty diff handling
        Console.WriteLine("2. Testing empty diff handling...");
        try
        {
            var result = differ.Process("document", "   ");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   ✓ Caught ArgumentException: {ex.Message}");
        }

        // Example 3: Invalid diff format
        Console.WriteLine("3. Testing invalid diff format...");
        try
        {
            var invalidDiff = "This is not a valid diff format\nAnother invalid line";
            var result = differ.Process("document", invalidDiff);
        }
        catch (InvalidDiffFormatException ex)
        {
            Console.WriteLine($"   ✓ Caught InvalidDiffFormatException at line {ex.LineNumber}: {ex.Message}");
        }

        // Example 4: Diff application failure
        Console.WriteLine("4. Testing diff application failure...");
        try
        {
            var document = "line 1\nline 2\nline 3";
            var mismatchedDiff = @" line 1
-line 999
+line 2 modified
 line 3";
            var result = differ.Process(document, mismatchedDiff);
        }
        catch (DiffApplicationException ex)
        {
            Console.WriteLine($"   ✓ Caught DiffApplicationException: {ex.Message}");
        }

        // Example 5: Async cancellation
        Console.WriteLine("5. Testing async cancellation...");
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            var largeDoc = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i}"));
            var diff = " Line 1\n-Line 2\n+Line 2 modified\n Line 3";

            var result = await differ.ProcessAsync(largeDoc, diff, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   ✓ Caught OperationCanceledException: Processing was cancelled");
        }

        // Example 6: Comprehensive error handling pattern
        Console.WriteLine("6. Demonstrating comprehensive error handling...");
        await DemonstrateComprehensiveErrorHandling(differ);

        Console.WriteLine();
        Console.WriteLine("✓ All error handling patterns demonstrated successfully!");
    }

    private static async Task DemonstrateComprehensiveErrorHandling(TextDiffer differ)
    {
        var testCases = new[]
        {
            ("Valid case", "line 1\nline 2", " line 1\n-line 2\n+line 2 modified"),
            ("Null document", null!, "valid diff"),
            ("Invalid diff", "document", "invalid diff content"),
            ("Mismatched context", "line A\nline B", " line X\n-line Y\n+line Z")
        };

        foreach (var (name, document, diff) in testCases)
        {
            try
            {
                Console.WriteLine($"   Testing: {name}");
                var result = await ProcessWithRetry(differ, document, diff);
                if (result != null)
                {
                    Console.WriteLine($"     ✓ Success: Applied {result.Changes.ChangedLines + result.Changes.AddedLines + result.Changes.DeletedLines} changes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     ❌ Failed: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    private static async Task<ProcessResult?> ProcessWithRetry(TextDiffer differ, string document, string diff, int maxRetries = 2)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Try async first, fallback to sync
                if (attempt == 1)
                {
                    return await differ.ProcessAsync(document, diff);
                }
                else
                {
                    return differ.Process(document, diff);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"     Attempt {attempt}: Cancelled, retrying with sync...");
                continue;
            }
            catch (TextDiffException ex) when (attempt < maxRetries)
            {
                Console.WriteLine($"     Attempt {attempt}: {ex.GetType().Name}, retrying...");
                await Task.Delay(100); // Brief delay before retry
                continue;
            }
        }
        return null;
    }
}