using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QubeCell : MonoBehaviour
{
    public Vector2Int coordinates;
    public bool isOccupied;
    public Color cellColor;
    public Color originalColor; // 원본 색상 저장 (하이라이트용)
    public int clearTimer = 0; // 소거 후 경과 턴 (4턴 동안 어두운 색 유지)

    private Image image;
    private Outline outline;
    private TextMeshProUGUI turnTimerText;

    // 색상 상수
    private static readonly Color emptyColor = new Color(0.176f, 0.204f, 0.212f, 1f); // 빈 셀 (#2D3436)
    private static readonly Color clearedColor = new Color(0.08f, 0.1f, 0.11f, 1f); // 소거된 셀 (더 어두움)

    private void Awake()
    {
        image = GetComponent<Image>();

        // Outline 컴포넌트 추가 (없으면 생성)
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        // 기본 외곽선 설정 (비활성화 상태)
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(3, 3);
        outline.enabled = false;

        // TurnTimer 텍스트 생성
        CreateTurnTimerText();
    }

    private void CreateTurnTimerText()
    {
        // 자식 GameObject로 텍스트 생성
        GameObject textObj = new GameObject("TurnTimerText");
        textObj.transform.SetParent(transform);

        // RectTransform 설정
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(60, 60);
        rectTransform.localScale = Vector3.one;

        // TextMeshProUGUI 컴포넌트 추가
        turnTimerText = textObj.AddComponent<TextMeshProUGUI>();
        turnTimerText.fontSize = 36;
        turnTimerText.fontStyle = FontStyles.Bold;
        turnTimerText.alignment = TextAlignmentOptions.Center;
        turnTimerText.color = Color.white;
        turnTimerText.raycastTarget = false;

        // 기본적으로 비활성화
        turnTimerText.enabled = false;
    }

    public void SetColor(Color color)
    {
        cellColor = color;
        if (image != null)
        {
            image.color = color;
        }
    }

    public void SetOccupied(bool occupied, Color color, bool wasCleared = false)
    {
        isOccupied = occupied;
        if (occupied)
        {
            originalColor = color; // 원본 색상 저장
            SetColor(color);
            clearTimer = 0; // 블록 배치 시 clearTimer 리셋
        }
        else
        {
            // 빈 셀
            if (wasCleared)
            {
                // 소거된 셀: 8턴 동안 더 어두운 색 (테스트용)
                clearTimer = 8;
                originalColor = clearedColor;
                SetColor(clearedColor);
            }
            else
            {
                // 일반 빈 셀
                originalColor = emptyColor;
                SetColor(emptyColor);
            }

            // 비어있으면 외곽선도 비활성화
            SetOutline(false);
            // 턴 타이머 텍스트도 숨김
            SetTurnTimer(-1);
        }
    }

    // 매 턴마다 호출하여 clearTimer 감소 및 색상 업데이트
    public void UpdateClearTimer()
    {
        if (clearTimer > 0)
        {
            clearTimer--;

            if (clearTimer == 0)
            {
                // clearTimer가 0이 되면 일반 빈 셀 색상으로 복구
                originalColor = emptyColor;
                SetColor(emptyColor);
            }
        }
    }

    public void SetOutline(bool enabled, Color? outlineColor = null)
    {
        if (outline != null)
        {
            outline.enabled = enabled;
            if (enabled && outlineColor.HasValue)
            {
                outline.effectColor = outlineColor.Value;
            }
        }
    }

    public void SetTurnTimer(int remainingTurns, bool isCenter = false)
    {
        if (turnTimerText == null) return;

        if (remainingTurns > 0)
        {
            turnTimerText.text = remainingTurns.ToString();
            turnTimerText.enabled = true;

            // 중앙 셀이면 2배 크기 (72), 아니면 기본 크기 (36)
            turnTimerText.fontSize = isCenter ? 72 : 36;
        }
        else
        {
            turnTimerText.enabled = false;
        }
    }
}
