namespace TextDiff.Helpers;

/*
의도:
1. 들여쓰기 처리를 위한 유틸리티 메서드 제공
2. 일관된 들여쓰기 추출 및 제거 로직 제공

유의사항:
1. 빈 문자열 처리 주의
2. 탭과 스페이스 모두 고려
3. 전체 라인이 공백인 경우 처리
*/

public static class TextUtils
{

    public static string RemoveIndentation(string line)
    {
        if (string.IsNullOrEmpty(line))
            return line;

        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i]))
        {
            i++;
        }
        return i < line.Length ? line.Substring(i) : line;
    }

    public static string ExtractIndentation(string line)
    {
        if (string.IsNullOrEmpty(line))
            return string.Empty;

        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i]))
        {
            i++;
        }
        return i > 0 ? line.Substring(0, i) : string.Empty;
    }

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
        // 1. 양쪽 끝의 공백 제거
        string trimmedLine1 = line1.Trim();
        string trimmedLine2 = line2.Trim();

        // 2. 연속된 공백을 단일 공백으로 치환
        trimmedLine1 = NormalizeWhitespace(trimmedLine1);
        trimmedLine2 = NormalizeWhitespace(trimmedLine2);

        return trimmedLine1 == trimmedLine2;
    }

    private static string NormalizeWhitespace(string input)
    {
        return string.Join(" ", input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool LinesMatchIgnoreIndentation(string line1, string line2)
    {
        // 들여쓰기를 제거하고 비교
        string trimmed1 = line1.TrimStart();
        string trimmed2 = line2.TrimStart();

        // 양쪽 끝의 공백 제거
        trimmed1 = trimmed1.Trim();
        trimmed2 = trimmed2.Trim();

        // 연속된 공백을 단일 공백으로 치환
        trimmed1 = NormalizeWhitespace(trimmed1);
        trimmed2 = NormalizeWhitespace(trimmed2);

        return trimmed1 == trimmed2;
    }

    public static int GetRelativeIndentation(string baseLine, string newLine)
    {
        int baseIndent = baseLine.TakeWhile(char.IsWhiteSpace).Count();
        int newIndent = newLine.TakeWhile(char.IsWhiteSpace).Count();

        // 상대적 들여쓰기 계산 (최소 0 반환)
        return Math.Max(0, newIndent - baseIndent);
    }

    public static bool LinesMatchProgressive(string line1, string line2)
    {
        // 삭제/추가 라인에 대해서는 기존의 유연한 매칭 유지
        return HasTextSimilarity(line1, line2);
    }

    public static bool HasAnySimilarity(string line1, string line2)
    {
        // 1. 정확히 일치
        if (line1 == line2)
            return true;

        // 2. 앞쪽 공백 제거 후 일치
        if (line1.TrimStart() == line2.TrimStart())
            return true;

        // 3. 모든 공백 제거 후 일치
        if (line1.Trim() == line2.Trim())
            return true;

        // 모든 비교에서 실패하면 false
        return false;
    }

    public static bool HasTextSimilarity(string line1, string line2)
    {
        if (string.IsNullOrWhiteSpace(line1) && string.IsNullOrWhiteSpace(line2))
            return true;

        // 1. 정확히 일치
        if (line1 == line2)
            return true;

        // 2. 앞쪽 공백만 다른 경우
        if (line1.TrimStart() == line2.TrimStart())
            return true;

        // 3. 모든 공백이 다른 경우
        if (line1.Trim() == line2.Trim())
            return true;

        return false;
    }
}