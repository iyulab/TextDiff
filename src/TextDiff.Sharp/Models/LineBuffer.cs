namespace TextDiff.Models;

public class LineBuffer
{
    private readonly List<string> _lines = new();

    public void AddLine(string line) => _lines.Add(line);

    public override string ToString() => string.Join(Environment.NewLine, _lines);
}
