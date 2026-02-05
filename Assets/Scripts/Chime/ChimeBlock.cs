using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChimeBlock : MonoBehaviour
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

    public ChimeBlockShape shape;
    public Vector2Int position;
    public ChimeGrid grid;

    private Vector2Int[] currentCells;
    private List<GameObject> cellObjects = new List<GameObject>();
    private int rotation = 0;

    public void Initialize(ChimeBlockShape blockShape, ChimeGrid chimeGrid)
    {
        shape = blockShape;
        grid = chimeGrid;
        currentCells = (Vector2Int[])shape.cells.Clone();

        position = CalculateSpawnPosition();
        InitializeRectTransform();
        CreateVisuals();
        UpdatePlacementVisualFeedback(); // 초기 시각적 피드백 설정
    }

    private Vector2Int CalculateSpawnPosition()
    {
        return new Vector2Int(ChimeGrid.WIDTH / 2 - 1, SPAWN_Y_POSITION);
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
        UpdateOutlineBorders(); // 외곽선만 표시
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

        // 배경은 투명
        Image image = cellObj.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        SetupCellRectTransform(cellObj.GetComponent<RectTransform>());

        // 4개의 아웃라인 border 생성
        CreateCellBorders(cellObj, grid.cellSize);

        return cellObj;
    }

    private void CreateCellBorders(GameObject cellObj, float cellSize)
    {
        float borderThickness = 4f;

        // 상단
        CreateBorder(cellObj, "TopBorder", cellSize, borderThickness, 0, cellSize / 2);
        // 하단
        CreateBorder(cellObj, "BottomBorder", cellSize, borderThickness, 0, -cellSize / 2);
        // 좌측
        CreateBorder(cellObj, "LeftBorder", borderThickness, cellSize, -cellSize / 2, 0);
        // 우측
        CreateBorder(cellObj, "RightBorder", borderThickness, cellSize, cellSize / 2, 0);
    }

    private void CreateBorder(GameObject parent, string name, float width, float height, float xPos, float yPos)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(parent.transform, false);

        RectTransform rect = borderObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchoredPosition = new Vector2(xPos, yPos);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = Color.white;
        borderImage.raycastTarget = false;

        // Canvas 추가하여 렌더링 순서를 block보다 위에 표시
        Canvas canvas = borderObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        // 초기에는 비활성화 (UpdateOutlineBorders에서 활성화)
        borderObj.SetActive(false);
    }

    private void UpdateOutlineBorders()
    {
        // currentCells를 HashSet으로 변환 (빠른 검색)
        HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(currentCells);

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int cell = currentCells[i];
            GameObject cellObj = cellObjects[i];

            // 각 방향에 인접한 셀이 있는지 확인
            bool hasTop = cellSet.Contains(cell + Vector2Int.up);
            bool hasBottom = cellSet.Contains(cell + Vector2Int.down);
            bool hasLeft = cellSet.Contains(cell + Vector2Int.left);
            bool hasRight = cellSet.Contains(cell + Vector2Int.right);

            // 인접 셀이 없는 방향만 border 활성화 (외곽선)
            Transform topBorder = cellObj.transform.Find("TopBorder");
            Transform bottomBorder = cellObj.transform.Find("BottomBorder");
            Transform leftBorder = cellObj.transform.Find("LeftBorder");
            Transform rightBorder = cellObj.transform.Find("RightBorder");

            if (topBorder != null) topBorder.gameObject.SetActive(!hasTop);
            if (bottomBorder != null) bottomBorder.gameObject.SetActive(!hasBottom);
            if (leftBorder != null) leftBorder.gameObject.SetActive(!hasLeft);
            if (rightBorder != null) rightBorder.gameObject.SetActive(!hasRight);
        }
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
        // GridLayoutGroup 기반 위치 계산
        // 그리드 중앙이 (0,0)이고, 왼쪽 아래가 시작점
        float gridWidth = ChimeGrid.WIDTH * grid.cellSize + (ChimeGrid.WIDTH - 1) * grid.spacing;
        float gridHeight = ChimeGrid.HEIGHT * grid.cellSize + (ChimeGrid.HEIGHT - 1) * grid.spacing;

        // 왼쪽 아래 모서리 위치
        float leftX = -gridWidth / 2f;
        float bottomY = -gridHeight / 2f;

        // 각 셀의 중심 위치
        float xPos = leftX + gridPos.x * cellStep + grid.cellSize / 2f;
        float yPos = bottomY + gridPos.y * cellStep + grid.cellSize / 2f;

        return new Vector2(xPos, yPos);
    }

    public bool CanMove(Vector2Int direction)
    {
        Vector2Int newPos = position + direction;

        // 새 위치가 완전히 유효하면 이동 허용
        if (IsPositionValid(newPos, currentCells, checkOccupancy: false))
        {
            return true;
        }

        // 현재 위치가 그리드 밖에 있고, 새 위치가 더 많은 셀이 그리드 내에 있으면 이동 허용
        // (회전으로 인해 그리드 밖으로 나간 경우 안쪽으로 돌아올 수 있도록)
        int currentCellsInGrid = CountCellsInGrid(position);
        int newCellsInGrid = CountCellsInGrid(newPos);

        return newCellsInGrid > currentCellsInGrid;
    }

    private int CountCellsInGrid(Vector2Int pos)
    {
        int count = 0;
        foreach (var cell in currentCells)
        {
            Vector2Int checkPos = pos + cell;
            if (grid.IsValidPosition(checkPos))
            {
                count++;
            }
        }
        return count;
    }

    public bool CanPlace()
    {
        return IsPositionValid(position, currentCells, checkOccupancy: true);
    }

    public bool CanPlaceAnywhere()
    {
        // 그리드 전체를 검색해서 블록을 배치할 수 있는 위치가 하나라도 있는지 확인
        for (int y = 0; y < ChimeGrid.HEIGHT; y++)
        {
            for (int x = 0; x < ChimeGrid.WIDTH; x++)
            {
                Vector2Int testPos = new Vector2Int(x, y);
                if (IsPositionValid(testPos, currentCells, checkOccupancy: true))
                {
                    return true;
                }
            }
        }
        return false;
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
        // 먼저 wall kick으로 유효한 위치 찾기 시도
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

        // wall kick 실패해도 회전은 항상 허용 (360도 이상 회전 가능)
        rotation = (rotation + (clockwise ? 1 : 3)) % ROTATION_COUNT;
        return true;
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

            // 배치 시 border(아웃라인) 제거
            RemoveCellBorders(cellObj);

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

    private void RemoveCellBorders(GameObject cellObj)
    {
        // 자식 오브젝트들 (border) 삭제
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in cellObj.transform)
        {
            toDestroy.Add(child.gameObject);
        }
        foreach (var obj in toDestroy)
        {
            Destroy(obj);
        }
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
        bool canPlace = CanPlace();
        Color outlineColor = canPlace ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        foreach (var cellObj in cellObjects)
        {
            // 각 셀의 border 색상 변경
            UpdateCellBorderColors(cellObj, outlineColor);
        }
    }

    private void UpdateCellBorderColors(GameObject cellObj, Color color)
    {
        // 자식 오브젝트들 (TopBorder, BottomBorder, LeftBorder, RightBorder) 순회
        foreach (Transform child in cellObj.transform)
        {
            Image borderImage = child.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = color;
            }
        }
    }
}
