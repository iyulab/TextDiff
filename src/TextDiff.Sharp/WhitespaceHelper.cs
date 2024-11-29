using System.Text;

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

    /// <summary>
    /// 원본 라인의 들여쓰기를 보존하면서 새로운 내용으로 라인을 생성합니다.
    /// </summary>
    public static string PreserveIndentation(string originalLine, string newContent)
    {
        string indentation = ExtractLeadingWhitespace(originalLine);
        string trimmedNewContent = newContent.TrimStart(' ', '\t');
        return indentation + trimmedNewContent;
    }

    /// <summary>
    /// 주어진 라인들의 공통된 들여쓰기를 찾습니다.
    /// </summary>
    public static string FindCommonIndentation(IEnumerable<string> lines)
    {
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (!nonEmptyLines.Any()) return string.Empty;

        var indentations = nonEmptyLines.Select(ExtractLeadingWhitespace);

        // 가장 짧은 들여쓰기를 찾음
        int minLength = indentations.Min(i => i.Length);
        if (minLength == 0) return string.Empty;

        // 모든 라인의 공통된 들여쓰기 부분을 찾음
        var commonIndent = new StringBuilder();
        for (int i = 0; i < minLength; i++)
        {
            char current = indentations.First()[i];
            if (indentations.All(indent => indent[i] == current))
            {
                commonIndent.Append(current);
            }
            else break;
        }

        return commonIndent.ToString();
    }
}