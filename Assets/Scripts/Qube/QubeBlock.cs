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

    public QubeBlockShape shape;
    public Vector2Int position;
    public QubeGrid grid;

    private const float GHOST_ALPHA = 0.3f;
    private const float GHOST_OUTLINE_WIDTH = 3f;

    private Vector2Int[] currentCells;
    private List<GameObject> cellObjects = new List<GameObject>();
    private List<GameObject> ghostObjects = new List<GameObject>();
    private bool ghostEnabled = true;

    public void Initialize(QubeBlockShape blockShape, QubeGrid qubeGrid)
    {
        shape = blockShape;
        grid = qubeGrid;
        currentCells = (Vector2Int[])shape.cells.Clone();

        position = CalculateSpawnPosition();
        InitializeRectTransform();
        CreateVisuals();
        CreateGhostVisuals();
        UpdateGhostVisuals();
        UpdatePlacementVisualFeedback();
    }

    public void Initialize(QubeBlockShape blockShape, Vector2Int[] preRotatedCells, QubeGrid qubeGrid)
    {
        shape = blockShape;
        grid = qubeGrid;
        currentCells = (Vector2Int[])preRotatedCells.Clone();

        position = CalculateSpawnPosition();
        InitializeRectTransform();
        CreateVisuals();
        CreateGhostVisuals();
        UpdateGhostVisuals();
        UpdatePlacementVisualFeedback();
    }

    private Vector2Int spawnPosition = new Vector2Int(-100, -100);

    public void SetSpawnPosition(Vector2Int pos)
    {
        spawnPosition = pos;
    }

    private Vector2Int CalculateSpawnPosition()
    {
        return spawnPosition;
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
        float fullSize = grid.cellSize + grid.spacing;
        rect.sizeDelta = new Vector2(fullSize, fullSize);
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
            cellObjects[i].SetActive(true);
        }
    }

    /// <summary>
    /// 화면 좌표 기준으로 블록 위치 설정 (그리드 밖에서 드래그 중일 때 사용)
    /// </summary>
    public void SetScreenPosition(Vector2 screenPos, RectTransform canvasRect, Camera uiCamera)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCamera, out localPoint);

        // 블록의 부모 기준 위치 설정
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;

        // 셀들을 블록 중심 기준으로 재배치
        float cellStep = grid.cellSize + grid.spacing;
        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int cell = currentCells[i];
            RectTransform cellRect = cellObjects[i].GetComponent<RectTransform>();
            cellRect.anchoredPosition = new Vector2(cell.x * cellStep, cell.y * cellStep);
            cellObjects[i].SetActive(true);
        }
    }

    private Vector2 CalculateCellPosition(Vector2Int gridPos, float cellStep)
    {
        // GridLayoutGroup 기반 위치 계산
        // 그리드 중앙이 (0,0)이고, 왼쪽 아래가 시작점
        float gridWidth = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
        float gridHeight = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;

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
        return IsPositionValid(newPos, currentCells, checkOccupancy: false);
    }

    public bool CanPlace()
    {
        return IsPositionValid(position, currentCells, checkOccupancy: true);
    }

    public bool CanPlaceAnywhere()
    {
        // 그리드 전체를 검색해서 블록을 배치할 수 있는 위치가 하나라도 있는지 확인
        for (int y = 0; y < QubeGrid.HEIGHT; y++)
        {
            for (int x = 0; x < QubeGrid.WIDTH; x++)
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
            UpdateGhostVisuals();
            UpdatePlacementVisualFeedback();
        }
    }

    /// <summary>
    /// 지정 위치 근처에서 배치 가능한 가장 가까운 위치를 탐색합니다.
    /// 찾으면 true + outPos, 없으면 false.
    /// </summary>
    public bool FindNearestPlaceablePosition(Vector2Int center, out Vector2Int outPos)
    {
        outPos = center;

        // 현재 위치가 배치 가능하면 그대로
        if (IsPositionValid(center, currentCells, checkOccupancy: true))
        {
            outPos = center;
            return true;
        }

        // BFS로 가까운 위치부터 탐색 (최대 거리 4)
        for (int dist = 1; dist <= 4; dist++)
        {
            for (int dx = -dist; dx <= dist; dx++)
            {
                for (int dy = -dist; dy <= dist; dy++)
                {
                    if (Mathf.Abs(dx) != dist && Mathf.Abs(dy) != dist) continue; // 테두리만
                    Vector2Int testPos = center + new Vector2Int(dx, dy);
                    if (IsPositionValid(testPos, currentCells, checkOccupancy: true))
                    {
                        outPos = testPos;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        position = newPosition;

        // SetScreenPosition이 anchoredPosition을 바꿨을 수 있으므로 리셋
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        UpdateVisuals();
        UpdateGhostVisuals();
        UpdatePlacementVisualFeedback();
    }

    /// <summary>
    /// 셀 배열에 시계방향 회전을 지정 횟수만큼 적용합니다.
    /// 블록 큐에서 사전 회전된 블록 생성 시 사용됩니다.
    /// </summary>
    public static Vector2Int[] ApplyRotation(Vector2Int[] cells, int rotationCount)
    {
        Vector2Int[] result = (Vector2Int[])cells.Clone();
        rotationCount = ((rotationCount % ROTATION_COUNT) + ROTATION_COUNT) % ROTATION_COUNT;

        for (int r = 0; r < rotationCount; r++)
        {
            for (int i = 0; i < result.Length; i++)
            {
                Vector2Int cell = result[i];
                result[i] = new Vector2Int(cell.y, -cell.x); // 시계방향 90°
            }
        }

        return result;
    }

    public void SetGhostEnabled(bool enabled)
    {
        ghostEnabled = enabled;
        UpdateGhostVisuals();
    }

    private void CreateGhostVisuals()
    {
        // 셀 집합 (외곽 변 판별용)
        HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(currentCells);
        Color ghostColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        float cellStep = grid.cellSize + grid.spacing;

        // 컨테이너
        GameObject container = new GameObject("GhostOutline");
        container.transform.SetParent(grid.transform.parent);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        containerRect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        containerRect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.localScale = Vector3.one;

        // 각 셀의 4방향을 검사하여 외곽 변에만 보더 생성
        foreach (var cell in currentCells)
        {
            // 상: 위 셀이 블록에 없으면 상변 그리기
            if (!cellSet.Contains(cell + Vector2Int.up))
                CreateGhostBorder(container.transform, cell, Vector2Int.up, cellStep, ghostColor);
            // 하
            if (!cellSet.Contains(cell + Vector2Int.down))
                CreateGhostBorder(container.transform, cell, Vector2Int.down, cellStep, ghostColor);
            // 좌
            if (!cellSet.Contains(cell + Vector2Int.left))
                CreateGhostBorder(container.transform, cell, Vector2Int.left, cellStep, ghostColor);
            // 우
            if (!cellSet.Contains(cell + Vector2Int.right))
                CreateGhostBorder(container.transform, cell, Vector2Int.right, cellStep, ghostColor);
        }

        ghostObjects.Add(container);
    }

    private void CreateGhostBorder(Transform parent, Vector2Int cell, Vector2Int dir, float cellStep, Color color)
    {
        float x = cell.x * cellStep;
        float y = cell.y * cellStep;
        float bx, by, bw, bh;

        if (dir == Vector2Int.up)
        {
            bx = x; by = y + grid.cellSize - GHOST_OUTLINE_WIDTH / 2f;
            bw = grid.cellSize; bh = GHOST_OUTLINE_WIDTH;
        }
        else if (dir == Vector2Int.down)
        {
            bx = x; by = y - GHOST_OUTLINE_WIDTH / 2f;
            bw = grid.cellSize; bh = GHOST_OUTLINE_WIDTH;
        }
        else if (dir == Vector2Int.left)
        {
            bx = x - GHOST_OUTLINE_WIDTH / 2f; by = y;
            bw = GHOST_OUTLINE_WIDTH; bh = grid.cellSize;
        }
        else // right
        {
            bx = x + grid.cellSize - GHOST_OUTLINE_WIDTH / 2f; by = y;
            bw = GHOST_OUTLINE_WIDTH; bh = grid.cellSize;
        }

        GameObject obj = new GameObject("GhostEdge");
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(bx, by);
        rect.sizeDelta = new Vector2(bw, bh);
    }

    private void UpdateGhostVisuals()
    {
        if (ghostObjects.Count == 0) return;

        bool canPlace = CanPlace();
        bool showGhost = ghostEnabled && canPlace;

        GameObject ghostObj = ghostObjects[0];
        ghostObj.SetActive(showGhost);

        if (showGhost)
        {
            float cellStep = grid.cellSize + grid.spacing;
            float gw = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
            float gh = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;

            // 컨테이너 위치 = 블록 position(0,0) 셀의 그리드 좌상 기준 위치
            float xPos = -gw / 2f + position.x * cellStep;
            float yPos = -gh / 2f + position.y * cellStep;

            RectTransform rect = ghostObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(xPos, yPos);
        }
    }

    public void DestroyGhostVisuals()
    {
        foreach (var obj in ghostObjects)
        {
            if (obj != null) Destroy(obj);
        }
        ghostObjects.Clear();
    }

    private void OnDestroy()
    {
        DestroyGhostVisuals();
    }

    public void Place()
    {
        DestroyGhostVisuals();
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

        // 글로벌 좌표 셋 (채움 조각 판정용)
        HashSet<Vector2Int> globalPosSet = new HashSet<Vector2Int>();
        for (int i = 0; i < currentCells.Length; i++)
        {
            globalPosSet.Add(position + currentCells[i]);
        }

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int globalPos = position + currentCells[i];

            grid.SetCellOccupied(globalPos, true, placedColor, false, blockId);

            GameObject cellObj = cellObjects[i];

            // 배치 시 셀 크기를 cellSize로 축소 (간격으로 블록 구분)
            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
            cellRect.sizeDelta = new Vector2(grid.cellSize, grid.cellSize);

            // 배치된 블록의 색상을 변경
            Image cellImage = cellObj.GetComponent<Image>();
            if (cellImage != null)
            {
                cellImage.color = placedColor;
            }

            cellObj.name = $"PlacedCell_{globalPos.x}_{globalPos.y}";
            cellObj.transform.SetParent(placedContainer, worldPositionStays: true);
        }

        // 같은 블록 내 인접 셀 사이에 채움 조각 생성 (간격 메우기)
        float cellStep = grid.cellSize + grid.spacing;
        foreach (Vector2Int pos in globalPosSet)
        {
            // 오른쪽 인접 확인
            Vector2Int right = pos + Vector2Int.right;
            if (globalPosSet.Contains(right))
            {
                CreateFillPiece(pos, right, true, cellStep, placedColor, placedContainer);
            }
            // 위쪽 인접 확인
            Vector2Int up = pos + Vector2Int.up;
            if (globalPosSet.Contains(up))
            {
                CreateFillPiece(pos, up, false, cellStep, placedColor, placedContainer);
            }
        }

        cellObjects.Clear();
        Destroy(gameObject);
    }

    private void CreateFillPiece(Vector2Int from, Vector2Int to, bool horizontal, float cellStep, Color color, Transform container)
    {
        GameObject fill = new GameObject($"Fill_{from.x}_{from.y}_{to.x}_{to.y}");
        fill.transform.SetParent(container, false);

        Image img = fill.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rect = fill.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.anchorMax = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);
        rect.pivot = new Vector2(ANCHOR_CENTER, ANCHOR_CENTER);

        // 두 셀의 중간 위치 계산
        Vector2 posA = CalculateCellPosition(from, cellStep);
        Vector2 posB = CalculateCellPosition(to, cellStep);
        rect.anchoredPosition = (posA + posB) / 2f;

        if (horizontal)
        {
            rect.sizeDelta = new Vector2(grid.spacing, grid.cellSize);
        }
        else
        {
            rect.sizeDelta = new Vector2(grid.cellSize, grid.spacing);
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
