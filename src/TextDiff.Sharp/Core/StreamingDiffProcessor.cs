using System.Text;
using TextDiff.Helpers;
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
    private readonly IDiffBlockParser _blockParser;
    private const int DefaultBufferSize = 8192;

    public StreamingDiffProcessor(
        IContextMatcher contextMatcher,
        IChangeTracker changeTracker,
        IDiffBlockParser? blockParser = null)
    {
        _contextMatcher = contextMatcher;
        _changeTracker = changeTracker;
        _blockParser = blockParser ?? new DiffBlockParser();
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

        var blocks = _blockParser.Parse(diffLines).ToList();

        // Detect line separator from document (fall back to diff, then platform default)
        string? lineSeparator = TextUtils.DetectLineSeparator(document) ?? TextUtils.DetectLineSeparator(diff);

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

    private async Task<List<DiffBlock>> ParseDiffStreamAsync(Stream diffStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(diffStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, DefaultBufferSize);
        var diffLines = new List<string>();

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            diffLines.Add(line);
        }

        return _blockParser.Parse(diffLines.ToArray()).ToList();
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
        var docLines = documentLines.ToArray();

        foreach (var block in diffBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int blockPosition = currentPosition;

            if (block.BeforeContext.Any() || block.Removals.Any())
            {
                blockPosition = _contextMatcher.FindPosition(docLines, currentPosition, block);
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

        if (block.BeforeContext.Any() || block.Removals.Any())
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
            var resultLine = TextUtils.ApplyIndentationPreservation(originalLine, block.Removals[i], addedLine);
            await outputWriter.WriteLineAsync(resultLine);
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
            var resultLine = TextUtils.ApplyIndentationPreservation(originalLine, block.Removals[i], addedLine);
            buffer.AddLine(resultLine);
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