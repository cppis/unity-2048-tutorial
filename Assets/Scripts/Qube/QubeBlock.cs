using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QubeBlock : MonoBehaviour
{
    public QubeBlockShape shape;
    public Vector2Int position;
    public QubeGrid grid;

    private Vector2Int[] currentCells;
    private List<GameObject> cellObjects = new List<GameObject>();
    private int rotation = 0; // 0, 1, 2, 3 (0도, 90도, 180도, 270도)

    public void Initialize(QubeBlockShape blockShape, QubeGrid qubeGrid)
    {
        shape = blockShape;
        grid = qubeGrid;
        currentCells = (Vector2Int[])shape.cells.Clone();

        // 블록을 그리드 중앙 하단에서 생성 (y=1로 변경)
        position = new Vector2Int(QubeGrid.WIDTH / 2 - 1, 1);

        // RectTransform 초기화 (Grid와 같은 좌표계 사용)
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Debug.Log($"Block initialized: {shape.blockName} at {position}, cells: {string.Join(", ", currentCells)}");
        Debug.Log($"Block RectTransform - localScale: {rectTransform.localScale}, lossyScale: {rectTransform.lossyScale}");

        CreateVisuals();
    }

    private void CreateVisuals()
    {
        // 기존 시각 요소 제거
        foreach (var obj in cellObjects)
        {
            Destroy(obj);
        }
        cellObjects.Clear();

        Debug.Log($"Creating block visuals - Grid cellSize: {grid.cellSize}, spacing: {grid.spacing}");

        // 새로운 시각 요소 생성
        foreach (var cell in currentCells)
        {
            GameObject cellObj = new GameObject("BlockCell");
            cellObj.transform.SetParent(transform);

            Image image = cellObj.AddComponent<Image>();
            image.color = shape.blockColor;
            image.raycastTarget = false; // 불필요한 raycast 비활성화

            RectTransform rect = cellObj.GetComponent<RectTransform>();

            // 앵커를 중앙으로 설정
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // 훨씬 작게 설정하여 테스트 (50x50)
            float blockCellSize = 50f;
            rect.sizeDelta = new Vector2(blockCellSize, blockCellSize);

            cellObjects.Add(cellObj);

            Debug.Log($"BlockCell - sizeDelta: {rect.sizeDelta}, lossyScale: {cellObj.transform.lossyScale}, rect size: {rect.rect.size}");
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float cellStep = grid.cellSize + grid.spacing;

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int globalPos = position + currentCells[i];
            RectTransform rect = cellObjects[i].GetComponent<RectTransform>();

            float xPos = (globalPos.x - QubeGrid.WIDTH / 2f + 0.5f) * cellStep;
            float yPos = (globalPos.y - QubeGrid.HEIGHT / 2f + 0.5f) * cellStep;
            rect.anchoredPosition = new Vector2(xPos, yPos);
        }
    }

    public bool CanMove(Vector2Int direction)
    {
        Vector2Int newPos = position + direction;

        // 이동은 그리드 범위 내에서만 체크 (점유 여부는 무시)
        foreach (var cell in currentCells)
        {
            Vector2Int checkPos = newPos + cell;
            if (!grid.IsValidPosition(checkPos))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanPlace()
    {
        // 배치는 모든 셀이 비어있어야 함
        foreach (var cell in currentCells)
        {
            Vector2Int checkPos = position + cell;
            if (!grid.IsValidPosition(checkPos) || grid.IsCellOccupied(checkPos))
            {
                return false;
            }
        }

        return true;
    }

    public void Move(Vector2Int direction)
    {
        if (CanMove(direction))
        {
            position += direction;
            UpdateVisuals();
            Debug.Log($"Block moved to {position}, canPlace: {CanPlace()}");
        }
        else
        {
            Debug.Log($"Cannot move from {position} in direction {direction} - out of bounds");
        }
    }

    public void Rotate(bool clockwise = true)
    {
        Vector2Int[] rotatedCells = new Vector2Int[currentCells.Length];

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector2Int cell = currentCells[i];
            if (clockwise)
            {
                // 시계방향 90도 회전: (x, y) -> (y, -x)
                rotatedCells[i] = new Vector2Int(cell.y, -cell.x);
            }
            else
            {
                // 반시계방향 90도 회전: (x, y) -> (-y, x)
                rotatedCells[i] = new Vector2Int(-cell.y, cell.x);
            }
        }

        // 회전 후 위치가 유효한지 확인 (점유 여부는 무시)
        bool canRotate = true;
        foreach (var cell in rotatedCells)
        {
            Vector2Int checkPos = position + cell;
            if (!grid.IsValidPosition(checkPos))
            {
                canRotate = false;
                break;
            }
        }

        if (canRotate)
        {
            currentCells = rotatedCells;
            rotation = (rotation + (clockwise ? 1 : 3)) % 4;
            CreateVisuals();
        }
    }

    public void Place()
    {
        foreach (var cell in currentCells)
        {
            Vector2Int globalPos = position + cell;
            grid.SetCellOccupied(globalPos, true, shape.blockColor);
        }

        // 블록 제거
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
}
