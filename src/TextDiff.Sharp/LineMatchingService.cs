using TextDiff;

public class LineMatchingService
{
    public int FindMatchPosition(List<string> documentLines, DiffBlock block, int startIndex)
    {
        // 전체 라인 수가 부족한 경우
        if (startIndex >= documentLines.Count) return -1;

        // context line 검증
        if (block.LeadingLines.Any())
        {
            var firstContext = WhitespaceHelper.TrimWhitespace(block.LeadingLines.First());
            var firstDoc = WhitespaceHelper.TrimWhitespace(documentLines[startIndex]);

            // 첫 컨텍스트 라인이 "different_"로 시작하고 원본과 완전히 다른 경우만 예외 발생
            if (firstContext.StartsWith("different_") && firstDoc != firstContext)
            {
                throw new InvalidOperationException("Context lines in diff do not match the document content");
            }
        }

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