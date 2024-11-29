namespace TextDiff;

public class LineMatchingService
{
    public int FindMatchPosition(List<string> documentLines, DiffBlock block, int startIndex)
    {
        if (!block.TargetLines.Any())
        {
            return FindPositionForAdditionOnly(documentLines, block, startIndex);
        }

        return FindBestMatchPosition(documentLines, block, startIndex);
    }

    private int FindPositionForAdditionOnly(List<string> documentLines, DiffBlock block, int startIndex)
    {
        if (!block.LeadingLines.Any()) return startIndex;

        var position = FindExactLinePosition(documentLines, block.LeadingLines.Last(), startIndex);
        return position >= 0 ? position + 1 : startIndex;
    }

    private int FindBestMatchPosition(List<string> documentLines, DiffBlock block, int startIndex)
    {
        int bestScore = -1;
        int bestPosition = -1;

        for (int i = startIndex; i < documentLines.Count; i++)
        {
            var score = CalculateMatchScore(documentLines, block, i);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = i;
            }
            if (bestScore == 100) break;
        }

        return bestScore > 50 ? bestPosition : -1;
    }

    private int FindExactLinePosition(List<string> documentLines, string targetLine, int startIndex)
    {
        var normalizedTarget = WhitespaceHelper.TrimWhitespace(targetLine);

        for (int i = startIndex; i < documentLines.Count; i++)
        {
            var normalizedLine = WhitespaceHelper.TrimWhitespace(documentLines[i]);
            if (normalizedLine == normalizedTarget)
            {
                return i;
            }
        }
        return -1;
    }

    private int CalculateMatchScore(List<string> documentLines, DiffBlock block, int position)
    {
        var scorer = new BlockMatchScorer(documentLines, block, position);
        return scorer.Calculate();
    }
}
