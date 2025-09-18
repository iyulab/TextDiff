using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Defines the contract for finding the correct position in a document where diff blocks should be applied.
/// Implementations use context matching algorithms to locate the precise insertion point for changes.
/// </summary>
/// <remarks>
/// The IContextMatcher interface abstracts the complex logic of positioning diff blocks
/// within target documents. This is crucial for successful diff application because:
/// - Line numbers in diffs may not match current document state
/// - Documents may have been modified since the diff was created
/// - Context lines provide the positioning anchor points
/// - Fuzzy matching may be needed for partially matching contexts
///
/// Key responsibilities:
/// - Match context lines against document content
/// - Handle variations in whitespace and formatting
/// - Support both exact and approximate matching strategies
/// - Provide robust positioning even with document changes
/// - Maintain performance for large documents
///
/// The matcher works with both before and after context to ensure accurate
/// positioning and validate that changes will be applied correctly.
/// </remarks>
/// <example>
/// <code>
/// var matcher = new ContextMatcher();
/// var block = new DiffBlock();
/// block.BeforeContext.Add("  function calculate() {");
/// block.BeforeContext.Add("    var result = 0;");
///
/// // Find where this context appears in the document
/// int position = matcher.FindPosition(documentLines, 0, block);
/// if (position >= 0)
/// {
///     Console.WriteLine($"Found matching context at line {position}");
/// }
/// </code>
/// </example>
public interface IContextMatcher
{
    /// <summary>
    /// Finds the position in the document where the specified diff block should be applied.
    /// </summary>
    /// <param name="documentLines">
    /// The document content as an array of lines to search within.
    /// </param>
    /// <param name="startPosition">
    /// The starting line position for the search. Context matching will begin
    /// from this position and search forward through the document.
    /// </param>
    /// <param name="block">
    /// The diff block containing context lines to match against the document.
    /// The block's BeforeContext property provides the matching criteria.
    /// </param>
    /// <returns>
    /// The zero-based line position where the diff block should be applied,
    /// or -1 if no matching context could be found in the document.
    /// </returns>
    /// <remarks>
    /// The position finding algorithm:
    /// 1. Starts searching from the specified startPosition
    /// 2. Attempts to match the block's BeforeContext lines against document content
    /// 3. Uses fuzzy matching to handle minor formatting differences
    /// 4. Returns the position where changes should be inserted/applied
    /// 5. Continues searching if initial matches fail validation
    ///
    /// The returned position represents the line where the first change
    /// (addition or removal) should be applied, taking into account the
    /// context lines that establish the correct location.
    ///
    /// Matching strategies may include:
    /// - Exact line matching for precise contexts
    /// - Whitespace-normalized matching for formatting tolerance
    /// - Fuzzy matching for handling minor document changes
    /// - Multi-line context matching for better accuracy
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentLines"/> or <paramref name="block"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="startPosition"/> is negative or beyond the document length.
    /// </exception>
    int FindPosition(string[] documentLines, int startPosition, DiffBlock block);
}
