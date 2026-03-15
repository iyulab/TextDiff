namespace TextDiff.Helpers;

public static class DiffLineHelper
{
    public static bool IsValidDiffLine(char firstChar) => firstChar is ' ' or '+' or '-';

    public static bool IsHeaderLine(string line)
    {
        return line.StartsWith("---") || line.StartsWith("+++") ||
               line.StartsWith("diff ") || line.StartsWith("index ") ||
               line.StartsWith("@@") || line.Equals("...") ||
               line.StartsWith("\\") ||
               // Git extended headers
               line.StartsWith("old mode ") || line.StartsWith("new mode ") ||
               line.StartsWith("new file mode ") || line.StartsWith("deleted file mode ") ||
               line.StartsWith("similarity index ") || line.StartsWith("dissimilarity index ") ||
               line.StartsWith("rename from ") || line.StartsWith("rename to ") ||
               line.StartsWith("copy from ") || line.StartsWith("copy to ");
    }

    public static string ExtractContent(string line)
    {
        if (line.Length <= 1) return string.Empty;

        // In unified diff format the prefix is exactly one character (space/+/-).
        // Strip only that single prefix character to preserve the verbatim content.
        return line.Substring(1);
    }
}