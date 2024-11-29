namespace TextDiff;

public class DiffLine
{
    public char Type { get; }  // ' ', '+', '-'
    public string Content { get; }
    public string RawContent { get; }

    public DiffLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            Type = ' ';
            Content = "";
            RawContent = "";
            return;
        }

        Type = line[0];
        RawContent = line;
        Content = line.Length > 1 ? line.Substring(1) : "";
    }

    public bool IsContext => Type == ' ';
    public bool IsAddition => Type == '+';
    public bool IsRemoval => Type == '-';
    public bool IsEmpty => string.IsNullOrEmpty(RawContent);

    public string GetNormalizedContent()
    {
        return Content.TrimStart(' ', '\t');
    }

    public string GetIndentation()
    {
        int i = 0;
        while (i < Content.Length && (Content[i] == ' ' || Content[i] == '\t'))
        {
            i++;
        }
        return Content.Substring(0, i);
    }
}

public static class DiffLineExtensions
{
    public static bool HasNextContextLine(this List<DiffLine> lines, int currentIndex)
    {
        return currentIndex < lines.Count - 1 && lines[currentIndex + 1].IsContext;
    }
}