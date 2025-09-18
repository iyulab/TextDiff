using TextDiff;
using TextDiff.Core;
using TextDiff.Models;

namespace TextDiff.Examples;

public static class CustomDependenciesExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Custom Dependencies Example ===");
        Console.WriteLine("This example would demonstrate custom implementations of:");
        Console.WriteLine("- IDiffBlockParser");
        Console.WriteLine("- IContextMatcher");
        Console.WriteLine("- IChangeTracker");
        Console.WriteLine();
        Console.WriteLine("For simplicity, using default implementations with logging wrapper...");

        var loggingTracker = new LoggingChangeTracker();
        var differ = new TextDiffer(changeTracker: loggingTracker);

        var document = "line 1\nline 2\nline 3";
        var diff = " line 1\n-line 2\n+line 2 modified\n line 3";

        var result = differ.Process(document, diff);

        Console.WriteLine($"✓ Processing completed");
        Console.WriteLine($"Change tracking log:");
        foreach (var entry in loggingTracker.Log)
        {
            Console.WriteLine($"  {entry}");
        }
    }

    private class LoggingChangeTracker : IChangeTracker
    {
        private readonly List<string> _log = new();
        private readonly IChangeTracker _innerTracker = new ChangeTracker();

        public IReadOnlyList<string> Log => _log;

        public void TrackChanges(DiffBlock block, ChangeStats stats)
        {
            _log.Add($"Tracking block: +{block.Additions.Count} -{block.Removals.Count} changes");
            _innerTracker.TrackChanges(block, stats);
            _log.Add($"Current totals: +{stats.AddedLines} -{stats.DeletedLines} ~{stats.ChangedLines}");
        }
    }
}