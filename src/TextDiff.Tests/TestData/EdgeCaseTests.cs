namespace TextDiff.Tests.TestData;

public class EdgeCaseTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_VeryLargeDocument_HandlesEfficiently()
    {
        // Arrange
        var lines = Enumerable.Range(1, 10000).Select(i => $"Line {i}").ToArray();
        string document = string.Join("\n", lines);
        string diff = " Line 1\n- Line 2\n+ Modified Line 2\n Line 3";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _differ.Process(document, diff);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Modified Line 2", result.Text);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Process_DocumentWithSpecialCharacters_PreservesEncoding()
    {
        // Arrange
        string document = "Hello 🌍\nSpecial chars: áéíóú\nEmoji: 😀🚀\nChinese: 你好";
        string diff = " Hello 🌍\n Special chars: áéíóú\n- Emoji: 😀🚀\n+ Emoji: 🎉✨\n Chinese: 你好";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("Hello 🌍", result.Text);
        Assert.Contains("áéíóú", result.Text);
        Assert.Contains("🎉✨", result.Text);
        Assert.Contains("你好", result.Text);
        Assert.DoesNotContain("😀🚀", result.Text);
    }

    [Fact]
    public void Process_DocumentWithVariousLineEndings_HandlesCorrectly()
    {
        // Arrange
        string document = "line1\r\nline2\nline3\r\nline4";
        string diff = " line1\n- line2\n+ modified line2\n line3";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("modified line2", result.Text);
        Assert.DoesNotContain("line2\n", result.Text.Replace("modified line2", ""));
    }

    [Fact]
    public void Process_DocumentWithTabs_PreservesIndentation()
    {
        // Arrange
        string document = "\tif (true) {\n\t\treturn value;\n\t}";
        string diff = " \tif (true) {\n-\t\treturn value;\n+\t\treturn newValue;\n \t}";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("\treturn newValue;", result.Text);
        Assert.Equal(3, result.Text.Split('\n').Length);
    }

    [Fact]
    public void Process_DocumentWithMixedWhitespace_PreservesStructure()
    {
        // Arrange
        string document = "  function() {\n    var x = 1;\n      return x;\n  }";
        string diff = "   function() {\n-    var x = 1;\n+    var x = 2;\n       return x;\n   }";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("var x = 2;", result.Text);
        Assert.Contains("  function() {", result.Text);
        Assert.Contains("      return x;", result.Text);
    }

    [Fact]
    public void Process_DocumentWithEmptyLines_HandlesCorrectly()
    {
        // Arrange
        string document = "line1\n\nline3\n\n\nline6";
        string diff = " line1\n \n- line3\n+ modified line3\n \n \n line6";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        var normalizedText = result.Text.Replace("\r\n", "\n");
        var lines = normalizedText.Split('\n');
        Assert.Equal("line1", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("modified line3", lines[2]);
        Assert.Equal("", lines[3]);
        Assert.Equal("", lines[4]);
        Assert.Equal("line6", lines[5]);
    }

    [Fact]
    public void Process_DocumentWithTrailingWhitespace_PreservesAccurately()
    {
        // Arrange
        string document = "line1   \nline2\t\nline3 ";
        string diff = " line1   \n- line2\t\n+ line2_modified\t\n line3 ";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.Contains("line1   ", result.Text);
        Assert.Contains("line2_modified\t", result.Text);
        Assert.Contains("line3 ", result.Text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Process_DocumentWithVariableSizes_HandlesCorrectly(int lineCount)
    {
        // Arrange
        var lines = Enumerable.Range(1, lineCount).Select(i => $"Line {i}");
        string document = string.Join("\n", lines);
        string diff = lineCount > 1 ? " Line 1\n+ Inserted Line" : "+ First Line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.NotNull(result);
        if (lineCount > 1)
        {
            Assert.Contains("Line 1", result.Text);
            Assert.Contains("Inserted Line", result.Text);
        }
        else
        {
            Assert.Contains("First Line", result.Text);
        }
    }
}