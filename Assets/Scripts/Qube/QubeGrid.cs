using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QubeGrid : MonoBehaviour
{
    public static int WIDTH = 10;
    public static int HEIGHT = 8;

    private const float ANCHOR_CENTER = 0.5f;
    private const string PLACED_BLOCKS_CONTAINER_NAME = "PlacedBlocks";
    private const string PLACED_CELL_NAME_FORMAT = "PlacedCell_{0}_{1}";

    public float cellSize = 80f;
    public float spacing = 5f;

    [Header("Grid Lines")]
    public float lineThickness = 2f;
    public Color lineColor = new Color(0.3f, 0.35f, 0.38f, 1f);

    private QubeCell[,] cells;
    private GameObject placedBlocksContainer;
    private GameObject gridLinesObject;
    private QubeGridLines gridLines;

    public void SetSize(int width, int height)
    {
        WIDTH = width;
        HEIGHT = height;
    }

    private void Awake()
    {
        cells = new QubeCell[WIDTH, HEIGHT];
        SetupRectTransform();
        SetupGridLines();
        CreatePlacedBlocksContainer();
        CreateGrid();
    }

    private void SetupRectTransform()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            float totalWidth = WIDTH * cellSize + (WIDTH - 1) * spacing;
            float totalHeight = HEIGHT * cellSize + (HEIGHT - 1) * spacing;

            rect.sizeDelta = new Vector2(totalWidth, totalHeight);
            rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
            rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
            rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        }
    }

    private void SetupGridLines()
    {
        // 기존 GridLayoutGroup 제거
        GridLayoutGroup glg = GetComponent<GridLayoutGroup>();
        if (glg != null)
            DestroyImmediate(glg);

        // 기존 라인 오브젝트 제거
        if (gridLinesObject != null)
            DestroyImmediate(gridLinesObject);

        // 별도 자식 오브젝트에 그리드 라인 생성 (기존 Image와 충돌 방지)
        gridLinesObject = new GameObject("GridLines");
        gridLinesObject.transform.SetParent(transform, false);

        RectTransform lineRect = gridLinesObject.AddComponent<RectTransform>();
        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.one;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;

        // 맨 아래 레이어에 배치
        gridLinesObject.transform.SetAsFirstSibling();

        gridLines = gridLinesObject.AddComponent<QubeGridLines>();
        gridLines.gridWidth = WIDTH;
        gridLines.gridHeight = HEIGHT;
        gridLines.cellSize = cellSize;
        gridLines.spacing = spacing;
        gridLines.lineThickness = lineThickness;
        gridLines.lineColor = lineColor;

        gridLines.Refresh();

        // 아웃라인 컨테이너 초기화 (PlacedBlocks 위에 렌더링)
        Vector2 gridPos = GetComponent<RectTransform>().anchoredPosition;
        gridLines.InitOutlineContainer(transform.parent, gridPos);
    }

    private void CreatePlacedBlocksContainer()
    {
        placedBlocksContainer = new GameObject(PLACED_BLOCKS_CONTAINER_NAME);
        placedBlocksContainer.transform.SetParent(transform.parent);

        RectTransform rect = placedBlocksContainer.AddComponent<RectTransform>();
        SetupCenteredRectTransform(rect);
        rect.anchoredPosition = GetComponent<RectTransform>().anchoredPosition;
    }

    private void SetupCenteredRectTransform(RectTransform rect)
    {
        rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.localScale = Vector3.one;
    }

    private void CreateGrid()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                cells[x, y] = new QubeCell(new Vector2Int(x, y));
            }
        }
    }

    public QubeCell GetCell(int x, int y)
    {
        if (IsValidPosition(x, y))
        {
            return cells[x, y];
        }
        return null;
    }

    public QubeCell GetCell(Vector2Int coords)
    {
        return GetCell(coords.x, coords.y);
    }

    public QubeGridLines GetGridLines()
    {
        return gridLines;
    }

    public bool IsValidPosition(Vector2Int coords)
    {
        return IsValidPosition(coords.x, coords.y);
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT;
    }

    public bool IsCellOccupied(Vector2Int coords)
    {
        QubeCell cell = GetCell(coords);
        return cell != null && cell.isOccupied;
    }

    public void SetCellOccupied(Vector2Int coords, bool occupied, Color color, bool wasCleared = false, int blockId = -1)
    {
        QubeCell cell = GetCell(coords);
        if (cell != null)
        {
            cell.SetOccupied(occupied, color, wasCleared, blockId);
        }

        if (!occupied && wasCleared)
        {
            RemovePlacedBlockVisual(coords);
        }
    }

    private void RemovePlacedBlockVisual(Vector2Int coords)
    {
        string targetName = string.Format(PLACED_CELL_NAME_FORMAT, coords.x, coords.y);
        Transform child = placedBlocksContainer.transform.Find(targetName);

        if (child != null)
        {
            Destroy(child.gameObject);
        }

        RemoveFillPieces(coords);
    }

    public Transform GetPlacedBlocksContainer()
    {
        return placedBlocksContainer.transform;
    }

    public void ClearGrid()
    {
        ClearAllCells();
        ClearPlacedBlockVisuals();
        if (gridLines != null)
            gridLines.ClearAllOutlines();
    }

    public void RebuildGrid()
    {
        ClearPlacedBlockVisuals();

        cells = new QubeCell[WIDTH, HEIGHT];
        SetupRectTransform();
        SetupGridLines();
        CreateGrid();
    }

    private void ClearAllCells()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                cells[x, y].SetOccupied(false, Color.clear);
            }
        }
    }

    private void ClearPlacedBlockVisuals()
    {
        foreach (Transform child in placedBlocksContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void RemoveFillPieces(Vector2Int coords)
    {
        string pattern = $"_{coords.x}_{coords.y}";
        for (int i = placedBlocksContainer.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = placedBlocksContainer.transform.GetChild(i);
            if (child.name.StartsWith("Fill_") && child.name.Contains(pattern))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
