using TextDiff.Exceptions;
using TextDiff.Helpers;
using TextDiff.Models;
using Xunit.Abstractions;

namespace TextDiff.Tests;

public class DiffProcessorTests
{
    private readonly TextDiffer _differ;
    private readonly ITestOutputHelper _output;

    public DiffProcessorTests(ITestOutputHelper output)
    {
        _differ = new TextDiffer();
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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 1, 0); // 변경 없음

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
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 0, 0, 1); // 변경 없음

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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
        var result = _differ.Process(document, diff);

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

    [Fact]
    public void TestDashInOriginalContent()
    {
        // Arrange
        var document = @"function-name
    my-variable
    some-text
    end-line";

        var diff = @" function-name
- my-variable
+ new-variable
 some-text
- end-line
+ final-line";

        var expectedResult = @"function-name
    new-variable
    some-text
    final-line";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestDashAsFirstCharacter()
    {
        // Arrange
        var document = @"-start-line
    -middle-line
    -end-line";

        var diff = @" -start-line
- -middle-line
+ -new-middle-line
 -end-line";

        var expectedResult = @"-start-line
    -new-middle-line
    -end-line";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 1, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    #region WhitespaceTests

    [Fact]
    public void TestEmptyLinesInContent()
    {
        // Arrange
        var document = @"line1

line3

line5";

        var diff = @" line1
 
- line3
+ new_line3
 
- line5
+ new_line5";

        var expectedResult = @"line1

new_line3

new_line5";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestMixedIndentation()
    {
        // Arrange
        var document = $"line1{Environment.NewLine}\tindented_tab{Environment.NewLine}    indented_space{Environment.NewLine}\t    mixed_indent";

        var diff = @" line1
- 	indented_tab
+ 	new_indented_tab
     indented_space
- 	    mixed_indent
+ 	    new_mixed_indent";

        var expectedResult = $"line1{Environment.NewLine}\tnew_indented_tab{Environment.NewLine}    indented_space{Environment.NewLine}\t    new_mixed_indent";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);

        // 추가 검증: 들여쓰기가 정확히 유지되는지 확인
        var resultLines = TextUtils.SplitLines(result.Text);
        Assert.Equal("\tnew_indented_tab", resultLines[1]); // 탭 들여쓰기
        Assert.Equal("    indented_space", resultLines[2]); // 스페이스 들여쓰기
        Assert.Equal("\t    new_mixed_indent", resultLines[3]); // 혼합 들여쓰기
    }

    #endregion

    #region SpecialCharacterTests

    [Fact]
    public void TestPlusSignInOriginalContent()
    {
        // Arrange
        var document = @"C++ Code
    a+b+c
    x+y+z
    end";

        var diff = @" C++ Code
- a+b+c
+ a+b+c+d
 x+y+z
- end
+ end_plus+";

        var expectedResult = @"C++ Code
    a+b+c+d
    x+y+z
    end_plus+";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestUnicodeCharacters()
    {
        // Arrange
        var document = @"안녕하세요
    こんにちは
    你好";

        var diff = @" 안녕하세요
- こんにちは
+ さようなら
 你好";

        var expectedResult = @"안녕하세요
    さようなら
    你好";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 1, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    #endregion

    #region ErrorCases

    [Fact]
    public void TestInvalidDiffFormat_EmptyLines()
    {
        // Arrange
        var document = "line1\nline2\nline3";
        var diff = "line1\n\n+new_line\n";  // 빈 줄이 있고 첫 줄이 컨텍스트 표시(' ')로 시작하지 않음

        // Act & Assert
        var ex = Assert.Throws<InvalidDiffFormatException>(() =>
            _differ.Process(document, diff));
        Assert.Contains("Invalid diff line format", ex.Message);
    }

    [Theory]
    [InlineData(
    @">line1
line2
+line3",
    "Line must start with space, '+'")]  // 첫 줄에 잘못된 문자로 시작
    [InlineData(
    @"line1
line2
+line3",
    "Line must start with space, '+'")]  // 첫 줄에 컨텍스트 표시 누락
    [InlineData(
    @" line1
# line2
+ new_line2",
    "Line must start with space, '+'")]  // 지원하지 않는 문자 사용
    public void TestInvalidDiffFormats(string diff, string _)
    {
        // Arrange
        var document = @"line1
line2
line3";

        // Act & Assert
        var ex = Assert.Throws<InvalidDiffFormatException>(() =>
            _differ.Process(document, diff));
        Assert.Contains("Invalid diff line format", ex.Message);
    }

    [Fact]
    public void TestInvalidDiffMatchingFailure()
    {
        // Arrange
        var document = @"line1
line2
line3";

        var diff = @" line1
- - line2
+ new_line2";  // 매칭에 실패하는 케이스

        // Act & Assert
        var ex = Assert.Throws<DiffApplicationException>(() =>
            _differ.Process(document, diff));
        Assert.Contains("Cannot find matching position for block", ex.Message);
    }

    [Fact]
    public void TestMismatchedContextLines()
    {
        // Arrange
        var document = @"line1
line2
line3";

        var diff = @" different_line1
- line2
+ new_line2
 line3";

        // Act & Assert
        Assert.Throws<DiffApplicationException>(() => _differ.Process(document, diff));
    }

    #endregion

    #region EdgeCases

    [Fact]
    public void TestVeryLongDocument()
    {
        // Arrange
        var documentLines = Enumerable.Range(1, 10000).Select(i => $"line{i}").ToList();
        var document = string.Join(Environment.NewLine, documentLines);

        var diff = @" line4998
 line4999
- line5000
+ new_line5000
 line5001
 line5002";

        var resultLines = new List<string>(documentLines);
        resultLines[4999] = "new_line5000";  // 0-based index for line5000
        var expectedResult = string.Join(Environment.NewLine, resultLines);

        // Act
        var result = _differ.Process(document, diff);

        // Log
        _output.WriteLine($"Document length: {documentLines.Count} lines");
        _output.WriteLine($"Changed line 5000 from 'line5000' to 'new_line5000'");
        if (result.Text != expectedResult)
        {
            // 문제를 디버깅하기 위한 추가 로깅
            var actualLines = result.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            _output.WriteLine($"Actual change position: Looking for change near line {Array.IndexOf(actualLines, "new_line5000") + 1}");
        }

        // Assert
        AssertWithOutput(document, diff, expectedResult, result, 1, 0, 0);
    }

    [Fact]
    public void TestDuplicateLines()
    {
        // Arrange
        var document = @"duplicate
duplicate
unique
duplicate
duplicate";

        var diff = @" duplicate
- duplicate
+ modified
 unique
 duplicate
 duplicate";

        var expectedResult = @"duplicate
modified
unique
duplicate
duplicate";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, changed: 1, added: 0, deleted: 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    [Fact]
    public void TestChangesAtDocumentBoundaries()
    {
        // Arrange
        var document = @"first_line
middle
last_line";

        var diff = @"- first_line
+ new_first_line
 middle
- last_line
+ new_last_line";

        var expectedResult = @"new_first_line
middle
new_last_line";

        // Act
        var result = _differ.Process(document, diff);

        // Log
        AssertWithOutput(document, diff, expectedResult, result, 2, 0, 0);

        // Assert
        Assert.Equal(expectedResult, result.Text);
    }

    #endregion
}

