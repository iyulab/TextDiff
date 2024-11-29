using Xunit.Abstractions;

namespace TextDiff.Tests;

public class DiffProcessorTests
{
    private readonly DiffProcessor _processor;
    private readonly ITestOutputHelper _output;

    public DiffProcessorTests(ITestOutputHelper output)
    {
        _processor = new DiffProcessor();
        _output = output;
    }

    [Fact]
    public void TestSimpleDeleteAndInsert()
    {
        // Arrange
        var document = @"line1
line2
line3
line4";

        var diff = @" line1
- line2
- line3
+ new_line2
+ new_line3
 line4";

        var expectedResult = @"line1
new_line2
new_line3
line4";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestInsertOnly()
    {
        // Arrange
        var document = @"line1
    line2
    line3";

        var diff = @" line1
+ line1.5
 line2
 line3";

        var expectedResult = @"line1
    line1.5
    line2
    line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 1, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestDeleteOnly()
    {
        // Arrange
        var document = @"line1
    line2
    line3";

        var diff = @" line1
- line2
 line3";

        var expectedResult = @"line1
    line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 1);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestComplexChanges()
    {
        // Arrange
        var document = @"header1
    line1
    line2
    line3
    line4
    footer1
    footer2";

        var diff = @" header1
- line1
+ new_line1
 line2
- line3
+ new_line3
+ added_line3.1
 line4
 footer1
- footer2
+ footer2_modified";

        var expectedResult = @"header1
    new_line1
    line2
    new_line3
    added_line3.1
    line4
    footer1
    footer2_modified";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 3, 1, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestCSharpFunction()
    {
        // Arrange
        var document = @"public void FunctionA()
{
    Console.WriteLine(""Original Line 1"");
    Console.WriteLine(""Original Line 2"");
}";

        var diff = @" public void FunctionA()
{
-     Console.WriteLine(""Original Line 1"");
-     Console.WriteLine(""Original Line 2"");
+     Console.WriteLine(""Updated Line 1"");
+     Console.WriteLine(""Updated Line 2"");
}";

        var expectedResult = @"public void FunctionA()
{
    Console.WriteLine(""Updated Line 1"");
    Console.WriteLine(""Updated Line 2"");
}";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestAmbiguousLineMatching()
    {
        // Arrange
        var document = @"line1
    line2
    line1
    line2
    line3";

        var diff = @" line1
- line2
 line3";

        var expectedResult = @"line1
    line2
    line1
    line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 1);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    #region Additional Tests

    [Fact]
    public void TestAddMultipleLines()
    {
        // Arrange
        var document = @"start
end";

        var diff = @" start
+ middle1
+ middle2
+ middle3
 end";

        var expectedResult = @"start
middle1
middle2
middle3
end";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 3, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestRemoveMultipleLines()
    {
        // Arrange
        var document = @"start
middle1
middle2
middle3
end";

        var diff = @" start
- middle1
- middle2
- middle3
 end";

        var expectedResult = @"start
end";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 3);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestReplaceMultipleLines()
    {
        // Arrange
        var document = @"start
middle1
middle2
middle3
end";

        var diff = @" start
- middle1
- middle2
- middle3
+ newMiddle1
+ newMiddle2
 end";

        var expectedResult = @"start
newMiddle1
newMiddle2
end";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 1);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestNoChanges()
    {
        // Arrange
        var document = @"line1
line2
line3";

        var diff = @" line1
 line2
 line3";

        var expectedResult = @"line1
line2
line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestWhitespaceOnlyChanges()
    {
        // Arrange
        var document = @"line1
    line2
    line3";

        var diff = @" line1
- line2
+ line2 
 line3";

        var expectedResult = @"line1
    line2 
    line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 1, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestEmptyDocument_AddLines()
    {
        // Arrange
        var document = @"";

        var diff = @"+ line1
+ line2
+ line3";

        var expectedResult = @"line1
line2
line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 3, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestEmptyDiff_NoChanges()
    {
        // Arrange
        var document = @"line1
line2
line3";

        var diff = @" line1
 line2
 line3";

        var expectedResult = @"line1
line2
line3";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestDiffWithMixedChangeTypes()
    {
        // Arrange
        var document = @"line1
line2
line3
line4
line5";

        var diff = @" line1
- line2
+ line2_modified
 line3
+ line3.1
- line4
+ line4_modified
 line5";

        var expectedResult = @"line1
line2_modified
line3
line3.1
line4_modified
line5";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 1, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestMultipleAdjacentChanges()
    {
        // Arrange
        var document = @"line1
line2
line3
line4
line5";

        var diff = @" line1
- line2
- line3
+ line2_modified
+ line3_modified
 line4
- line5
+ line5_modified";

        var expectedResult = @"line1
line2_modified
line3_modified
line4
line5_modified";

        // Act
        var result = _processor.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 3, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    #endregion

    private void AssertWithOutput(string document, string diff, string expectedResult, ProcessResult result, int changed, int added, int deleted)
    {
        _output.WriteLine("Original:");
        _output.WriteLine("```");
        _output.WriteLine(document);
        _output.WriteLine("```");

        _output.WriteLine("\nDiff:");
        _output.WriteLine("```");
        _output.WriteLine(diff);
        _output.WriteLine("```");

        _output.WriteLine("\nExpected Result:");
        _output.WriteLine("```");
        _output.WriteLine(expectedResult);
        _output.WriteLine("```");

        _output.WriteLine("\nResult:");
        _output.WriteLine("```");
        _output.WriteLine(result.Text);
        _output.WriteLine("```");

        _output.WriteLine("\nSummary (Expected/Actual):");
        _output.WriteLine($"changed: {changed}/{result.Changes.ChangedLines}");
        _output.WriteLine($"added: {added}/{result.Changes.AddedLines}");
        _output.WriteLine($"deleted: {deleted}/{result.Changes.DeletedLines}");

        Assert.Equal(expectedResult, result.Text);

        Assert.Equal(changed, result.Changes.ChangedLines);
        Assert.Equal(added, result.Changes.AddedLines);
        Assert.Equal(deleted, result.Changes.DeletedLines);
    }
}

