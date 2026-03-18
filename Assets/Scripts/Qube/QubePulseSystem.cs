using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QubePulseSystem : MonoBehaviour
{
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;

    private const float BONUS_MULTIPLIER_TWO = 1.5f;
    private const float BONUS_MULTIPLIER_THREE_PLUS = 2.0f;
    private const float CASCADE_CELL_DELAY = 0.03f;
    private const float CASCADE_FADE_DURATION = 0.2f;

    private List<QubeQuad> trackedQuads = new List<QubeQuad>();

    public delegate void OnPulseDelegate(int score, List<QubeQuad> removedQuads);
    public event OnPulseDelegate OnPulse;

    /// <summary>
    /// 블록 배치 후 호출: Quad 감지 + 하이라이트 (소거는 하지 않음)
    /// </summary>
    public void ProcessTurn()
    {
        // 1. 소거 셀 clearTimer 감소
        for (int x = 0; x < QubeGrid.WIDTH; x++)
        {
            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                QubeCell cell = grid.GetCell(x, y);
                if (cell != null)
                {
                    cell.UpdateClearTimer();
                }
            }
        }

        // 1.5 소거 셀 비주얼 갱신
        grid.UpdateCellVisuals();

        // 2. 새로운 영역 감지
        List<QubeQuad> detectedQuads = quadDetector.DetectQuads();

        // 3. 기존 Quad와 병합/추가
        foreach (var newQuad in detectedQuads)
        {
            ProcessNewQuad(newQuad);
        }

        // 4. 하이라이트 갱신 (소거 없음)
        quadDetector.HighlightQuads(trackedQuads);
    }

    /// <summary>
    /// Phase 1에서 Quad 클릭 시 호출: 클릭한 Quad + 인접 Quad 연쇄 소거 + 보너스 점수
    /// </summary>
    public void RemoveQuad(QubeQuad quad)
    {
        if (!trackedQuads.Contains(quad)) return;

        // BFS로 인접 Quad 연쇄 수집
        List<QubeQuad> chainQuads = CollectAdjacentChain(quad);

        // 모든 연쇄 Quad를 trackedQuads에서 제거
        foreach (var q in chainQuads)
        {
            trackedQuads.Remove(q);
        }

        // 점수 계산: 각 Quad 점수 합산 × 연쇄 보너스
        int baseScore = 0;
        foreach (var q in chainQuads)
        {
            baseScore += q.GetScore();
        }

        float multiplier = GetChainMultiplier(chainQuads.Count);
        int totalScore = Mathf.RoundToInt(baseScore * multiplier);

        OnPulse?.Invoke(totalScore, chainQuads);

        StartCoroutine(CascadeRemoveChain(chainQuads));
    }

    /// <summary>
    /// BFS로 시작 Quad에 인접한 모든 tracked Quad를 수집합니다.
    /// </summary>
    private List<QubeQuad> CollectAdjacentChain(QubeQuad startQuad)
    {
        List<QubeQuad> chain = new List<QubeQuad>();
        Queue<QubeQuad> bfsQueue = new Queue<QubeQuad>();
        HashSet<QubeQuad> visited = new HashSet<QubeQuad>();

        bfsQueue.Enqueue(startQuad);
        visited.Add(startQuad);

        while (bfsQueue.Count > 0)
        {
            QubeQuad current = bfsQueue.Dequeue();
            chain.Add(current);

            foreach (var candidate in trackedQuads)
            {
                if (visited.Contains(candidate)) continue;
                if (current.IsAdjacentTo(candidate))
                {
                    visited.Add(candidate);
                    bfsQueue.Enqueue(candidate);
                }
            }
        }

        return chain;
    }

    private float GetChainMultiplier(int chainCount)
    {
        if (chainCount >= 3) return BONUS_MULTIPLIER_THREE_PLUS;
        if (chainCount >= 2) return BONUS_MULTIPLIER_TWO;
        return 1f;
    }

    private IEnumerator CascadeRemoveChain(List<QubeQuad> quads)
    {
        Transform placedContainer = grid.GetPlacedBlocksContainer();

        // 각 Quad를 순차적으로 캐스케이드 제거
        foreach (var quad in quads)
        {
            List<Vector2Int> sortedCells = new List<Vector2Int>(quad.cells);
            sortedCells.Sort((a, b) =>
            {
                int sum = (a.x + a.y).CompareTo(b.x + b.y);
                return sum != 0 ? sum : a.x.CompareTo(b.x);
            });

            foreach (var cellPos in sortedCells)
            {
                string targetName = $"PlacedCell_{cellPos.x}_{cellPos.y}";
                Transform cellTransform = placedContainer.Find(targetName);

                if (cellTransform != null)
                {
                    StartCoroutine(FadeCellOut(cellTransform.gameObject));
                }

                grid.SetCellOccupied(cellPos, false, Color.clear, wasCleared: true);

                yield return new WaitForSeconds(CASCADE_CELL_DELAY);
            }

            // Quad 간 딜레이 (연쇄 연출)
            yield return new WaitForSeconds(CASCADE_FADE_DURATION);
        }

        // 소거 셀 비주얼 갱신
        grid.UpdateCellVisuals();

        // 소거 후 Quad 재감지
        RefreshQuads();
    }

    private IEnumerator FadeCellOut(GameObject cellObj)
    {
        Image img = cellObj.GetComponent<Image>();
        RectTransform rect = cellObj.GetComponent<RectTransform>();
        if (img == null || rect == null) yield break;

        Color startColor = img.color;
        Vector3 startScale = rect.localScale;
        float elapsed = 0f;

        while (elapsed < CASCADE_FADE_DURATION)
        {
            if (cellObj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / CASCADE_FADE_DURATION;

            // 축소 + 페이드
            float scale = Mathf.Lerp(1f, 0.3f, t);
            rect.localScale = startScale * scale;
            img.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        if (cellObj != null) Destroy(cellObj);
    }

    /// <summary>
    /// 클릭한 셀 좌표가 속한 Quad 반환 (없으면 null)
    /// </summary>
    public QubeQuad GetQuadAtCell(Vector2Int cellPos)
    {
        foreach (var quad in trackedQuads)
        {
            if (quad.Contains(cellPos))
                return quad;
        }
        return null;
    }

    public List<QubeQuad> GetActiveQuads()
    {
        return trackedQuads;
    }

    public bool HasActiveQuads()
    {
        return trackedQuads.Count > 0;
    }

    public void ClearAllQuads()
    {
        trackedQuads.Clear();
    }

    private void RefreshQuads()
    {
        // 기존 Quad 목록 초기화 후 재감지
        trackedQuads.Clear();
        List<QubeQuad> detectedQuads = quadDetector.DetectQuads();
        foreach (var newQuad in detectedQuads)
        {
            ProcessNewQuad(newQuad);
        }
        quadDetector.HighlightQuads(trackedQuads);
    }

    private void ProcessNewQuad(QubeQuad newQuad)
    {
        // 기존 Quad와 동일하면 무시
        foreach (var existingQuad in trackedQuads)
        {
            if (IsSameQuad(newQuad, existingQuad))
                return;
        }

        List<QubeQuad> overlappingQuads = new List<QubeQuad>();
        List<QubeQuad> adjacentQuads = new List<QubeQuad>();

        foreach (var existingQuad in trackedQuads)
        {
            if (newQuad.OverlapsWith(existingQuad))
                overlappingQuads.Add(existingQuad);
            else if (newQuad.IsAdjacentTo(existingQuad))
                adjacentQuads.Add(existingQuad);
        }

        if (overlappingQuads.Count > 0)
        {
            ProcessOverlappingQuads(newQuad, overlappingQuads, adjacentQuads);
        }
        else if (adjacentQuads.Count > 0)
        {
            ProcessAdjacentQuads(newQuad, adjacentQuads);
        }
        else
        {
            trackedQuads.Add(newQuad);
        }
    }

    private void ProcessOverlappingQuads(QubeQuad newQuad, List<QubeQuad> overlappingQuads, List<QubeQuad> adjacentQuads)
    {
        List<QubeQuad> allRelatedQuads = new List<QubeQuad>(overlappingQuads);
        allRelatedQuads.AddRange(adjacentQuads);

        QubeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        foreach (var quad in allRelatedQuads)
        {
            if (!mergedQuad.CanMergeWith(quad))
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad);
        }

        int totalOldSize = allRelatedQuads.Sum(q => q.size);

        if (canMergeAll && mergedQuad.size > totalOldSize)
        {
            foreach (var quad in allRelatedQuads)
                trackedQuads.Remove(quad);
            trackedQuads.Add(mergedQuad);
        }
        else if (!canMergeAll && newQuad.size >= 4)
        {
            List<QubeQuad> containedQuads = new List<QubeQuad>();
            foreach (var existingQuad in overlappingQuads)
            {
                bool isContained = existingQuad.cells.All(c => newQuad.cells.Contains(c));
                if (isContained)
                    containedQuads.Add(existingQuad);
            }

            if (containedQuads.Count > 0)
            {
                foreach (var quad in containedQuads)
                    trackedQuads.Remove(quad);
                trackedQuads.Add(newQuad);
            }
        }
    }

    private void ProcessAdjacentQuads(QubeQuad newQuad, List<QubeQuad> adjacentQuads)
    {
        QubeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        foreach (var quad in adjacentQuads)
        {
            if (!mergedQuad.CanMergeWith(quad))
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad);
        }

        if (canMergeAll && mergedQuad.size > newQuad.size)
        {
            foreach (var quad in adjacentQuads)
                trackedQuads.Remove(quad);
            trackedQuads.Add(mergedQuad);
        }
        else
        {
            trackedQuads.Add(newQuad);
        }
    }

    private bool IsSameQuad(QubeQuad a, QubeQuad b)
    {
        return a.minX == b.minX && a.maxX == b.maxX &&
               a.minY == b.minY && a.maxY == b.maxY;
    }
}
