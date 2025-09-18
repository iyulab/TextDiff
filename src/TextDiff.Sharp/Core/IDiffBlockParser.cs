using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Defines the contract for parsing unified diff content into structured diff blocks.
/// Implementations convert raw diff text into processable <see cref="DiffBlock"/> objects.
/// </summary>
/// <remarks>
/// The IDiffBlockParser interface abstracts the parsing logic for unified diff format,
/// enabling different parsing strategies and supporting extensibility. Key responsibilities:
/// - Parse unified diff lines into structured diff blocks
/// - Handle various diff formats and edge cases
/// - Maintain context information for accurate positioning
/// - Support both simple and complex diff scenarios
///
/// Implementations should handle:
/// - Standard unified diff format (context lines, additions, removals)
/// - Header lines (@@ -start,length +start,length @@)
/// - File metadata (---, +++ lines)
/// - Binary file indicators
/// - Malformed or incomplete diff content
///
/// The parser operates on pre-split diff lines to optimize memory usage
/// and enable streaming scenarios.
/// </remarks>
/// <example>
/// <code>
/// // Typical usage pattern
/// var parser = new DiffBlockParser();
/// string[] diffLines = diff.Split('\n');
/// var blocks = parser.Parse(diffLines);
///
/// foreach (var block in blocks)
/// {
///     Console.WriteLine($"Block with {block.Additions.Count} additions, {block.Removals.Count} removals");
/// }
/// </code>
/// </example>
public interface IDiffBlockParser
{
    /// <summary>
    /// Parses unified diff lines into a sequence of structured diff blocks.
    /// </summary>
    /// <param name="diffLines">
    /// An array of diff lines in unified diff format, typically obtained by splitting
    /// diff content on line breaks.
    /// </param>
    /// <returns>
    /// A sequence of <see cref="DiffBlock"/> objects representing the parsed diff content.
    /// Each block contains context lines, additions, and removals for a specific
    /// section of the document.
    /// </returns>
    /// <remarks>
    /// The parsing process:
    /// 1. Identifies diff block boundaries using @@ headers
    /// 2. Categorizes lines by prefix: ' ' (context), '+' (addition), '-' (removal)
    /// 3. Groups related changes into logical blocks
    /// 4. Preserves context information for accurate positioning
    /// 5. Handles edge cases like empty blocks or malformed content
    ///
    /// The returned sequence is lazily evaluated, allowing efficient processing
    /// of large diff files without loading all blocks into memory simultaneously.
    ///
    /// Lines are expected to follow unified diff conventions:
    /// - Context lines: prefixed with ' ' (space)
    /// - Addition lines: prefixed with '+'
    /// - Removal lines: prefixed with '-'
    /// - Header lines: starting with '@@'
    /// - File metadata: starting with '---' or '+++'
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="diffLines"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidDiffFormatException">
    /// Thrown when the diff format is invalid or cannot be parsed.
    /// </exception>
    IEnumerable<DiffBlock> Parse(string[] diffLines);
}