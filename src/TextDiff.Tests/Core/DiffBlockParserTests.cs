using TextDiff.Core;

namespace TextDiff.Tests.Core;

public class DiffBlockParserTests
{
    private readonly DiffBlockParser _parser = new();

    [Fact]
    public void Parse_SimpleReplacement_ProducesSingleBlock()
    {
        var lines = new[]
        {
            " context",
            "-removed",
            "+added",
            " context2"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(new[] { "context" }, blocks[0].BeforeContext);
        Assert.Equal(new[] { "removed" }, blocks[0].Removals);
        Assert.Equal(new[] { "added" }, blocks[0].Additions);
        Assert.Equal(new[] { "context2" }, blocks[0].AfterContext);
    }

    [Fact]
    public void Parse_PureAddition_NoRemovals()
    {
        var lines = new[] { "+new line" };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Empty(blocks[0].Removals);
        Assert.Equal(new[] { "new line" }, blocks[0].Additions);
    }

    [Fact]
    public void Parse_PureDeletion_NoAdditions()
    {
        var lines = new[] { "-old line" };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(new[] { "old line" }, blocks[0].Removals);
        Assert.Empty(blocks[0].Additions);
    }

    [Fact]
    public void Parse_GitHeaders_AreSkipped()
    {
        var lines = new[]
        {
            "diff --git a/file.txt b/file.txt",
            "index abc..def 100644",
            "--- a/file.txt",
            "+++ b/file.txt",
            "@@ -1,3 +1,3 @@",
            " context",
            "-removed",
            "+added"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(new[] { "removed" }, blocks[0].Removals);
        Assert.Equal(new[] { "added" }, blocks[0].Additions);
    }

    [Fact]
    public void Parse_EllipsisSeparator_ProducesMultipleBlocks()
    {
        var lines = new[]
        {
            "-block1removed",
            "+block1added",
            "...",
            "-block2removed",
            "+block2added"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Equal(2, blocks.Count);
        Assert.Equal(new[] { "block1removed" }, blocks[0].Removals);
        Assert.Equal(new[] { "block2removed" }, blocks[1].Removals);
    }

    [Fact]
    public void Parse_NoNewlineAtEof_LineIsSkipped()
    {
        var lines = new[]
        {
            "-old",
            "+new",
            @"\ No newline at end of file"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(new[] { "old" }, blocks[0].Removals);
        Assert.Equal(new[] { "new" }, blocks[0].Additions);
    }

    [Fact]
    public void Parse_SecondDiffGitHeader_StopsProcessing()
    {
        // Parser tracks seenFirstDiffHeader; the second "diff " line triggers stop
        var lines = new[]
        {
            "diff --git a/first.txt b/first.txt",
            "-first",
            "+First",
            "diff --git a/second.txt b/second.txt",
            "-second",
            "+Second"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(new[] { "first" }, blocks[0].Removals);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptySequence()
    {
        var lines = Array.Empty<string>();

        var blocks = _parser.Parse(lines).ToList();

        Assert.Empty(blocks);
    }

    [Fact]
    public void Parse_OnlyHunkHeader_ReturnsEmptySequence()
    {
        var lines = new[] { "@@ -1,3 +1,3 @@" };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Empty(blocks);
    }

    [Fact]
    public void Parse_MultipleAdditions_AllInSingleBlock()
    {
        var lines = new[]
        {
            " context",
            "+line1",
            "+line2",
            "+line3"
        };

        var blocks = _parser.Parse(lines).ToList();

        Assert.Single(blocks);
        Assert.Equal(3, blocks[0].Additions.Count);
        Assert.Equal(new[] { "line1", "line2", "line3" }, blocks[0].Additions);
    }
}
