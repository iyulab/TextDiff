namespace TextDiff;

public class ChangeStats
{
    private int _deletedLines;
    private int _changedLines;
    private int _addedLines;

    public void UpdateStats(DiffBlock block)
    {
        if (block.TargetLines.Any() && block.InsertLines.Any())
        {
            // 변경된 라인 수를 세기 위해 더 짧은 쪽의 길이를 사용
            var pairCount = Math.Min(block.TargetLines.Count, block.InsertLines.Count);
            _changedLines += pairCount;  // 각 대응하는 라인을 changed로 카운트

            // 추가 라인이 있다면 added로 처리
            if (block.InsertLines.Count > pairCount)
            {
                _addedLines += block.InsertLines.Count - pairCount;
            }

            // 삭제된 라인이 더 많다면 deleted로 처리
            if (block.TargetLines.Count > pairCount)
            {
                _deletedLines += block.TargetLines.Count - pairCount;
            }
        }
        else
        {
            // 단순 추가/삭제인 경우
            _deletedLines += block.TargetLines.Count;
            _addedLines += block.InsertLines.Count;
        }
    }

    public DocumentChangeResult ToResult() => new()
    {
        DeletedLines = _deletedLines,
        ChangedLines = _changedLines,
        AddedLines = _addedLines
    };
}
