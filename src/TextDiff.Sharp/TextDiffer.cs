using TextDiff.Core;
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
}