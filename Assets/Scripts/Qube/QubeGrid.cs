using UnityEngine;

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

    private void Awake()
    {
        CreatePlacedBlocksContainer();
        CreateGrid();
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
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
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

        SetupCellTransform(cellObj.GetComponent<RectTransform>(), x, y);

        return cell;
    }

    private void SetupCellTransform(RectTransform cellRect, int x, int y)
    {
        cellRect.sizeDelta = new Vector2(cellSize, cellSize);
        cellRect.anchoredPosition = CalculateCellPosition(x, y);
    }

    private Vector2 CalculateCellPosition(int x, int y)
    {
        float cellStep = cellSize + spacing;
        float xPos = (x - WIDTH / 2f + ANCHOR_CENTER) * cellStep;
        float yPos = (y - HEIGHT / 2f + ANCHOR_CENTER) * cellStep;
        return new Vector2(xPos, yPos);
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
