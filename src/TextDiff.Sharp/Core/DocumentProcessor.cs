using TextDiff.Core;
using TextDiff.Helpers;
using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Processes diff blocks and applies them to the original document.
/// </summary>
/// <remarks>
/// Indentation handling rules:
/// - Changed lines: Preserve original line's indentation
/// - Added lines: Use diff's indentation as-is
/// </remarks>
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

        // Copy before context lines
        foreach (var line in block.BeforeContext)
        {
            if (_currentPosition < _documentLines.Length)
            {
                _resultBuffer.AddLine(_documentLines[_currentPosition]);
            }
            _currentPosition++;
        }

        // Process changed lines
        int removalIndex = 0;
        int minCount = Math.Min(block.Removals.Count, block.Additions.Count);

        // 1. Changed lines (paired removal and addition)
        for (int i = 0; i < minCount; i++)
        {
            var originalLine = _documentLines[_currentPosition + i];
            var addedLine = block.Additions[i];

            // Extract and apply original line's indentation
            string indentation = TextUtils.ExtractIndentation(originalLine);
            string newContent = TextUtils.RemoveIndentation(addedLine); // Remove added content's indentation
            _resultBuffer.AddLine(indentation + newContent);

            removalIndex++;
        }

        // 2. Pure additions (new lines without corresponding removals)
        for (int i = removalIndex; i < block.Additions.Count; i++)
        {
            var addedLine = block.Additions[i];
            _resultBuffer.AddLine(addedLine); // Use diff's indentation as-is
        }

        _currentPosition += block.Removals.Count;

        // Copy after context lines
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

        return new ProcessResult(_resultBuffer.ToString(), _changes);
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