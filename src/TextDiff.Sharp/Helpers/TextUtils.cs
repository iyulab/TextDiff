namespace TextDiff.Helpers;

/// <summary>
/// Provides utility methods for text processing, including indentation handling,
/// line splitting, and flexible text matching for diff operations.
/// </summary>
/// <remarks>
/// TextUtils contains a comprehensive set of static methods designed to support
/// diff processing operations. Key functionality includes:
/// - Indentation extraction and removal for consistent formatting
/// - Line splitting with proper handling of different line ending styles
/// - Context and content matching with various tolerance levels
/// - Whitespace normalization for robust text comparison
///
/// The utilities handle edge cases like:
/// - Empty and null strings
/// - Mixed tab and space indentation
/// - Lines containing only whitespace
/// - Different line ending formats (CRLF, LF)
/// - Variations in whitespace formatting
///
/// These methods are optimized for performance and memory efficiency,
/// making them suitable for processing large documents and frequent operations.
/// </remarks>
public static class TextUtils
{

    /// <summary>
    /// Removes leading whitespace (indentation) from a text line.
    /// </summary>
    /// <param name="line">The line to remove indentation from.</param>
    /// <returns>
    /// The line with all leading whitespace removed, or the original line
    /// if it contains no leading whitespace or is empty.
    /// </returns>
    /// <remarks>
    /// This method removes all leading whitespace characters (spaces, tabs, etc.)
    /// from the beginning of a line. If the entire line consists of whitespace,
    /// the original line is returned unchanged to preserve the whitespace-only
    /// line structure.
    /// </remarks>
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

    /// <summary>
    /// Extracts the leading whitespace (indentation) from a text line.
    /// </summary>
    /// <param name="line">The line to extract indentation from.</param>
    /// <returns>
    /// A string containing only the leading whitespace characters,
    /// or an empty string if the line has no leading whitespace.
    /// </returns>
    /// <remarks>
    /// This method captures the indentation pattern (spaces, tabs, etc.)
    /// from the beginning of a line. This is useful for preserving or
    /// transferring indentation patterns between lines during diff processing.
    /// </remarks>
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

    /// <summary>
    /// Splits text into an array of lines, handling different line ending formats.
    /// </summary>
    /// <param name="text">The text to split into lines.</param>
    /// <returns>
    /// An array of strings representing the individual lines,
    /// or an empty array if the input text is null or empty.
    /// </returns>
    /// <remarks>
    /// This method handles both Windows (CRLF) and Unix (LF) line endings,
    /// ensuring consistent line splitting regardless of the source platform.
    /// Empty lines are preserved in the result array to maintain the
    /// original document structure.
    /// </remarks>
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

    /// <summary>
    /// Determines whether two lines match using normalized whitespace comparison.
    /// </summary>
    /// <param name="line1">The first line to compare.</param>
    /// <param name="line2">The second line to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the lines match after whitespace normalization;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method provides flexible line matching by:
    /// 1. Trimming leading and trailing whitespace from both lines
    /// 2. Normalizing consecutive whitespace characters to single spaces
    /// 3. Comparing the normalized content for equality
    ///
    /// This approach handles minor formatting differences while ensuring
    /// that meaningful content changes are detected accurately.
    /// </remarks>
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

    /// <summary>
    /// Normalizes whitespace in a string by converting consecutive whitespace characters to single spaces.
    /// </summary>
    /// <param name="input">The input string to normalize.</param>
    /// <returns>A string with normalized whitespace.</returns>
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