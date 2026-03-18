using UnityEngine;

/// <summary>
/// 그리드 셀의 데이터를 관리하는 순수 데이터 클래스.
/// 시각적 렌더링은 QubeGridLines가 담당합니다.
/// </summary>
public class QubeCell
{
    private const int CLEAR_TIMER_DURATION = 3;

    private static readonly Color EMPTY_COLOR = new Color(0f, 0f, 0f, 0f); // 투명 (배경 그라데이션이 보임)
    private static readonly Color CLEARED_COLOR = new Color(0f, 0f, 0f, 0.5f); // 반투명 검정 (배경 위에 어두운 오버레이)

    public Vector2Int coordinates;
    public bool isOccupied;
    public Color cellColor;
    public Color originalColor;
    public int clearTimer = 0;
    public int blockId = -1;

    public QubeCell(Vector2Int coords)
    {
        coordinates = coords;
        isOccupied = false;
        cellColor = EMPTY_COLOR;
        originalColor = EMPTY_COLOR;
    }

    public void SetColor(Color color)
    {
        cellColor = color;
    }

    public void SetOccupied(bool occupied, Color color, bool wasCleared = false, int blockIdValue = -1)
    {
        isOccupied = occupied;

        if (occupied)
        {
            originalColor = color;
            clearTimer = 0;
            blockId = blockIdValue;
            cellColor = new Color(0, 0, 0, 0); // 투명 (배치된 블록 비주얼이 표시)
        }
        else
        {
            blockId = -1;

            if (wasCleared)
            {
                clearTimer = CLEAR_TIMER_DURATION;
                UpdateClearedColor();
            }
            else
            {
                originalColor = EMPTY_COLOR;
                SetColor(EMPTY_COLOR);
            }
        }
    }

    public void UpdateClearTimer()
    {
        if (clearTimer > 0)
        {
            clearTimer--;
            UpdateClearedColor();
        }
    }

    private void UpdateClearedColor()
    {
        if (clearTimer <= 0)
        {
            originalColor = EMPTY_COLOR;
            SetColor(EMPTY_COLOR);
            return;
        }

        float t = 1f - (float)clearTimer / CLEAR_TIMER_DURATION;
        Color blended = Color.Lerp(CLEARED_COLOR, EMPTY_COLOR, t);
        originalColor = blended;
        SetColor(blended);
    }
}
