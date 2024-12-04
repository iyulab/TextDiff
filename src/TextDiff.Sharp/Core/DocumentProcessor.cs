namespace TextDiff.Core;

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

    private void ProcessBlock(DiffBlock block)
    {
        int blockPosition = _currentPosition;

        if (block.BeforeContext.Any())
        {
            blockPosition = _contextMatcher.FindPosition(_documentLines, _currentPosition, block);
            if (blockPosition == -1)
            {
                blockPosition = FindPositionByRemovals(block);
                if (blockPosition == -1)
                {
                    throw new InvalidOperationException($"Cannot find matching position for block: {block}");
                }
            }
        }

        // Copy any lines before the block position
        CopyLinesUntilPosition(blockPosition);

        // Skip over the BeforeContext lines while copying them
        foreach (var line in block.BeforeContext)
        {
            if (_currentPosition < _documentLines.Length)
            {
                _resultBuffer.AddLine(_documentLines[_currentPosition]);
            }
            _currentPosition++;
        }

        // Get indentation from the line being replaced
        string indentation = "";
        if (block.Removals.Any() && _currentPosition < _documentLines.Length)
        {
            indentation = TextUtils.ExtractIndentation(_documentLines[_currentPosition]);
        }
        else if (block.BeforeContext.Any() && _currentPosition > 0)
        {
            // If no removal line, try to get indentation from the last context line
            indentation = TextUtils.ExtractIndentation(_documentLines[_currentPosition - 1]);
        }

        // Skip the lines that are being removed
        _currentPosition += block.Removals.Count;

        // Add the new lines, preserving indentation
        foreach (var line in block.Additions)
        {
            string newLine = line.TrimStart();
            if (!string.IsNullOrEmpty(indentation) && !string.IsNullOrWhiteSpace(line))
            {
                newLine = indentation + newLine;
            }
            _resultBuffer.AddLine(newLine);
        }

        // Process after context if any
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

    private int FindPositionByRemovals(DiffBlock block)
    {
        int searchStart = Math.Max(0, _currentPosition - block.BeforeContext.Count);

        for (int i = searchStart; i < _documentLines.Length; i++)
        {
            if (i + block.Removals.Count <= _documentLines.Length)
            {
                bool match = true;
                for (int j = 0; j < block.Removals.Count; j++)
                {
                    if (!TextUtils.LinesMatch(_documentLines[i + j], block.Removals[j]))
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
        }
        return -1;
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