namespace TextDiff;

public static class DiffBlockExtensions
{
    public static bool HasChanges(this DiffBlock block) =>
        block.TargetLines.Any() || block.InsertLines.Any();
}