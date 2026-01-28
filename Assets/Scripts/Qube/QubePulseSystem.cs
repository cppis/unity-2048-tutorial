using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QubePulseSystem : MonoBehaviour
{
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;

    private int globalTurnCounter = 0;
    private int pulseInterval = 4; // 4턴마다 펄스
    private List<QubeQuad> activeQuads = new List<QubeQuad>();

    public delegate void OnPulseDelegate(int score);
    public event OnPulseDelegate OnPulse;

    public void IncrementTurn()
    {
        globalTurnCounter++;

        // Quad 검출
        activeQuads = quadDetector.DetectQuads();
        quadDetector.HighlightQuads(activeQuads);

        // 펄스 체크
        if (globalTurnCounter >= pulseInterval)
        {
            TriggerPulse();
        }
    }

    public void TriggerPulse()
    {
        if (activeQuads.Count > 0)
        {
            StartCoroutine(PulseCoroutine());
        }

        // 턴 카운터 리셋
        globalTurnCounter = 0;
    }

    private IEnumerator PulseCoroutine()
    {
        int totalScore = 0;

        // 모든 Quad 소거
        foreach (var quad in activeQuads)
        {
            totalScore += quad.GetScore();

            foreach (var cellPos in quad.cells)
            {
                grid.SetCellOccupied(cellPos, false, Color.clear);
            }
        }

        // 동시 소거 보너스
        if (activeQuads.Count >= 2)
        {
            float multiplier = activeQuads.Count >= 3 ? 2.0f : 1.5f;
            totalScore = Mathf.RoundToInt(totalScore * multiplier);
        }

        // 펄스 이벤트 발생
        OnPulse?.Invoke(totalScore);

        yield return new WaitForSeconds(0.3f);

        // Quad 리스트 클리어
        activeQuads.Clear();
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
        return activeQuads;
    }

    public void ManualPulse()
    {
        // 수동 펄스 (Zen 모드 옵션)
        activeQuads = quadDetector.DetectQuads();
        if (activeQuads.Count > 0)
        {
            TriggerPulse();
        }
    }
}
