using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Defines the contract for tracking and accumulating change statistics during diff processing.
/// Implementations analyze diff blocks and update comprehensive change metrics.
/// </summary>
/// <remarks>
/// The IChangeTracker interface abstracts the logic for analyzing and recording
/// the quantitative impact of diff operations. Key responsibilities:
/// - Analyze diff blocks to categorize types of changes
/// - Distinguish between additions, removals, and modifications
/// - Accumulate statistics across multiple diff blocks
/// - Provide accurate change metrics for reporting and validation
/// - Support different change classification strategies
///
/// Change tracking is essential for:
/// - Generating accurate change reports and summaries
/// - Validating that expected modifications were applied
/// - Performance monitoring and optimization analysis
/// - User feedback about processing scope and impact
/// - Quality assurance and testing validation
///
/// The tracker operates on individual diff blocks and maintains running
/// totals that represent the cumulative impact of all processed changes.
/// </remarks>
/// <example>
/// <code>
/// var tracker = new ChangeTracker();
/// var stats = new ChangeStats();
/// var block = new DiffBlock();
/// block.Additions.Add("new line");
/// block.Removals.Add("old line");
///
/// tracker.TrackChanges(block, stats);
/// Console.WriteLine($"Changes: +{stats.AddedLines} -{stats.DeletedLines} ~{stats.ChangedLines}");
/// </code>
/// </example>
public interface IChangeTracker
{
    /// <summary>
    /// Analyzes a diff block and updates the provided change statistics.
    /// </summary>
    /// <param name="block">
    /// The diff block to analyze for change impact. Contains additions, removals,
    /// and context lines that determine the scope of modifications.
    /// </param>
    /// <param name="stats">
    /// The change statistics object to update with the analysis results.
    /// Counts will be incremented based on the changes found in the block.
    /// </param>
    /// <remarks>
    /// The tracking process analyzes the diff block to categorize changes:
    ///
    /// **Addition Tracking:**
    /// - Counts lines in the block's Additions collection
    /// - Represents net new content added to the document
    /// - Increases the document's total line count
    ///
    /// **Removal Tracking:**
    /// - Counts lines in the block's Removals collection
    /// - Represents content deleted from the original document
    /// - Decreases the document's total line count
    ///
    /// **Modification Tracking:**
    /// - Identifies paired removals and additions (line modifications)
    /// - Represents content changes rather than pure additions/deletions
    /// - Maintains the document's line count but alters content
    ///
    /// **Context Handling:**
    /// - Context lines (BeforeContext, AfterContext) are not counted as changes
    /// - They provide positioning information but don't affect statistics
    /// - Used for validation but not included in change metrics
    ///
    /// The implementation should handle edge cases like empty blocks,
    /// blocks with only context lines, and complex change patterns.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="block"/> or <paramref name="stats"/> is <see langword="null"/>.
    /// </exception>
    void TrackChanges(DiffBlock block, ChangeStats stats);
}
