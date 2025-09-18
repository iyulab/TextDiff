using TextDiff.Exceptions;

namespace TextDiff.Tests.TestData;

public class ValidationTests
{
    private readonly TextDiffer _differ = new();

    [Fact]
    public void Process_NullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        string diff = " line1\n+ line2";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _differ.Process(null!, diff));
        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public void Process_NullDiff_ThrowsArgumentNullException()
    {
        // Arrange
        string document = "line1\nline2";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _differ.Process(document, null!));
        Assert.Equal("diff", exception.ParamName);
    }

    [Fact]
    public void Process_EmptyDiff_ThrowsArgumentException()
    {
        // Arrange
        string document = "line1\nline2";
        string diff = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _differ.Process(document, diff));
        Assert.Equal("diff", exception.ParamName);
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Process_WhitespaceDiff_ThrowsArgumentException()
    {
        // Arrange
        string document = "line1\nline2";
        string diff = "   \n\t\n  ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _differ.Process(document, diff));
        Assert.Equal("diff", exception.ParamName);
        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Process_InvalidDiffFormat_ThrowsInvalidDiffFormatException()
    {
        // Arrange
        string document = "line1\nline2";
        string diff = "invalid line format\n+ valid line";

        // Act & Assert
        var exception = Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
        Assert.Contains("Invalid diff line format", exception.Message);
        Assert.Equal(1, exception.LineNumber);
    }

    [Fact]
    public void Process_DiffWithoutValidLines_ThrowsInvalidDiffFormatException()
    {
        // Arrange
        string document = "line1\nline2";
        string diff = "--- a/file.txt\n+++ b/file.txt\ninvalid format line";

        // Act & Assert
        var exception = Assert.Throws<InvalidDiffFormatException>(() => _differ.Process(document, diff));
        Assert.Contains("Invalid diff line format", exception.Message);
    }

    [Fact]
    public void Process_ValidDiffWithHeaders_Succeeds()
    {
        // Arrange
        string document = "line1\nline2";
        string diff = @"diff --git a/file.txt b/file.txt
index 1234567..abcdefg 100644
--- a/file.txt
+++ b/file.txt
@@ -1,2 +1,2 @@
 line1
-line2
+modified line2";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("modified line2", result.Text);
    }

    [Fact]
    public void Process_EmptyDocument_WithValidDiff_Succeeds()
    {
        // Arrange
        string document = "";
        string diff = "+ first line\n+ second line";

        // Act
        var result = _differ.Process(document, diff);

        // Assert
        Assert.NotNull(result);
        // Handle both \n and \r\n line endings
        string expectedText = "first line\nsecond line";
        string actualNormalized = result.Text.Replace("\r\n", "\n");
        Assert.Equal(expectedText, actualNormalized);
    }

    [Theory]
    [InlineData("+ added line", "")]
    [InlineData("- removed line", "removed line")]
    public void Process_ValidDiffFormats_Succeed(string diff, string document)
    {
        // Act & Assert (should not throw)
        var result = _differ.Process(document, diff);
        Assert.NotNull(result);
    }
}