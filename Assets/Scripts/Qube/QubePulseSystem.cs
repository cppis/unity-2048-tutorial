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
    }

    private void ProcessNewQuad(QubeQuad newQuad)
    {
        // 기존 Quad들과 겹치거나 인접한지 확인
        List<QubeQuad> relatedQuads = new List<QubeQuad>();

        foreach (var existingQuad in trackedQuads)
        {
            // 겹치거나 인접한 Quad 찾기
            if (newQuad.OverlapsWith(existingQuad) || newQuad.IsAdjacentTo(existingQuad))
            {
                relatedQuads.Add(existingQuad);
            }
        }

        if (relatedQuads.Count == 0)
        {
            // 완전히 새로운 Quad (겹치지도 인접하지도 않음)
            newQuad.creationTurn = globalTurnCounter;
            trackedQuads.Add(newQuad);
            Debug.Log($"→ New Quad added: {newQuad.width}x{newQuad.height} at ({newQuad.minX},{newQuad.minY})");
        }
        else
        {
            // 겹치거나 인접한 Quad가 있음 - 병합 시도
            QubeQuad mergedQuad = newQuad;
            List<QubeQuad> quadsToMerge = new List<QubeQuad>(relatedQuads);

            // 모든 관련 Quad들을 하나로 병합 시도
            bool canMergeAll = true;
            foreach (var quad in relatedQuads)
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
                foreach (var quad in relatedQuads)
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
        Debug.Log("All quads cleared");
    }
}
