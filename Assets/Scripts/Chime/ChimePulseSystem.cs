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

        // 2. 새로운 영역 감지 (이미 추적 중인 셀들은 제외)
        HashSet<Vector2Int> trackedCells = GetTrackedCells();
        List<ChimeQuad> detectedQuads = quadDetector.DetectQuads(trackedCells);
        Debug.Log($"Detected {detectedQuads.Count} potential new quads (excluding {trackedCells.Count} tracked cells)");

        // 3. 새로 감지된 영역들을 추가
        foreach (var newQuad in detectedQuads)
        {
            AddNewQuad(newQuad);
        }

        // 4. turnTimer가 pulseInterval인 Quad들 소거
        List<ChimeQuad> quadsToRemove = trackedQuads.Where(q => q.turnTimer >= PULSE_INTERVAL).ToList();
        if (quadsToRemove.Count > 0)
        {
            Debug.Log($"Removing {quadsToRemove.Count} quads that reached 4 turns");
            StartCoroutine(RemoveQuads(quadsToRemove));

            // 5. quad 제거 후 남은 셀들로 새로운 quad 감지
            // (페이드 아웃 중인 셀은 IsCellOccupied에서 false 반환되므로 제외됨)
            HashSet<Vector2Int> remainingTrackedCells = GetTrackedCells();
            List<ChimeQuad> additionalQuads = quadDetector.DetectQuads(remainingTrackedCells);
            Debug.Log($"After removal, detected {additionalQuads.Count} potential new quads from remaining cells");
            foreach (var newQuad in additionalQuads)
            {
                AddNewQuad(newQuad);
            }
        }

        // 6. 현재 활성화된 Quad들 하이라이트 (PULSE_INTERVAL 전달)
        quadDetector.HighlightQuads(trackedQuads, PULSE_INTERVAL);
        Debug.Log($"=== Turn {globalTurnCounter} Ended: {trackedQuads.Count} active quads ===\n");

        // 턴 처리 완료
        isProcessingTurn = false;
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
                // 소거된 셀은 wasCleared=true로 설정 (8턴 동안 어두운 색)
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

    // 현재 추적 중인 모든 quad의 셀 목록 반환
    private HashSet<Vector2Int> GetTrackedCells()
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        foreach (var quad in trackedQuads)
        {
            foreach (var cell in quad.cells)
            {
                cells.Add(cell);
            }
        }
        return cells;
    }

    // 새로운 quad 추가 (셀 중복 없이)
    private void AddNewQuad(ChimeQuad newQuad)
    {
        // 이미 동일한 quad가 추적 중인지 확인
        foreach (var existingQuad in trackedQuads)
        {
            if (IsSameQuad(newQuad, existingQuad))
            {
                Debug.Log($"[AddNewQuad] Same quad already tracked, ignoring");
                return;
            }
        }

        newQuad.creationTurn = globalTurnCounter;
        trackedQuads.Add(newQuad);
        Debug.Log($"[AddNewQuad] New Quad added: {newQuad.width}x{newQuad.height} at ({newQuad.minX},{newQuad.minY})");
    }
}
