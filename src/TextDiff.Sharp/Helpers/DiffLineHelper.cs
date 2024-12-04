namespace TextDiff.Helpers;

public static class DiffLineHelper
{
    public static bool IsValidDiffLine(char firstChar) => firstChar is ' ' or '+' or '-';

    public static string ExtractContent(string line)
    {
        if (line.Length <= 1) return string.Empty;

        // 첫 문자가 공백/+/-이고 그 다음이 공백인 경우 둘 다 제거
        if (line.Length > 2 && line[1] == ' ')
        {
            return line.Substring(2);
        }
        // 그 외의 경우 첫 문자만 제거
        return line.Substring(1);
    }
}