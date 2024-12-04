namespace TextDiff.Models;

public class DiffBlock
{
    public List<string> BeforeContext { get; } = new();
    public List<string> Removals { get; } = new();
    public List<string> Additions { get; } = new();
    public List<string> AfterContext { get; } = new();

    public bool HasChanges() => Removals.Any() || Additions.Any();

    public override string ToString()
    {
        return $"BeforeContext: [{string.Join(", ", BeforeContext)}], " +
               $"Removals: [{string.Join(", ", Removals)}], " +
               $"Additions: [{string.Join(", ", Additions)}], " +
               $"AfterContext: [{string.Join(", ", AfterContext)}]";
    }
}
