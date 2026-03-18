using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QubeScoreFeedback : MonoBehaviour
{
    private const float POPUP_DURATION = 0.6f;
    private const float POPUP_RISE = 60f;
    private const float COMBO_DURATION = 0.8f;
    private const float COMBO_SCALE = 1.5f;
    private const float SHAKE_DURATION = 0.3f;
    private const int POPUP_FONT_SIZE = 40;
    private const int COMBO_FONT_SIZE = 56;

    [Header("References")]
    public RectTransform canvasRect;

    public void ShowScorePopup(int score, Vector2 position)
    {
        GameObject popup = CreateTextObject($"+{score}", POPUP_FONT_SIZE, Color.white, position);
        StartCoroutine(PopupAnimation(popup));
    }

    public void ShowComboText(int comboCount, float multiplier)
    {
        string text = comboCount >= 3
            ? $"x{multiplier:F1} AMAZING!"
            : $"x{multiplier:F1} COMBO!";
        Color color = comboCount >= 3
            ? new Color(1.00f, 0.18f, 0.47f)   // 마젠타 (새 팔레트)
            : new Color(1.00f, 0.72f, 0.00f);   // 앰버 (새 팔레트)

        GameObject combo = CreateTextObject(text, COMBO_FONT_SIZE, color, Vector2.zero);
        StartCoroutine(ComboAnimation(combo));
    }

    public void ShakeScreen(float intensity)
    {
        if (canvasRect != null)
        {
            StartCoroutine(ShakeAnimation(intensity));
        }
    }

    private GameObject CreateTextObject(string text, int fontSize, Color color, Vector2 position)
    {
        GameObject textObj = new GameObject("ScorePopup");
        textObj.transform.SetParent(transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400f, 80f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);

        // 글로우 효과 (TMP 언더레이)
        tmp.ForceMeshUpdate();
        Material mat = tmp.fontMaterial; // TMP는 인스턴스 복사본 반환
        if (mat.HasProperty("_UnderlayColor"))
        {
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(color.r, color.g, color.b, 0.4f));
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", 0f);
            mat.SetFloat("_UnderlaySoftness", 0.5f);
        }

        Canvas canvas = textObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 2000;

        return textObj;
    }

    private IEnumerator PopupAnimation(GameObject popup)
    {
        RectTransform rect = popup.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = popup.GetComponent<TextMeshProUGUI>();
        Vector2 startPos = rect.anchoredPosition;
        Color startColor = tmp.color;
        float elapsed = 0f;

        while (elapsed < POPUP_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / POPUP_DURATION;

            rect.anchoredPosition = startPos + Vector2.up * (POPUP_RISE * t);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        Destroy(popup);
    }

    private IEnumerator ComboAnimation(GameObject combo)
    {
        RectTransform rect = combo.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = combo.GetComponent<TextMeshProUGUI>();
        Color startColor = tmp.color;
        float elapsed = 0f;

        while (elapsed < COMBO_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / COMBO_DURATION;

            // 스케일업 후 축소
            float scale = t < 0.3f
                ? Mathf.Lerp(0.5f, COMBO_SCALE, t / 0.3f)
                : Mathf.Lerp(COMBO_SCALE, 1f, (t - 0.3f) / 0.7f);
            rect.localScale = Vector3.one * scale;

            // 후반부 페이드아웃
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(combo);
    }

    private IEnumerator ShakeAnimation(float intensity)
    {
        Vector2 originalPos = canvasRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < SHAKE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / SHAKE_DURATION);
            float offsetX = Random.Range(-intensity, intensity) * t;
            float offsetY = Random.Range(-intensity, intensity) * t;
            canvasRect.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
            yield return null;
        }

        canvasRect.anchoredPosition = originalPos;
    }
}
