namespace TextDiff.DiffX;

/// <summary>
/// Reads and extracts applicable diff sections from DiffX format files.
/// </summary>
/// <remarks>
/// This interface follows TextDiff.Sharp's core philosophy of focusing solely on
/// diff application. It extracts only the unified diff content from DiffX files,
/// leaving metadata management to the caller.
///
/// DiffX is an extensible diff format that supports:
/// - Multi-file diffs with structured organization
/// - Rich metadata (commit info, timestamps, authors)
/// - Binary diff support
///
/// This reader focuses on extracting the `#...diff:` sections which contain
/// standard unified diff content that can be processed by TextDiffer.
/// </remarks>
public interface IDiffXReader
{
    /// <summary>
    /// Determines whether the content is in DiffX format.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns><c>true</c> if the content starts with a valid DiffX header; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// DiffX format is identified by the first line starting with "#diffx:".
    /// This method performs a quick check without parsing the entire content.
    /// </remarks>
    bool IsDiffX(string content);

    /// <summary>
    /// Extracts file diff entries from DiffX content.
    /// </summary>
    /// <param name="diffXContent">The DiffX format content.</param>
    /// <returns>
    /// An enumerable of <see cref="DiffXFileEntry"/> objects, each containing
    /// the extracted unified diff content for a single file.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diffXContent"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="diffXContent"/> is not in valid DiffX format.</exception>
    /// <remarks>
    /// This method extracts only the content from `#...diff:` sections.
    /// Binary diffs (indicated by `#...diff: op=binary`) are skipped as they
    /// require specialized handling outside the scope of unified diff processing.
    /// </remarks>
    IEnumerable<DiffXFileEntry> ExtractFileDiffs(string diffXContent);
}
