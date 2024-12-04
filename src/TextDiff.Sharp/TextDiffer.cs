namespace TextDiff;

public class TextDiffer
{
    private readonly IDiffBlockParser _blockParser;
    private readonly IContextMatcher _contextMatcher;
    private readonly IChangeTracker _changeTracker;

    public TextDiffer(
        IDiffBlockParser blockParser = null,
        IContextMatcher contextMatcher = null,
        IChangeTracker changeTracker = null)
    {
        _blockParser = blockParser ?? new DiffBlockParser();
        _contextMatcher = contextMatcher ?? new ContextMatcher();
        _changeTracker = changeTracker ?? new ChangeTracker();
    }

    public ProcessResult Process(string document, string diff)
    {
        var documentLines = TextUtils.SplitLines(document);
        var diffLines = TextUtils.SplitLines(diff);
        var blocks = _blockParser.Parse(diffLines).ToList();

        var processor = new DocumentProcessor(documentLines, _contextMatcher, _changeTracker);
        return processor.ApplyBlocks(blocks);
    }
}