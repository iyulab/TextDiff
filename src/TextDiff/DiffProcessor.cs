namespace TextDiff;

public class DiffProcessor
{
    private readonly DiffParser _parser;
    private readonly LineMatchingService _matchingService;

    public DiffProcessor()
    {
        _parser = new DiffParser();
        _matchingService = new LineMatchingService();
    }

    public ProcessResult Process(string documentText, string diffText)
    {
        var documentLines = TextHelper.SplitLines(documentText);
        var blocks = _parser.Parse(diffText);
        var changes = CalculateChanges(documentLines, blocks, out var changeResult);
        var resultLines = ApplyChanges(documentLines, changes);

        // 결과 라인 리스트의 끝에 있는 빈 문자열 제거
        while (resultLines.Count > 0 && string.IsNullOrEmpty(resultLines[^1]))
        {
            resultLines.RemoveAt(resultLines.Count - 1);
        }

        return new ProcessResult
        {
            Text = string.Join(Environment.NewLine, resultLines),
            Changes = changeResult
        };
    }

    private List<string> ApplyChanges(List<string> documentLines, List<DocumentChange> changes)
    {
        var resultLines = new List<string>(documentLines);
        foreach (var change in changes.OrderByDescending(c => c.LineNumber))
        {
            if (change.LineNumber >= 0 && change.LineNumber <= resultLines.Count)
            {
                if (change.LinesToRemove.Any())
                {
                    resultLines.RemoveRange(change.LineNumber,
                        Math.Min(change.LinesToRemove.Count, resultLines.Count - change.LineNumber));
                }
                resultLines.InsertRange(change.LineNumber, change.LinesToInsert);
            }
        }
        return resultLines;
    }

    private List<DocumentChange> CalculateChanges(List<string> documentLines, List<DiffBlock> blocks,
        out DocumentChangeResult result)
    {
        var changes = new List<DocumentChange>();
        var stats = new ChangeStats();
        var lastMatchedLine = -1;

        foreach (var block in blocks)
        {
            var matchPosition = _matchingService.FindMatchPosition(documentLines, block, lastMatchedLine + 1);
            if (matchPosition >= 0)
            {
                var change = CreateDocumentChange(documentLines, block, matchPosition);
                changes.Add(change);

                stats.UpdateStats(block);
                lastMatchedLine = matchPosition;
            }
        }

        result = stats.ToResult();
        return changes;
    }

    private DocumentChange CreateDocumentChange(List<string> documentLines, DiffBlock block, int position)
    {
        // 변경이 일어나는 위치의 들여쓰기를 파악
        string indentation = "";
        if (position > 0 && position < documentLines.Count)
        {
            // 현재 위치의 라인이나 이전 라인에서 들여쓰기를 가져옴
            var currentLine = documentLines[position];
            var prevLine = documentLines[position - 1];

            // 현재 라인과 이전 라인 중에서 들여쓰기가 있는 쪽을 선택
            indentation = WhitespaceHelper.ExtractLeadingWhitespace(currentLine);
            if (string.IsNullOrEmpty(indentation))
            {
                indentation = WhitespaceHelper.ExtractLeadingWhitespace(prevLine);
            }
        }

        return new DocumentChange
        {
            LineNumber = position,
            LinesToRemove = block.TargetLines,
            LinesToInsert = block.InsertLines
                .Select(line => indentation + line.TrimStart())
                .ToList()
        };
    }
}
