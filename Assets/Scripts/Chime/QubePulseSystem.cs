using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QubePulseSystem : MonoBehaviour
{
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;

    private const int PULSE_INTERVAL = 8;
    private const float BONUS_MULTIPLIER_TWO = 1.5f;
    private const float BONUS_MULTIPLIER_THREE_PLUS = 2.0f;
    private const float REMOVAL_DELAY = 0.3f;

    private int globalTurnCounter = 0;
    private List<QubeQuad> trackedQuads = new List<QubeQuad>();
    private bool isProcessingTurn = false; // 턴 처리 중 플래그

    public delegate void OnPulseDelegate(int score);
    public event OnPulseDelegate OnPulse;

    public void IncrementTurn()
    {
        if (isProcessingTurn)
        {
            Debug.LogWarning("Turn is already being processed, skipping duplicate call");
            return;
        }

        isProcessingTurn = true;
        globalTurnCounter++;
        Debug.Log($"\n=== Turn {globalTurnCounter} Started ===");

        // 0. 모든 셀의 clearTimer 감소 (소거된 셀 색상 복구 처리)
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

        // 1. 기존 Quad들의 turnTimer 증가
        foreach (var quad in trackedQuads)
        {
            quad.turnTimer++;
            Debug.Log($"Quad {quad.width}x{quad.height} at ({quad.minX},{quad.minY}): turnTimer = {quad.turnTimer}/8");
        }

        // 2. 새로운 영역 감지
        List<QubeQuad> detectedQuads = quadDetector.DetectQuads();
        Debug.Log($"Detected {detectedQuads.Count} potential new quads");

        // 3. 새로 감지된 영역들을 기존 Quad와 병합하거나 새로 추가
        foreach (var newQuad in detectedQuads)
        {
            ProcessNewQuad(newQuad);
        }

        // 4. turnTimer가 pulseInterval인 Quad들 소거
        List<QubeQuad> quadsToRemove = trackedQuads.Where(q => q.turnTimer >= PULSE_INTERVAL).ToList();
        if (quadsToRemove.Count > 0)
        {
            Debug.Log($"Removing {quadsToRemove.Count} quads that reached 4 turns");
            StartCoroutine(RemoveQuads(quadsToRemove));
        }

        // 5. 현재 활성화된 Quad들 하이라이트 (PULSE_INTERVAL 전달)
        quadDetector.HighlightQuads(trackedQuads, PULSE_INTERVAL);
        Debug.Log($"=== Turn {globalTurnCounter} Ended: {trackedQuads.Count} active quads ===\n");

        // 턴 처리 완료
        isProcessingTurn = false;
    }

    private void ProcessNewQuad(QubeQuad newQuad)
    {
        Debug.Log($"\n[ProcessNewQuad] NEW QUAD: {newQuad.width}x{newQuad.height} at ({newQuad.minX},{newQuad.minY}), size={newQuad.size}");
        Debug.Log($"[ProcessNewQuad] Currently tracking {trackedQuads.Count} quads");

        // 기존 Quad와 완전히 동일한지 먼저 확인 (같은 영역이면 무시)
        foreach (var existingQuad in trackedQuads)
        {
            if (IsSameQuad(newQuad, existingQuad))
            {
                Debug.Log($"[ProcessNewQuad] ✓ Same quad already tracked, ignoring (turnTimer={existingQuad.turnTimer})");
                return;
            }
        }

        // 기존 Quad들과 겹치거나 인접한지 확인
        List<QubeQuad> overlappingQuads = new List<QubeQuad>();
        List<QubeQuad> adjacentQuads = new List<QubeQuad>();

        foreach (var existingQuad in trackedQuads)
        {
            Debug.Log($"[ProcessNewQuad] Checking existing Quad: {existingQuad.width}x{existingQuad.height} at ({existingQuad.minX},{existingQuad.minY}), turnTimer={existingQuad.turnTimer}");

            bool overlaps = newQuad.OverlapsWith(existingQuad);
            bool adjacent = newQuad.IsAdjacentTo(existingQuad);

            Debug.Log($"[ProcessNewQuad]   → Overlaps: {overlaps}, Adjacent: {adjacent}");

            if (overlaps)
            {
                overlappingQuads.Add(existingQuad);
            }
            else if (adjacent)
            {
                adjacentQuads.Add(existingQuad);
            }
        }

        Debug.Log($"[ProcessNewQuad] Found {overlappingQuads.Count} overlapping, {adjacentQuads.Count} adjacent quads");

        // Case 1: 겹치는 Quad가 있는 경우
        if (overlappingQuads.Count > 0)
        {
            ProcessOverlappingQuads(newQuad, overlappingQuads, adjacentQuads);
        }
        // Case 2: 인접한 Quad만 있는 경우
        else if (adjacentQuads.Count > 0)
        {
            ProcessAdjacentQuads(newQuad, adjacentQuads);
        }
        // Case 3: 완전히 새로운 Quad
        else
        {
            newQuad.creationTurn = globalTurnCounter;
            trackedQuads.Add(newQuad);
            Debug.Log($"→ New Quad added: {newQuad.width}x{newQuad.height} at ({newQuad.minX},{newQuad.minY})");
        }
    }

    private void ProcessOverlappingQuads(QubeQuad newQuad, List<QubeQuad> overlappingQuads, List<QubeQuad> adjacentQuads)
    {
        Debug.Log($"[ProcessOverlappingQuads] Processing {overlappingQuads.Count} overlapping + {adjacentQuads.Count} adjacent quads");

        // 새로 감지된 Quad가 기존 Quad들을 포함하거나 확장하는 경우
        // 모든 관련 Quad들 (overlapping + adjacent)과 병합 시도
        List<QubeQuad> allRelatedQuads = new List<QubeQuad>(overlappingQuads);
        allRelatedQuads.AddRange(adjacentQuads);

        QubeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        foreach (var quad in allRelatedQuads)
        {
            Debug.Log($"[ProcessOverlappingQuads] Trying to merge with Quad {quad.width}x{quad.height}");
            bool canMerge = mergedQuad.CanMergeWith(quad);
            Debug.Log($"[ProcessOverlappingQuads]   → CanMerge: {canMerge}");

            if (!canMerge)
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad, globalTurnCounter);
            Debug.Log($"[ProcessOverlappingQuads]   → Merged! New size: {mergedQuad.width}x{mergedQuad.height} (size={mergedQuad.size})");
        }

        // 기존 Quad들의 총 크기 계산
        int totalOldSize = allRelatedQuads.Sum(q => q.size);

        Debug.Log($"[ProcessOverlappingQuads] canMergeAll={canMergeAll}, mergedQuad.size={mergedQuad.size}, newQuad.size={newQuad.size}, totalOldSize={totalOldSize}");
        Debug.Log($"[ProcessOverlappingQuads] Merge condition: {canMergeAll} && {mergedQuad.size} > {totalOldSize} = {canMergeAll && mergedQuad.size > totalOldSize}");

        // 병합 가능하고, 결과가 기존보다 크면 교체 (실제 확장된 경우만)
        if (canMergeAll && mergedQuad.size > totalOldSize)
        {
            foreach (var quad in allRelatedQuads)
            {
                trackedQuads.Remove(quad);
                Debug.Log($"→ Removed old Quad: {quad.width}x{quad.height} (turnTimer was {quad.turnTimer})");
            }

            trackedQuads.Add(mergedQuad);
            Debug.Log($"→ Expanded to {mergedQuad.width}x{mergedQuad.height} Quad (turnTimer={mergedQuad.turnTimer}, creationTurn={mergedQuad.creationTurn})");
        }
        // 병합 실패했지만 새 Quad가 기존 Quad 중 일부를 완전히 포함하는 경우
        else if (!canMergeAll && newQuad.size >= 4)
        {
            // 새 Quad에 완전히 포함되는 기존 Quad들 찾기
            List<QubeQuad> containedQuads = new List<QubeQuad>();
            foreach (var existingQuad in overlappingQuads)
            {
                bool isContained = true;
                foreach (var cell in existingQuad.cells)
                {
                    if (!newQuad.cells.Contains(cell))
                    {
                        isContained = false;
                        break;
                    }
                }
                if (isContained)
                {
                    containedQuads.Add(existingQuad);
                }
            }

            // 포함된 Quad가 있으면 제거하고 새 Quad 추가
            if (containedQuads.Count > 0)
            {
                // 포함된 Quad들 중 가장 오래된 것의 turnTimer 보존
                int maxTurnTimer = 0;
                foreach (var quad in containedQuads)
                {
                    maxTurnTimer = Mathf.Max(maxTurnTimer, quad.turnTimer);
                    trackedQuads.Remove(quad);
                    Debug.Log($"→ Removed contained Quad: {quad.width}x{quad.height} (turnTimer was {quad.turnTimer})");
                }
                newQuad.creationTurn = globalTurnCounter;
                newQuad.turnTimer = maxTurnTimer; // 가장 오래된 Quad의 turnTimer 유지
                trackedQuads.Add(newQuad);
                Debug.Log($"→ Added new Quad {newQuad.width}x{newQuad.height} (replaced {containedQuads.Count} smaller quad(s), preserved turnTimer={maxTurnTimer})");
            }
            else
            {
                Debug.Log($"→ Cannot merge overlapping Quads or no expansion - keeping existing Quads");
            }
        }
        else
        {
            Debug.Log($"→ Cannot merge overlapping Quads or no expansion - keeping existing Quads");
        }
    }

    private void ProcessAdjacentQuads(QubeQuad newQuad, List<QubeQuad> adjacentQuads)
    {
        // 인접한 Quad들과 병합하여 더 큰 Quad 생성 시도
        QubeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        foreach (var quad in adjacentQuads)
        {
            if (!mergedQuad.CanMergeWith(quad))
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad, globalTurnCounter);
        }

        // 병합해서 더 큰 Quad가 되는 경우에만 병합
        if (canMergeAll && mergedQuad.size > newQuad.size)
        {
            foreach (var quad in adjacentQuads)
            {
                trackedQuads.Remove(quad);
                Debug.Log($"→ Removed old Quad: {quad.width}x{quad.height} (turnTimer was {quad.turnTimer})");
            }

            trackedQuads.Add(mergedQuad);
            Debug.Log($"→ Merged adjacent Quads into {mergedQuad.width}x{mergedQuad.height} (turnTimer={mergedQuad.turnTimer}, creationTurn={mergedQuad.creationTurn})");
        }
        else
        {
            // 병합 불가능하거나 크기가 같으면 새 Quad 추가
            newQuad.creationTurn = globalTurnCounter;
            trackedQuads.Add(newQuad);
            Debug.Log($"→ New Quad added (adjacent but cannot merge): {newQuad.width}x{newQuad.height}");
        }
    }

    private IEnumerator RemoveQuads(List<QubeQuad> quadsToRemove)
    {
        int totalScore = 0;

        // 모든 Quad 소거
        foreach (var quad in quadsToRemove)
        {
            totalScore += quad.GetScore();

            foreach (var cellPos in quad.cells)
            {
                // 소거된 셀은 wasCleared=true로 설정 (4턴 동안 어두운 색)
                grid.SetCellOccupied(cellPos, false, Color.clear, wasCleared: true);
            }

            // trackedQuads에서 제거
            trackedQuads.Remove(quad);
            Debug.Log($"Removed Quad: {quad.width}x{quad.height}, Score: {quad.GetScore()}");
        }

        // 동시 소거 보너스
        if (quadsToRemove.Count >= 2)
        {
            float multiplier = quadsToRemove.Count >= 3 ? BONUS_MULTIPLIER_THREE_PLUS : BONUS_MULTIPLIER_TWO;
            totalScore = Mathf.RoundToInt(totalScore * multiplier);
            Debug.Log($"Bonus applied! x{multiplier}");
        }

        // 점수 이벤트 발생
        OnPulse?.Invoke(totalScore);

        yield return new WaitForSeconds(REMOVAL_DELAY);
    }

    public int GetTurnCounter()
    {
        return globalTurnCounter;
    }

    public int GetPulseInterval()
    {
        return PULSE_INTERVAL;
    }

    public List<QubeQuad> GetActiveQuads()
    {
        return trackedQuads;
    }

    public void ManualPulse()
    {
        // 수동 펄스: 모든 Quad 즉시 소거
        if (trackedQuads.Count > 0)
        {
            List<QubeQuad> allQuads = new List<QubeQuad>(trackedQuads);
            StartCoroutine(RemoveQuads(allQuads));
        }
    }

    public void ClearAllQuads()
    {
        // 게임 리셋 시 모든 Quad 클리어
        trackedQuads.Clear();
        globalTurnCounter = 0;
        isProcessingTurn = false; // 플래그 초기화
        Debug.Log("All quads cleared");
    }

    // 두 Quad가 완전히 동일한지 확인 (같은 영역인지)
    private bool IsSameQuad(QubeQuad a, QubeQuad b)
    {
        return a.minX == b.minX && a.maxX == b.maxX &&
               a.minY == b.minY && a.maxY == b.maxY;
    }
}
