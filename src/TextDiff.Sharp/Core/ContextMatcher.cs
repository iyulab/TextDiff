namespace TextDiff.Core;

public class ContextMatcher : IContextMatcher
{
    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        if (!block.BeforeContext.Any())
            return startPosition;

        // Find first matching position after startPosition
        for (int i = startPosition; i < documentLines.Length; i++)
        {
            if (IsMatchingPosition(documentLines, i, block))
            {
                // For lines that appear multiple times, prefer later matches
                // but only if we're not at document boundaries
                int bestMatch = FindBestMatch(documentLines, i, block);
                if (bestMatch >= startPosition)
                    return bestMatch;
            }
        }

        // If we get here and couldn't find a match, it's an error
        throw new InvalidOperationException(
            $"Cannot find matching position for block: {block}");
    }

    private bool IsMatchingPosition(string[] documentLines, int position, DiffBlock block)
    {
        // Check if we have enough lines
        if (position + block.BeforeContext.Count + block.Removals.Count > documentLines.Length)
            return false;

        // Verify the context matches
        for (int i = 0; i < block.BeforeContext.Count; i++)
        {
            if (!TextUtils.LinesMatch(documentLines[position + i], block.BeforeContext[i]))
                return false;
        }

        // Verify the removals match
        int removalPos = position + block.BeforeContext.Count;
        for (int i = 0; i < block.Removals.Count; i++)
        {
            if (!TextUtils.LinesMatch(documentLines[removalPos + i], block.Removals[i]))
                return false;
        }

        return true;
    }

    private int FindBestMatch(string[] documentLines, int firstMatch, DiffBlock block)
    {
        int bestMatch = firstMatch;
        int nextPosition = firstMatch + 1;
        int maxPosition = documentLines.Length - (block.BeforeContext.Count + block.Removals.Count);

        // Look for later matches that preserve more context
        while (nextPosition <= maxPosition)
        {
            if (IsMatchingPosition(documentLines, nextPosition, block))
            {
                // Check if this position would be better
                if (IsBetterMatch(documentLines, nextPosition, bestMatch, block))
                {
                    bestMatch = nextPosition;
                }
            }
            nextPosition++;
        }

        return bestMatch;
    }

    private bool IsBetterMatch(string[] documentLines, int newPos, int currentPos, DiffBlock block)
    {
        // Calculate how many lines after the block match with the document
        int newMatchingLines = CountMatchingLinesAfter(documentLines, newPos, block);
        int currentMatchingLines = CountMatchingLinesAfter(documentLines, currentPos, block);

        // If new position matches more lines after, it's better
        if (newMatchingLines > currentMatchingLines)
            return true;

        // If they match the same number of lines but new position has more context before,
        // it's better
        if (newMatchingLines == currentMatchingLines)
        {
            int newContextBefore = CountMatchingLinesBefore(documentLines, newPos);
            int currentContextBefore = CountMatchingLinesBefore(documentLines, currentPos);
            return newContextBefore > currentContextBefore;
        }

        return false;
    }

    private int CountMatchingLinesAfter(string[] documentLines, int position, DiffBlock block)
    {
        int startPos = position + block.BeforeContext.Count + block.Removals.Count;
        int count = 0;

        while (startPos + count < documentLines.Length &&
               count < block.AfterContext.Count &&
               TextUtils.LinesMatch(documentLines[startPos + count], block.AfterContext[count]))
        {
            count++;
        }

        return count;
    }

    private int CountMatchingLinesBefore(string[] documentLines, int position)
    {
        int count = 0;
        int currentPos = position - 1;

        while (currentPos >= 0 && count < 3)  // Check up to 3 lines before
        {
            count++;
            currentPos--;
        }

        return count;
    }
}