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

        // 빈 문서에 대한 순수 추가 작업인 경우 특별 처리
        if (documentLines.Count == 0 && blocks.Count == 1 &&
            blocks[0].InsertLines.Any() && !blocks[0].TargetLines.Any())
        {
            var addedLines = blocks[0].InsertLines.ToList();
            return new ProcessResult
            {
                Text = string.Join(Environment.NewLine, addedLines),
                Changes = new DocumentChangeResult { AddedLines = addedLines.Count }
            };
        }

        var changes = CalculateChanges(documentLines, blocks, out var changeResult);
        var resultLines = ApplyChanges(documentLines, changes);

        // 결과 라인 리스트의 끝에 있는 빈 문자열 제거
        while (resultLines.Count > 0 && string.IsNullOrEmpty(resultLines[resultLines.Count - 1]))
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
        string commonIndentation;

        // 변경되는 라인이 있는 경우
        if (block.TargetLines.Any())
        {
            var targetPositions = Enumerable.Range(position, block.TargetLines.Count);
            var targetLines = targetPositions
                .Where(i => i < documentLines.Count)
                .Select(i => documentLines[i])
                .ToList();

            commonIndentation = WhitespaceHelper.FindCommonIndentation(targetLines);
        }
        // 순수 추가인 경우
        else
        {
            var nextLine = position < documentLines.Count
                ? documentLines[position]
                : position > 0 ? documentLines[position - 1] : "";

            commonIndentation = WhitespaceHelper.ExtractLeadingWhitespace(nextLine);
        }

        return new DocumentChange
        {
            LineNumber = position,
            LinesToRemove = block.TargetLines,
            LinesToInsert = block.InsertLines
                .Select(line => commonIndentation + line.TrimStart())
                .ToList()
        };
    }
}
