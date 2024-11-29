namespace TextDiff;

public class DiffParser
{
    public List<DiffBlock> Parse(string diffText)
    {
        var lines = TextHelper.SplitLines(diffText)
            .Select(line => new DiffLine(line))
            .Where(line => !line.IsEmpty)
            .ToList();

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

    private List<string> SplitLines(string text) =>
        text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

    private bool ShouldCloseBlock(List<DiffLine> lines, int currentIndex, bool isInBlock, DiffBlock block)
    {
        bool isLastLine = currentIndex == lines.Count - 1;
        bool nextLineIsContext = !isLastLine && lines[currentIndex + 1].IsContext;
        bool hasChanges = block.HasChanges();

        return (isInBlock && hasChanges && (isLastLine || nextLineIsContext)) ||
               (!isInBlock && block.InsertLines.Any());
    }
}
