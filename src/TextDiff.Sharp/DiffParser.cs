using TextDiff;

public class DiffParser
{
    public List<DiffBlock> Parse(string diffText)
    {
        var lines = TextHelper.SplitLines(diffText)
            .Select(line => new DiffLine(line))
            .Where(line => !line.IsEmpty)
            .ToList();

        ValidateFormat(lines);

        var blocks = new List<DiffBlock>();
        var currentBlock = new DiffBlock();
        var isInBlock = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.IsContext)
            {
                if (isInBlock && currentBlock.TargetLines.Any())
                {
                    currentBlock.AddTrailingLine(line);
                }
                else if (!currentBlock.InsertLines.Any())
                {
                    currentBlock.AddLeadingLine(line);
                }
            }
            else
            {
                if (!isInBlock)
                {
                    isInBlock = true;
                    currentBlock = new DiffBlock();
                    if (i > 0 && lines[i - 1].IsContext)
                    {
                        currentBlock.AddLeadingLine(lines[i - 1]);
                    }
                }

                if (line.IsRemoval)
                {
                    currentBlock.AddTargetLine(line);
                }
                else if (line.IsAddition)
                {
                    currentBlock.AddInsertLine(line);
                }
            }

            bool shouldCloseBlock = ShouldCloseBlock(lines, i, isInBlock, currentBlock);
            if (shouldCloseBlock)
            {
                if (lines.HasNextContextLine(i))
                {
                    currentBlock.AddTrailingLine(lines[i + 1]);
                }

                blocks.Add(currentBlock);
                currentBlock = new DiffBlock();
                isInBlock = false;
            }
        }

        if (currentBlock.HasChanges())
        {
            blocks.Add(currentBlock);
        }

        return blocks;
    }

    private void ValidateFormat(List<DiffLine> lines)
    {
        if (lines.Count == 0) return;

        bool inChangeBlock = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // 허용되지 않는 문자로 시작하는 라인이 있는지 검사
            if (line.Type != ' ' && line.Type != '+' && line.Type != '-')
            {
                throw new FormatException($"Invalid diff format: Line must start with space, '+', or '-'. Found: '{line.RawContent}'");
            }

            // 중복된 제어 문자 검사 (예: "- - line")
            if ((line.IsAddition || line.IsRemoval) &&
                (line.Content.StartsWith("+ ") || line.Content.StartsWith("- ")))
            {
                throw new FormatException($"Invalid diff format: Duplicate control characters found in line: '{line.RawContent}'");
            }

            // 컨텍스트 라인이면 변경 블록이 끝남
            if (line.IsContext)
            {
                inChangeBlock = false;
                continue;
            }

            // 변경 블록 내부가 아닌데 변경이 나타나면 새로운 블록 시작
            if (!inChangeBlock && (line.IsAddition || line.IsRemoval))
            {
                // 첫 번째 블록이 아니고, 이전 라인이 컨텍스트가 아니면 에러
                if (i > 0 && !lines[i - 1].IsContext)
                {
                    throw new FormatException("Invalid diff format: Change block must be preceded by a context line");
                }
                inChangeBlock = true;
            }
        }
    }

    private bool ShouldCloseBlock(List<DiffLine> lines, int currentIndex, bool isInBlock, DiffBlock block)
    {
        bool isLastLine = currentIndex == lines.Count - 1;
        bool nextLineIsContext = !isLastLine && lines[currentIndex + 1].IsContext;
        bool hasChanges = block.HasChanges();

        return (isInBlock && hasChanges && (isLastLine || nextLineIsContext)) ||
               (!isInBlock && block.InsertLines.Any());
    }
}