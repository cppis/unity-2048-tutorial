using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChimeCell : MonoBehaviour
{
    private const float ANCHOR_CENTER = 0.5f;
    private const float TIMER_TEXT_SIZE = 60f;
    private const int TIMER_FONT_SIZE_NORMAL = 36;
    private const int TIMER_FONT_SIZE_CENTER = 72;
    private const int CLEAR_TIMER_DURATION = 8;
    private const float OUTLINE_THICKNESS = 6f; // 외곽선 두께

    private static readonly Color EMPTY_COLOR = new Color(0.176f, 0.204f, 0.212f, 1f);
    private static readonly Color CLEARED_COLOR = new Color(0.08f, 0.1f, 0.11f, 1f);

    public Vector2Int coordinates;
    public bool isOccupied;
    public Color cellColor;
    public Color originalColor;
    public int clearTimer = 0;
    public int blockId = -1; // -1 means no block assigned

    private Image image;
    private TextMeshProUGUI turnTimerText;

    // 4개의 변 (상하좌우)
    private Image topBorder;
    private Image bottomBorder;
    private Image leftBorder;
    private Image rightBorder;

    private void Awake()
    {
        InitializeComponents();
        CreateBorders();
        CreateTurnTimerText();
    }

    private void InitializeComponents()
    {
        image = GetComponent<Image>();
    }

    private void CreateBorders()
    {
        // 셀의 크기를 가져옴 (RectTransform 사용)
        RectTransform cellRect = GetComponent<RectTransform>();
        Vector2 cellSize = cellRect.sizeDelta;

        // 4개의 변 생성
        topBorder = CreateBorderImage("TopBorder", cellSize.x, OUTLINE_THICKNESS, 0, cellSize.y / 2);
        bottomBorder = CreateBorderImage("BottomBorder", cellSize.x, OUTLINE_THICKNESS, 0, -cellSize.y / 2);
        leftBorder = CreateBorderImage("LeftBorder", OUTLINE_THICKNESS, cellSize.y, -cellSize.x / 2, 0);
        rightBorder = CreateBorderImage("RightBorder", OUTLINE_THICKNESS, cellSize.y, cellSize.x / 2, 0);

        // 초기에는 모두 비활성화
        topBorder.enabled = false;
        bottomBorder.enabled = false;
        leftBorder.enabled = false;
        rightBorder.enabled = false;
    }

    private Image CreateBorderImage(string name, float width, float height, float xPos, float yPos)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(transform, false);

        RectTransform rectTransform = borderObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        rectTransform.sizeDelta = new Vector2(width, height);

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = Color.yellow;
        borderImage.raycastTarget = false;

        // Canvas 추가하여 렌더링 순서를 높임
        Canvas canvas = borderObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        return borderImage;
    }

    private void CreateTurnTimerText()
    {
        GameObject textObj = new GameObject("TurnTimerText");
        textObj.transform.SetParent(transform, false); // worldPositionStays = false

        RectTransform rectTransform = SetupTimerRectTransform(textObj);
        turnTimerText = SetupTimerTextComponent(textObj);

        // Canvas 컴포넌트 추가하여 렌더링 순서 제어
        Canvas canvas = textObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000; // 매우 높은 값으로 설정하여 항상 최상위에 렌더링

        // GraphicRaycaster는 필요없으므로 추가하지 않음 (성능 최적화)

        // CanvasGroup으로 raycast 제어
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Debug.Log($"[CreateTurnTimerText] Cell ({coordinates.x},{coordinates.y}): Created turn counter text with Canvas sortingOrder=1000");
    }

    private RectTransform SetupTimerRectTransform(GameObject textObj)
    {
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(TIMER_TEXT_SIZE, TIMER_TEXT_SIZE);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private TextMeshProUGUI SetupTimerTextComponent(GameObject textObj)
    {
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = TIMER_FONT_SIZE_NORMAL;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        textComponent.enabled = false;

        // 텍스트 outline 추가 (어두운 배경에서도 보이도록)
        textComponent.outlineWidth = 0.2f;
        textComponent.outlineColor = Color.black;

        return textComponent;
    }

    public void SetColor(Color color)
    {
        cellColor = color;
        if (image != null)
        {
            image.color = color;
        }
    }

    public void SetOccupied(bool occupied, Color color, bool wasCleared = false, int blockIdValue = -1)
    {
        isOccupied = occupied;

        if (occupied)
        {
            HandleOccupiedState(color, blockIdValue);
        }
        else
        {
            HandleEmptyState(wasCleared);
        }
    }

    private void HandleOccupiedState(Color color, int blockIdValue)
    {
        originalColor = color;
        clearTimer = 0;
        blockId = blockIdValue;

        // Outline이 작동하려면 Image가 활성화되어 있어야 하므로
        // image.enabled = false를 제거하고 대신 투명하게 설정
        if (image != null)
        {
            image.enabled = true;
            image.color = new Color(0, 0, 0, 0); // 완전 투명
        }
    }

    private void HandleEmptyState(bool wasCleared)
    {
        blockId = -1; // Reset block ID when cell becomes empty

        if (wasCleared)
        {
            SetClearedState();
        }
        else
        {
            SetNormalEmptyState();
        }

        if (image != null)
        {
            image.enabled = true;
        }

        SetOutline(false, false, false, false);
        SetTurnTimer(-1);
    }

    private void SetClearedState()
    {
        clearTimer = CLEAR_TIMER_DURATION;
        originalColor = CLEARED_COLOR;
        SetColor(CLEARED_COLOR);
    }

    private void SetNormalEmptyState()
    {
        originalColor = EMPTY_COLOR;
        SetColor(EMPTY_COLOR);
    }

    public void UpdateClearTimer()
    {
        if (clearTimer > 0)
        {
            clearTimer--;

            if (clearTimer == 0)
            {
                originalColor = EMPTY_COLOR;
                SetColor(EMPTY_COLOR);
            }
        }
    }

    public void SetOutline(bool top, bool bottom, bool left, bool right, Color? outlineColor = null)
    {
        if (topBorder == null || bottomBorder == null || leftBorder == null || rightBorder == null)
            return;

        Color borderColor = outlineColor ?? Color.yellow;

        topBorder.enabled = top;
        bottomBorder.enabled = bottom;
        leftBorder.enabled = left;
        rightBorder.enabled = right;

        if (top) topBorder.color = borderColor;
        if (bottom) bottomBorder.color = borderColor;
        if (left) leftBorder.color = borderColor;
        if (right) rightBorder.color = borderColor;
    }

    public void SetTurnTimer(int remainingTurns, bool isCenter = false, Vector2? offset = null)
    {
        if (turnTimerText == null)
        {
            Debug.LogWarning($"[SetTurnTimer] turnTimerText is null at ({coordinates.x},{coordinates.y})");
            return;
        }

        Debug.Log($"[SetTurnTimer] Cell ({coordinates.x},{coordinates.y}): remainingTurns={remainingTurns}, isCenter={isCenter}");

        if (remainingTurns > 0)
        {
            ShowTurnTimer(remainingTurns, isCenter, offset);
        }
        else
        {
            HideTurnTimer();
            Debug.Log($"[SetTurnTimer] Cell ({coordinates.x},{coordinates.y}): Hiding timer (remainingTurns <= 0)");
        }
    }

    private void ShowTurnTimer(int remainingTurns, bool isCenter, Vector2? offset)
    {
        turnTimerText.text = remainingTurns.ToString();
        turnTimerText.enabled = true;
        turnTimerText.fontSize = isCenter ? TIMER_FONT_SIZE_CENTER : TIMER_FONT_SIZE_NORMAL;
        turnTimerText.gameObject.SetActive(true);

        UpdateTimerPosition(offset);

        Debug.Log($"[ShowTurnTimer] Cell ({coordinates.x},{coordinates.y}): text='{turnTimerText.text}', enabled={turnTimerText.enabled}, fontSize={turnTimerText.fontSize}, gameObject.active={turnTimerText.gameObject.activeSelf}, color={turnTimerText.color}, offset={offset}");
        Debug.Log($"[ShowTurnTimer] TextMeshPro component: {turnTimerText.GetType().Name}, parent={turnTimerText.transform.parent.name}");
    }

    private void UpdateTimerPosition(Vector2? offset)
    {
        RectTransform textRect = turnTimerText.GetComponent<RectTransform>();
        textRect.anchoredPosition = offset ?? Vector2.zero;
    }

    private void HideTurnTimer()
    {
        turnTimerText.enabled = false;
    }
}
