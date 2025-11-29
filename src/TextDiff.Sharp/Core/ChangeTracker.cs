using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Default implementation of change tracking for diff processing.
/// </summary>
public class ChangeTracker : IChangeTracker
{
    /// <inheritdoc />
    public void TrackChanges(DiffBlock block, ChangeStats stats)
    {
        // Count all added/removed lines (including empty lines)
        var minCount = Math.Min(block.Removals.Count, block.Additions.Count);
        stats.ChangedLines += minCount;
        stats.AddedLines += block.Additions.Count - minCount;
        stats.DeletedLines += block.Removals.Count - minCount;
    }
}