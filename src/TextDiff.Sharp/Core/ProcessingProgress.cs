namespace TextDiff.Core;

/// <summary>
/// Represents progress information for long-running diff processing operations.
/// Provides detailed feedback about processing stage, items completed, and percentage progress.
/// </summary>
/// <remarks>
/// ProcessingProgress is designed for use with IProgress&lt;T&gt; to provide real-time feedback
/// during async diff processing operations. It tracks:
/// - Current processing stage (parsing, applying changes, etc.)
/// - Number of items processed (lines, blocks, etc.)
/// - Total expected items for accurate percentage calculation
///
/// This enables responsive user interfaces and monitoring of long-running operations,
/// particularly useful when processing large documents or multiple files.
///
/// The progress information is immutable to ensure thread safety during
/// concurrent progress reporting scenarios.
/// </remarks>
/// <example>
/// <code>
/// var progress = new Progress&lt;ProcessingProgress&gt;(p =&gt;
/// {
///     Console.WriteLine($"{p.Stage}: {p.PercentComplete:F1}% ({p.ProcessedItems}/{p.TotalItems})");
/// });
///
/// var result = await differ.ProcessAsync(document, diff, CancellationToken.None, progress);
/// </code>
/// </example>
public class ProcessingProgress
{
    /// <summary>
    /// Gets the current processing stage description.
    /// </summary>
    /// <value>A human-readable description of the current processing phase.</value>
    /// <remarks>
    /// Common stage values include:
    /// - "Parsing diff" - Reading and parsing the diff content
    /// - "Processing blocks" - Applying diff blocks to the document
    /// - "Generating output" - Creating the final result
    /// - "Completed" - Processing finished successfully
    ///
    /// The stage description helps users understand what phase of processing
    /// is currently active, enabling more informative progress displays.
    /// </remarks>
    public string Stage { get; }

    /// <summary>
    /// Gets the number of items that have been processed so far.
    /// </summary>
    /// <value>The count of completed processing units (lines, blocks, etc.).</value>
    /// <remarks>
    /// The meaning of "items" depends on the processing stage:
    /// - During parsing: number of diff lines processed
    /// - During application: number of document lines processed
    /// - During output: number of result lines generated
    ///
    /// This value should always be less than or equal to <see cref="TotalItems"/>
    /// and increases monotonically during processing.
    /// </remarks>
    public long ProcessedItems { get; }

    /// <summary>
    /// Gets the total number of items that will be processed.
    /// </summary>
    /// <value>The complete count of processing units for the current operation.</value>
    /// <remarks>
    /// This represents the denominator for percentage calculations and helps
    /// estimate remaining processing time. The total may be:
    /// - Known precisely (e.g., total line count from pre-analysis)
    /// - Estimated (e.g., based on file size)
    /// - Updated during processing (e.g., when discovering additional work)
    ///
    /// A value of 0 indicates indeterminate progress, and <see cref="PercentComplete"/>
    /// will return 0.0 in such cases.
    /// </remarks>
    public long TotalItems { get; }

    /// <summary>
    /// Gets the completion percentage for the current processing operation.
    /// </summary>
    /// <value>A percentage value between 0.0 and 100.0 indicating completion progress.</value>
    /// <remarks>
    /// This calculated property provides a convenient percentage representation
    /// of processing progress. The calculation is:
    /// - (ProcessedItems / TotalItems) * 100.0 when TotalItems > 0
    /// - 0.0 when TotalItems is 0 (indeterminate progress)
    ///
    /// The percentage is precise to multiple decimal places and suitable
    /// for both display and programmatic threshold checking.
    /// </remarks>
    public double PercentComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100.0 : 0.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingProgress"/> class.
    /// </summary>
    /// <param name="stage">The current processing stage description.</param>
    /// <param name="processedItems">The number of items processed so far.</param>
    /// <param name="totalItems">The total number of items to be processed.</param>
    /// <remarks>
    /// Creates an immutable progress snapshot representing the current state
    /// of a long-running operation. All parameters are stored directly without
    /// validation to maximize performance during frequent progress updates.
    ///
    /// It is the caller's responsibility to ensure that:
    /// - Stage descriptions are meaningful and consistent
    /// - ProcessedItems is not greater than TotalItems
    /// - Values accurately represent the current processing state
    /// </remarks>
    public ProcessingProgress(string stage, long processedItems, long totalItems)
    {
        Stage = stage;
        ProcessedItems = processedItems;
        TotalItems = totalItems;
    }
}
