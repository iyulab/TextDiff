using System.Text;

namespace TextDiff.Models;

/// <summary>
/// Memory-optimized line buffer using StringBuilder to reduce allocations.
/// </summary>
public class OptimizedLineBuffer
{
    private readonly StringBuilder _buffer;
    private bool _hasLines;

    public OptimizedLineBuffer(int capacity = 4096)
    {
        _buffer = new StringBuilder(capacity);
        _hasLines = false;
    }

    public void AddLine(string line)
    {
        if (_hasLines)
        {
            _buffer.AppendLine();
        }
        _buffer.Append(line);
        _hasLines = true;
    }

    public void AddLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AddLine(line);
        }
    }

    public int EstimatedLength => _buffer.Length;

    public override string ToString() => _buffer.ToString();

    public void Clear()
    {
        _buffer.Clear();
        _hasLines = false;
    }
}