using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QubeBlock : MonoBehaviour
{
    private const float ANCHOR_CENTER = 0.5f;
    private const int SPAWN_Y_POSITION = 1;
    private const int ROTATION_COUNT = 4;

    // HSV 색상 조절 상수
    private const float PLACED_SATURATION = 0.6f; // 배치 후 채도 감소

    private static int nextBlockId = 0; // Static counter for unique block IDs

    private static readonly Vector2Int[] WALL_KICK_OFFSETS = new Vector2Int[]
    {
        new Vector2Int(0, 0),   // 원래 위치
        new Vector2Int(-1, 0),  // 왼쪽
        new Vector2Int(1, 0),   // 오른쪽
        new Vector2Int(0, 1),   // 위
        new Vector2Int(0, -1),  // 아래
        new Vector2Int(-1, 1),  // 왼쪽 위
        new Vector2Int(1, 1),   // 오른쪽 위
        new Vector2Int(-1, -1), // 왼쪽 아래
        new Vector2Int(1, -1)   // 오른쪽 아래
    };

    public QubeBlockShape shape;
    public Vector2Int position;
    public QubeGrid grid;

    private Vector2Int[] currentCells;
    private List<GameObject> cellObjects = new List<GameObject>();
    private int rotation = 0;

    public void Initialize(QubeBlockShape blockShape, QubeGrid qubeGrid)
    {
        shape = blockShape;
        grid = qubeGrid;
        currentCells = (Vector2Int[])shape.cells.Clone();

        position = CalculateSpawnPosition();
        InitializeRectTransform();
        CreateVisuals();
        UpdatePlacementVisualFeedback(); // 초기 시각적 피드백 설정
    }

    private Vector2Int CalculateSpawnPosition()
    {
        return new Vector2Int(QubeGrid.WIDTH / 2 - 1, SPAWN_Y_POSITION);
    }

    private void InitializeRectTransform()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void CreateVisuals()
    {
        ClearExistingVisuals();

        foreach (var cell in currentCells)
        {
            GameObject cellObj = CreateCellVisual();
            cellObjects.Add(cellObj);
        }

        UpdateVisuals();
    }

    private void ClearExistingVisuals()
    {
        foreach (var obj in cellObjects)
        {
            Destroy(obj);
        }
        cellObjects.Clear();
    }

    private GameObject CreateCellVisual()
    {
        GameObject cellObj = new GameObject("BlockCell");
        cellObj.transform.SetParent(transform);

        Image image = cellObj.AddComponent<Image>();
        image.color = shape.blockColor;
        image.raycastTarget = false;

        SetupCellRectTransform(cellObj.GetComponent<RectTransform>());

        return cellObj;
    }

    private void SetupCellRectTransform(RectTransform rect)
    {
        rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.sizeDelta = new Vector2(grid.cellSize, grid.cellSize);
        rect.localScale = Vector3.one;
    }

    private void UpdateVisuals()
    {
        float cellStep = grid.cellSize + grid.spacing;

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int globalPos = position + currentCells[i];
            RectTransform rect = cellObjects[i].GetComponent<RectTransform>();
            rect.anchoredPosition = CalculateCellPosition(globalPos, cellStep);
        }
    }

    private Vector2 CalculateCellPosition(Vector2Int gridPos, float cellStep)
    {
        float xPos = (gridPos.x - QubeGrid.WIDTH / 2f + ANCHOR_CENTER) * cellStep;
        float yPos = (gridPos.y - QubeGrid.HEIGHT / 2f + ANCHOR_CENTER) * cellStep;
        return new Vector2(xPos, yPos);
    }

    public bool CanMove(Vector2Int direction)
    {
        Vector2Int newPos = position + direction;
        return IsPositionValid(newPos, currentCells, checkOccupancy: false);
    }

    public bool CanPlace()
    {
        return IsPositionValid(position, currentCells, checkOccupancy: true);
    }

    private bool IsPositionValid(Vector2Int pos, Vector2Int[] cells, bool checkOccupancy)
    {
        foreach (var cell in cells)
        {
            Vector2Int checkPos = pos + cell;

            if (!grid.IsValidPosition(checkPos))
                return false;

            if (checkOccupancy && grid.IsCellOccupied(checkPos))
                return false;
        }

        return true;
    }

    public void Move(Vector2Int direction)
    {
        if (CanMove(direction))
        {
            position += direction;
            UpdateVisuals();
            UpdatePlacementVisualFeedback(); // 이동 후 피드백 업데이트
        }
    }

    public void Rotate(bool clockwise = true)
    {
        Vector2Int[] rotatedCells = CalculateRotatedCells(clockwise);

        if (TryRotateWithWallKick(rotatedCells, clockwise))
        {
            currentCells = rotatedCells;
            CreateVisuals();
            UpdatePlacementVisualFeedback(); // 회전 후 피드백 업데이트
        }
    }

    private Vector2Int[] CalculateRotatedCells(bool clockwise)
    {
        Vector2Int[] rotatedCells = new Vector2Int[currentCells.Length];

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int cell = currentCells[i];
            rotatedCells[i] = clockwise
                ? new Vector2Int(cell.y, -cell.x)   // 시계방향
                : new Vector2Int(-cell.y, cell.x);  // 반시계방향
        }

        return rotatedCells;
    }

    private bool TryRotateWithWallKick(Vector2Int[] rotatedCells, bool clockwise)
    {
        foreach (var offset in WALL_KICK_OFFSETS)
        {
            Vector2Int testPos = position + offset;

            if (IsPositionValid(testPos, rotatedCells, checkOccupancy: false))
            {
                position = testPos;
                rotation = (rotation + (clockwise ? 1 : 3)) % ROTATION_COUNT;
                return true;
            }
        }

        return false;
    }

    public void Place()
    {
        Transform placedContainer = grid.GetPlacedBlocksContainer();
        int blockId = nextBlockId++; // Generate unique block ID

        // 배치 시 색상 계산 (HSV 기반 - 채도만 감소, 밝기는 유지)
        Color baseColor = shape.blockColor;
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);

        s = Mathf.Clamp01(s * PLACED_SATURATION); // 채도만 감소 (40% 감소)
        // v는 원본 유지

        Color placedColor = Color.HSVToRGB(h, s, v);
        placedColor.a = 1f;

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int globalPos = position + currentCells[i];

            grid.SetCellOccupied(globalPos, true, placedColor, false, blockId);

            GameObject cellObj = cellObjects[i];

            // 배치된 블록의 색상을 변경
            Image cellImage = cellObj.GetComponent<Image>();
            if (cellImage != null)
            {
                cellImage.color = placedColor;
            }

            cellObj.name = $"PlacedCell_{globalPos.x}_{globalPos.y}";
            cellObj.transform.SetParent(placedContainer, worldPositionStays: true);
        }

        cellObjects.Clear();
        Destroy(gameObject);
    }

    public Vector2Int[] GetGlobalPositions()
    {
        Vector2Int[] globalPositions = new Vector2Int[currentCells.Length];
        for (int i = 0; i < currentCells.Length; i++)
        {
            globalPositions[i] = position + currentCells[i];
        }
        return globalPositions;
    }

    public void UpdatePlacementVisualFeedback()
    {
        // 드래그 중에는 원본 색상 그대로 사용
        Color visualColor = shape.blockColor;

        foreach (var cellObj in cellObjects)
        {
            Image image = cellObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = visualColor;
            }
        }
    }
}
