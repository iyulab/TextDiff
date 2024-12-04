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
            if (string.IsNullOrEmpty(line)) continue;

            // Check for invalid format - line must start with space, '+' or '-'
            char firstChar = line[0];
            if (!DiffLineHelper.IsValidDiffLine(firstChar))
                throw new FormatException($"Invalid diff format: Line must start with space, '+' or '-': {line}");

            // For removal/addition lines, verify the format
            if (firstChar != ' ')
            {
                // The second character should be a space for proper diff format
                if (line.Length < 2 || (line[1] != ' ' && !string.IsNullOrEmpty(line)))
                    throw new FormatException($"Invalid diff format: After '+' or '-', expected space but got: {line}");
            }

            var content = DiffLineHelper.ExtractContent(line);

            switch (firstChar)
            {
                case ' ':
                    if (!isInChanges)
                    {
                        currentBlock.BeforeContext.Add(content);
                    }
                    else
                    {
                        bool hasMoreChanges = HasMoreChanges(diffLines, i + 1);
                        if (hasMoreChanges && currentBlock.HasChanges())
                        {
                            // Finish current block
                            yield return currentBlock;

                            // Start new block with this context line
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
            if (!string.IsNullOrEmpty(lines[i]))
            {
                char firstChar = lines[i][0];
                if (firstChar == '-' || firstChar == '+')
                    return true;
            }
        }
        return false;
    }
}