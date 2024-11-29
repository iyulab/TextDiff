namespace TextDiff;

public class ProcessResult
{
    public string Text { get; set; } = string.Empty;
    public DocumentChangeResult Changes { get; set; } = new DocumentChangeResult();
}
