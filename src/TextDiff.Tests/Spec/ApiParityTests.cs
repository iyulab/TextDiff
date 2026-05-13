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

        Assert.True(syncResult.Text == asyncResult.Text, $"[{testName}] Process vs ProcessAsync text mismatch");
        Assert.True(syncResult.Text == optimizedResult.Text, $"[{testName}] Process vs ProcessOptimized text mismatch");
        Assert.True(syncResult.Changes.AddedLines == asyncResult.Changes.AddedLines, $"[{testName}] AddedLines mismatch (sync vs async)");
        Assert.True(syncResult.Changes.DeletedLines == asyncResult.Changes.DeletedLines, $"[{testName}] DeletedLines mismatch (sync vs async)");
        Assert.True(syncResult.Changes.ChangedLines == asyncResult.Changes.ChangedLines, $"[{testName}] ChangedLines mismatch (sync vs async)");
        Assert.True(syncResult.Changes.AddedLines == optimizedResult.Changes.AddedLines, $"[{testName}] AddedLines mismatch (sync vs optimized)");
        Assert.True(syncResult.Changes.DeletedLines == optimizedResult.Changes.DeletedLines, $"[{testName}] DeletedLines mismatch (sync vs optimized)");
        Assert.True(syncResult.Changes.ChangedLines == optimizedResult.Changes.ChangedLines, $"[{testName}] ChangedLines mismatch (sync vs optimized)");
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

        // StreamingDiffProcessor uses WriteLineAsync which appends a trailing newline
        // after every line including the last. This is a known behavioral difference
        // from the in-memory Process() API. TrimEnd normalizes for comparison.
        Assert.Equal(syncResult.Text.TrimEnd('\n', '\r'), streamText.TrimEnd('\n', '\r'));
    }
}
