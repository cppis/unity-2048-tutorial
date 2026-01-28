using UnityEngine;

public class QubeGrid : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 6;

    public GameObject cellPrefab;
    public float cellSize = 80f;
    public float spacing = 5f;

    private QubeCell[,] cells = new QubeCell[WIDTH, HEIGHT];

    private void Awake()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, transform);
                QubeCell cell = cellObj.GetComponent<QubeCell>();

                cell.coordinates = new Vector2Int(x, y);
                cell.SetOccupied(false, Color.clear);

                // 셀 위치 설정
                RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);

                float xPos = (x - WIDTH / 2f + 0.5f) * (cellSize + spacing);
                float yPos = (y - HEIGHT / 2f + 0.5f) * (cellSize + spacing);
                cellRect.anchoredPosition = new Vector2(xPos, yPos);

                cells[x, y] = cell;

                // 첫 셀만 로그 출력
                if (x == 0 && y == 0)
                {
                    Debug.Log($"Grid cell (0,0) - sizeDelta: {cellRect.sizeDelta}, lossyScale: {cellRect.lossyScale}, rect: {cellRect.rect}");
                }
            }
        }

        Debug.Log($"Grid created: {WIDTH}x{HEIGHT}, all cells initialized as empty");
    }

    public QubeCell GetCell(int x, int y)
    {
        if (x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT)
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
        return coords.x >= 0 && coords.x < WIDTH && coords.y >= 0 && coords.y < HEIGHT;
    }

    public bool IsCellOccupied(Vector2Int coords)
    {
        QubeCell cell = GetCell(coords);
        return cell != null && cell.isOccupied;
    }

    public void SetCellOccupied(Vector2Int coords, bool occupied, Color color)
    {
        QubeCell cell = GetCell(coords);
        if (cell != null)
        {
            cell.SetOccupied(occupied, color);
            Debug.Log($"Cell {coords} set to {(occupied ? "occupied" : "empty")}");
        }
    }

    // 모든 셀을 초기화
    public void ClearGrid()
    {
        Debug.Log("Clearing all grid cells");
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                cells[x, y].SetOccupied(false, Color.clear);
            }
        }
    }
}
