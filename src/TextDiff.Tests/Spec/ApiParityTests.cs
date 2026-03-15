using System.IO;
using System.Text;
using TextDiff;

namespace TextDiff.Tests.Spec;

public class ApiParityTests
{
    private readonly TextDiffer _differ = new();

    [Theory]
    [MemberData(nameof(ParityTestCases))]
    public async Task AllApis_ProduceSameResult(string testName, string document, string diff)
    {
        // Process (sync)
        var syncResult = _differ.Process(document, diff);

        // ProcessAsync
        var asyncResult = await _differ.ProcessAsync(document, diff);

        // ProcessOptimized
        var optimizedResult = _differ.ProcessOptimized(document, diff);

        Assert.Equal(syncResult.Text, asyncResult.Text);
        Assert.Equal(syncResult.Text, optimizedResult.Text);
        Assert.Equal(syncResult.Changes.AddedLines, asyncResult.Changes.AddedLines);
        Assert.Equal(syncResult.Changes.DeletedLines, asyncResult.Changes.DeletedLines);
        Assert.Equal(syncResult.Changes.ChangedLines, asyncResult.Changes.ChangedLines);
        Assert.Equal(syncResult.Changes.AddedLines, optimizedResult.Changes.AddedLines);
        Assert.Equal(syncResult.Changes.DeletedLines, optimizedResult.Changes.DeletedLines);
        Assert.Equal(syncResult.Changes.ChangedLines, optimizedResult.Changes.ChangedLines);
    }

    public static IEnumerable<object[]> ParityTestCases()
    {
        yield return new object[] { "SimpleReplace", "line1\nline2\nline3", " line1\n-line2\n+modified2\n line3" };
        yield return new object[] { "MultiAdd", "line1\nline2", " line1\n+new1\n+new2\n line2" };
        yield return new object[] { "MultiDelete", "line1\nline2\nline3\nline4", " line1\n-line2\n-line3\n line4" };
        yield return new object[] { "CreateFromEmpty", "", "+line1\n+line2" };
        yield return new object[] { "DeleteAll", "line1\nline2", "-line1\n-line2" };
        yield return new object[] { "MixedOps", "a\nb\nc\nd\ne", " a\n-b\n+B\n c\n-d\n+D\n+D2\n e" };
        yield return new object[] { "WhitespaceReplace", " ", "- \n+content" };
        yield return new object[] { "GitHeaders", "old", "--- a/f.txt\n+++ b/f.txt\n@@ -1 +1 @@\n-old\n+new" };
        yield return new object[] { "MultiHunk", "a\nb\nc\nd\ne",
            "@@ -1,2 +1,2 @@\n-a\n+A\n b\n@@ -4,2 +4,2 @@\n d\n-e\n+E" };

        yield return new object[] { "TabToSpace", "\tindented", "-\tindented\n+    indented" };

        var largeDoc = string.Join("\n", Enumerable.Range(1, 500).Select(i => $"line{i}"));
        yield return new object[] { "LargeDoc", largeDoc, " line499\n-line500\n+MODIFIED500" };
    }

    [Fact]
    public async Task ProcessStreamsAsync_MatchesProcess()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n-line2\n+modified2\n line3";

        var syncResult = _differ.Process(document, diff);

        using var docStream = new MemoryStream(Encoding.UTF8.GetBytes(document));
        using var diffStream = new MemoryStream(Encoding.UTF8.GetBytes(diff));
        using var outputStream = new MemoryStream();

        var streamResult = await _differ.ProcessStreamsAsync(docStream, diffStream, outputStream);

        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream);
        string streamText = await reader.ReadToEndAsync();

        // Stream result may have trailing newline from WriteLineAsync
        Assert.Equal(syncResult.Text.TrimEnd('\n', '\r'), streamText.TrimEnd('\n', '\r'));
    }
}
