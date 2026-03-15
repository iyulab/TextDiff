using System.Text;
using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Streaming diff processor for handling large files with minimal memory usage.
/// Processes document line by line instead of loading everything into memory.
/// </summary>
public class StreamingDiffProcessor
{
    private readonly IContextMatcher _contextMatcher;
    private readonly IChangeTracker _changeTracker;
    private const int DefaultBufferSize = 8192;

    public StreamingDiffProcessor(IContextMatcher contextMatcher, IChangeTracker changeTracker)
    {
        _contextMatcher = contextMatcher;
        _changeTracker = changeTracker;
    }

    /// <summary>
    /// Process diff with streaming approach to minimize memory usage.
    /// </summary>
    /// <remarks>
    /// For streaming operations, the result is written directly to the output stream.
    /// The returned ProcessResult.Text will be empty; use the output stream to access the result.
    /// </remarks>
    public async Task<ProcessResult> ProcessStreamingAsync(
        Stream documentStream,
        Stream diffStream,
        Stream outputStream,
        CancellationToken cancellationToken = default,
        IProgress<ProcessingProgress>? progress = null)
    {
        // Reset context matcher state for thread safety
        _contextMatcher.Reset();

        // Detect line separator from document stream before parsing
        string lineSeparator = await DetectLineSeparatorFromStreamAsync(documentStream, cancellationToken);

        var diffBlocks = await ParseDiffStreamAsync(diffStream, cancellationToken);
        var changes = new ChangeStats();

        using var documentReader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, DefaultBufferSize, leaveOpen: true);
        using var outputWriter = new StreamWriter(outputStream, Encoding.UTF8, DefaultBufferSize, leaveOpen: true);

        // Apply detected line separator so output round-trips cleanly
        outputWriter.NewLine = lineSeparator;

        await ProcessDocumentStreamAsync(documentReader, outputWriter, diffBlocks, changes, cancellationToken, progress);

        // For streaming operations, text is written to output stream, not returned
        return new ProcessResult(string.Empty, changes);
    }

    /// <summary>
    /// Detects the line separator from a seekable stream by reading raw bytes.
    /// Resets the stream position to 0 after detection.
    /// Falls back to "\n" for non-seekable streams.
    /// </summary>
    private static async Task<string> DetectLineSeparatorFromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            return "\n";

        // Read a small chunk to find the first newline
        var buffer = new byte[Math.Min(4096, stream.Length)];
        stream.Position = 0;
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        stream.Position = 0;

        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == (byte)'\n')
            {
                return (i > 0 && buffer[i - 1] == (byte)'\r') ? "\r\n" : "\n";
            }
        }

        return "\n"; // Default to LF if no newline found
    }

    /// <summary>
    /// Process diff from string inputs with streaming optimizations.
    /// </summary>
    public ProcessResult ProcessWithStreaming(string document, string diff, int bufferSizeHint = DefaultBufferSize)
    {
        // Reset context matcher state for thread safety
        _contextMatcher.Reset();

        var documentLines = MemoryEfficientTextUtils.SplitLinesEfficient(document);
        var diffLines = MemoryEfficientTextUtils.SplitLinesEfficient(diff);

        var parser = new DiffBlockParser();
        var blocks = parser.Parse(diffLines).ToList();

        // Detect line separator from document (fall back to diff, then platform default)
        string? lineSeparator = DetectLineSeparator(document) ?? DetectLineSeparator(diff);

        var buffer = new OptimizedLineBuffer(Math.Max(bufferSizeHint, document.Length / 10), lineSeparator);
        var changes = new ChangeStats();
        int currentPosition = 0;

        foreach (var block in blocks)
        {
            ProcessBlockStreaming(documentLines, buffer, block, ref currentPosition, changes);
        }

        // Copy remaining lines
        while (currentPosition < documentLines.Length)
        {
            buffer.AddLine(documentLines[currentPosition]);
            currentPosition++;
        }

        return new ProcessResult(buffer.ToString(), changes);
    }

    /// <summary>
    /// Detects the line separator used in the given text.
    /// Returns "\r\n" for CRLF, "\n" for LF, or null when the text is too
    /// short to determine (single-line / empty).
    /// </summary>
    private static string? DetectLineSeparator(string text)
    {
        int idx = text.IndexOf('\n');
        if (idx < 0)
            return null;
        return idx > 0 && text[idx - 1] == '\r' ? "\r\n" : "\n";
    }

    private async Task<List<DiffBlock>> ParseDiffStreamAsync(Stream diffStream, CancellationToken cancellationToken)
    {
        var blocks = new List<DiffBlock>();
        var parser = new DiffBlockParser();

        using var reader = new StreamReader(diffStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, DefaultBufferSize);
        var diffLines = new List<string>();

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            diffLines.Add(line);
        }

        return parser.Parse(diffLines.ToArray()).ToList();
    }

    private async Task ProcessDocumentStreamAsync(
        StreamReader documentReader,
        StreamWriter outputWriter,
        List<DiffBlock> diffBlocks,
        ChangeStats changes,
        CancellationToken cancellationToken,
        IProgress<ProcessingProgress>? progress)
    {
        var documentLines = new List<string>();
        string? line;
        long totalLines = 0;
        long processedLines = 0;

        // Pre-read document to apply diff blocks (required for context matching)
        while ((line = await documentReader.ReadLineAsync(cancellationToken)) != null)
        {
            documentLines.Add(line);
            totalLines++;

            if (totalLines % 1000 == 0)
            {
                progress?.Report(new ProcessingProgress("Reading document", totalLines, 0));
            }
        }

        int currentPosition = 0;

        foreach (var block in diffBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int blockPosition = currentPosition;

            if (block.BeforeContext.Any() || block.Removals.Any())
            {
                blockPosition = _contextMatcher.FindPosition(documentLines.ToArray(), currentPosition, block);
                if (blockPosition == -1)
                {
                    throw new InvalidOperationException($"Cannot find matching position for block: {block}");
                }
            }

            // Copy lines until block position
            while (currentPosition < blockPosition && currentPosition < documentLines.Count)
            {
                await outputWriter.WriteLineAsync(documentLines[currentPosition]);
                currentPosition++;
                processedLines++;
            }

            // Process block
            currentPosition = await ProcessBlockStreamingAsync(outputWriter, documentLines, block, currentPosition, changes);
            processedLines += block.BeforeContext.Count + block.Additions.Count + block.AfterContext.Count;

            progress?.Report(new ProcessingProgress("Processing blocks", processedLines, totalLines));
        }

        // Copy remaining lines
        while (currentPosition < documentLines.Count)
        {
            await outputWriter.WriteLineAsync(documentLines[currentPosition]);
            currentPosition++;
        }

        await outputWriter.FlushAsync();
    }

    private void ProcessBlockStreaming(
        string[] documentLines,
        OptimizedLineBuffer buffer,
        DiffBlock block,
        ref int currentPosition,
        ChangeStats changes)
    {
        int blockPosition = currentPosition;

        if (block.BeforeContext.Any())
        {
            blockPosition = _contextMatcher.FindPosition(documentLines, currentPosition, block);
            if (blockPosition == -1)
            {
                throw new InvalidOperationException($"Cannot find matching position for block: {block}");
            }
        }

        // Copy lines until block position
        while (currentPosition < blockPosition && currentPosition < documentLines.Length)
        {
            buffer.AddLine(documentLines[currentPosition]);
            currentPosition++;
        }

        // Process context and changes
        ProcessContextAndChanges(buffer, documentLines, block, ref currentPosition);
        _changeTracker.TrackChanges(block, changes);
    }

    private async Task<int> ProcessBlockStreamingAsync(
        StreamWriter outputWriter,
        List<string> documentLines,
        DiffBlock block,
        int currentPosition,
        ChangeStats changes)
    {
        // Process before context
        foreach (var contextLine in block.BeforeContext)
        {
            if (currentPosition < documentLines.Count)
            {
                await outputWriter.WriteLineAsync(documentLines[currentPosition]);
            }
            currentPosition++;
        }

        // Process changes
        int removalIndex = 0;
        int minCount = Math.Min(block.Removals.Count, block.Additions.Count);

        // Changed lines (preserve original indentation only for non-whitespace lines,
        // and only when the diff does not explicitly change the indentation style)
        for (int i = 0; i < minCount; i++)
        {
            var originalLine = documentLines[currentPosition + i];
            var addedLine = block.Additions[i];
            var removalLine = block.Removals[i];

            if (string.IsNullOrWhiteSpace(originalLine))
            {
                await outputWriter.WriteLineAsync(addedLine);
            }
            else
            {
                string removalIndentation = TextUtils.ExtractIndentation(removalLine);
                string additionIndentation = TextUtils.ExtractIndentation(addedLine);

                if (removalIndentation != additionIndentation)
                {
                    await outputWriter.WriteLineAsync(addedLine);
                }
                else
                {
                    string indentation = TextUtils.ExtractIndentation(originalLine);
                    string newContent = TextUtils.RemoveIndentation(addedLine);
                    await outputWriter.WriteLineAsync(indentation + newContent);
                }
            }

            removalIndex++;
        }

        // Pure additions
        for (int i = removalIndex; i < block.Additions.Count; i++)
        {
            await outputWriter.WriteLineAsync(block.Additions[i]);
        }

        currentPosition += block.Removals.Count;

        // Process after context
        foreach (var contextLine in block.AfterContext)
        {
            if (currentPosition < documentLines.Count)
            {
                await outputWriter.WriteLineAsync(documentLines[currentPosition]);
            }
            currentPosition++;
        }

        _changeTracker.TrackChanges(block, changes);
        return currentPosition;
    }

    private void ProcessContextAndChanges(
        OptimizedLineBuffer buffer,
        string[] documentLines,
        DiffBlock block,
        ref int currentPosition)
    {
        // Process before context
        foreach (var _ in block.BeforeContext)
        {
            if (currentPosition < documentLines.Length)
            {
                buffer.AddLine(documentLines[currentPosition]);
            }
            currentPosition++;
        }

        // Process changes
        int removalIndex = 0;
        int minCount = Math.Min(block.Removals.Count, block.Additions.Count);

        // Changed lines (preserve original indentation only for non-whitespace lines,
        // and only when the diff does not explicitly change the indentation style)
        for (int i = 0; i < minCount; i++)
        {
            var originalLine = documentLines[currentPosition + i];
            var addedLine = block.Additions[i];
            var removalLine = block.Removals[i];

            if (string.IsNullOrWhiteSpace(originalLine))
            {
                buffer.AddLine(addedLine);
            }
            else
            {
                string removalIndentation = TextUtils.ExtractIndentation(removalLine);
                string additionIndentation = TextUtils.ExtractIndentation(addedLine);

                if (removalIndentation != additionIndentation)
                {
                    buffer.AddLine(addedLine);
                }
                else
                {
                    string indentation = TextUtils.ExtractIndentation(originalLine);
                    string newContent = TextUtils.RemoveIndentation(addedLine);
                    buffer.AddLine(indentation + newContent);
                }
            }

            removalIndex++;
        }

        // Pure additions
        for (int i = removalIndex; i < block.Additions.Count; i++)
        {
            buffer.AddLine(block.Additions[i]);
        }

        currentPosition += block.Removals.Count;

        // Process after context
        foreach (var _ in block.AfterContext)
        {
            if (currentPosition < documentLines.Length)
            {
                buffer.AddLine(documentLines[currentPosition]);
            }
            currentPosition++;
        }
    }
}

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