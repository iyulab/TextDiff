/*
의도:
1. 들여쓰기 처리 규칙
   - 변경(changed)되는 라인: 원본 라인의 들여쓰기 유지
   - 새로 추가(added)되는 라인: diff의 들여쓰기 사용

유의사항:
1. 라인 변경/추가 여부를 정확히 판단해야 함
2. 원본 라인의 들여쓰기를 정확히 추출하고 보존해야 함
3. diff의 들여쓰기도 정확히 보존해야 함
*/

public class DocumentProcessor
{
    private readonly string[] _documentLines;
    private readonly IContextMatcher _contextMatcher;
    private readonly IChangeTracker _changeTracker;
    private readonly LineBuffer _resultBuffer;
    private readonly ChangeStats _changes;
    private int _currentPosition;

    public DocumentProcessor(
        string[] documentLines,
        IContextMatcher contextMatcher,
        IChangeTracker changeTracker)
    {
        _documentLines = documentLines;
        _contextMatcher = contextMatcher;
        _changeTracker = changeTracker;
        _resultBuffer = new LineBuffer();
        _changes = new ChangeStats();
        _currentPosition = 0;
    }

    private void ProcessBlock(DiffBlock block)
    {
        int blockPosition = _currentPosition;

        if (block.BeforeContext.Any())
        {
            blockPosition = _contextMatcher.FindPosition(_documentLines, _currentPosition, block);
            if (blockPosition == -1)
            {
                throw new InvalidOperationException($"Cannot find matching position for block: {block}");
            }
        }

        CopyLinesUntilPosition(blockPosition);

        // 컨텍스트 라인 복사
        foreach (var line in block.BeforeContext)
        {
            if (_currentPosition < _documentLines.Length)
            {
                _resultBuffer.AddLine(_documentLines[_currentPosition]);
            }
            _currentPosition++;
        }

        // 변경되는 라인 처리
        int removalIndex = 0;
        int minCount = Math.Min(block.Removals.Count, block.Additions.Count);

        // 1. Changed lines (removal과 addition이 모두 있는 경우)
        for (int i = 0; i < minCount; i++)
        {
            var originalLine = _documentLines[_currentPosition + i];
            var addedLine = block.Additions[i];

            // 원본 라인의 들여쓰기 추출 및 적용
            string indentation = TextUtils.ExtractIndentation(originalLine);
            string newContent = TextUtils.RemoveIndentation(addedLine); // 추가된 내용의 들여쓰기 제거
            _resultBuffer.AddLine(indentation + newContent);

            removalIndex++;
        }

        // 2. Added lines (순수하게 추가되는 라인)
        for (int i = removalIndex; i < block.Additions.Count; i++)
        {
            var addedLine = block.Additions[i];
            _resultBuffer.AddLine(addedLine); // diff의 들여쓰기를 그대로 사용
        }

        _currentPosition += block.Removals.Count;

        // 후행 컨텍스트 라인 복사
        foreach (var line in block.AfterContext)
        {
            if (_currentPosition < _documentLines.Length)
            {
                _resultBuffer.AddLine(_documentLines[_currentPosition]);
            }
            _currentPosition++;
        }

        _changeTracker.TrackChanges(block, _changes);
    }

    public ProcessResult ApplyBlocks(IEnumerable<DiffBlock> blocks)
    {
        foreach (var block in blocks)
        {
            ProcessBlock(block);
        }

        CopyRemainingLines();

        return new ProcessResult
        {
            Text = _resultBuffer.ToString(),
            Changes = _changes
        };
    }

    private void CopyLinesUntilPosition(int position)
    {
        while (_currentPosition < position && _currentPosition < _documentLines.Length)
        {
            _resultBuffer.AddLine(_documentLines[_currentPosition]);
            _currentPosition++;
        }
    }

    private void CopyRemainingLines()
    {
        while (_currentPosition < _documentLines.Length)
        {
            _resultBuffer.AddLine(_documentLines[_currentPosition]);
            _currentPosition++;
        }
    }
}