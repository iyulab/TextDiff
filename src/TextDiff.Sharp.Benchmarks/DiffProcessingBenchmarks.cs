using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TextDiff.Helpers;
using TextDiff.Models;

namespace TextDiff.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[GcServer(true)]
public class DiffProcessingBenchmarks
{
    private string _smallDocument = null!;
    private string _mediumDocument = null!;
    private string _largeDocument = null!;
    private string _simpleDiff = null!;
    private string _complexDiff = null!;

    private TextDiffer _differ = null!;

    [GlobalSetup]
    public void Setup()
    {
        _differ = new TextDiffer();

        // Small document (1KB)
        _smallDocument = string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Line {i} with some content"));

        // Medium document (100KB)
        _mediumDocument = string.Join("\n", Enumerable.Range(1, 5000).Select(i => $"Line {i} with some additional content for testing"));

        // Large document (10MB)
        _largeDocument = string.Join("\n", Enumerable.Range(1, 500000).Select(i => $"Line {i} with substantial content for performance testing"));

        // Simple diff
        _simpleDiff = " Line 1 with some content\n- Line 2 with some content\n+ Line 2 MODIFIED with some content\n Line 3 with some content";

        // Complex diff with multiple blocks
        _complexDiff = @" Line 1 with some additional content for testing
- Line 2 with some additional content for testing
+ Line 2 MODIFIED with some additional content for testing
 Line 3 with some additional content for testing
 Line 4 with some additional content for testing
 Line 5 with some additional content for testing
- Line 6 with some additional content for testing
+ Line 6 CHANGED with some additional content for testing
 Line 7 with some additional content for testing
 Line 8 with some additional content for testing
 Line 9 with some additional content for testing
- Line 10 with some additional content for testing
+ Line 10 UPDATED with some additional content for testing
 Line 11 with some additional content for testing";
    }

    [Benchmark(Baseline = true)]
    public ProcessResult ProcessSmallDocument_Original()
    {
        return _differ.Process(_smallDocument, _simpleDiff);
    }

    [Benchmark]
    public ProcessResult ProcessSmallDocument_Optimized()
    {
        return _differ.ProcessOptimized(_smallDocument, _simpleDiff);
    }

    [Benchmark]
    public ProcessResult ProcessMediumDocument_Original()
    {
        return _differ.Process(_mediumDocument, _complexDiff);
    }

    [Benchmark]
    public ProcessResult ProcessMediumDocument_Optimized()
    {
        return _differ.ProcessOptimized(_mediumDocument, _complexDiff);
    }

    [Benchmark]
    public ProcessResult ProcessLargeDocument_Original()
    {
        return _differ.Process(_largeDocument, _complexDiff);
    }

    [Benchmark]
    public ProcessResult ProcessLargeDocument_Optimized()
    {
        return _differ.ProcessOptimized(_largeDocument, _complexDiff);
    }
}

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class AsyncProcessingBenchmarks
{
    private string _document = null!;
    private string _diff = null!;
    private TextDiffer _differ = null!;

    [GlobalSetup]
    public void Setup()
    {
        _differ = new TextDiffer();
        _document = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i} with content for async testing"));
        _diff = @" Line 1 with content for async testing
- Line 2 with content for async testing
+ Line 2 MODIFIED with content for async testing
 Line 3 with content for async testing";
    }

    [Benchmark(Baseline = true)]
    public ProcessResult ProcessSync()
    {
        return _differ.Process(_document, _diff);
    }

    [Benchmark]
    public async Task<ProcessResult> ProcessAsync()
    {
        return await _differ.ProcessAsync(_document, _diff);
    }

    [Benchmark]
    public ProcessResult ProcessOptimized()
    {
        return _differ.ProcessOptimized(_document, _diff);
    }
}

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class MemoryEfficiencyBenchmarks
{
    private string _text = null!;

    [GlobalSetup]
    public void Setup()
    {
        _text = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i} with content"));
    }

    [Benchmark(Baseline = true)]
    public string[] SplitLines_Original()
    {
        return TextUtils.SplitLines(_text);
    }

    [Benchmark]
    public string[] SplitLines_Efficient()
    {
        return MemoryEfficientTextUtils.SplitLinesEfficient(_text);
    }

    [Benchmark]
    public string LineBuffer_Original()
    {
        var buffer = new LineBuffer();
        for (int i = 0; i < 1000; i++)
        {
            buffer.AddLine($"Line {i}");
        }
        return buffer.ToString();
    }

    [Benchmark]
    public string LineBuffer_Optimized()
    {
        var buffer = new OptimizedLineBuffer();
        for (int i = 0; i < 1000; i++)
        {
            buffer.AddLine($"Line {i}");
        }
        return buffer.ToString();
    }
}