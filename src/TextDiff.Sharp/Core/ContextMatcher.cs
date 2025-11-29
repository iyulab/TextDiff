using System.Linq;
using TextDiff.Helpers;
using TextDiff.Models;

namespace TextDiff.Core;

/// <summary>
/// Finds the exact position in the document where a diff block should be applied.
/// Uses context matching to locate the correct application point.
/// </summary>
/// <remarks>
/// Key features:
/// - Considers both removal text and surrounding context for optimal matching
/// - Analyzes text content, whitespace, and indentation for scoring
/// - Tracks previous match position for maintaining relative positions across blocks
/// - Selects the closest position among candidates with similar scores
///
/// Matching algorithm:
/// 1. Attempts matching at all possible positions in the document
/// 2. Validates context text similarity
/// 3. Calculates match score based on content, whitespace, and empty lines
/// 4. Selects the candidate closest to previous match among those scoring above 80% of max
/// </remarks>
public class ContextMatcher : IContextMatcher
{
    private int _lastMatchEnd = 0;
    private const double CONTINUITY_WEIGHT = 2.0;
    private const double CONTEXT_WEIGHT = 1.0;
    private const double PATTERN_WEIGHT = 0.5;

    /// <inheritdoc />
    public void Reset()
    {
        _lastMatchEnd = 0;
    }

    /// <inheritdoc />
    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        var candidates = new List<MatchCandidate>();
        var pattern = AnalyzeContextPattern(block);
        bool isProgressiveBlock = IsProgressiveBlock(block);

        // Try matching at each possible position
        for (int i = startPosition; i < documentLines.Length - block.BeforeContext.Count; i++)
        {
            if (block.Removals.Any() && !ValidateRemovalPosition(documentLines, i, block))
                continue;

            var match = TryMatchWithContext(documentLines, i, block, pattern, isProgressiveBlock);
            if (match.IsMatch)
            {
                candidates.Add(new MatchCandidate(i, match.Score));
            }
        }

        if (!candidates.Any())
        {
            throw new InvalidOperationException($"Cannot find matching position for block: {block}");
        }

        var bestMatch = SelectBestMatch(candidates);
        _lastMatchEnd = bestMatch.Position + block.BeforeContext.Count + block.Removals.Count;

        return bestMatch.Position;
    }

    private bool IsProgressiveBlock(DiffBlock block)
    {
        // Consider as progressive block when context is minimal and changes are large
        return block.BeforeContext.Count <= 2 &&
               (block.Additions.Count > 2 || block.Removals.Count > 2);
    }

    private (bool IsMatch, double Score) TryMatchWithContext(
        string[] documentLines,
        int position,
        DiffBlock block,
        ContextPattern pattern,
        bool isProgressiveBlock)
    {
        if (!IsBasicContextMatch(documentLines, position, block))
            return (false, 0);

        // Calculate individual score components
        var continuityScore = CalculateContinuityScore(position, isProgressiveBlock);
        var contextScore = EvaluateSurroundingContext(documentLines, position, block);
        var patternScore = CalculatePatternSimilarity(documentLines, position, block, pattern);

        // Calculate final score with weights
        double finalScore = (continuityScore * CONTINUITY_WEIGHT +
                           contextScore * CONTEXT_WEIGHT +
                           patternScore * PATTERN_WEIGHT) /
                           (CONTINUITY_WEIGHT + CONTEXT_WEIGHT + PATTERN_WEIGHT);

        return (true, finalScore);
    }

    private double CalculateContinuityScore(int position, bool isProgressiveBlock)
    {
        if (_lastMatchEnd == 0)
            return 1.0;

        double distance = Math.Abs(position - _lastMatchEnd);

        if (isProgressiveBlock)
        {
            // Progressive blocks get higher weight for continuity
            if (position == _lastMatchEnd) return 1.0;
            if (distance <= 2) return 0.9;
            if (distance <= 5) return 0.7;
            return Math.Max(0.1, 1.0 - (distance / 20.0));
        }

        // Standard blocks use default scoring
        return Math.Max(0.1, 1.0 - (distance / 50.0));
    }

    private bool ValidateRemovalPosition(string[] documentLines, int position, DiffBlock block)
    {
        int removalStart = position + block.BeforeContext.Count;
        if (removalStart + block.Removals.Count > documentLines.Length)
            return false;

        for (int i = 0; i < block.Removals.Count; i++)
        {
            if (!TextUtils.HasTextSimilarity(
                documentLines[removalStart + i],
                block.Removals[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBasicContextMatch(string[] documentLines, int position, DiffBlock block)
    {
        for (int i = 0; i < block.BeforeContext.Count; i++)
        {
            if (position + i >= documentLines.Length)
                return false;

            var line = block.BeforeContext[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (!TextUtils.HasTextSimilarity(
                documentLines[position + i],
                line))
            {
                return false;
            }
        }

        return true;
    }

    private double EvaluateSurroundingContext(string[] documentLines, int position, DiffBlock block)
    {
        double score = 1.0;

        if (position > 0)
        {
            var prevLines = GetPreviousLines(documentLines, position, 2);
            score *= CalculateContextSimilarity(prevLines, block);
        }

        int afterStart = position + block.BeforeContext.Count + block.Removals.Count;
        if (afterStart < documentLines.Length)
        {
            var afterLines = GetNextLines(documentLines, afterStart, 2);
            if (block.AfterContext.Any())
            {
                score *= CalculateAfterContextSimilarity(afterLines, block.AfterContext);
            }
        }

        return score;
    }

    private MatchCandidate SelectBestMatch(List<MatchCandidate> candidates)
    {
        var maxScore = candidates.Max(c => c.Score);
        var threshold = maxScore * 0.8; // Stricter threshold for better accuracy

        var bestCandidates = candidates
            .Where(c => c.Score >= threshold)
            .ToList();

        return bestCandidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => Math.Abs(c.Position - _lastMatchEnd))
            .First();
    }

    private List<string> GetPreviousLines(string[] documentLines, int position, int count)
    {
        var lines = new List<string>();
        for (int i = Math.Max(0, position - count); i < position; i++)
        {
            lines.Add(documentLines[i]);
        }
        return lines;
    }

    private List<string> GetNextLines(string[] documentLines, int position, int count)
    {
        var lines = new List<string>();
        for (int i = position; i < Math.Min(documentLines.Length, position + count); i++)
        {
            lines.Add(documentLines[i]);
        }
        return lines;
    }

    private double CalculateContextSimilarity(List<string> contextLines, DiffBlock block)
    {
        // Calculate similarity with previous context
        double similarity = 1.0;
        if (block.BeforeContext.Any() && contextLines.Any())
        {
            int commonPatterns = contextLines.Count(cl =>
                block.BeforeContext.Any(bc => TextUtils.HasTextSimilarity(cl, bc)));
            similarity = (commonPatterns + 1.0) / (contextLines.Count + 1.0);
        }
        return similarity;
    }

    private double CalculateAfterContextSimilarity(List<string> afterLines, List<string> afterContext)
    {
        if (!afterLines.Any() || !afterContext.Any())
            return 1.0;

        double similarity = 0;
        for (int i = 0; i < Math.Min(afterLines.Count, afterContext.Count); i++)
        {
            if (TextUtils.HasTextSimilarity(afterLines[i], afterContext[i]))
                similarity += 1.0;
        }

        return (similarity + 1.0) / (Math.Min(afterLines.Count, afterContext.Count) + 1.0);
    }

    private double CalculatePatternSimilarity(
        string[] documentLines,
        int position,
        DiffBlock block,
        ContextPattern pattern)
    {
        // Calculate context pattern similarity
        double similarity = 1.0;

        // Compare indentation patterns
        double indentScore = CompareIndentation(documentLines, position, block);
        similarity *= (indentScore + 1.0) / 2.0;

        // Compare line length patterns
        double lengthScore = CompareLineLengths(documentLines, position, block);
        similarity *= (lengthScore + 1.0) / 2.0;

        return similarity;
    }

    private double CompareIndentation(string[] documentLines, int position, DiffBlock block)
    {
        if (!block.BeforeContext.Any())
            return 1.0;

        var docIndents = documentLines
            .Skip(position)
            .Take(block.BeforeContext.Count)
            .Select(l => l.Length - l.TrimStart().Length);

        var contextIndents = block.BeforeContext
            .Select(l => l.Length - l.TrimStart().Length);

        int matchCount = docIndents.Zip(contextIndents, (d, c) => d == c).Count(x => x);
        return (double)matchCount / block.BeforeContext.Count;
    }

    private double CompareLineLengths(string[] documentLines, int position, DiffBlock block)
    {
        if (!block.BeforeContext.Any())
            return 1.0;

        var docLengths = documentLines
            .Skip(position)
            .Take(block.BeforeContext.Count)
            .Select(l => l.TrimEnd().Length)
            .ToList();

        var contextLengths = block.BeforeContext
            .Select(l => l.TrimEnd().Length)
            .ToList();

        double totalDiff = 0;
        int count = 0;

        for (int i = 0; i < Math.Min(docLengths.Count, contextLengths.Count); i++)
        {
            int d = docLengths[i];
            int c = contextLengths[i];

            if (d == 0 || c == 0) continue;
            totalDiff += Math.Abs(d - c) / (double)Math.Max(d, c);
            count++;
        }

        return count > 0 ? 1.0 - (totalDiff / count) : 1.0;
    }

    private class ContextPattern
    {
        public List<LinePattern> LeadingPatterns { get; set; } = new();
        public List<LinePattern> TrailingPatterns { get; set; } = new();
    }

    private class LinePattern
    {
        public int Indentation { get; set; }
        public int ContentLength { get; set; }
    }

    private class MatchCandidate
    {
        public int Position { get; }
        public double Score { get; }

        public MatchCandidate(int position, double score)
        {
            Position = position;
            Score = score;
        }
    }

    private ContextPattern AnalyzeContextPattern(DiffBlock block)
    {
        return new ContextPattern
        {
            LeadingPatterns = block.BeforeContext
                .Select(line => new LinePattern
                {
                    Indentation = line.Length - line.TrimStart().Length,
                    ContentLength = line.Trim().Length
                })
                .ToList(),
            TrailingPatterns = block.AfterContext
                .Select(line => new LinePattern
                {
                    Indentation = line.Length - line.TrimStart().Length,
                    ContentLength = line.Trim().Length
                })
                .ToList()
        };
    }
}