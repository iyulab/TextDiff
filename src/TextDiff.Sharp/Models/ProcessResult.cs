namespace TextDiff.Models;

/// <summary>
/// Represents the result of a diff processing operation.
/// Contains both the transformed document text and comprehensive change statistics.
/// </summary>
/// <remarks>
/// ProcessResult is the primary return type for all TextDiffer operations.
/// It encapsulates:
/// - The final document text after applying all diff changes
/// - Detailed statistics about what modifications were performed
///
/// This immutable result object ensures that processing outcomes are
/// predictable and that change tracking information is always available
/// for reporting, validation, or further processing steps.
///
/// The result is designed to be safe for concurrent access and can be
/// passed between different processing stages without risk of modification.
/// </remarks>
/// <example>
/// <code>
/// var differ = new TextDiffer();
/// var result = differ.Process(originalDocument, diffContent);
///
/// // Access the transformed text
/// Console.WriteLine($"Updated document:\n{result.Text}");
///
/// // Review change statistics
/// Console.WriteLine($"Changes applied:");
/// Console.WriteLine($"  Modified: {result.Changes.ChangedLines} lines");
/// Console.WriteLine($"  Added: {result.Changes.AddedLines} lines");
/// Console.WriteLine($"  Deleted: {result.Changes.DeletedLines} lines");
///
/// // Calculate total impact
/// int totalChanges = result.Changes.ChangedLines +
///                   result.Changes.AddedLines +
///                   result.Changes.DeletedLines;
/// Console.WriteLine($"Total impact: {totalChanges} lines affected");
/// </code>
/// </example>
public class ProcessResult
{
    /// <summary>
    /// Gets the final document text after applying all diff changes.
    /// </summary>
    /// <value>The complete document content with all modifications applied.</value>
    /// <remarks>
    /// This property contains the result of applying the unified diff to the original document.
    /// The text includes:
    /// - All original lines that were not modified
    /// - Modified lines with their new content
    /// - Newly added lines inserted at the correct positions
    /// - Proper line endings and formatting preserved from the original document
    ///
    /// The text is guaranteed to be non-null and represents a valid document
    /// that can be written to a file or used for further processing.
    /// </remarks>
    public string Text { get; private set; }

    /// <summary>
    /// Gets the comprehensive statistics about changes applied during processing.
    /// </summary>
    /// <value>A <see cref="ChangeStats"/> object containing detailed modification metrics.</value>
    /// <remarks>
    /// This property provides quantitative information about the scope and impact
    /// of the diff processing operation. The statistics include:
    /// - Number of lines that were modified (content changed)
    /// - Number of lines that were added to the document
    /// - Number of lines that were removed from the document
    ///
    /// These metrics are useful for:
    /// - Generating change reports and summaries
    /// - Validating that expected modifications were applied
    /// - Performance analysis and optimization
    /// - User feedback about processing results
    /// </remarks>
    public ChangeStats Changes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessResult"/> class.
    /// </summary>
    /// <param name="text">The final document text after applying diff changes.</param>
    /// <param name="changes">The statistics about changes that were applied.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> or <paramref name="changes"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// This constructor ensures that ProcessResult instances are always created
    /// with valid, non-null data. The immutable design prevents accidental
    /// modification of results after creation.
    ///
    /// Both parameters are required as they represent the essential outcomes
    /// of any diff processing operation: the transformed content and the
    /// metrics describing what changes were made.
    /// </remarks>
    public ProcessResult(string text, ChangeStats changes)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }
}