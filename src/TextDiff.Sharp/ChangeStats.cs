namespace TextDiff;

public class ChangeStats
{
    private int _deletedLines;
    private int _changedLines;
    private int _addedLines;

    public void UpdateStats(DiffBlock block)
    {
        // Changed Lines: min(deleted, added)
        var changed = Math.Min(block.TargetLines.Count, block.InsertLines.Count);
        _changedLines += changed;

        // Added Lines: added - changed
        _addedLines += block.InsertLines.Count - changed;

        // Deleted Lines: deleted - changed
        _deletedLines += block.TargetLines.Count - changed;
    }

    public DocumentChangeResult ToResult() => new()
    {
        DeletedLines = _deletedLines,
        ChangedLines = _changedLines,
        AddedLines = _addedLines
    };
}
