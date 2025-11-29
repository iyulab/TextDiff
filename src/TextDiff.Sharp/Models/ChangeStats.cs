namespace TextDiff.Models;

/// <summary>
/// Represents statistics about changes applied during diff processing.
/// Provides quantitative metrics about the scope and impact of modifications.
/// </summary>
/// <remarks>
/// ChangeStats tracks three types of line modifications:
/// - Changed lines: Existing lines that were modified (combination of removal + addition)
/// - Added lines: New lines inserted into the document
/// - Deleted lines: Lines removed from the original document
///
/// This information is useful for:
/// - Generating change summaries and reports
/// - Calculating diff complexity metrics
/// - Determining processing performance characteristics
/// - Providing user feedback about modification scope
/// </remarks>
/// <example>
/// <code>
/// var stats = new ChangeStats();
/// // Stats are accumulated during processing
/// Console.WriteLine($"Total modifications: {stats.ChangedLines + stats.AddedLines + stats.DeletedLines} lines");
/// Console.WriteLine($"Net line change: {stats.AddedLines - stats.DeletedLines}");
/// </code>
/// </example>
public sealed class ChangeStats
{
    /// <summary>
    /// Gets or sets the number of lines that were modified (changed content).
    /// </summary>
    /// <value>The count of lines where content was altered but the line position remained the same.</value>
    /// <remarks>
    /// This represents lines where the original content was replaced with new content.
    /// These are typically lines that appear as both removal (-) and addition (+) pairs
    /// in the unified diff format, indicating a content modification rather than
    /// pure insertion or deletion.
    /// </remarks>
    public int ChangedLines { get; set; }

    /// <summary>
    /// Gets or sets the number of lines that were added to the document.
    /// </summary>
    /// <value>The count of new lines inserted into the document.</value>
    /// <remarks>
    /// This represents net additions - lines that appear in the diff with a '+' prefix
    /// and don't have corresponding removal lines. These increase the total
    /// line count of the document.
    /// </remarks>
    public int AddedLines { get; set; }

    /// <summary>
    /// Gets or sets the number of lines that were removed from the document.
    /// </summary>
    /// <value>The count of lines deleted from the original document.</value>
    /// <remarks>
    /// This represents net deletions - lines that appear in the diff with a '-' prefix
    /// and don't have corresponding addition lines. These decrease the total
    /// line count of the document.
    /// </remarks>
    public int DeletedLines { get; set; }

    /// <summary>
    /// Gets the total number of affected lines (changed + added + deleted).
    /// </summary>
    public int TotalAffectedLines => ChangedLines + AddedLines + DeletedLines;

    /// <summary>
    /// Gets the net line change (added - deleted).
    /// </summary>
    /// <remarks>
    /// A positive value indicates the document grew in size.
    /// A negative value indicates the document shrank in size.
    /// </remarks>
    public int NetLineChange => AddedLines - DeletedLines;
}
