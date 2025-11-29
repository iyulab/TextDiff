using System.Text;
using System.Text.RegularExpressions;

namespace TextDiff.DiffX;

/// <summary>
/// Reads and extracts applicable diff sections from DiffX format files.
/// </summary>
/// <remarks>
/// DiffX Section Hierarchy:
/// <code>
/// #diffx: encoding=utf-8, version=1.0       ← File header (level 0)
/// #.preamble:                               ← Preamble (level 1)
/// #.meta:                                   ← Global metadata (level 1)
/// #.change:                                 ← Change/commit (level 1)
///   #..preamble:                            ← Commit message (level 2)
///   #..meta:                                ← Commit metadata (level 2)
///   #..file:                                ← File entry (level 2)
///     #...meta:                             ← File metadata (level 3)
///     #...diff:                             ← Diff content (level 3) ← TARGET
/// </code>
/// </remarks>
public partial class DiffXReader : IDiffXReader
{
    // Section header pattern: #[dots]name: options
    // Level is determined by number of dots: 0=diffx, 1=.section, 2=..section, 3=...section
    private static readonly Regex SectionHeaderRegex = SectionHeaderPattern();

    // Options pattern: key=value pairs
    private static readonly Regex OptionRegex = OptionPattern();

    // JSON-like metadata for path extraction (simplified parser)
    private static readonly Regex JsonPathRegex = JsonPathPattern();
    private static readonly Regex JsonOpRegex = JsonOpPattern();

    /// <inheritdoc />
    public bool IsDiffX(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        // Find first line end
        var span = content.AsSpan();
        var newlineIndex = span.IndexOfAny('\r', '\n');
        var firstLine = newlineIndex > 0 ? span.Slice(0, newlineIndex) : span;

        return firstLine.StartsWith("#diffx:");
    }

    /// <inheritdoc />
    public IEnumerable<DiffXFileEntry> ExtractFileDiffs(string diffXContent)
    {
        if (diffXContent is null)
            throw new ArgumentNullException(nameof(diffXContent));

        if (!IsDiffX(diffXContent))
            throw new ArgumentException("Content is not in valid DiffX format.", nameof(diffXContent));

        return ExtractFileDiffsCore(diffXContent);
    }

    private IEnumerable<DiffXFileEntry> ExtractFileDiffsCore(string diffXContent)
    {
        var lines = diffXContent.Split('\n');

        // Global state from header
        string globalEncoding = "utf-8";

        // Current file state
        string? currentPath = null;
        string? currentOp = null;
        string currentEncoding = globalEncoding;
        bool isBinary = false;

        // Diff content accumulator
        var diffBuilder = new StringBuilder();
        bool inDiffSection = false;
        int? expectedDiffLength = null;
        int currentDiffLength = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Check for section headers
            if (line.StartsWith("#"))
            {
                // If we were in a diff section, yield it
                if (inDiffSection && diffBuilder.Length > 0)
                {
                    yield return CreateEntry(currentPath, currentOp, diffBuilder.ToString().TrimEnd(), currentEncoding, isBinary);
                    diffBuilder.Clear();
                    inDiffSection = false;
                    expectedDiffLength = null;
                    currentDiffLength = 0;
                }

                var match = SectionHeaderRegex.Match(line);
                if (match.Success)
                {
                    var level = match.Groups["level"].Value.Length;
                    var name = match.Groups["name"].Value;
                    var options = match.Groups["options"].Value;

                    switch ((level, name))
                    {
                        case (0, "diffx"):
                            // Parse global options
                            globalEncoding = ParseOption(options, "encoding") ?? "utf-8";
                            currentEncoding = globalEncoding;
                            break;

                        case (2, "file"):
                            // Reset file-level state
                            currentPath = null;
                            currentOp = null;
                            currentEncoding = globalEncoding;
                            isBinary = false;
                            break;

                        case (3, "meta"):
                            // Parse file metadata (next lines or inline JSON)
                            (currentPath, currentOp) = ParseFileMeta(lines, i, options);
                            break;

                        case (3, "diff"):
                            // Start diff section
                            inDiffSection = true;
                            expectedDiffLength = ParseLength(options);
                            currentDiffLength = 0;

                            // Check for binary operation
                            var opValue = ParseOption(options, "op");
                            if (opValue == "binary")
                            {
                                isBinary = true;
                            }
                            break;
                    }
                }

                continue;
            }

            // Accumulate diff content
            if (inDiffSection)
            {
                // Handle Windows line endings
                var cleanLine = line.TrimEnd('\r');
                diffBuilder.AppendLine(cleanLine);
                currentDiffLength += cleanLine.Length + 1; // +1 for newline

                // Check if we've reached expected length
                if (expectedDiffLength.HasValue && currentDiffLength >= expectedDiffLength.Value)
                {
                    // Look ahead to see if next line is a section header
                    if (i + 1 < lines.Length && lines[i + 1].StartsWith("#"))
                    {
                        yield return CreateEntry(currentPath, currentOp, diffBuilder.ToString().TrimEnd(), currentEncoding, isBinary);
                        diffBuilder.Clear();
                        inDiffSection = false;
                        expectedDiffLength = null;
                        currentDiffLength = 0;
                    }
                }
            }
        }

        // Yield final diff if any
        if (inDiffSection && diffBuilder.Length > 0)
        {
            yield return CreateEntry(currentPath, currentOp, diffBuilder.ToString().TrimEnd(), currentEncoding, isBinary);
        }
    }

    private static DiffXFileEntry CreateEntry(string? path, string? op, string diffContent, string encoding, bool isBinary)
    {
        return new DiffXFileEntry
        {
            Path = path,
            Operation = op,
            DiffContent = diffContent,
            Encoding = encoding,
            IsBinary = isBinary
        };
    }

    private static string? ParseOption(string options, string key)
    {
        foreach (Match match in OptionRegex.Matches(options))
        {
            if (match.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return match.Groups["value"].Value;
            }
        }
        return null;
    }

    private static int? ParseLength(string options)
    {
        var lengthStr = ParseOption(options, "length");
        if (lengthStr != null && int.TryParse(lengthStr, out var length))
        {
            return length;
        }
        return null;
    }

    private static (string? path, string? op) ParseFileMeta(string[] lines, int metaLineIndex, string options)
    {
        string? path = null;
        string? op = null;

        // Check for inline format specification
        var format = ParseOption(options, "format");

        if (format == "json")
        {
            // Next line(s) contain JSON
            var lengthStr = ParseOption(options, "length");
            if (lengthStr != null && int.TryParse(lengthStr, out var length))
            {
                // Collect JSON content
                var jsonBuilder = new StringBuilder();
                int currentLength = 0;
                for (int j = metaLineIndex + 1; j < lines.Length && currentLength < length; j++)
                {
                    var jsonLine = lines[j];
                    if (jsonLine.StartsWith("#")) break;
                    jsonBuilder.AppendLine(jsonLine);
                    currentLength += jsonLine.Length + 1;
                }

                var json = jsonBuilder.ToString();
                path = ExtractJsonValue(json, JsonPathRegex);
                op = ExtractJsonValue(json, JsonOpRegex);
            }
        }
        else
        {
            // Inline options format: key=value pairs
            // Or look at next line for JSON
            if (metaLineIndex + 1 < lines.Length)
            {
                var nextLine = lines[metaLineIndex + 1];
                if (nextLine.TrimStart().StartsWith("{"))
                {
                    // Simple JSON parsing for path and op
                    var jsonBuilder = new StringBuilder();
                    for (int j = metaLineIndex + 1; j < lines.Length; j++)
                    {
                        var jsonLine = lines[j];
                        if (jsonLine.StartsWith("#")) break;
                        jsonBuilder.AppendLine(jsonLine);
                        if (jsonLine.TrimEnd().EndsWith("}")) break;
                    }

                    var json = jsonBuilder.ToString();
                    path = ExtractJsonValue(json, JsonPathRegex);
                    op = ExtractJsonValue(json, JsonOpRegex);
                }
            }
        }

        return (path, op);
    }

    private static string? ExtractJsonValue(string json, Regex pattern)
    {
        var match = pattern.Match(json);
        return match.Success ? match.Groups["value"].Value : null;
    }

    [GeneratedRegex(@"^#(?<level>\.{0,3})(?<name>[a-z]+):\s*(?<options>.*)$", RegexOptions.Compiled)]
    private static partial Regex SectionHeaderPattern();

    [GeneratedRegex(@"(?<key>[A-Za-z][A-Za-z0-9_-]*)=(?<value>[^\s,]+)", RegexOptions.Compiled)]
    private static partial Regex OptionPattern();

    [GeneratedRegex(@"""path""\s*:\s*""(?<value>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex JsonPathPattern();

    [GeneratedRegex(@"""op""\s*:\s*""(?<value>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex JsonOpPattern();
}
