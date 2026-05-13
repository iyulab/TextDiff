namespace TextDiff.Models;

/// <summary>
/// Represents the result of a streaming diff processing operation.
/// </summary>
/// <remarks>
/// For streaming operations, the processed document is written directly to the output stream.
/// This type provides only change statistics; the text content is not held in memory.
/// </remarks>
public record StreamProcessResult(ChangeStats Changes);
