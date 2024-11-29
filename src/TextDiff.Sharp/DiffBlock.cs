namespace TextDiff;

public class DiffBlock
{
    private readonly List<string> _targetLines = new();
    private readonly List<string> _leadingLines = new();
    private readonly List<string> _trailingLines = new();
    private readonly List<string> _insertLines = new();

    public IReadOnlyList<string> TargetLines => _targetLines;
    public IReadOnlyList<string> LeadingLines => _leadingLines;
    public IReadOnlyList<string> TrailingLines => _trailingLines;
    public IReadOnlyList<string> InsertLines => _insertLines;

    public void AddTargetLine(DiffLine line) => _targetLines.Add(line.Content);
    public void AddLeadingLine(DiffLine line) => _leadingLines.Add(line.Content);
    public void AddTrailingLine(DiffLine line) => _trailingLines.Add(line.Content);
    public void AddInsertLine(DiffLine line) => _insertLines.Add(line.Content);

    public bool HasChanges() => _targetLines.Any() || _insertLines.Any();

    public string GetCommonIndentation()
    {
        var allLines = _leadingLines.Concat(_targetLines).Concat(_trailingLines)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(line => {
                int i = 0;
                while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
                {
                    i++;
                }
                return line.Substring(0, i);
            })
            .Where(indent => !string.IsNullOrEmpty(indent));

        // 들여쓰기가 없으면 빈 문자열 반환
        if (!allLines.Any()) return "";

        // 가장 많이 사용된 들여쓰기를 반환
        return allLines
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }
}