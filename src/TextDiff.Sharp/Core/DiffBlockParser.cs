using TextDiff.Exceptions;

namespace TextDiff.Core;

public class DiffBlockParser : IDiffBlockParser
{
    public IEnumerable<DiffBlock> Parse(string[] diffLines)
    {
        var currentBlock = new DiffBlock();
        var isInChanges = false;

        for (int i = 0; i < diffLines.Length; i++)
        {
            var line = diffLines[i];

            // Skip file headers
            if (line.StartsWith("---") || line.StartsWith("+++") ||
                line.StartsWith("diff ") || line.StartsWith("index "))
                continue;

            // Skip comment
            if (line.Equals("..."))
                continue;

            // Start new block on hunk header
            if (line.StartsWith("@@"))
            {
                if (currentBlock.HasChanges())
                {
                    yield return currentBlock;
                }
                currentBlock = new DiffBlock();
                isInChanges = false;
                continue;
            }

            // Treat empty lines as context
            if (string.IsNullOrEmpty(line))
            {
                if (!isInChanges)
                {
                    currentBlock.BeforeContext.Add(line);
                }
                else
                {
                    if (HasMoreChanges(diffLines, i + 1))
                    {
                        yield return currentBlock;
                        currentBlock = new DiffBlock();
                        currentBlock.BeforeContext.Add(line);
                        isInChanges = false;
                    }
                    else
                    {
                        currentBlock.AfterContext.Add(line);
                    }
                }
                continue;
            }

            // Check for invalid format
            if (!DiffLineHelper.IsValidDiffLine(line[0]))
                throw new InvalidDiffFormatException($"Invalid diff format: Line must start with space, '+' or '-': {line}");

            // Extract content after the prefix character (space/+/-)
            string content = DiffLineHelper.ExtractContent(line);

            switch (line[0])
            {
                case ' ':
                    if (!isInChanges)
                    {
                        currentBlock.BeforeContext.Add(content);
                    }
                    else
                    {
                        if (HasMoreChanges(diffLines, i + 1))
                        {
                            yield return currentBlock;
                            currentBlock = new DiffBlock();
                            currentBlock.BeforeContext.Add(content);
                            isInChanges = false;
                        }
                        else
                        {
                            currentBlock.AfterContext.Add(content);
                        }
                    }
                    break;

                case '-':
                    isInChanges = true;
                    currentBlock.Removals.Add(content);
                    break;

                case '+':
                    isInChanges = true;
                    currentBlock.Additions.Add(content);
                    break;
            }
        }

        if (currentBlock.HasChanges())
        {
            yield return currentBlock;
        }
    }

    private bool HasMoreChanges(string[] lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("---") || line.StartsWith("+++") || line.StartsWith("@@"))
                continue;

            if (line.Length > 0 && (line[0] == '-' || line[0] == '+'))
                return true;
        }
        return false;
    }
}