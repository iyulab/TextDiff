namespace TextDiff.Core;

public class ChangeTracker : IChangeTracker
{
    public void TrackChanges(DiffBlock block, ChangeStats stats)
    {
        var minCount = Math.Min(block.Removals.Count, block.Additions.Count);
        stats.ChangedLines += minCount;
        stats.AddedLines += block.Additions.Count - minCount;
        stats.DeletedLines += block.Removals.Count - minCount;
    }
}
