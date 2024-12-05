using System.Linq;

namespace TextDiff.Core;

/// <summary>
/// 문서 내에서 diff 블록이 적용될 정확한 위치를 찾는 컨텍스트 매칭 클래스입니다.
/// 이 클래스는 주어진 diff 블록(삭제될 텍스트와 그 전후 컨텍스트)을 기반으로
/// 문서 내에서 가장 적절한 적용 위치를 찾아냅니다.
/// </summary>
/// <remarks>
/// 주요 기능:
/// - 삭제될 텍스트와 주변 컨텍스트를 모두 고려하여 최적의 매칭 위치를 찾습니다.
/// - 텍스트 내용, 공백, 들여쓰기 등을 종합적으로 분석하여 매칭 점수를 계산합니다.
/// - 이전 매칭 위치를 추적하여 연속된 diff 블록들의 상대적 위치를 유지합니다.
/// - 비슷한 점수의 여러 후보 위치 중 이전 매칭과 가장 가까운 위치를 선택합니다.
/// 
/// 매칭 알고리즘:
/// 1. 문서 내 가능한 모든 위치에서 매칭을 시도합니다.
/// 2. 컨텍스트 텍스트의 유사도를 검증합니다.
/// 3. 텍스트 내용, 공백, 빈 줄 등에 가중치를 부여하여 매칭 점수를 계산합니다.
/// 4. 최고 점수의 90% 이상인 후보들 중에서 이전 매칭 위치와 가장 가까운 것을 선택합니다.
/// </remarks>

public class ContextMatcher : IContextMatcher
{
    private int _lastMatchEnd = 0;
    private const double CONTINUITY_WEIGHT = 2.0;
    private const double CONTEXT_WEIGHT = 1.0;
    private const double PATTERN_WEIGHT = 0.5;

    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        var candidates = new List<MatchCandidate>();
        var pattern = AnalyzeContextPattern(block);
        bool isProgressiveBlock = IsProgressiveBlock(block);

        // 각 가능한 위치에서 매칭 시도
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
        // 컨텍스트가 적고 변경사항이 많은 경우 진행형 블록으로 간주
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

        // 점수 계산을 위한 개별 컴포넌트
        var continuityScore = CalculateContinuityScore(position, isProgressiveBlock);
        var contextScore = EvaluateSurroundingContext(documentLines, position, block);
        var patternScore = CalculatePatternSimilarity(documentLines, position, block, pattern);

        // 가중치를 적용한 최종 점수 계산
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
            // 진행형 블록의 경우 연속성에 더 높은 가중치 부여
            if (position == _lastMatchEnd) return 1.0;
            if (distance <= 2) return 0.9;
            if (distance <= 5) return 0.7;
            return Math.Max(0.1, 1.0 - (distance / 20.0));
        }

        // 일반 블록의 경우 기존 로직 유지
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
        var threshold = maxScore * 0.8; // 더 엄격한 임계값 적용

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
        // 이전 컨텍스트와의 유사도 계산
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
        // 컨텍스트 패턴 유사도 계산
        double similarity = 1.0;

        // 들여쓰기 패턴 비교
        double indentScore = CompareIndentation(documentLines, position, block);
        similarity *= (indentScore + 1.0) / 2.0;

        // 라인 길이 패턴 비교
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