namespace TextDiff.Models;

public class LineBuffer
{
    private readonly List<string> _lines = new();
    private readonly string _lineSeparator;

    public LineBuffer(string? lineSeparator = null)
    {
        _lineSeparator = lineSeparator ?? Environment.NewLine;
    }

    public void AddLine(string line) => _lines.Add(line);

    public override string ToString() => string.Join(_lineSeparator, _lines);
}
