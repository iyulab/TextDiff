namespace TextDiff.Core;

public class ChangeTracker : IChangeTracker
{
    public void TrackChanges(DiffBlock block, ChangeStats stats)
    {
        // 추가/삭제된 모든 라인을 카운트 (빈 줄 포함)
        var minCount = Math.Min(block.Removals.Count, block.Additions.Count);
        stats.ChangedLines += minCount;
        stats.AddedLines += block.Additions.Count - minCount;
        stats.DeletedLines += block.Removals.Count - minCount;
    }
}