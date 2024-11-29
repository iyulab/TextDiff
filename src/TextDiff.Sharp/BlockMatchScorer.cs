namespace TextDiff;

public class BlockMatchScorer
{
    private readonly List<string> _documentLines;
    private readonly DiffBlock _block;
    private readonly int _position;
    private int _score;
    private int _maxScore;

    public BlockMatchScorer(List<string> documentLines, DiffBlock block, int position)
    {
        _documentLines = documentLines;
        _block = block;
        _position = position;
        _score = 0;
        _maxScore = 0;
    }

    public int Calculate()
    {
        ScoreLeadingLines();
        ScoreTargetLines();
        ScoreTrailingLines();

        return _maxScore > 0 ? (_score * 100) / _maxScore : 0;
    }

    private void ScoreLeadingLines()
    {
        for (int i = 0; i < _block.LeadingLines.Count; i++)
        {
            _maxScore += 100;
            int checkIndex = _position - _block.LeadingLines.Count + i;

            if (IsValidIndex(checkIndex))
            {
                _score += CalculateLineMatchScore(_documentLines[checkIndex], _block.LeadingLines[i]);
            }
        }
    }

    private void ScoreTargetLines()
    {
        for (int i = 0; i < _block.TargetLines.Count; i++)
        {
            _maxScore += 100;
            int checkIndex = _position + i;

            if (IsValidIndex(checkIndex))
            {
                _score += CalculateLineMatchScore(_documentLines[checkIndex], _block.TargetLines[i]);
            }
        }
    }

    private void ScoreTrailingLines()
    {
        for (int i = 0; i < _block.TrailingLines.Count; i++)
        {
            _maxScore += 100;
            int checkIndex = _position + _block.TargetLines.Count + i;

            if (IsValidIndex(checkIndex))
            {
                _score += CalculateLineMatchScore(_documentLines[checkIndex], _block.TrailingLines[i]);
            }
        }
    }

    private bool IsValidIndex(int index) => index >= 0 && index < _documentLines.Count;

    private int CalculateLineMatchScore(string docLine, string blockLine)
    {
        var normalizedDocLine = WhitespaceHelper.TrimWhitespace(docLine);
        var normalizedBlockLine = WhitespaceHelper.TrimWhitespace(blockLine);

        if (normalizedDocLine == normalizedBlockLine) return 100;

        var similarity = StringSimilarityCalculator.Calculate(normalizedDocLine, normalizedBlockLine);
        return (int)(similarity * 80);
    }
}
