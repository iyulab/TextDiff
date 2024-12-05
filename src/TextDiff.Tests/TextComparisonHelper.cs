using System.Text;
using TextDiff.Helpers;

namespace TextDiff.Tests;

public static class TextComparisonHelper
{
    public static bool AreTextsEqual(string expected, string actual)
    {
        if (expected == actual) return true;

        // Split into lines and filter out empty lines
        var expectedLines = TextUtils.SplitLines(expected)
            .Where(line => !IsEmptyOrNoLine(line))
            .ToArray();
        var actualLines = TextUtils.SplitLines(actual)
            .Where(line => !IsEmptyOrNoLine(line))
            .ToArray();

        if (expectedLines.Length != actualLines.Length)
            return false;

        for (int i = 0; i < expectedLines.Length; i++)
        {
            var expectedLine = expectedLines[i];
            var actualLine = actualLines[i];

            if (RemoveIndentation(expectedLine) != RemoveIndentation(actualLine))
                return false;
        }

        return true;
    }

    public static string GetDifference(string expected, string actual)
    {
        // Split and filter out empty lines
        var expectedLines = TextUtils.SplitLines(expected)
            .Where(line => !IsEmptyOrNoLine(line))
            .ToArray();
        var actualLines = TextUtils.SplitLines(actual)
            .Where(line => !IsEmptyOrNoLine(line))
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine("Differences found:");

        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "(no line)";
            var actualLine = i < actualLines.Length ? actualLines[i] : "(no line)";

            if (RemoveIndentation(expectedLine) != RemoveIndentation(actualLine))
            {
                sb.AppendLine($"  Expected: '{expectedLine.Trim()}'");
                sb.AppendLine($"  Actual  : '{actualLine.Trim()}'");
            }
        }

        return sb.ToString();
    }

    private static string RemoveIndentation(string line)
    {
        return line.Trim();
    }

    private static bool IsEmptyOrNoLine(string line)
    {
        return string.IsNullOrWhiteSpace(line) || line == "(no line)";
    }
}