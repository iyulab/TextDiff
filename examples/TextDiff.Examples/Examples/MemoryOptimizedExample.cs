using TextDiff;
using TextDiff.Helpers;
using TextDiff.Models;

namespace TextDiff.Examples;

public static class MemoryOptimizedExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Memory-Optimized Processing Example ===");
        Console.WriteLine();

        // Generate test data
        var largeDocument = GenerateLargeDocument(5000);
        var complexDiff = GenerateComplexDiff();

        Console.WriteLine($"Document size: {largeDocument.Length / 1024}KB ({largeDocument.Split('\n').Length} lines)");
        Console.WriteLine($"Diff size: {complexDiff.Length} bytes");
        Console.WriteLine();

        var differ = new TextDiffer();

        // Memory estimation
        var documentLines = MemoryEfficientTextUtils.SplitLinesEfficient(largeDocument);
        var averageLineLength = documentLines.Length > 0 ? largeDocument.Length / documentLines.Length : 0;
        var estimatedMemory = MemoryEfficientTextUtils.EstimateMemoryUsage(
            documentLines.Length,
            averageLineLength,
            10 // estimated diff blocks
        );

        Console.WriteLine($"Estimated memory usage: {estimatedMemory / 1024 / 1024:F2} MB");
        Console.WriteLine();

        // Compare processing methods
        await CompareProcessingMethods(differ, largeDocument, complexDiff);
    }

    private static async Task CompareProcessingMethods(TextDiffer differ, string document, string diff)
    {
        // Method 1: Standard processing
        Console.WriteLine("1. Standard Processing:");
        await MeasureProcessingSync("Standard", () => differ.Process(document, diff));

        // Method 2: Optimized processing
        Console.WriteLine("2. Memory-Optimized Processing:");
        await MeasureProcessingSync("Optimized", () => differ.ProcessOptimized(document, diff));

        // Method 3: Optimized with custom buffer size
        Console.WriteLine("3. Optimized with Custom Buffer:");
        await MeasureProcessingSync("Custom Buffer", () => differ.ProcessOptimized(document, diff, 16384));

        // Method 4: Async processing
        Console.WriteLine("4. Async Processing:");
        await MeasureProcessing("Async", () => differ.ProcessAsync(document, diff));
    }

    private static async Task MeasureProcessing(string method, Func<Task<ProcessResult>> processFunc)
    {
        try
        {
            var memoryBefore = GC.GetTotalMemory(true);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await processFunc();

            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            Console.WriteLine($"   {method}:");
            Console.WriteLine($"     Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"     Memory: {memoryUsed / 1024:F1}KB");
            Console.WriteLine($"     Changes: +{result.Changes.AddedLines} -{result.Changes.DeletedLines} ~{result.Changes.ChangedLines}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {method}: Failed - {ex.Message}");
            Console.WriteLine();
        }
    }

    private static async Task MeasureProcessingSync(string method, Func<ProcessResult> processFunc)
    {
        try
        {
            var memoryBefore = GC.GetTotalMemory(true);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = processFunc();

            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;

            Console.WriteLine($"   {method}:");
            Console.WriteLine($"     Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"     Memory: {memoryUsed / 1024:F1}KB");
            Console.WriteLine($"     Changes: +{result.Changes.AddedLines} -{result.Changes.DeletedLines} ~{result.Changes.ChangedLines}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {method}: Failed - {ex.Message}");
            Console.WriteLine();
        }
    }

    private static string GenerateLargeDocument(int lines)
    {
        var random = new Random(42); // Fixed seed for consistent results
        var content = new List<string>();

        for (int i = 1; i <= lines; i++)
        {
            var lineType = random.Next(4);
            switch (lineType)
            {
                case 0:
                    content.Add($"// Comment line {i}: This explains the code below");
                    break;
                case 1:
                    content.Add($"function processData{i}(input) {{");
                    content.Add($"    var result = transform(input);");
                    content.Add($"    return validate(result);");
                    content.Add($"}}");
                    content.Add("");
                    break;
                case 2:
                    content.Add($"var config{i} = {{");
                    content.Add($"    enabled: true,");
                    content.Add($"    timeout: {random.Next(1000, 5000)},");
                    content.Add($"    retries: {random.Next(1, 5)}");
                    content.Add($"}};");
                    content.Add("");
                    break;
                default:
                    content.Add($"console.log('Processing item {i} with data: ' + JSON.stringify(data));");
                    break;
            }
        }

        return string.Join("\n", content);
    }

    private static string GenerateComplexDiff()
    {
        return @" // Comment line 1: This explains the code below
-// Comment line 2: This explains the code below
+// Comment line 2: This explains the UPDATED code below
 // Comment line 3: This explains the code below
 function processData1(input) {
-    var result = transform(input);
+    var result = enhancedTransform(input);
+    var metadata = extractMetadata(input);
     return validate(result);
 }

 var config1 = {
     enabled: true,
-    timeout: 2000,
+    timeout: 5000,
     retries: 3
 };

+// New configuration section
+var advancedConfig = {
+    caching: true,
+    compression: 'gzip'
+};

 console.log('Processing item 1 with data: ' + JSON.stringify(data));";
    }
}