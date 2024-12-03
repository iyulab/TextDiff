namespace TextDiff;

public class DocumentChange
{
    public int LineNumber { get; set; }
    public IReadOnlyList<string> LinesToRemove { get; set; } = new List<string>();
    public IReadOnlyList<string> LinesToInsert { get; set; } = new List<string>();
}
