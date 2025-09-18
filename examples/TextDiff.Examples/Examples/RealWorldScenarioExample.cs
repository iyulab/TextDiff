using TextDiff;
using TextDiff.Exceptions;
using TextDiff.Models;

namespace TextDiff.Examples;

public static class RealWorldScenarioExample
{
    public static async Task Run()
    {
        Console.WriteLine("=== Real-World Scenario: Code Review System ===");
        Console.WriteLine();

        var codeReviewSystem = new CodeReviewSystem();

        // Simulate code review workflow
        var originalCode = await File.ReadAllTextAsync("SampleFiles/sample_document.txt");
        var reviewDiff = await File.ReadAllTextAsync("SampleFiles/sample_diff.txt");

        var review = await codeReviewSystem.ProcessCodeReview(
            "feature-branch-123",
            originalCode,
            reviewDiff,
            "john.doe@company.com"
        );

        Console.WriteLine($"Code Review Result: {review.Status}");
        if (review.IsSuccessful)
        {
            Console.WriteLine($"Changes applied: {review.ChangesSummary}");
            Console.WriteLine($"Processing time: {review.ProcessingTime}ms");
        }
    }

    private class CodeReviewSystem
    {
        private readonly TextDiffer _differ = new();

        public async Task<CodeReviewResult> ProcessCodeReview(
            string branchName,
            string originalCode,
            string reviewDiff,
            string reviewerEmail)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"Processing code review for branch: {branchName}");
                Console.WriteLine($"Reviewer: {reviewerEmail}");

                var result = await _differ.ProcessAsync(originalCode, reviewDiff);

                stopwatch.Stop();

                return new CodeReviewResult
                {
                    Status = "Applied",
                    IsSuccessful = true,
                    ChangesSummary = $"+{result.Changes.AddedLines} -{result.Changes.DeletedLines} ~{result.Changes.ChangedLines}",
                    ProcessingTime = stopwatch.ElapsedMilliseconds,
                    UpdatedCode = result.Text
                };
            }
            catch (InvalidDiffFormatException ex)
            {
                return new CodeReviewResult
                {
                    Status = "Invalid Diff Format",
                    IsSuccessful = false,
                    ErrorMessage = $"Line {ex.LineNumber}: {ex.Message}",
                    ProcessingTime = stopwatch.ElapsedMilliseconds
                };
            }
            catch (DiffApplicationException ex)
            {
                return new CodeReviewResult
                {
                    Status = "Merge Conflict",
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = stopwatch.ElapsedMilliseconds
                };
            }
        }
    }

    private class CodeReviewResult
    {
        public string Status { get; set; } = "";
        public bool IsSuccessful { get; set; }
        public string? ChangesSummary { get; set; }
        public string? ErrorMessage { get; set; }
        public long ProcessingTime { get; set; }
        public string? UpdatedCode { get; set; }
    }
}