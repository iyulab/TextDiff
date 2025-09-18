namespace TextDiff.Exceptions;

/// <summary>
/// Exception thrown when the diff format is invalid or cannot be parsed.
/// </summary>
public class InvalidDiffFormatException : TextDiffException
{
    public int? LineNumber { get; }

    public InvalidDiffFormatException(string message) : base(message) { }

    public InvalidDiffFormatException(string message, int lineNumber) : base(message)
    {
        LineNumber = lineNumber;
    }

    public InvalidDiffFormatException(string message, Exception innerException) : base(message, innerException) { }
}