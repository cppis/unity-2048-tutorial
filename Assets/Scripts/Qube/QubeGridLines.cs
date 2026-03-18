using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 그리드 라인을 Image 오브젝트로 렌더링합니다.
/// 솔리드 컬러 Image는 텍스처 스케일링이 없으므로 해상도와 무관하게 항상 선명합니다.
/// </summary>
public class QubeGridLines : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 8;
    public float cellSize = 80f;
    public float spacing = 5f;
    public float lineThickness = 2f;
    public Color lineColor = new Color(0.3f, 0.35f, 0.38f, 1f);

    // 글로우 & 도트 설정
    private const float GLOW_THICKNESS_MULTIPLIER = 3f;
    private const float GLOW_ALPHA = 0.12f;
    private const float DOT_SIZE = 5f;
    private const float DOT_ALPHA = 0.4f;

    // 아웃라인 펄스 설정
    private const float PULSE_MIN_ALPHA = 0.6f;
    private const float PULSE_MAX_ALPHA = 1.0f;
    private const float PULSE_SPEED = 2.5f;

    private List<GameObject> lineObjects = new List<GameObject>();
    private Coroutine pulseCoroutine;

    public void BuildLines()
    {
        ClearLines();

        float cellStep = cellSize + spacing;
        float totalWidth = gridWidth * cellSize + (gridWidth - 1) * spacing;
        float totalHeight = gridHeight * cellSize + (gridHeight - 1) * spacing;

        float left = -totalWidth / 2f;
        float bottom = -totalHeight / 2f;
        float right = left + totalWidth;
        float top = bottom + totalHeight;

        float glowThickness = lineThickness * GLOW_THICKNESS_MULTIPLIER;
        Color glowColor = new Color(lineColor.r, lineColor.g, lineColor.b, GLOW_ALPHA);

        // 외곽 테두리 (글로우 + 3배 두께 라인)
        float borderThickness = lineThickness * 3f;
        float borderGlowThickness = borderThickness * GLOW_THICKNESS_MULTIPLIER;
        Color borderGlowColor = new Color(lineColor.r, lineColor.g, lineColor.b, GLOW_ALPHA * 2f);

        // 상/하는 원래 폭, 좌/우만 borderThickness/2만큼 상하 연장하여 모서리 채움
        float bHalf = borderThickness / 2f;
        float bgHalf = borderGlowThickness / 2f;

        CreateRect("BorderGlow_Bottom", left, bottom - bgHalf, totalWidth, borderGlowThickness, borderGlowColor);
        CreateRect("BorderGlow_Top", left, top - bgHalf, totalWidth, borderGlowThickness, borderGlowColor);
        CreateRect("BorderGlow_Left", left - bgHalf, bottom - bgHalf, borderGlowThickness, totalHeight + borderGlowThickness, borderGlowColor);
        CreateRect("BorderGlow_Right", right - bgHalf, bottom - bgHalf, borderGlowThickness, totalHeight + borderGlowThickness, borderGlowColor);

        CreateRect("Border_Bottom", left, bottom - bHalf, totalWidth, borderThickness);
        CreateRect("Border_Top", left, top - bHalf, totalWidth, borderThickness);
        CreateRect("Border_Left", left - bHalf, bottom - bHalf, borderThickness, totalHeight + borderThickness);
        CreateRect("Border_Right", right - bHalf, bottom - bHalf, borderThickness, totalHeight + borderThickness);

        // 세로 내부 라인 (글로우 + 라인)
        for (int x = 1; x < gridWidth; x++)
        {
            float xPos = left + x * cellStep - spacing / 2f - lineThickness / 2f;
            float xGlow = left + x * cellStep - spacing / 2f - glowThickness / 2f;
            CreateRect($"VGlow_{x}", xGlow, bottom, glowThickness, totalHeight, glowColor);
            CreateRect($"VLine_{x}", xPos, bottom, lineThickness, totalHeight);
        }

        // 가로 내부 라인 (글로우 + 라인)
        for (int y = 1; y < gridHeight; y++)
        {
            float yPos = bottom + y * cellStep - spacing / 2f - lineThickness / 2f;
            float yGlow = bottom + y * cellStep - spacing / 2f - glowThickness / 2f;
            CreateRect($"HGlow_{y}", left, yGlow, totalWidth, glowThickness, glowColor);
            CreateRect($"HLine_{y}", left, yPos, totalWidth, lineThickness);
        }

        // 교차점 도트
        Color dotColor = new Color(lineColor.r + 0.15f, lineColor.g + 0.15f, lineColor.b + 0.15f, DOT_ALPHA);
        for (int x = 1; x < gridWidth; x++)
        {
            for (int y = 1; y < gridHeight; y++)
            {
                float dotX = left + x * cellStep - spacing / 2f - DOT_SIZE / 2f;
                float dotY = bottom + y * cellStep - spacing / 2f - DOT_SIZE / 2f;
                CreateRect($"Dot_{x}_{y}", dotX, dotY, DOT_SIZE, DOT_SIZE, dotColor);
            }
        }
    }

    private void CreateRect(string name, float x, float y, float width, float height)
    {
        CreateRect(name, x, y, width, height, lineColor);
    }

    private void CreateRect(string name, float x, float y, float width, float height, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);

        lineObjects.Add(obj);
    }

    private void ClearLines()
    {
        foreach (var obj in lineObjects)
        {
            if (obj != null) Destroy(obj);
        }
        lineObjects.Clear();
    }

    // ==================== Quad 하이라이트 API ====================

    private List<GameObject> outlineObjects = new List<GameObject>();
    private GameObject outlineContainer;

    /// <summary>
    /// 아웃라인 컨테이너를 초기화합니다. PlacedBlocks 위에 렌더링되도록 Canvas sortingOrder 설정.
    /// Grid의 부모에 생성하여 PlacedBlocks와 같은 레벨에 배치합니다.
    /// </summary>
    public void InitOutlineContainer(Transform gridParent, Vector2 gridPosition)
    {
        if (outlineContainer != null)
            DestroyImmediate(outlineContainer);

        outlineContainer = new GameObject("QuadOutlines");
        outlineContainer.transform.SetParent(gridParent, false);

        RectTransform containerRect = outlineContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = gridPosition;
        containerRect.localScale = Vector3.one;

        // PlacedBlocks 위에 렌더링
        Canvas canvas = outlineContainer.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        CanvasGroup cg = outlineContainer.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    /// <summary>
    /// Quad 전체를 감싸는 연속 아웃라인 4변을 생성합니다.
    /// </summary>
    public void AddQuadOutline(int minX, int minY, int maxX, int maxY, Color outlineColor)
    {
        if (outlineContainer == null) return;

        float cellStep = cellSize + spacing;
        float totalWidth = gridWidth * cellSize + (gridWidth - 1) * spacing;
        float totalHeight = gridHeight * cellSize + (gridHeight - 1) * spacing;

        float gridLeft = -totalWidth / 2f;
        float gridBottom = -totalHeight / 2f;
        float outlineThickness = 9f;
        float glowOutlineThickness = outlineThickness * 2.5f;

        // Quad 영역의 픽셀 좌표 (spacing 포함)
        float qLeft = gridLeft + minX * cellStep;
        float qBottom = gridBottom + minY * cellStep;
        float qRight = gridLeft + maxX * cellStep + cellSize;
        float qTop = gridBottom + maxY * cellStep + cellSize;
        float qWidth = qRight - qLeft;
        float qHeight = qTop - qBottom;

        // 글로우 레이어 (아웃라인 뒤, 더 두꺼운 반투명)
        Color glowColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.2f);
        outlineObjects.Add(CreateOutlineRect("QuadGlow_B",
            qLeft, qBottom - glowOutlineThickness / 2f, qWidth, glowOutlineThickness, glowColor));
        outlineObjects.Add(CreateOutlineRect("QuadGlow_T",
            qLeft, qTop - glowOutlineThickness / 2f, qWidth, glowOutlineThickness, glowColor));
        outlineObjects.Add(CreateOutlineRect("QuadGlow_L",
            qLeft - glowOutlineThickness / 2f, qBottom - glowOutlineThickness / 2f, glowOutlineThickness, qHeight + glowOutlineThickness, glowColor));
        outlineObjects.Add(CreateOutlineRect("QuadGlow_R",
            qRight - glowOutlineThickness / 2f, qBottom - glowOutlineThickness / 2f, glowOutlineThickness, qHeight + glowOutlineThickness, glowColor));

        // 메인 아웃라인
        outlineObjects.Add(CreateOutlineRect("QuadOutline_B",
            qLeft, qBottom - outlineThickness / 2f, qWidth, outlineThickness, outlineColor));
        outlineObjects.Add(CreateOutlineRect("QuadOutline_T",
            qLeft, qTop - outlineThickness / 2f, qWidth, outlineThickness, outlineColor));
        outlineObjects.Add(CreateOutlineRect("QuadOutline_L",
            qLeft - outlineThickness / 2f, qBottom - outlineThickness / 2f, outlineThickness, qHeight + outlineThickness, outlineColor));
        outlineObjects.Add(CreateOutlineRect("QuadOutline_R",
            qRight - outlineThickness / 2f, qBottom - outlineThickness / 2f, outlineThickness, qHeight + outlineThickness, outlineColor));
    }

    private GameObject CreateOutlineRect(string name, float x, float y, float width, float height, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(outlineContainer.transform, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);

        return obj;
    }

    public void ClearAllOutlines()
    {
        StopPulse();
        foreach (var obj in outlineObjects)
        {
            if (obj != null) Destroy(obj);
        }
        outlineObjects.Clear();
    }

    /// <summary>
    /// 아웃라인 펄스(호흡) 애니메이션 시작
    /// </summary>
    public void StartPulse()
    {
        StopPulse();
        if (outlineObjects.Count > 0)
        {
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }
    }

    public void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private IEnumerator PulseAnimation()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * PULSE_SPEED) + 1f) / 2f;
            float alpha = Mathf.Lerp(PULSE_MIN_ALPHA, PULSE_MAX_ALPHA, t);

            foreach (var obj in outlineObjects)
            {
                if (obj == null) continue;
                Image img = obj.GetComponent<Image>();
                if (img == null) continue;

                Color c = img.color;
                // 글로우는 비율 유지, 메인 아웃라인은 직접 적용
                if (obj.name.StartsWith("QuadGlow_"))
                    img.color = new Color(c.r, c.g, c.b, 0.2f * alpha);
                else if (obj.name.StartsWith("QuadOutline_"))
                    img.color = new Color(c.r, c.g, c.b, alpha);
            }

            yield return null;
        }
    }

    /// <summary>
    /// 쿼드 크기에 따른 아웃라인 색상 반환
    /// </summary>
    public static Color GetOutlineColorBySize(int quadSize)
    {
        if (quadSize >= 16)
            return new Color(1.00f, 0.18f, 0.47f); // 마젠타 (거대)
        if (quadSize >= 9)
            return new Color(1.00f, 0.72f, 0.00f); // 앰버 (대형)
        if (quadSize >= 6)
            return new Color(0.18f, 0.85f, 0.64f); // 틸 (중형)
        return Color.yellow; // 기본 (소형)
    }

    public void Refresh()
    {
        BuildLines();
    }
}
