using TextDiff.Exceptions;

namespace TextDiff.Core;

public class DiffBlockParser : IDiffBlockParser
{
    public IEnumerable<DiffBlock> Parse(string[] diffLines)
    {
        var currentBlock = new DiffBlock();
        var isInChanges = false;
        var startedAfterYield = false;
        var seenFirstDiffHeader = false;

        for (int i = 0; i < diffLines.Length; i++)
        {
            var line = diffLines[i];

            // A "diff " line marks the start of a per-file section.
            // Stop after the first file to avoid applying unrelated file diffs.
            if (line.StartsWith("diff "))
            {
                if (seenFirstDiffHeader)
                {
                    // Second (or later) file section — stop here
                    break;
                }
                seenFirstDiffHeader = true;
                continue;
            }

            // Skip file headers (standard and git extended).
            // Use "--- " / "+++ " (with trailing space) rather than "---" / "+++"
            // so that diff content lines whose payload starts with dashes or pluses
            // are not misidentified as headers.  A real header always has a space
            // after the three marker characters (e.g. "--- a/file.txt"), whereas a
            // diff addition line with content "+++ foo" looks like "++++ foo" in the
            // raw diff — four pluses, not three followed by a space.
            if (line.StartsWith("--- ") || line.StartsWith("+++ ") ||
                line.StartsWith("index ") ||
                line.StartsWith("old mode ") || line.StartsWith("new mode ") ||
                line.StartsWith("new file mode ") || line.StartsWith("deleted file mode ") ||
                line.StartsWith("similarity index ") || line.StartsWith("dissimilarity index ") ||
                line.StartsWith("rename from ") || line.StartsWith("rename to ") ||
                line.StartsWith("copy from ") || line.StartsWith("copy to "))
                continue;

            // Ellipsis ("...") marks a gap in context between sections.
            // Yield the current block if it has changes, then start fresh so that
            // the next section is matched independently without the prior context.
            if (line.Equals("..."))
            {
                if (currentBlock.HasChanges())
                {
                    yield return currentBlock;
                }
                currentBlock = new DiffBlock();
                isInChanges = false;
                startedAfterYield = false;
                continue;
            }

            // Skip "No newline at end of file" marker
            // This appears as: \ No newline at end of file
            if (line.StartsWith("\\"))
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
                startedAfterYield = false;
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
                        if (startedAfterYield)
                        {
                            // Sliding window: replace BeforeContext to avoid accumulating
                            // non-adjacent context lines from separate hunks
                            currentBlock.BeforeContext.Clear();
                        }
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
                            startedAfterYield = true;
                        }
                        else
                        {
                            currentBlock.AfterContext.Add(content);
                        }
                    }
                    break;

                case '-':
                    isInChanges = true;
                    startedAfterYield = false;
                    currentBlock.Removals.Add(content);
                    break;

                case '+':
                    isInChanges = true;
                    startedAfterYield = false;
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
            if (line.StartsWith("--- ") || line.StartsWith("+++ ") || line.StartsWith("@@"))
                continue;
            // Skip "No newline at end of file" marker
            if (line.StartsWith("\\"))
                continue;

            if (line.Length > 0 && (line[0] == '-' || line[0] == '+'))
                return true;
        }
        return false;
    }
}