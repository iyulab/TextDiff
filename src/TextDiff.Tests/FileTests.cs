using Xunit;
using System.IO;
using Xunit.Abstractions;
using TextDiff.Models;

namespace TextDiff.Tests;

public class FileTests
{
    private readonly TextDiffer _differ;
    private readonly ITestOutputHelper _output;
    private const string TestFilesPath = "TestFiles";

    public FileTests(ITestOutputHelper output)
    {
        _differ = new TextDiffer();
        _output = output;
    }

    private void AssertWithOutput(string original, string diff, string expected, ProcessResult result, int changed, int added, int deleted)
    {
        _output.WriteLine("=== Origianl Text ===");
        _output.WriteLine(original);

        _output.WriteLine("=== Diff Text ===");
        _output.WriteLine(diff);

        _output.WriteLine("=== Expected Text ===");
        _output.WriteLine(expected);

        _output.WriteLine("=== Result Text ===");
        _output.WriteLine(result.Text);

        _output.WriteLine("=== Change Stats (Expected/Actual) ===");
        _output.WriteLine($"Changed Lines: {changed}/{result.Changes.ChangedLines}");
        _output.WriteLine($"Added Lines: {added}/{result.Changes.AddedLines}");
        _output.WriteLine($"Deleted Lines: {deleted}/{result.Changes.DeletedLines}");

        if (!TextComparisonHelper.AreTextsEqual(expected, result.Text))
        {
            var difference = TextComparisonHelper.GetDifference(expected, result.Text);
            Assert.True(false, difference);
        }

        Assert.Equal(changed, result.Changes.ChangedLines);   
        Assert.Equal(added, result.Changes.AddedLines);     
        Assert.Equal(deleted, result.Changes.DeletedLines);   
    }

    [Fact]
    public void TestFile1_ProcessDiff_ShouldMatchExpectedOutput()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_1.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_1_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_1_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 5, 1, 0);
    }

    [Fact]
    public void TestFile2_Additions_ProcessDiff_ShouldMatchExpectedOutput()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_2.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_2_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_2_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 0, 5, 0);
    }

    [Fact]
    public void TestFile3_Deletions_ProcessDiff_ShouldMatchExpectedOutput()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_3.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_3_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_3_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 0, 0, 5);
    }

    [Fact]
    public void TestFile4_ComplexDiff_ProcessDiff_ShouldMatchExpectedOutput()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_4.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_4_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_4_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 2, 6, 0);
        
    }

    [Fact]
    public void TestFile5_UnifiedDiff_Format_ShouldMatchExpectedOutput()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_5.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_5_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_5_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 1, 4, 0);
    }

    [Fact]
    public void TestFile6_MultiParts()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_6.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_6_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_6_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 1, 4, 0);
    }

    [Fact]
    public void TestFile7_MultiParts()
    {
        // Arrange
        var sourceFile = Path.Combine(TestFilesPath, "file_7.txt");
        var diffFile = Path.Combine(TestFilesPath, "file_7_diff.txt");
        var expectedFile = Path.Combine(TestFilesPath, "file_7_changed.txt");
        var sourceContent = File.ReadAllText(sourceFile);
        var diffContent = File.ReadAllText(diffFile);
        var expectedContent = File.ReadAllText(expectedFile);

        // Act
        var result = _differ.Process(sourceContent, diffContent);
        AssertWithOutput(sourceContent, diffContent, expectedContent, result, 1, 4, 0);
    }
}
