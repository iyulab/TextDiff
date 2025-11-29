namespace TextDiff.DiffX;

/// <summary>
/// Represents a single file's diff entry extracted from a DiffX file.
/// </summary>
/// <remarks>
/// This record contains only the information needed for diff application:
/// - The unified diff content that can be processed by TextDiffer
/// - Basic file identification (path, operation) for routing purposes
///
/// Detailed metadata (author, timestamp, commit info) is intentionally omitted
/// as it falls outside TextDiff.Sharp's responsibility of diff application.
/// </remarks>
public record DiffXFileEntry
{
    /// <summary>
    /// Gets the file path extracted from the file metadata section.
    /// </summary>
    /// <value>
    /// The path of the file being modified, or <c>null</c> if not specified in metadata.
    /// </value>
    /// <remarks>
    /// This is typically extracted from the `#...meta:` section's "path" field.
    /// When null, the caller may need to determine the target from diff headers.
    /// </remarks>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the operation type for this file change.
    /// </summary>
    /// <value>
    /// One of: "create", "modify", "delete", "move", "copy", or <c>null</c> if not specified.
    /// </value>
    /// <remarks>
    /// This information helps the caller determine how to handle the diff:
    /// - create: New file, apply diff to empty document
    /// - modify: Update existing file
    /// - delete: Remove file (diff may be empty)
    /// - move/copy: File relocation with optional modifications
    /// </remarks>
    public string? Operation { get; init; }

    /// <summary>
    /// Gets the extracted unified diff content.
    /// </summary>
    /// <value>The standard unified diff format content.</value>
    /// <remarks>
    /// This content can be directly passed to <see cref="TextDiffer.Process"/>
    /// for application to the target document.
    /// </remarks>
    public required string DiffContent { get; init; }

    /// <summary>
    /// Gets the encoding specified for this diff section.
    /// </summary>
    /// <value>The encoding name, defaulting to "utf-8".</value>
    /// <remarks>
    /// Inherited from the parent DiffX file header or overridden in file metadata.
    /// </remarks>
    public string Encoding { get; init; } = "utf-8";

    /// <summary>
    /// Gets a value indicating whether this is a binary diff.
    /// </summary>
    /// <value><c>true</c> if this entry contains binary diff content; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Binary diffs require specialized handling and cannot be processed
    /// by the standard unified diff processor.
    /// </remarks>
    public bool IsBinary { get; init; }
}
