namespace TextDiff.Exceptions;

/// <summary>
/// Base exception for all TextDiff-related errors.
/// </summary>
public class TextDiffException : Exception
{
    public TextDiffException(string message) : base(message) { }

    public TextDiffException(string message, Exception innerException) : base(message, innerException) { }
}