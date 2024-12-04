namespace TextDiff.Models;

public class ProcessResult
{
    public string Text { get; set; }
    public ChangeStats Changes { get; set; }
}