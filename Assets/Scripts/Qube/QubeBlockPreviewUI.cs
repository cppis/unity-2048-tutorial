using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class QubeBlockPreviewUI : MonoBehaviour
{
    private const float SLOT_SIZE = 120f;
    private const float SLOT_SPACING = 10f;
    private const float MINI_CELL_SIZE = 22f;
    private const float MINI_CELL_SPACING = 2f;
    private const float FIRST_SLOT_SCALE = 1.3f;
    private const float MINI_BEVEL_THICKNESS = 1.5f;

    public System.Action<int> OnSlotDragStarted;

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

            // 배경 (raycast 활성화로 터치/클릭 감지)
            Image bg = slotObj.AddComponent<Image>();
            bg.sprite = CreateRoundedRectSprite(64, 64, 8);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.10f, 0.13f, 0.16f, 0.85f);
            bg.raycastTarget = true;

            // 첫 번째 슬롯에 글로우 보더 추가
            if (i == 0)
            {
                AddSlotGlowBorder(slotObj, rect);
            }

            // 드래그 감지용 컴포넌트
            SlotDragHandler dragHandler = slotObj.AddComponent<SlotDragHandler>();
            dragHandler.slotIndex = i;
            dragHandler.previewUI = this;

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

        // 남은 슬롯 비우기
        for (int i = count; i < slotContainers.Count; i++)
        {
            ClearSlot(slotContainers[i]);
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

            // 미니셀 베벨
            QubeBevel.AddBevel(cellObj, cellSize, MINI_BEVEL_THICKNESS);
        }
    }

    private Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color fill = Color.white;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 모서리 라운딩 체크
                bool inCorner = false;
                int cx = 0, cy = 0;

                if (x < radius && y < radius) { cx = radius; cy = radius; inCorner = true; }
                else if (x >= width - radius && y < radius) { cx = width - radius; cy = radius; inCorner = true; }
                else if (x < radius && y >= height - radius) { cx = radius; cy = height - radius; inCorner = true; }
                else if (x >= width - radius && y >= height - radius) { cx = width - radius; cy = height - radius; inCorner = true; }

                if (inCorner)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();

        // 9-slice용 border 설정
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private void AddSlotGlowBorder(GameObject slotObj, RectTransform slotRect)
    {
        GameObject glowObj = new GameObject("GlowBorder");
        glowObj.transform.SetParent(slotObj.transform, false);

        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-2f, -2f);
        glowRect.offsetMax = new Vector2(2f, 2f);

        Image glowImg = glowObj.AddComponent<Image>();
        glowImg.sprite = CreateRoundedRectSprite(64, 64, 10);
        glowImg.type = Image.Type.Sliced;
        glowImg.color = new Color(0.00f, 0.82f, 1.00f, 0.25f); // 시안 글로우
        glowImg.raycastTarget = false;

        // 메인 슬롯 뒤에 배치
        glowObj.transform.SetAsFirstSibling();
    }

    internal void NotifyDragStarted(int slotIndex)
    {
        OnSlotDragStarted?.Invoke(slotIndex);
    }

    public void SetSlotsInteractable(bool interactable)
    {
        for (int i = 0; i < slotContainers.Count; i++)
        {
            Image bg = slotContainers[i].GetComponent<Image>();
            if (bg != null)
                bg.raycastTarget = interactable && (i == 0); // 첫 번째 슬롯만 활성화
        }
    }
}

public class SlotDragHandler : MonoBehaviour, IPointerDownHandler
{
    public int slotIndex;
    public QubeBlockPreviewUI previewUI;

    public void OnPointerDown(PointerEventData eventData)
    {
        previewUI.NotifyDragStarted(slotIndex);
    }
}
