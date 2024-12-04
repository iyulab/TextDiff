namespace TextDiff.Core;

public interface IDiffBlockParser
{
    IEnumerable<DiffBlock> Parse(string[] diffLines);
}