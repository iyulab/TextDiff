namespace TextDiff;

public static class WhitespaceHelper
{
    public static string TrimWhitespace(string text) =>
        text.Trim().TrimStart(' ', '\t');

    public static string ExtractLeadingWhitespace(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
        {
            i++;
        }
        return line.Substring(0, i);
    }
}
