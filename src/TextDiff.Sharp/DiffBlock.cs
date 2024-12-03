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

    public void AddInsertLine(DiffLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Content))
        {
            _insertLines.Add(string.Empty);
            return;
        }

        // Indentation과 Content를 결합하여 삽입 라인에 실제 들여쓰기 복원
        _insertLines.Add(line.Indentation + line.Content);
    }
}