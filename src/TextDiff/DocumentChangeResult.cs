namespace TextDiff;

public class DocumentChangeResult
{
    public int DeletedLines { get; set; }
    public int ChangedLines { get; set; }
    public int AddedLines { get; set; }
}
