namespace TextDiff;

public static class StringSimilarityCalculator
{
    public static double Calculate(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2)) return 1.0;
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2)) return 0.0;

        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        return 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string str1, string str2)
    {
        var matrix = new int[str1.Length + 1, str2.Length + 1];

        for (int i = 0; i <= str1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= str2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= str1.Length; i++)
            for (int j = 1; j <= str2.Length; j++)
            {
                var cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }

        return matrix[str1.Length, str2.Length];
    }
}
