#pragma warning disable CS0618 // ProcessAsync is deprecated; these tests intentionally verify the deprecated API

namespace TextDiff.Tests.Core;

public class CancellationTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public async Task ProcessAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n-line2\n+modified\n line3";

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _differ.ProcessAsync(document, diff, cts.Token));
    }

    [Fact]
    public async Task ProcessAsync_NotCancelled_CompletesNormally()
    {
        string document = "line1\nline2\nline3";
        string diff = " line1\n-line2\n+modified\n line3";

        using var cts = new CancellationTokenSource();

        var result = await _differ.ProcessAsync(document, diff, cts.Token);

        Assert.NotNull(result);
        Assert.Contains("modified", result.Text);
    }

    [Fact]
    public async Task ProcessStreamsAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var docStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("line1\nline2"));
        using var diffStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("-line1\n+modified"));
        using var outStream = new System.IO.MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _differ.ProcessStreamsAsync(docStream, diffStream, outStream, cts.Token));
    }
}
