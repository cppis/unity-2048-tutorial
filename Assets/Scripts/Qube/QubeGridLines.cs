using UnityEngine;
using UnityEngine.UI;
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

    private List<GameObject> lineObjects = new List<GameObject>();

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

        // 외곽 테두리
        CreateRect("Border_Bottom", left, bottom - lineThickness / 2f, totalWidth, lineThickness);
        CreateRect("Border_Top", left, top - lineThickness / 2f, totalWidth, lineThickness);
        CreateRect("Border_Left", left - lineThickness / 2f, bottom, lineThickness, totalHeight);
        CreateRect("Border_Right", right - lineThickness / 2f, bottom, lineThickness, totalHeight);

        // 세로 내부 라인
        for (int x = 1; x < gridWidth; x++)
        {
            float xPos = left + x * cellStep - spacing / 2f - lineThickness / 2f;
            CreateRect($"VLine_{x}", xPos, bottom, lineThickness, totalHeight);
        }

        // 가로 내부 라인
        for (int y = 1; y < gridHeight; y++)
        {
            float yPos = bottom + y * cellStep - spacing / 2f - lineThickness / 2f;
            CreateRect($"HLine_{y}", left, yPos, totalWidth, lineThickness);
        }
    }

    private void CreateRect(string name, float x, float y, float width, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        Image img = obj.AddComponent<Image>();
        img.color = lineColor;
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

        // Quad 영역의 픽셀 좌표 (spacing 포함)
        float qLeft = gridLeft + minX * cellStep;
        float qBottom = gridBottom + minY * cellStep;
        float qRight = gridLeft + maxX * cellStep + cellSize;
        float qTop = gridBottom + maxY * cellStep + cellSize;
        float qWidth = qRight - qLeft;
        float qHeight = qTop - qBottom;

        // 4변 연속 라인
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
        foreach (var obj in outlineObjects)
        {
            if (obj != null) Destroy(obj);
        }
        outlineObjects.Clear();
    }

    public void Refresh()
    {
        BuildLines();
    }
}
