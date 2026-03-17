using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QubeBlockPreviewUI : MonoBehaviour
{
    private const float SLOT_SIZE = 120f;
    private const float SLOT_SPACING = 10f;
    private const float MINI_CELL_SIZE = 22f;
    private const float MINI_CELL_SPACING = 2f;
    private const float FIRST_SLOT_SCALE = 1.3f;

    private List<RectTransform> slotContainers = new List<RectTransform>();

    private void Awake()
    {
        SetupLayout();
        CreateSlots();
    }

    private void SetupLayout()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = SLOT_SPACING;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void CreateSlots()
    {
        for (int i = 0; i < QubeBlockQueue.QUEUE_SIZE; i++)
        {
            GameObject slotObj = new GameObject($"PreviewSlot_{i}");
            slotObj.transform.SetParent(transform, false);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(SLOT_SIZE, SLOT_SIZE);

            // 배경
            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.15f, 0.8f);
            bg.raycastTarget = false;

            slotContainers.Add(rect);
        }
    }

    public void UpdatePreview(QubeBlockEntry[] entries)
    {
        int count = Mathf.Min(entries.Length, slotContainers.Count);

        for (int i = 0; i < count; i++)
        {
            ClearSlot(slotContainers[i]);
            RenderBlockInSlot(slotContainers[i], entries[i], i == 0);
        }
    }

    private void ClearSlot(RectTransform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            Destroy(slot.GetChild(i).gameObject);
        }
    }

    private void RenderBlockInSlot(RectTransform slot, QubeBlockEntry entry, bool isFirst)
    {
        float scale = isFirst ? FIRST_SLOT_SCALE : 1f;
        float cellSize = MINI_CELL_SIZE * scale;
        float cellSpacing = MINI_CELL_SPACING * scale;
        float cellStep = cellSize + cellSpacing;

        // 블록 셀들의 바운드 계산
        Vector2Int min = entry.rotatedCells[0];
        Vector2Int max = entry.rotatedCells[0];
        foreach (var cell in entry.rotatedCells)
        {
            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }

        Vector2 blockSize = new Vector2(
            (max.x - min.x + 1) * cellStep - cellSpacing,
            (max.y - min.y + 1) * cellStep - cellSpacing
        );

        // 각 셀 렌더링 (슬롯 중앙 기준)
        foreach (var cell in entry.rotatedCells)
        {
            GameObject cellObj = new GameObject("MiniCell");
            cellObj.transform.SetParent(slot, false);

            RectTransform cellRect = cellObj.AddComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);

            float xPos = (cell.x - min.x) * cellStep - blockSize.x / 2f + cellSize / 2f;
            float yPos = (cell.y - min.y) * cellStep - blockSize.y / 2f + cellSize / 2f;
            cellRect.anchoredPosition = new Vector2(xPos, yPos);

            Image img = cellObj.AddComponent<Image>();
            img.color = entry.shape.blockColor;
            img.raycastTarget = false;
        }
    }
}
