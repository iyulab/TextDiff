namespace TextDiff.Helpers;

public static class DiffLineHelper
{
    public static bool IsValidDiffLine(char firstChar) => firstChar is ' ' or '+' or '-';

    public static string ExtractContent(string line)
    {
        if (line.Length <= 1) return string.Empty;

        // In unified diff format the prefix is exactly one character (space/+/-).
        // Strip only that single prefix character to preserve the verbatim content.
        return line.Substring(1);
    }
}