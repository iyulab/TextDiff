using System.Text;

namespace TextDiff.Helpers;

/// <summary>
/// Memory-efficient text processing utilities with reduced allocations.
/// </summary>
public static class MemoryEfficientTextUtils
{
    /// <summary>
    /// Split text into lines with minimal memory allocations using ReadOnlySpan.
    /// </summary>
    public static string[] SplitLinesEfficient(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        var span = text.AsSpan();
        var lines = new List<string>();

        int start = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '\n')
            {
                int end = i;
                // Handle \r\n
                if (end > 0 && span[end - 1] == '\r')
                    end--;

                lines.Add(span.Slice(start, end - start).ToString());
                start = i + 1;
            }
        }

        // Add remaining content
        if (start < span.Length)
        {
            lines.Add(span.Slice(start).ToString());
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Extract indentation with minimal string allocations.
    /// </summary>
    public static string ExtractIndentationEfficient(ReadOnlySpan<char> line)
    {
        int indentLength = 0;
        while (indentLength < line.Length && char.IsWhiteSpace(line[indentLength]))
        {
            indentLength++;
        }

        return indentLength > 0 ? line.Slice(0, indentLength).ToString() : string.Empty;
    }

    /// <summary>
    /// Remove indentation with minimal string allocations.
    /// </summary>
    public static string RemoveIndentationEfficient(ReadOnlySpan<char> line)
    {
        int start = 0;
        while (start < line.Length && char.IsWhiteSpace(line[start]))
        {
            start++;
        }

        return start < line.Length ? line.Slice(start).ToString() : string.Empty;
    }

    /// <summary>
    /// Memory-efficient line matching using ReadOnlySpan.
    /// </summary>
    public static bool LinesMatchEfficient(ReadOnlySpan<char> line1, ReadOnlySpan<char> line2)
    {
        // Quick reference equality check
        if (line1.SequenceEqual(line2))
            return true;

        // Trim whitespace from both sides
        var trimmed1 = line1.Trim();
        var trimmed2 = line2.Trim();

        if (trimmed1.SequenceEqual(trimmed2))
            return true;

        // Normalize whitespace for comparison
        return NormalizeAndCompare(trimmed1, trimmed2);
    }

    private static bool NormalizeAndCompare(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2)
    {
        // Use stack-allocated buffers for small strings
        Span<char> buffer1 = stackalloc char[Math.Min(span1.Length, 256)];
        Span<char> buffer2 = stackalloc char[Math.Min(span2.Length, 256)];

        if (span1.Length > 256 || span2.Length > 256)
        {
            // Fall back to heap allocation for very long lines
            return NormalizeWhitespaceString(span1.ToString()).Equals(
                   NormalizeWhitespaceString(span2.ToString()));
        }

        int len1 = NormalizeWhitespace(span1, buffer1);
        int len2 = NormalizeWhitespace(span2, buffer2);

        if (len1 != len2) return false;

        return buffer1.Slice(0, len1).SequenceEqual(buffer2.Slice(0, len2));
    }

    private static int NormalizeWhitespace(ReadOnlySpan<char> input, Span<char> output)
    {
        int writeIndex = 0;
        bool lastWasSpace = false;

        for (int i = 0; i < input.Length && writeIndex < output.Length; i++)
        {
            char c = input[i];
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    output[writeIndex++] = ' ';
                    lastWasSpace = true;
                }
            }
            else
            {
                output[writeIndex++] = c;
                lastWasSpace = false;
            }
        }

        return writeIndex;
    }

    private static string NormalizeWhitespaceString(string input)
    {
        return string.Join(" ", input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Estimate memory usage for a document processing operation.
    /// </summary>
    public static long EstimateMemoryUsage(int documentLineCount, int averageLineLength, int diffBlockCount)
    {
        // Original document lines
        long documentMemory = documentLineCount * (averageLineLength + 40); // string overhead

        // Result buffer (worst case: document + additions)
        long resultMemory = (long)(documentMemory * 1.5);

        // Diff blocks and processing overhead
        long processMemory = diffBlockCount * 1024;

        return (long)(documentMemory + resultMemory + processMemory);
    }
}