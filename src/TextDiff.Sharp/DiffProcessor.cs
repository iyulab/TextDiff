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
            var addedLines = blocks[0].InsertLines
                .Select(line => line.TrimStart())
                .ToList();

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

    private DocumentChange CreateDocumentChange(List<string> documentLines, DiffBlock block, int position)
    {
        // 원본 라인의 들여쓰기를 보존하여 삽입할 라인들을 생성
        var linesToInsert = new List<string>();
        foreach (var line in block.InsertLines)
        {
            string indentation = "";
            if (position < documentLines.Count)
            {
                indentation = WhitespaceHelper.ExtractLeadingWhitespace(documentLines[position]);
            }
            linesToInsert.Add(indentation + line.TrimStart());
        }

        return new DocumentChange
        {
            LineNumber = position,
            LinesToRemove = block.TargetLines,
            LinesToInsert = linesToInsert
        };
    }

    private List<DocumentChange> CalculateChanges(List<string> documentLines, List<DiffBlock> blocks,
        out DocumentChangeResult result)
    {
        var changes = new List<DocumentChange>();
        var stats = new ChangeStats();
        var lastMatchedLine = -1;

        foreach (var block in blocks)
        {
            var adjustedStartIndex = lastMatchedLine + 1;
            var matchPosition = _matchingService.FindMatchPosition(documentLines, block, adjustedStartIndex);

            if (matchPosition >= 0)
            {
                var change = CreateDocumentChange(documentLines, block, matchPosition);
                changes.Add(change);
                stats.UpdateStats(block);
                lastMatchedLine = matchPosition + Math.Max(block.TargetLines.Count, 1) - 1;
            }
        }

        result = stats.ToResult();
        return changes;
    }

    private List<string> ApplyChanges(List<string> documentLines, List<DocumentChange> changes)
    {
        var resultLines = new List<string>(documentLines);

        // 변경사항을 라인 번호의 역순으로 정렬 (뒤에서부터 처리)
        foreach (var change in changes.OrderByDescending(c => c.LineNumber))
        {
            if (change.LineNumber < 0 || change.LineNumber > resultLines.Count)
                continue;

            // 기존 라인 삭제
            if (change.LinesToRemove.Any())
            {
                int removeCount = Math.Min(change.LinesToRemove.Count, resultLines.Count - change.LineNumber);
                if (removeCount > 0)
                {
                    resultLines.RemoveRange(change.LineNumber, removeCount);
                }
            }

            // 새 라인 삽입 (이미 들여쓰기가 적용된 상태)
            if (change.LinesToInsert.Any())
            {
                resultLines.InsertRange(change.LineNumber, change.LinesToInsert);
            }
        }

        return resultLines;
    }
}