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
                .Select(line => line.TrimStart()) // 앞쪽 공백 제거
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
        return new DocumentChange
        {
            LineNumber = position,
            LinesToRemove = block.TargetLines,
            LinesToInsert = block.InsertLines.ToList()
        };
    }

    private List<DocumentChange> CalculateChanges(List<string> documentLines, List<DiffBlock> blocks,
    out DocumentChangeResult result)
    {
        var changes = new List<DocumentChange>();
        var stats = new ChangeStats();
        var lastMatchedLine = -1;
        int lineOffset = 0; // Tracks the net change in line numbers

        foreach (var block in blocks)
        {
            var adjustedStartIndex = lastMatchedLine + 1 + lineOffset;
            var matchPosition = _matchingService.FindMatchPosition(documentLines, block, adjustedStartIndex);
            if (matchPosition >= 0)
            {
                var change = CreateDocumentChange(documentLines, block, matchPosition + lineOffset);
                changes.Add(change);

                // Update stats
                stats.UpdateStats(block);

                // Update lineOffset based on insertions and deletions
                lineOffset += change.LinesToInsert.Count - change.LinesToRemove.Count;

                lastMatchedLine = matchPosition;
            }
            else
            {
                // Handle cases where no match is found
                Console.WriteLine("No match found for a diff block.");
            }
        }

        result = stats.ToResult();
        return changes;
    }

    private List<string> ApplyChanges(List<string> documentLines, List<DocumentChange> changes)
    {
        var resultLines = new List<string>(documentLines);
        foreach (var change in changes.OrderByDescending(c => c.LineNumber))
        {
            Console.WriteLine($"Applying change at line {change.LineNumber}: Remove {change.LinesToRemove.Count}, Insert {change.LinesToInsert.Count}");

            if (change.LineNumber >= 0 && change.LineNumber <= resultLines.Count)
            {
                var indentation = "";

                // 들여쓰기가 필요한지 확인
                bool needsIndentation = change.LinesToInsert.Any() &&
                    !change.LinesToInsert.Any(line => line.StartsWith("    ") || line.StartsWith("\t"));

                if (needsIndentation)
                {
                    // 삭제될 라인의 들여쓰기를 가져옴
                    if (change.LineNumber < resultLines.Count)
                    {
                        indentation = WhitespaceHelper.ExtractLeadingWhitespace(resultLines[change.LineNumber]);
                    }
                    // 이전 라인의 들여쓰기를 가져옴
                    else if (change.LineNumber > 0)
                    {
                        indentation = WhitespaceHelper.ExtractLeadingWhitespace(resultLines[change.LineNumber - 1]);
                    }
                }

                if (change.LinesToRemove.Any())
                {
                    int removeCount = Math.Min(change.LinesToRemove.Count, resultLines.Count - change.LineNumber);
                    Console.WriteLine($"Removing {removeCount} line(s) starting at line {change.LineNumber}");
                    resultLines.RemoveRange(change.LineNumber, removeCount);
                }

                var linesToInsert = needsIndentation
                    ? change.LinesToInsert.Select(line => indentation + line).ToList()
                    : change.LinesToInsert.ToList();

                Console.WriteLine($"Inserting {linesToInsert.Count} line(s) at line {change.LineNumber}");
                resultLines.InsertRange(change.LineNumber, linesToInsert);
            }
        }
        return resultLines;
    }
}
