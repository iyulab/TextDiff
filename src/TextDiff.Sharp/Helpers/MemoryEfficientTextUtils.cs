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