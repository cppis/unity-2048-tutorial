using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image 셀에 베벨(하이라이트+섀도우) 효과를 추가하는 유틸리티.
/// </summary>
public static class QubeBevel
{
    private const float HIGHLIGHT_ALPHA = 0.30f;
    private const float SHADOW_ALPHA = 0.25f;

    /// <summary>
    /// 셀 GameObject에 베벨 바(하이라이트 + 섀도우)를 자식으로 추가합니다.
    /// </summary>
    /// <param name="cellObj">베벨을 추가할 셀 GameObject</param>
    /// <param name="cellSize">셀의 크기 (width/height)</param>
    /// <param name="bevelThickness">베벨 바의 두께</param>
    public static void AddBevel(GameObject cellObj, float cellSize, float bevelThickness)
    {
        // 부모 셀의 피벗이 center(0.5, 0.5)일 수 있으므로
        // 자식의 앵커를 stretch로 설정하여 부모 크기에 맞춤
        float half = cellSize / 2f;

        // 상단 하이라이트 (밝은 바)
        CreateBevelBar(cellObj.transform, "Highlight_Top",
            new Vector2(-half, half - bevelThickness),
            new Vector2(cellSize, bevelThickness),
            new Color(1f, 1f, 1f, HIGHLIGHT_ALPHA));

        // 좌측 하이라이트
        CreateBevelBar(cellObj.transform, "Highlight_Left",
            new Vector2(-half, -half),
            new Vector2(bevelThickness, cellSize - bevelThickness),
            new Color(1f, 1f, 1f, HIGHLIGHT_ALPHA * 0.7f));

        // 하단 섀도우 (어두운 바)
        CreateBevelBar(cellObj.transform, "Shadow_Bottom",
            new Vector2(-half, -half),
            new Vector2(cellSize, bevelThickness),
            new Color(0f, 0f, 0f, SHADOW_ALPHA));

        // 우측 섀도우
        CreateBevelBar(cellObj.transform, "Shadow_Right",
            new Vector2(half - bevelThickness, -half + bevelThickness),
            new Vector2(bevelThickness, cellSize - bevelThickness),
            new Color(0f, 0f, 0f, SHADOW_ALPHA * 0.7f));
    }

    private static void CreateBevelBar(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(parent, false);

        Image img = bar.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rect = bar.GetComponent<RectTransform>();
        // 부모 중앙 앵커 + 좌하단 피벗 → position은 부모 중심 기준 좌하단 오프셋
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
