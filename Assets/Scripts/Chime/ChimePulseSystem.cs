using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChimePulseSystem : MonoBehaviour
{
    public ChimeGrid grid;
    public ChimeQuadDetector quadDetector;

    private const int PULSE_INTERVAL = 4;
    private const float BONUS_MULTIPLIER_TWO = 1.5f;
    private const float BONUS_MULTIPLIER_THREE_PLUS = 2.0f;
    private const float REMOVAL_DELAY = 0.3f;

    private int globalTurnCounter = 0;
    private List<ChimeQuad> trackedQuads = new List<ChimeQuad>();
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
        for (int x = 0; x < ChimeGrid.WIDTH; x++)
        {
            for (int y = 0; y < ChimeGrid.HEIGHT; y++)
            {
                ChimeCell cell = grid.GetCell(x, y);
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
        List<ChimeQuad> detectedQuads = quadDetector.DetectQuads();
        Debug.Log($"Detected {detectedQuads.Count} potential new quads");

        // 3. 새로 감지된 영역들을 기존 Quad와 병합하거나 새로 추가
        foreach (var newQuad in detectedQuads)
        {
            ProcessNewQuad(newQuad);
        }

        // 4. turnTimer가 pulseInterval인 Quad들 소거
        List<ChimeQuad> quadsToRemove = trackedQuads.Where(q => q.turnTimer >= PULSE_INTERVAL).ToList();
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

    private void ProcessNewQuad(ChimeQuad newQuad)
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
        List<ChimeQuad> overlappingQuads = new List<ChimeQuad>();
        List<ChimeQuad> adjacentQuads = new List<ChimeQuad>();

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

    private void ProcessOverlappingQuads(ChimeQuad newQuad, List<ChimeQuad> overlappingQuads, List<ChimeQuad> adjacentQuads)
    {
        Debug.Log($"[ProcessOverlappingQuads] Processing {overlappingQuads.Count} overlapping + {adjacentQuads.Count} adjacent quads");
        Debug.Log($"[ProcessOverlappingQuads] New quad: {newQuad.width}x{newQuad.height} (size={newQuad.size})");

        // 1. 새 Quad가 기존 Quad보다 크면 확장 (단순 대체)
        ChimeQuad largestOverlapping = null;
        int maxTurnTimer = 0;

        foreach (var existingQuad in overlappingQuads)
        {
            Debug.Log($"[ProcessOverlappingQuads] Comparing with existing: {existingQuad.width}x{existingQuad.height} (size={existingQuad.size})");

            // 새 Quad가 기존 Quad를 완전히 포함하거나 더 크면 확장
            if (newQuad.size > existingQuad.size)
            {
                if (largestOverlapping == null || existingQuad.size > largestOverlapping.size)
                {
                    largestOverlapping = existingQuad;
                }
                maxTurnTimer = Mathf.Max(maxTurnTimer, existingQuad.turnTimer);
            }
        }

        // 확장 가능한 경우
        if (largestOverlapping != null)
        {
            // 기존 겹치는 quad들 중 새 quad보다 작은 것들 제거
            List<ChimeQuad> toRemove = new List<ChimeQuad>();
            foreach (var existingQuad in overlappingQuads)
            {
                if (newQuad.size > existingQuad.size)
                {
                    toRemove.Add(existingQuad);
                }
            }

            foreach (var quad in toRemove)
            {
                trackedQuads.Remove(quad);
                Debug.Log($"→ Removed smaller Quad: {quad.width}x{quad.height} (turnTimer was {quad.turnTimer})");
            }

            // 새 Quad에 기존 turnTimer 유지
            newQuad.turnTimer = maxTurnTimer;
            newQuad.creationTurn = globalTurnCounter;
            trackedQuads.Add(newQuad);
            Debug.Log($"→ Expanded to {newQuad.width}x{newQuad.height} Quad (turnTimer={newQuad.turnTimer})");
            return;
        }

        // 2. 병합 시도 (인접한 quad들과 함께)
        List<ChimeQuad> allRelatedQuads = new List<ChimeQuad>(overlappingQuads);
        allRelatedQuads.AddRange(adjacentQuads);

        ChimeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        foreach (var quad in allRelatedQuads)
        {
            bool canMerge = mergedQuad.CanMergeWith(quad);
            if (!canMerge)
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad, globalTurnCounter);
        }

        int totalOldSize = allRelatedQuads.Sum(q => q.size);

        if (canMergeAll && mergedQuad.size > totalOldSize)
        {
            foreach (var quad in allRelatedQuads)
            {
                trackedQuads.Remove(quad);
                Debug.Log($"→ Removed old Quad: {quad.width}x{quad.height} (turnTimer was {quad.turnTimer})");
            }

            trackedQuads.Add(mergedQuad);
            Debug.Log($"→ Merged to {mergedQuad.width}x{mergedQuad.height} Quad (turnTimer={mergedQuad.turnTimer})");
        }
        else
        {
            Debug.Log($"→ No expansion possible - keeping existing Quads");
        }
    }

    private void ProcessAdjacentQuads(ChimeQuad newQuad, List<ChimeQuad> adjacentQuads)
    {
        // 인접한 Quad들과 병합하여 더 큰 Quad 생성 시도
        ChimeQuad mergedQuad = newQuad;
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

    private IEnumerator RemoveQuads(List<ChimeQuad> quadsToRemove)
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

    public List<ChimeQuad> GetActiveQuads()
    {
        return trackedQuads;
    }

    public void ManualPulse()
    {
        // 수동 펄스: 모든 Quad 즉시 소거
        if (trackedQuads.Count > 0)
        {
            List<ChimeQuad> allQuads = new List<ChimeQuad>(trackedQuads);
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
    private bool IsSameQuad(ChimeQuad a, ChimeQuad b)
    {
        return a.minX == b.minX && a.maxX == b.maxX &&
               a.minY == b.minY && a.maxY == b.maxY;
    }
}
