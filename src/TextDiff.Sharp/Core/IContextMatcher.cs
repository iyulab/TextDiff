namespace TextDiff.Core;

public interface IContextMatcher
{
    int FindPosition(string[] documentLines, int startPosition, DiffBlock block);
}
