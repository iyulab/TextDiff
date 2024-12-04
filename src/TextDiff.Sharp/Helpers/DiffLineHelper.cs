namespace TextDiff.Helpers;

public static class DiffLineHelper
{
    public static bool IsValidDiffLine(char firstChar) => firstChar is ' ' or '+' or '-';

    public static string ExtractContent(string line)
    {
        return line.Length > 1 ? line.Substring(line[1] == ' ' ? 2 : 1) : string.Empty;
    }
}
