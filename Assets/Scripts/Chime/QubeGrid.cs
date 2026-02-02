using UnityEngine;
using UnityEngine.UI;

public class QubeGrid : MonoBehaviour
{
    public const int WIDTH = 12;
    public const int HEIGHT = 9;

    private const float ANCHOR_CENTER = 0.5f;
    private const string PLACED_BLOCKS_CONTAINER_NAME = "PlacedBlocks";
    private const string PLACED_CELL_NAME_FORMAT = "PlacedCell_{0}_{1}";

    public GameObject cellPrefab;
    public float cellSize = 80f;
    public float spacing = 5f;

    private QubeCell[,] cells = new QubeCell[WIDTH, HEIGHT];
    private GameObject placedBlocksContainer;
    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        SetupGridLayout();
        CreatePlacedBlocksContainer();
        CreateGrid();
    }

    private void SetupGridLayout()
    {
        // GridLayoutGroup 설정 또는 생성
        gridLayout = GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = gameObject.AddComponent<GridLayoutGroup>();
        }

        // Grid Layout 설정
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = WIDTH;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.LowerLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        // RectTransform 설정
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            // 그리드 전체 크기 계산
            float totalWidth = WIDTH * cellSize + (WIDTH - 1) * spacing;
            float totalHeight = HEIGHT * cellSize + (HEIGHT - 1) * spacing;

            rect.sizeDelta = new Vector2(totalWidth, totalHeight);
            rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
            rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
            rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        }
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
        // GridLayoutGroup은 왼쪽 아래에서 시작하여 오른쪽으로 진행
        // 따라서 y를 0부터 시작하여 위로 올라가면서 생성
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                cells[x, y] = CreateCell(x, y);
            }
        }
    }

    private QubeCell CreateCell(int x, int y)
    {
        GameObject cellObj = Instantiate(cellPrefab, transform);
        QubeCell cell = cellObj.GetComponent<QubeCell>();

        cell.coordinates = new Vector2Int(x, y);
        cell.SetOccupied(false, Color.clear);
        cellObj.name = $"Cell_{x}_{y}";

        // GridLayoutGroup이 자동으로 배치하므로 수동 위치 설정 불필요
        // 단, 크기는 명시적으로 설정
        RectTransform cellRect = cellObj.GetComponent<RectTransform>();
        if (cellRect != null)
        {
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);
        }

        return cell;
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
    }

    public Transform GetPlacedBlocksContainer()
    {
        return placedBlocksContainer.transform;
    }

    public void ClearGrid()
    {
        ClearAllCells();
        ClearPlacedBlockVisuals();
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
}
