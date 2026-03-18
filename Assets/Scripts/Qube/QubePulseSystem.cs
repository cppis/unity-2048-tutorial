using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QubePulseSystem : MonoBehaviour
{
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;

    private const float BONUS_MULTIPLIER_TWO = 1.5f;
    private const float BONUS_MULTIPLIER_THREE_PLUS = 2.0f;

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
    /// Phase 1에서 Quad 클릭 시 호출: 해당 Quad 소거 + 점수
    /// </summary>
    public void RemoveQuad(QubeQuad quad)
    {
        if (!trackedQuads.Contains(quad)) return;

        int score = quad.GetScore();

        foreach (var cellPos in quad.cells)
        {
            grid.SetCellOccupied(cellPos, false, Color.clear, wasCleared: true);
        }

        trackedQuads.Remove(quad);

        List<QubeQuad> removed = new List<QubeQuad> { quad };
        OnPulse?.Invoke(score, removed);

        // 소거 후 Quad 재감지 (소거로 인해 기존 Quad가 깨질 수 있음)
        RefreshQuads();
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
