namespace TextDiff.Exceptions;

/// <summary>
/// Exception thrown when a diff cannot be applied to the original document.
/// </summary>
public class DiffApplicationException : TextDiffException
{
    public int? DocumentPosition { get; }

    public DiffApplicationException(string message) : base(message) { }

    public DiffApplicationException(string message, int documentPosition) : base(message)
    {
        DocumentPosition = documentPosition;
    }

    public DiffApplicationException(string message, Exception innerException) : base(message, innerException) { }
}