namespace TextDiff;

public class DiffLine
{
    public char Type { get; }  // ' ', '+', '-'
    public string Content { get; }
    public string RawContent { get; }
    public string Indentation { get; }

    public DiffLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            Type = ' ';
            Content = "";
            RawContent = "";
            Indentation = "";
            return;
        }

        Type = line[0];
        RawContent = line;

        if (line.Length > 1)
        {
            // Extract everything after the control character
            string remaining = line.Substring(1);

            // If the remaining starts with a space, remove it (diff format's space)
            if (remaining.StartsWith(" "))
            {
                remaining = remaining.Substring(1);
            }

            // Extract leading whitespace after the first space
            Indentation = WhitespaceHelper.ExtractLeadingWhitespace(remaining);

            // Get content without the control character, first space, and leading whitespace
            Content = remaining.TrimStart();
        }
        else
        {
            Content = "";
            Indentation = "";
        }
    }

    public bool IsContext => Type == ' ';
    public bool IsAddition => Type == '+';
    public bool IsRemoval => Type == '-';
    public bool IsEmpty => string.IsNullOrEmpty(RawContent);
}

public static class DiffLineExtensions
{
    public static bool HasNextContextLine(this List<DiffLine> lines, int currentIndex)
    {
        return currentIndex < lines.Count - 1 && lines[currentIndex + 1].IsContext;
    }
}
