namespace TextDiff.Helpers;

public static class DiffBlockHelper
{
    public static DiffBlock CreateNewBlockWithContext(string contextLine)
    {
        var block = new DiffBlock();
        block.BeforeContext.Add(contextLine);
        return block;
    }
}