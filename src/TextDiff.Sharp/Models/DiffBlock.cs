namespace TextDiff.Models;

/// <summary>
/// Represents a block of changes in a unified diff format.
/// Each block contains context lines and the actual changes (additions/removals).
/// </summary>
/// <remarks>
/// A diff block is a fundamental unit of change in the unified diff format.
/// It consists of:
/// - Before context: Lines that appear before the changes for positioning
/// - Removals: Lines that should be removed from the original document
/// - Additions: Lines that should be added to the document
/// - After context: Lines that appear after the changes for positioning
/// </remarks>
/// <example>
/// <code>
/// var block = new DiffBlock();
/// block.BeforeContext.Add(" unchanged line");
/// block.Removals.Add("old line");
/// block.Additions.Add("new line");
/// block.AfterContext.Add(" another unchanged line");
/// </code>
/// </example>
public class DiffBlock
{
    /// <summary>
    /// Gets the list of context lines that appear before the changes.
    /// These lines are used to locate the correct position in the document.
    /// </summary>
    /// <value>A list of context lines preceding the changes.</value>
    public List<string> BeforeContext { get; } = new();

    /// <summary>
    /// Gets the list of lines that should be removed from the original document.
    /// These correspond to lines prefixed with '-' in the diff format.
    /// </summary>
    /// <value>A list of lines to be removed.</value>
    public List<string> Removals { get; } = new();

    /// <summary>
    /// Gets the list of lines that should be added to the document.
    /// These correspond to lines prefixed with '+' in the diff format.
    /// </summary>
    /// <value>A list of lines to be added.</value>
    public List<string> Additions { get; } = new();

    /// <summary>
    /// Gets the list of context lines that appear after the changes.
    /// These lines help confirm the correct positioning of the changes.
    /// </summary>
    /// <value>A list of context lines following the changes.</value>
    public List<string> AfterContext { get; } = new();

    /// <summary>
    /// Determines whether this diff block contains any actual changes.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the block contains removals or additions;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// A block with only context lines and no removals or additions
    /// is considered to have no changes.
    /// </remarks>
    public bool HasChanges() => Removals.Any() || Additions.Any();

    public override string ToString()
    {
        return $"BeforeContext: [{string.Join(", ", BeforeContext)}], " +
               $"Removals: [{string.Join(", ", Removals)}], " +
               $"Additions: [{string.Join(", ", Additions)}], " +
               $"AfterContext: [{string.Join(", ", AfterContext)}]";
    }
}
