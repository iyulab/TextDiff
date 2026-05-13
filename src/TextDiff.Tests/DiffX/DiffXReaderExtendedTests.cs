using TextDiff.DiffX;

namespace TextDiff.Tests.DiffX;

public class DiffXReaderExtendedTests
{
    private readonly DiffXReader _reader = new();

    // JSON format meta with explicit length (ParseFileMeta format=json branch)
    [Fact]
    public void ExtractFileDiffs_JsonFormatMetaWithLength_ParsesPath()
    {
        var json = "{ \"path\": \"src/foo.cs\", \"op\": \"modify\" }";
        var jsonLength = json.Length + 1; // +1 for newline
        var diffX = $"#diffx: encoding=utf-8, version=1.0\n" +
                    $"#.change:\n" +
                    $"#..file:\n" +
                    $"#...meta: format=json, length={jsonLength}\n" +
                    $"{json}\n" +
                    $"#...diff:\n" +
                    $" line1\n" +
                    $"-old\n" +
                    $"+new\n";

        var entries = _reader.ExtractFileDiffs(diffX).ToList();
        Assert.Single(entries);
        Assert.Equal("src/foo.cs", entries[0].Path);
    }

    // diff section with length= option (expectedDiffLength path)
    [Fact]
    public void ExtractFileDiffs_DiffSectionWithLength_ParsesContent()
    {
        var diffContent = " line1\n-old\n+new";
        var diffLength = diffContent.Length + 1;
        var diffX = $"#diffx: encoding=utf-8, version=1.0\n" +
                    $"#.change:\n" +
                    $"#..file:\n" +
                    $"#...meta:\n" +
                    $"{{ \"path\": \"file.cs\", \"op\": \"modify\" }}\n" +
                    $"#...diff: length={diffLength}\n" +
                    $"{diffContent}\n" +
                    $"#.change:\n";

        var entries = _reader.ExtractFileDiffs(diffX).ToList();
        Assert.True(entries.Count >= 1);
        Assert.NotEmpty(entries[0].DiffContent);
    }

    // ExtractFileDiffs — null content throws ArgumentNullException
    [Fact]
    public void ExtractFileDiffs_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _reader.ExtractFileDiffs(null!).ToList());
    }

    // ExtractFileDiffs — non-DiffX content throws ArgumentException
    [Fact]
    public void ExtractFileDiffs_NonDiffXContent_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _reader.ExtractFileDiffs("--- a/file.txt\n+++ b/file.txt\n").ToList());
    }

    // ProcessDiffX via TextDiffer — no entries throws
    [Fact]
    public void TextDiffer_ProcessDiffX_EmptyEntries_Throws()
    {
        // DiffX with no .diff sections
        var diffX = "#diffx: encoding=utf-8, version=1.0\n" +
                    "#.change:\n" +
                    "#..preamble:\n" +
                    "This is a preamble\n";

        var differ = new TextDiffer();
        Assert.Throws<TextDiff.Exceptions.DiffApplicationException>(() =>
            differ.ProcessDiffX("document", diffX));
    }
}
