using TextDiff;
using TextDiff.Models;

namespace TextDiff.Examples;

public static class BasicDiffExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Basic Diff Processing Example ===");
        Console.WriteLine();

        // Load sample files
        string originalDocument = await File.ReadAllTextAsync("SampleFiles/sample_document.txt");
        string diffContent = await File.ReadAllTextAsync("SampleFiles/sample_diff.txt");

        Console.WriteLine("Original Document:");
        Console.WriteLine(originalDocument);
        Console.WriteLine();

        Console.WriteLine("Diff Content:");
        Console.WriteLine(diffContent);
        Console.WriteLine();

        // Create differ and process
        var differ = new TextDiffer();
        ProcessResult result = differ.Process(originalDocument, diffContent);

        Console.WriteLine("Updated Document:");
        Console.WriteLine(result.Text);
        Console.WriteLine();

        // Display change statistics
        Console.WriteLine("Change Statistics:");
        Console.WriteLine($"  Lines Added: {result.Changes.AddedLines}");
        Console.WriteLine($"  Lines Deleted: {result.Changes.DeletedLines}");
        Console.WriteLine($"  Lines Modified: {result.Changes.ChangedLines}");
        Console.WriteLine($"  Total Changes: {result.Changes.AddedLines + result.Changes.DeletedLines + result.Changes.ChangedLines}");
        Console.WriteLine();

        // Save result to file
        await File.WriteAllTextAsync("SampleFiles/updated_document.txt", result.Text);
        Console.WriteLine("✓ Updated document saved to SampleFiles/updated_document.txt");
    }
}