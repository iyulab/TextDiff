using System.Text;
using TextDiff.Helpers;

namespace TextDiff.Tests;

public static class TextComparisonHelper
{
    public static bool AreTextsEqual(string expected, string actual)
    {
        if (expected == actual) return true;

        var expectedLines = TextUtils.SplitLines(expected);
        var actualLines = TextUtils.SplitLines(actual);

        if (expectedLines.Length != actualLines.Length)
            return false;

        for (int i = 0; i < expectedLines.Length; i++)
        {
            var expectedLine = expectedLines[i];
            var actualLine = actualLines[i];

            // 빈 줄인 경우 공백 개수 무시
            if (string.IsNullOrWhiteSpace(expectedLine) && string.IsNullOrWhiteSpace(actualLine))
                continue;

            // 일반 라인은 뒤쪽 공백만 제거하고 비교
            if (expectedLine.TrimEnd() != actualLine.TrimEnd())
                return false;
        }

        return true;
    }

    public static string GetDifference(string expected, string actual)
    {
        var expectedLines = TextUtils.SplitLines(expected);
        var actualLines = TextUtils.SplitLines(actual);

        var sb = new StringBuilder();
        sb.AppendLine("Differences found:");

        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "(no line)";
            var actualLine = i < actualLines.Length ? actualLines[i] : "(no line)";

            if (string.IsNullOrWhiteSpace(expectedLine) && string.IsNullOrWhiteSpace(actualLine))
                continue;

            if (expectedLine.TrimEnd() != actualLine.TrimEnd())
            {
                //sb.AppendLine($"Line {i + 1}:");
                sb.AppendLine($"  Expected: '{expectedLine}'");
                sb.AppendLine($"  Actual  : '{actualLine}'");
            }
        }

        return sb.ToString();
    }
}
