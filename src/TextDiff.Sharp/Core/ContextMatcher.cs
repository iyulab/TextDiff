/*
의도:
1. 중복된 컨텍스트 라인이 있을 때 가장 적절한 매칭 위치를 찾아야 함
2. AfterContext를 활용하여 더 정확한 위치를 결정
3. 변경 연속성을 고려한 매칭

유의사항:
1. BeforeContext와 AfterContext를 함께 고려하여 매칭
2. 이전 매치 위치(_lastMatchEnd)를 활용하여 일관성 있는 패치 적용
3. 동일 라인이 여러 번 나타날 때는 전후 문맥을 고려하여 결정
*/

public class ContextMatcher : IContextMatcher
{
    private int _lastMatchEnd = 0;

    public int FindPosition(string[] documentLines, int startPosition, DiffBlock block)
    {
        if (!block.BeforeContext.Any())
            return startPosition;

        // 1. 모든 가능한 매칭 위치 찾기
        var positions = new List<int>();
        for (int i = 0; i <= documentLines.Length - block.BeforeContext.Count; i++)
        {
            if (IsFullMatch(documentLines, i, block))
            {
                positions.Add(i);
            }
        }

        if (!positions.Any())
        {
            throw new InvalidOperationException($"Cannot find matching position for block: {block}");
        }

        // 2. 최적의 위치 선택
        int bestPosition = SelectBestMatch(documentLines, positions, block);
        _lastMatchEnd = bestPosition + block.BeforeContext.Count;
        return bestPosition;
    }

    private bool IsFullMatch(string[] documentLines, int position, DiffBlock block)
    {
        // BeforeContext 매칭
        for (int i = 0; i < block.BeforeContext.Count; i++)
        {
            if (position + i >= documentLines.Length ||
                !TextUtils.LinesMatch(documentLines[position + i], block.BeforeContext[i]))
            {
                return false;
            }
        }

        // Removals 매칭
        int removalPos = position + block.BeforeContext.Count;
        foreach (var removal in block.Removals)
        {
            if (removalPos >= documentLines.Length ||
                !TextUtils.LinesMatch(documentLines[removalPos], removal))
            {
                return false;
            }
            removalPos++;
        }

        // AfterContext 매칭 (있는 경우)
        if (block.AfterContext.Any())
        {
            int afterPos = position + block.BeforeContext.Count + block.Removals.Count;
            foreach (var afterLine in block.AfterContext)
            {
                if (afterPos >= documentLines.Length ||
                    !TextUtils.LinesMatch(documentLines[afterPos], afterLine))
                {
                    return false;
                }
                afterPos++;
            }
        }

        return true;
    }

    private int SelectBestMatch(string[] documentLines, List<int> positions, DiffBlock block)
    {
        if (positions.Count == 1)
            return positions[0];

        var scores = positions.ToDictionary(
            pos => pos,
            pos => CalculateMatchScore(documentLines, pos, block)
        );

        return scores
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => Math.Abs(kvp.Key - _lastMatchEnd))
            .Select(kvp => kvp.Key)
            .First();
    }

    private double CalculateMatchScore(string[] documentLines, int position, DiffBlock block)
    {
        double score = 0;

        // 1. removal 라인 이후의 컨텍스트와 일치하는지 확인
        int afterRemovalPos = position + block.BeforeContext.Count + block.Removals.Count;
        if (afterRemovalPos < documentLines.Length &&
            block.AfterContext.Any() &&
            TextUtils.LinesMatch(documentLines[afterRemovalPos], block.AfterContext[0]))
        {
            score += 50;
        }

        // 2. 이전 매치 이후의 위치인 경우 가산점
        if (position >= _lastMatchEnd)
        {
            score += 30;
        }

        // 3. 전체 패턴 매칭 점수
        if (HasConsistentContext(documentLines, position, block))
        {
            score += 20;
        }

        return score;
    }

    private bool HasConsistentContext(string[] documentLines, int position, DiffBlock block)
    {
        // 앞뒤 컨텍스트를 더 넓게 보고 일관성 확인
        int contextSize = 3;
        int start = Math.Max(0, position - contextSize);
        int end = Math.Min(documentLines.Length,
            position + block.BeforeContext.Count + block.Removals.Count + contextSize);

        // 주변 컨텍스트의 패턴이 일치하는지 확인
        List<string> expectedPattern = new List<string>();
        if (block.BeforeContext.Any()) expectedPattern.AddRange(block.BeforeContext);
        if (block.Removals.Any()) expectedPattern.AddRange(block.Removals);
        if (block.AfterContext.Any()) expectedPattern.AddRange(block.AfterContext);

        for (int i = start; i < end - expectedPattern.Count + 1; i++)
        {
            bool matches = true;
            for (int j = 0; j < expectedPattern.Count; j++)
            {
                if (i + j >= documentLines.Length ||
                    !TextUtils.LinesMatch(documentLines[i + j], expectedPattern[j]))
                {
                    matches = false;
                    break;
                }
            }
            if (matches) return true;
        }

        return false;
    }
}