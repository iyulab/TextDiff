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

    /// <summary>
    /// Applies indentation preservation logic for a paired change (removal + addition).
    /// Preserves the original line's indentation when the diff does not explicitly change
    /// the indentation style. Returns the addition line as-is when:
    /// - The original line is whitespace-only, or
    /// - The diff explicitly changes indentation (removal and addition have different indentation).
    /// </summary>
    public static string ApplyIndentationPreservation(string originalLine, string removalContent, string addedLine)
    {
        if (string.IsNullOrWhiteSpace(originalLine))
            return addedLine;

        string removalIndentation = ExtractIndentation(removalContent);
        string additionIndentation = ExtractIndentation(addedLine);

        if (removalIndentation != additionIndentation)
            return addedLine;

        string indentation = ExtractIndentation(originalLine);
        string newContent = RemoveIndentation(addedLine);
        return indentation + newContent;
    }

    /// <summary>
    /// Detects the line separator used in the given text.
    /// Returns "\r\n" for CRLF, "\n" for LF, or null when the text is too
    /// short to determine (single-line / empty).
    /// </summary>
    public static string? DetectLineSeparator(string text)
    {
        int idx = text.IndexOf('\n');
        if (idx < 0)
            return null;
        return idx > 0 && text[idx - 1] == '\r' ? "\r\n" : "\n";
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
        // 1. Trim whitespace from both ends
        string trimmedLine1 = line1.Trim();
        string trimmedLine2 = line2.Trim();

        // 2. Normalize consecutive whitespace to single spaces
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

    public static bool HasTextSimilarity(string line1, string line2)
    {
        if (string.IsNullOrWhiteSpace(line1) && string.IsNullOrWhiteSpace(line2))
            return true;

        // 1. Exact match
        if (line1 == line2)
            return true;

        // 2. Match with different leading whitespace only
        if (line1.TrimStart() == line2.TrimStart())
            return true;

        // 3. Match with different whitespace
        if (line1.Trim() == line2.Trim())
            return true;

        return false;
    }
}