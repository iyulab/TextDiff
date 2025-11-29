using TextDiff.Core;
using TextDiff.DiffX;
using TextDiff.Exceptions;
using TextDiff.Models;

namespace TextDiff;

/// <summary>
/// The primary class for applying unified diff content to original documents.
/// Provides comprehensive diff processing with multiple API variants for different use cases.
/// </summary>
/// <remarks>
/// TextDiffer is the main entry point for the TextDiff.Sharp library, offering a complete
/// solution for processing unified diff files and applying changes to text documents.
///
/// **Key Features:**
/// - Unified diff format parsing and application
/// - Multiple processing APIs: synchronous, asynchronous, streaming, and optimized
/// - Comprehensive error handling with specific exception types
/// - Change tracking and statistics reporting
/// - Memory-efficient processing for large files
/// - Cancellation and progress reporting support
/// - Dependency injection support for extensibility
///
/// **Processing Variants:**
/// - **Process()**: Standard synchronous processing for most use cases
/// - **ProcessAsync()**: Asynchronous processing with cancellation and progress support
/// - **ProcessStreamsAsync()**: Streaming processing for very large files
/// - **ProcessOptimized()**: Memory-optimized processing for performance-critical scenarios
///
/// **Thread Safety:**
/// TextDiffer instances are thread-safe for concurrent processing operations.
/// Multiple threads can safely call processing methods on the same instance.
///
/// **Dependency Injection:**
/// The class supports constructor injection of core interfaces, enabling
/// customization of parsing, context matching, and change tracking behaviors.
/// </remarks>
/// <example>
/// <code>
/// // Basic usage
/// var differ = new TextDiffer();
/// var result = differ.Process(originalDocument, diffContent);
/// Console.WriteLine($"Applied {result.Changes.AddedLines} additions, {result.Changes.DeletedLines} deletions");
///
/// // Async processing with progress
/// var progress = new Progress&lt;ProcessingProgress&gt;(p =&gt;
///     Console.WriteLine($"{p.Stage}: {p.PercentComplete:F1}%"));
/// var result = await differ.ProcessAsync(document, diff, CancellationToken.None, progress);
///
/// // Custom dependencies
/// var customParser = new AdvancedDiffBlockParser();
/// var customDiffer = new TextDiffer(blockParser: customParser);
/// </code>
/// </example>
public class TextDiffer
{
    private readonly IDiffBlockParser _blockParser;
    private readonly IContextMatcher _contextMatcher;
    private readonly IChangeTracker _changeTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextDiffer"/> class with optional custom dependencies.
    /// </summary>
    /// <param name="blockParser">
    /// The diff block parser to use for parsing unified diff content.
    /// If <see langword="null"/>, a default <see cref="DiffBlockParser"/> instance is used.
    /// </param>
    /// <param name="contextMatcher">
    /// The context matcher to use for finding application positions within documents.
    /// If <see langword="null"/>, a default <see cref="ContextMatcher"/> instance is used.
    /// </param>
    /// <param name="changeTracker">
    /// The change tracker to use for accumulating change statistics.
    /// If <see langword="null"/>, a default <see cref="ChangeTracker"/> instance is used.
    /// </param>
    /// <remarks>
    /// This constructor enables dependency injection scenarios where custom implementations
    /// of core interfaces may be required. Use cases include:
    /// - Custom parsing logic for non-standard diff formats
    /// - Advanced context matching algorithms for fuzzy matching
    /// - Specialized change tracking for reporting requirements
    /// - Testing scenarios with mock implementations
    ///
    /// When null values are provided, the constructor creates default implementations
    /// that handle standard unified diff processing scenarios.
    ///
    /// All dependencies are stored as readonly fields and used consistently
    /// across all processing methods, ensuring predictable behavior.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default implementation
    /// var differ = new TextDiffer();
    ///
    /// // Custom parser for advanced diff formats
    /// var customParser = new EnhancedDiffBlockParser();
    /// var differ = new TextDiffer(blockParser: customParser);
    ///
    /// // Full customization
    /// var differ = new TextDiffer(
    ///     blockParser: new CustomParser(),
    ///     contextMatcher: new FuzzyMatcher(),
    ///     changeTracker: new DetailedTracker()
    /// );
    /// </code>
    /// </example>
    public TextDiffer(
        IDiffBlockParser? blockParser = null,
        IContextMatcher? contextMatcher = null,
        IChangeTracker? changeTracker = null)
    {
        _blockParser = blockParser ?? new DiffBlockParser();
        _contextMatcher = contextMatcher ?? new ContextMatcher();
        _changeTracker = changeTracker ?? new ChangeTracker();
    }

    /// <summary>
    /// Processes a diff and applies it to the original document.
    /// </summary>
    /// <param name="document">The original document content.</param>
    /// <param name="diff">The diff content in unified diff format.</param>
    /// <returns>A ProcessResult containing the updated document and change statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when document or diff is null.</exception>
    /// <exception cref="ArgumentException">Thrown when document or diff is empty or contains only whitespace.</exception>
    /// <exception cref="InvalidDiffFormatException">Thrown when the diff format is invalid.</exception>
    /// <exception cref="DiffApplicationException">Thrown when the diff cannot be applied to the document.</exception>
    public ProcessResult Process(string document, string diff)
    {
        // Null validation
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (diff is null) throw new ArgumentNullException(nameof(diff));

        // Empty/whitespace validation
        if (string.IsNullOrWhiteSpace(diff))
            throw new ArgumentException("Diff cannot be empty or contain only whitespace.", nameof(diff));

        try
        {
            // Reset context matcher state for thread safety
            _contextMatcher.Reset();

            var documentLines = TextUtils.SplitLines(document);
            var diffLines = TextUtils.SplitLines(diff);

            // Validate diff format before processing
            ValidateDiffFormat(diffLines);

            var blocks = _blockParser.Parse(diffLines).ToList();

            var processor = new DocumentProcessor(documentLines, _contextMatcher, _changeTracker);
            return processor.ApplyBlocks(blocks);
        }
        catch (InvalidOperationException ex)
        {
            throw new DiffApplicationException($"Failed to apply diff to document: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is TextDiffException))
        {
            throw new TextDiffException($"Unexpected error during diff processing: {ex.Message}", ex);
        }
    }

    private static void ValidateDiffFormat(string[] diffLines)
    {
        if (diffLines.Length == 0)
            throw new InvalidDiffFormatException("Diff contains no lines.");

        bool hasValidDiffLine = false;
        for (int i = 0; i < diffLines.Length; i++)
        {
            var line = diffLines[i];
            if (line.Length > 0)
            {
                char prefix = line[0];
                if (prefix == '+' || prefix == '-' || prefix == ' ')
                {
                    hasValidDiffLine = true;
                }
                else if (prefix != '@' && prefix != '\\')
                {
                    // Allow common diff headers and special formats
                    if (!line.StartsWith("---") && !line.StartsWith("+++") &&
                        !line.StartsWith("diff ") && !line.StartsWith("index ") &&
                        !line.Equals("...") && !line.StartsWith("@@"))
                    {
                        throw new InvalidDiffFormatException(
                            $"Invalid diff line format at line {i + 1}: '{line}'", i + 1);
                    }
                }
            }
        }

        if (!hasValidDiffLine)
            throw new InvalidDiffFormatException("Diff does not contain any valid diff lines (lines starting with +, -, or space).");
    }

    /// <summary>
    /// Processes a diff asynchronously with support for cancellation and progress reporting.
    /// Optimized for large documents and long-running operations.
    /// </summary>
    /// <param name="document">The original document content.</param>
    /// <param name="diff">The diff content in unified diff format.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <param name="progress">Progress reporter for long-running operations.</param>
    /// <returns>A ProcessResult containing the updated document and change statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when document or diff is null.</exception>
    /// <exception cref="ArgumentException">Thrown when document or diff is empty or contains only whitespace.</exception>
    /// <exception cref="InvalidDiffFormatException">Thrown when the diff format is invalid.</exception>
    /// <exception cref="DiffApplicationException">Thrown when the diff cannot be applied to the document.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<ProcessResult> ProcessAsync(
        string document,
        string diff,
        CancellationToken cancellationToken = default,
        IProgress<ProcessingProgress>? progress = null)
    {
        // Input validation (same as sync version)
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (diff is null) throw new ArgumentNullException(nameof(diff));
        if (string.IsNullOrWhiteSpace(diff))
            throw new ArgumentException("Diff cannot be empty or contain only whitespace.", nameof(diff));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            progress?.Report(new ProcessingProgress("Parsing diff", 0, 100));

            var diffLines = TextUtils.SplitLines(diff);
            ValidateDiffFormat(diffLines);

            progress?.Report(new ProcessingProgress("Parsing diff blocks", 25, 100));
            var blocks = _blockParser.Parse(diffLines).ToList();

            progress?.Report(new ProcessingProgress("Processing document", 50, 100));

            // Use streaming processor for better performance on large documents
            var streamingProcessor = new StreamingDiffProcessor(_contextMatcher, _changeTracker);
            var result = await Task.Run(() => streamingProcessor.ProcessWithStreaming(document, diff), cancellationToken);

            progress?.Report(new ProcessingProgress("Completed", 100, 100));

            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new DiffApplicationException($"Failed to apply diff to document: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is TextDiffException) && !(ex is OperationCanceledException))
        {
            throw new TextDiffException($"Unexpected error during diff processing: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes a diff from streams asynchronously with minimal memory usage.
    /// Ideal for very large files that don't fit comfortably in memory.
    /// </summary>
    /// <param name="documentStream">Stream containing the original document.</param>
    /// <param name="diffStream">Stream containing the diff content.</param>
    /// <param name="outputStream">Stream to write the result to.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <param name="progress">Progress reporter for long-running operations.</param>
    /// <returns>A ProcessResult containing processing statistics.</returns>
    public async Task<ProcessResult> ProcessStreamsAsync(
        Stream documentStream,
        Stream diffStream,
        Stream outputStream,
        CancellationToken cancellationToken = default,
        IProgress<ProcessingProgress>? progress = null)
    {
        if (documentStream is null) throw new ArgumentNullException(nameof(documentStream));
        if (diffStream is null) throw new ArgumentNullException(nameof(diffStream));
        if (outputStream is null) throw new ArgumentNullException(nameof(outputStream));

        var streamingProcessor = new StreamingDiffProcessor(_contextMatcher, _changeTracker);
        return await streamingProcessor.ProcessStreamingAsync(
            documentStream, diffStream, outputStream, cancellationToken, progress);
    }

    /// <summary>
    /// Processes a diff with memory optimization for large documents.
    /// Uses streaming techniques while maintaining the synchronous API.
    /// </summary>
    /// <param name="document">The original document content.</param>
    /// <param name="diff">The diff content in unified diff format.</param>
    /// <param name="bufferSizeHint">Hint for buffer sizing optimization.</param>
    /// <returns>A ProcessResult containing the updated document and change statistics.</returns>
    public ProcessResult ProcessOptimized(string document, string diff, int bufferSizeHint = 8192)
    {
        // Input validation (same as original)
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (diff is null) throw new ArgumentNullException(nameof(diff));
        if (string.IsNullOrWhiteSpace(diff))
            throw new ArgumentException("Diff cannot be empty or contain only whitespace.", nameof(diff));

        try
        {
            var diffLines = MemoryEfficientTextUtils.SplitLinesEfficient(diff);
            ValidateDiffFormat(diffLines);

            var streamingProcessor = new StreamingDiffProcessor(_contextMatcher, _changeTracker);
            return streamingProcessor.ProcessWithStreaming(document, diff, bufferSizeHint);
        }
        catch (InvalidOperationException ex)
        {
            throw new DiffApplicationException($"Failed to apply diff to document: {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is TextDiffException))
        {
            throw new TextDiffException($"Unexpected error during diff processing: {ex.Message}", ex);
        }
    }

    #region DiffX Support

    /// <summary>
    /// Processes a diff that may be in either DiffX or unified diff format.
    /// Automatically detects the format and extracts applicable diff content.
    /// </summary>
    /// <param name="document">The original document content.</param>
    /// <param name="diffOrDiffX">The diff content in either DiffX or unified diff format.</param>
    /// <param name="filePath">
    /// Optional file path to filter DiffX entries. When specified, only the matching
    /// file's diff will be applied. When null, the first diff entry is used.
    /// </param>
    /// <returns>A ProcessResult containing the updated document and change statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when document or diff is null.</exception>
    /// <exception cref="ArgumentException">Thrown when diff is empty or no applicable diff found.</exception>
    /// <exception cref="InvalidDiffFormatException">Thrown when the diff format is invalid.</exception>
    /// <exception cref="DiffApplicationException">Thrown when the diff cannot be applied.</exception>
    /// <remarks>
    /// This method provides transparent DiffX support while maintaining backward compatibility.
    /// For standard unified diff input, it delegates directly to <see cref="Process"/>.
    /// For DiffX input, it extracts the relevant diff section before processing.
    /// </remarks>
    /// <example>
    /// <code>
    /// var differ = new TextDiffer();
    ///
    /// // Works with both formats
    /// var result1 = differ.ProcessDiffX(document, unifiedDiff);
    /// var result2 = differ.ProcessDiffX(document, diffXContent);
    ///
    /// // Target specific file in multi-file DiffX
    /// var result3 = differ.ProcessDiffX(document, diffXContent, "/src/main.py");
    /// </code>
    /// </example>
    public ProcessResult ProcessDiffX(string document, string diffOrDiffX, string? filePath = null)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (diffOrDiffX is null) throw new ArgumentNullException(nameof(diffOrDiffX));
        if (string.IsNullOrWhiteSpace(diffOrDiffX))
            throw new ArgumentException("Diff cannot be empty or contain only whitespace.", nameof(diffOrDiffX));

        var reader = new DiffXReader();

        // If not DiffX, fallback to standard processing
        if (!reader.IsDiffX(diffOrDiffX))
        {
            return Process(document, diffOrDiffX);
        }

        // Extract diff entries from DiffX
        var entries = reader.ExtractFileDiffs(diffOrDiffX).ToList();

        if (entries.Count == 0)
        {
            throw new DiffApplicationException("No applicable diff found in DiffX content.");
        }

        // Find target entry
        DiffXFileEntry? targetEntry;

        if (filePath != null)
        {
            targetEntry = entries.FirstOrDefault(e =>
                e.Path != null && e.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            if (targetEntry == null)
            {
                throw new DiffApplicationException(
                    $"No diff found for file '{filePath}'. " +
                    $"Available files: {string.Join(", ", entries.Where(e => e.Path != null).Select(e => e.Path))}");
            }
        }
        else
        {
            targetEntry = entries[0];
        }

        // Skip binary diffs
        if (targetEntry.IsBinary)
        {
            throw new DiffApplicationException(
                $"Cannot process binary diff for '{targetEntry.Path}'. " +
                "Binary diff support requires specialized handling.");
        }

        return Process(document, targetEntry.DiffContent);
    }

    /// <summary>
    /// Extracts all file diff entries from DiffX content without applying them.
    /// </summary>
    /// <param name="diffXContent">The DiffX format content.</param>
    /// <returns>An enumerable of <see cref="DiffXFileEntry"/> objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when content is not in DiffX format.</exception>
    /// <remarks>
    /// Use this method when you need to inspect DiffX content before applying,
    /// or when processing multiple files from a single DiffX.
    /// </remarks>
    /// <example>
    /// <code>
    /// var differ = new TextDiffer();
    /// var entries = differ.ExtractDiffXEntries(diffXContent);
    ///
    /// foreach (var entry in entries)
    /// {
    ///     var document = LoadDocument(entry.Path);
    ///     var result = differ.Process(document, entry.DiffContent);
    ///     SaveDocument(entry.Path, result.Text);
    /// }
    /// </code>
    /// </example>
    public IEnumerable<DiffXFileEntry> ExtractDiffXEntries(string diffXContent)
    {
        var reader = new DiffXReader();
        return reader.ExtractFileDiffs(diffXContent);
    }

    /// <summary>
    /// Determines whether the content is in DiffX format.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns><c>true</c> if the content is in DiffX format; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This is a quick check that only examines the first line of the content.
    /// Use this to determine which processing method to call.
    /// </remarks>
    public static bool IsDiffX(string content)
    {
        var reader = new DiffXReader();
        return reader.IsDiffX(content);
    }

    #endregion
}