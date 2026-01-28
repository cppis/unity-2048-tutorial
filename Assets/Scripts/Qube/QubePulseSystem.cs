using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QubePulseSystem : MonoBehaviour
{
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;

    private int globalTurnCounter = 0;
    private int pulseInterval = 8; // 8턴마다 펄스 (테스트용)
    private List<QubeQuad> trackedQuads = new List<QubeQuad>(); // 추적 중인 Quad들

    public delegate void OnPulseDelegate(int score);
    public event OnPulseDelegate OnPulse;

    public void IncrementTurn()
    {
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

        // 4. turnTimer가 4인 Quad들 소거
        List<QubeQuad> quadsToRemove = trackedQuads.Where(q => q.turnTimer >= pulseInterval).ToList();
        if (quadsToRemove.Count > 0)
        {
            Debug.Log($"Removing {quadsToRemove.Count} quads that reached 4 turns");
            StartCoroutine(RemoveQuads(quadsToRemove));
        }

        // 5. 현재 활성화된 Quad들 하이라이트 (pulseInterval 전달)
        quadDetector.HighlightQuads(trackedQuads, pulseInterval);
        Debug.Log($"=== Turn {globalTurnCounter} Ended: {trackedQuads.Count} active quads ===\n");
    }

    private void ProcessNewQuad(QubeQuad newQuad)
    {
        // 기존 Quad들과 겹치거나 병합 가능한지 확인
        List<QubeQuad> overlappingQuads = new List<QubeQuad>();

        foreach (var existingQuad in trackedQuads)
        {
            if (newQuad.OverlapsWith(existingQuad))
            {
                overlappingQuads.Add(existingQuad);
            }
        }

        if (overlappingQuads.Count == 0)
        {
            // 완전히 새로운 Quad
            newQuad.creationTurn = globalTurnCounter;
            trackedQuads.Add(newQuad);
            Debug.Log($"→ New Quad added: {newQuad.width}x{newQuad.height} at ({newQuad.minX},{newQuad.minY})");
        }
        else
        {
            // 겹치는 Quad가 있음 - 병합 시도
            QubeQuad mergedQuad = newQuad;
            List<QubeQuad> quadsToMerge = new List<QubeQuad>(overlappingQuads);

            // 모든 겹치는 Quad들을 하나로 병합 시도
            bool canMergeAll = true;
            foreach (var quad in overlappingQuads)
            {
                if (!mergedQuad.CanMergeWith(quad))
                {
                    canMergeAll = false;
                    break;
                }
                mergedQuad = mergedQuad.MergeWith(quad, globalTurnCounter);
            }

            if (canMergeAll && mergedQuad.size > newQuad.size)
            {
                // 더 큰 Quad로 병합 성공
                foreach (var quad in overlappingQuads)
                {
                    trackedQuads.Remove(quad);
                    Debug.Log($"→ Removed old Quad: {quad.width}x{quad.height}");
                }

                trackedQuads.Add(mergedQuad);
                Debug.Log($"→ Merged into larger Quad: {mergedQuad.width}x{mergedQuad.height} (turnTimer reset)");
            }
            else
            {
                // 병합 불가능 - 기존 Quad 유지
                Debug.Log($"→ Cannot merge - keeping existing Quads");
            }
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
            float multiplier = quadsToRemove.Count >= 3 ? 2.0f : 1.5f;
            totalScore = Mathf.RoundToInt(totalScore * multiplier);
            Debug.Log($"Bonus applied! x{multiplier}");
        }

        // 점수 이벤트 발생
        OnPulse?.Invoke(totalScore);

        yield return new WaitForSeconds(0.3f);
    }

    public int GetTurnCounter()
    {
        return globalTurnCounter;
    }

    public int GetPulseInterval()
    {
        return pulseInterval;
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
        Debug.Log("All quads cleared");
    }
}
