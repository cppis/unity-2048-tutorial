using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QubeCell : MonoBehaviour
{
    private const float ANCHOR_CENTER = 0.5f;
    private const float TIMER_TEXT_SIZE = 60f;
    private const int TIMER_FONT_SIZE_NORMAL = 36;
    private const int TIMER_FONT_SIZE_CENTER = 72;
    private const int CLEAR_TIMER_DURATION = 8;
    private const float OUTLINE_DISTANCE = 3f;

    private static readonly Color EMPTY_COLOR = new Color(0.176f, 0.204f, 0.212f, 1f);
    private static readonly Color CLEARED_COLOR = new Color(0.08f, 0.1f, 0.11f, 1f);
    private static readonly Vector2 OUTLINE_EFFECT_DISTANCE = new Vector2(OUTLINE_DISTANCE, OUTLINE_DISTANCE);

    public Vector2Int coordinates;
    public bool isOccupied;
    public Color cellColor;
    public Color originalColor;
    public int clearTimer = 0;

    private Image image;
    private Outline outline;
    private TextMeshProUGUI turnTimerText;

    private void Awake()
    {
        InitializeComponents();
        CreateTurnTimerText();
    }

    private void InitializeComponents()
    {
        image = GetComponent<Image>();
        outline = GetOrAddComponent<Outline>();
        SetupOutline();
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }

    private void SetupOutline()
    {
        outline.effectColor = Color.yellow;
        outline.effectDistance = OUTLINE_EFFECT_DISTANCE;
        outline.enabled = false;
    }

    private void CreateTurnTimerText()
    {
        GameObject textObj = new GameObject("TurnTimerText");
        textObj.transform.SetParent(transform);

        RectTransform rectTransform = SetupTimerRectTransform(textObj);
        turnTimerText = SetupTimerTextComponent(textObj);
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

    public void SetOccupied(bool occupied, Color color, bool wasCleared = false)
    {
        isOccupied = occupied;

        if (occupied)
        {
            HandleOccupiedState(color);
        }
        else
        {
            HandleEmptyState(wasCleared);
        }
    }

    private void HandleOccupiedState(Color color)
    {
        originalColor = color;
        clearTimer = 0;

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

        SetOutline(false);
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

    public void SetOutline(bool enabled, Color? outlineColor = null)
    {
        if (outline == null) return;

        outline.enabled = enabled;

        if (enabled && outlineColor.HasValue)
        {
            outline.effectColor = outlineColor.Value;
        }
    }

    public void SetTurnTimer(int remainingTurns, bool isCenter = false, Vector2? offset = null)
    {
        if (turnTimerText == null) return;

        if (remainingTurns > 0)
        {
            ShowTurnTimer(remainingTurns, isCenter, offset);
        }
        else
        {
            HideTurnTimer();
        }
    }

    private void ShowTurnTimer(int remainingTurns, bool isCenter, Vector2? offset)
    {
        turnTimerText.text = remainingTurns.ToString();
        turnTimerText.enabled = true;
        turnTimerText.fontSize = isCenter ? TIMER_FONT_SIZE_CENTER : TIMER_FONT_SIZE_NORMAL;

        UpdateTimerPosition(offset);
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
