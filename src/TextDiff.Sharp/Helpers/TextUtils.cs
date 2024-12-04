namespace TextDiff.Helpers;

public static class TextUtils
{
    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    public static bool MatchContextLines(string[] documentLines, int position, List<string> contextLines)
    {
        for (int i = 0; i < contextLines.Count; i++)
        {
            if (position + i >= documentLines.Length ||
                !LinesMatch(documentLines[position + i], contextLines[i]))
            {
                return false;
            }
        }
        return true;
    }

    public static bool MatchRemovalLines(string[] documentLines, int position, List<string> removalLines)
    {
        for (int i = 0; i < removalLines.Count; i++)
        {
            if (position + i >= documentLines.Length ||
                !LinesMatch(documentLines[position + i], removalLines[i]))
            {
                return false;
            }
        }
        return true;
    }

    public static bool LinesMatch(string line1, string line2)
    {
        return line1.TrimStart() == line2.TrimStart();
    }

    public static string ExtractIndentation(string line)
    {
        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i]))
        {
            i++;
        }
        return i > 0 ? line.Substring(0, i) : "";
    }
}
