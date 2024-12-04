namespace TextDiff.Models;

public class ChangeStats
{
    public int ChangedLines { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
}
