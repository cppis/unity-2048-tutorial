using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class Block : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float DEFAULT_DECELERATION = 10f;
    private const float DEFAULT_MIN_SPEED = 0.5f;
    private const float DEFAULT_FRICTION = 2.5f;
    private const float DEFAULT_SMOOTH_TIME = 0.1f;
    private const float MIN_DRAG_DISTANCE = 0.2f;
    private const float BLOCK_SIZE_OFFSET = 0.02f; // 2 pixels at 100 PPU
    private const float SELECTION_SCALE = 1.1f;
    private const float SELECTION_ALPHA = 0.3f;

    [Header("Block Properties")]
    public Color blockColor = Color.white;
    public int gridX;
    public int gridY;
    public int width = 1;
    public int height = 1;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer selectionIndicator;

    [Header("References")]
    public BlockGrid grid;

    [Header("Movement Settings")]
    public float deceleration = DEFAULT_DECELERATION;
    public float minSpeed = DEFAULT_MIN_SPEED;
    [Range(0f, 5f)] public float frictionPerCell = DEFAULT_FRICTION;
    public float smoothTime = DEFAULT_SMOOTH_TIME;

    private bool isSelected = false;
    private Vector3 dragOffset;
    private int originalGridX;
    private int originalGridY;
    private Vector3 dragStartPos;
    private Vector3 lastDragPos;
    private float dragStartTime;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private Vector2Int lastDragGrid; // Track last grid position during drag
    private bool isDraggingHorizontal; // Track drag direction

    // Track drag session start position for this block
    [System.NonSerialized]
    public int dragSessionStartX = -1;
    [System.NonSerialized]
    public int dragSessionStartY = -1;

    // Event triggered when adjacent blocks with matching colors are found
    public event Action<Block> OnAdjacentMatchFound;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (selectionIndicator == null && spriteRenderer != null)
        {
            GameObject indicatorObj = new GameObject("SelectionIndicator");
            indicatorObj.transform.SetParent(transform);
            indicatorObj.transform.localPosition = Vector3.zero;
            indicatorObj.transform.localScale = Vector3.one * SELECTION_SCALE;
            selectionIndicator = indicatorObj.AddComponent<SpriteRenderer>();
            selectionIndicator.sprite = spriteRenderer.sprite;
            selectionIndicator.color = new Color(1, 1, 1, SELECTION_ALPHA);
            selectionIndicator.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        SetSelected(false);
        targetPosition = transform.position;
    }

    private void Update()
    {
        // Smooth movement towards target position (only when not dragging)
        if (!isSelected && Vector3.Distance(transform.position, targetPosition) > 0.001f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else if (!isSelected)
        {
            transform.position = targetPosition;
            velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Initializes the block with position, color, and size
    /// </summary>
    public void Initialize(int x, int y, Color color, BlockGrid blockGrid, int w = 1, int h = 1)
    {
        gridX = x;
        gridY = y;
        blockColor = color;
        grid = blockGrid;
        width = w;
        height = h;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = blockColor;
        }

        UpdatePosition(true); // Immediate positioning on initialization
        UpdateScale();

        // Initialize target position after setting initial position
        targetPosition = transform.position;
    }

    private void UpdateScale()
    {
        if (grid != null)
        {
            float totalWidth = width * grid.cellSize + (width - 1) * grid.cellSpacing;
            float totalHeight = height * grid.cellSize + (height - 1) * grid.cellSpacing;

            float scaleX = totalWidth - BLOCK_SIZE_OFFSET;
            float scaleY = totalHeight - BLOCK_SIZE_OFFSET;

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider != null) collider.size = Vector2.one;
        }
    }

    /// <summary>
    /// Updates the block's grid position and visual position
    /// </summary>
    public void SetGridPosition(int x, int y, bool immediate = false)
    {
        gridX = x;
        gridY = y;
        UpdatePosition(immediate);
    }

    private void UpdatePosition(bool immediate = false)
    {
        if (grid != null)
        {
            // Position at center of the block's occupied cells
            Vector3 basePos = grid.GetWorldPosition(gridX, gridY);
            float totalSize = grid.cellSize + grid.cellSpacing;
            float offsetX = (width - 1) * totalSize * 0.5f;
            float offsetY = (height - 1) * totalSize * 0.5f;
            Vector3 newPosition = basePos + new Vector3(offsetX, offsetY, 0);

            if (immediate)
            {
                // Immediate positioning (for initialization)
                transform.position = newPosition;
                targetPosition = newPosition;
                velocity = Vector3.zero;
            }
            else
            {
                // Smooth positioning (for animation)
                targetPosition = newPosition;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(selected);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isMoving) return;

        if (grid != null)
        {
            grid.SelectBlock(this);
        }
        SetSelected(true);
        originalGridX = gridX;
        originalGridY = gridY;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (grid == null || isMoving) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        dragOffset = transform.position - worldPos;

        dragStartPos = worldPos;
        lastDragPos = worldPos;
        dragStartTime = Time.time;
        lastDragGrid = new Vector2Int(gridX, gridY); // Initialize last drag grid
        isDraggingHorizontal = false; // Will be determined in first OnDrag
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (grid == null || isMoving) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        lastDragPos = worldPos;

        // Calculate drag delta from original position (when drag started)
        Vector3 dragDelta = worldPos - dragStartPos;

        if (lastDragGrid.x == originalGridX && lastDragGrid.y == originalGridY)
        {
            float dragDistance = dragDelta.magnitude;
            if (dragDistance >= MIN_DRAG_DISTANCE)
            {
                isDraggingHorizontal = Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y);
            }
            else
            {
                return;
            }
        }

        // Use the determined direction consistently
        Vector2Int currentDragGrid;
        if (isDraggingHorizontal)
        {
            // Horizontal movement only
            Vector3 horizontalPos = new Vector3(worldPos.x + dragOffset.x, transform.position.y, 0);
            currentDragGrid = grid.GetGridPosition(horizontalPos);
            currentDragGrid.x = Mathf.Clamp(currentDragGrid.x, 0, grid.width - width);
            currentDragGrid.y = originalGridY; // Keep Y position fixed
        }
        else
        {
            // Vertical movement only
            Vector3 verticalPos = new Vector3(transform.position.x, worldPos.y + dragOffset.y, 0);
            currentDragGrid = grid.GetGridPosition(verticalPos);
            currentDragGrid.x = originalGridX; // Keep X position fixed
            currentDragGrid.y = Mathf.Clamp(currentDragGrid.y, 0, grid.height - height);
        }

        // Check if we moved to a different grid cell
        if (currentDragGrid.x != lastDragGrid.x || currentDragGrid.y != lastDragGrid.y)
        {
            bool allowMove = false;

            // Check movement restrictions based on move mode
            if (grid.moveMode == BlockMoveMode.FreeDrag)
            {
                // FreeDrag: no restrictions
                allowMove = true;
            }
            else if (grid.moveMode == BlockMoveMode.OneCell)
            {
                // OneCell: limit to one cell movement from original position
                int moveDistance = isDraggingHorizontal
                    ? Mathf.Abs(currentDragGrid.x - originalGridX)
                    : Mathf.Abs(currentDragGrid.y - originalGridY);

                allowMove = moveDistance <= 1;
            }
            else if (grid.moveMode == BlockMoveMode.SlideToEnd)
            {
                // SlideToEnd: allow one cell during drag (actual slide happens on OnEndDrag)
                int moveDistance = isDraggingHorizontal
                    ? Mathf.Abs(currentDragGrid.x - originalGridX)
                    : Mathf.Abs(currentDragGrid.y - originalGridY);

                allowMove = moveDistance <= 1;
            }

            if (allowMove)
            {
                // Try to move to the new grid position (pushing blocks if needed)
                // IMPORTANT: Use lastDragGrid (current position) as fromX/fromY
                // Pass originalGridX/Y so BlockGrid knows the drag start position
                bool canMove = grid.TryMoveBlockWithPushingPublic(this, lastDragGrid.x, lastDragGrid.y, currentDragGrid.x, currentDragGrid.y, originalGridX, originalGridY);

                if (canMove)
                {
                    // Successfully moved - update position
                    // TryMoveBlockWithPushingPublic already updated grid data and called SetGridPosition
                    gridX = currentDragGrid.x;
                    gridY = currentDragGrid.y;
                    lastDragGrid = currentDragGrid;
                }
                else
                {
                    // Cannot move - stay at current grid position
                    currentDragGrid = lastDragGrid;
                }
            }
            else
            {
                // Movement not allowed by current mode
                currentDragGrid = lastDragGrid;
            }
        }

        // Visual position follows the grid position smoothly
        // This prevents the block from visually "passing through" other blocks
        Vector3 gridWorldPos = grid.GetWorldPosition(gridX, gridY);
        float totalSize = grid.cellSize + grid.cellSpacing;
        float offsetX = (width - 1) * totalSize * 0.5f;
        float offsetY = (height - 1) * totalSize * 0.5f;
        targetPosition = gridWorldPos + new Vector3(offsetX, offsetY, 0);

        // Immediately update position for responsive feel during drag
        transform.position = targetPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (grid == null || isMoving) return;

        SetSelected(false);

        // Calculate actual grid distance moved during drag (not mouse movement distance)
        int gridDistanceMoved;
        int dx = 0, dy = 0;

        if (isDraggingHorizontal)
        {
            // Horizontal: calculate distance from original position
            gridDistanceMoved = gridX - originalGridX;
            dx = gridDistanceMoved > 0 ? 1 : (gridDistanceMoved < 0 ? -1 : 0);
        }
        else
        {
            // Vertical: calculate distance from original position
            gridDistanceMoved = gridY - originalGridY;
            dy = gridDistanceMoved > 0 ? 1 : (gridDistanceMoved < 0 ? -1 : 0);
        }

        int cellsAlreadyMoved = Mathf.Abs(gridDistanceMoved);

        // SlideToEnd mode: continue sliding to the end in the determined direction
        if (grid.moveMode == BlockMoveMode.SlideToEnd && (dx != 0 || dy != 0))
        {
            SlideToEnd(dx, dy);
            grid.ResetDragSession();
            return;
        }

        // OneCell and FreeDrag modes: handle normally
        if (cellsAlreadyMoved > 0)
        {
            SetGridPosition(gridX, gridY, false);

            // Check entire grid for merges
            bool merged = grid.CheckAndMergeAllPossibleRectangles();

            // Spawn a new random block only if no merge occurred
            if (!merged) grid.SpawnRandomBlocks(1);

            grid.ResetDragSession();
            return;
        }

        // If didn't move during drag, try to move one cell based on drag direction
        if (dx != 0 || dy != 0)
        {
            int targetX = originalGridX + dx;
            int targetY = originalGridY + dy;

            // Check bounds
            if (targetX >= 0 && targetX + width <= grid.width &&
                targetY >= 0 && targetY + height <= grid.height)
            {
                int currentGridX = gridX;
                int currentGridY = gridY;
                bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetX, targetY, originalGridX, originalGridY);

                if (canMove)
                {
                    gridX = targetX;
                    gridY = targetY;

                    // Check entire grid for merges
                    bool merged = grid.CheckAndMergeAllPossibleRectangles();

                    // Spawn a new random block only if no merge occurred
                    if (!merged) grid.SpawnRandomBlocks(1);
                }
                else
                {
                    SetGridPosition(gridX, gridY, false);
                }
            }
            else
            {
                SetGridPosition(gridX, gridY, false);
            }
        }
        else
        {
            SetGridPosition(gridX, gridY, false);
        }

        grid.ResetDragSession();
    }

    private void SlideToEnd(int dx, int dy)
    {
        // Use a set to track which blocks have already been slid to avoid infinite recursion
        System.Collections.Generic.HashSet<Block> alreadySlid = new System.Collections.Generic.HashSet<Block>();

        bool moved = SlideBlockRecursive(this, dx, dy, alreadySlid);

        if (moved)
        {
            // Check entire grid for merges
            bool merged = grid.CheckAndMergeAllPossibleRectangles();

            // Spawn a new random block only if no merge occurred
            if (!merged) grid.SpawnRandomBlocks(1);
        }
    }

    private bool SlideBlockRecursive(Block block, int dx, int dy, System.Collections.Generic.HashSet<Block> alreadySlid)
    {
        if (alreadySlid.Contains(block))
        {
            return false; // Already slid this block
        }

        alreadySlid.Add(block);
        bool blockMoved = false;

        // Continue sliding until blocked
        while (true)
        {
            int targetX = block.gridX + dx;
            int targetY = block.gridY + dy;

            // Check bounds
            if (targetX < 0 || targetX + block.width > grid.width ||
                targetY < 0 || targetY + block.height > grid.height)
            {
                break;
            }

            // Check which blocks will be pushed before moving
            var pushedBlocks = grid.GetBlocksToPushVisual(block, block.gridX, block.gridY, targetX, targetY);

            // If there are blocks to push, recursively slide them first
            if (pushedBlocks != null && pushedBlocks.Count > 0)
            {
                System.Collections.Generic.HashSet<Block> pushedBlockSet = new System.Collections.Generic.HashSet<Block>();
                foreach (var moveInfo in pushedBlocks)
                {
                    if (moveInfo.block != block)
                    {
                        pushedBlockSet.Add(moveInfo.block);
                    }
                }

                // Recursively slide all pushed blocks first
                foreach (Block pushedBlock in pushedBlockSet)
                {
                    SlideBlockRecursive(pushedBlock, dx, dy, alreadySlid);
                }
            }

            // Now try to move this block
            int currentGridX = block.gridX;
            int currentGridY = block.gridY;
            bool canMove = grid.TryMoveBlockWithPushingPublic(block, currentGridX, currentGridY, targetX, targetY,
                block.dragSessionStartX >= 0 ? block.dragSessionStartX : originalGridX,
                block.dragSessionStartY >= 0 ? block.dragSessionStartY : originalGridY);

            if (canMove)
            {
                block.gridX = targetX;
                block.gridY = targetY;
                blockMoved = true;
            }
            else
            {
                break;
            }
        }

        // Update visual position for this block
        if (blockMoved)
        {
            block.SetGridPosition(block.gridX, block.gridY, false);
        }

        return blockMoved;
    }

    private void SnapToNearestGrid()
    {
        Vector2Int targetGrid = grid.GetGridPosition(transform.position);
        targetGrid.x = Mathf.Clamp(targetGrid.x, 0, grid.width - width);
        targetGrid.y = Mathf.Clamp(targetGrid.y, 0, grid.height - height);

        if (targetGrid.x != gridX || targetGrid.y != gridY)
        {
            int currentGridX = gridX;
            int currentGridY = gridY;
            bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetGrid.x, targetGrid.y, originalGridX, originalGridY);

            if (canMove)
            {
                gridX = targetGrid.x;
                gridY = targetGrid.y;
                grid.CheckAndMergeAllPossibleRectangles();
            }
            else
            {
                gridX = originalGridX;
                gridY = originalGridY;
                SetGridPosition(originalGridX, originalGridY, false);
            }
        }
        else
        {
            SetGridPosition(gridX, gridY, false);
        }
    }

    private IEnumerator SlideWithDeceleration(int dx, int dy, float initialSpeed, int maxCells)
    {
        isMoving = true;
        float currentSpeed = initialSpeed;
        int cellsMoved = 0;

        while (currentSpeed > minSpeed && cellsMoved < maxCells)
        {
            int targetX = gridX + dx;
            int targetY = gridY + dy;

            if (targetX < 0 || targetX + width > grid.width ||
                targetY < 0 || targetY + height > grid.height)
            {
                break;
            }

            int currentGridX = gridX;
            int currentGridY = gridY;
            bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetX, targetY, originalGridX, originalGridY);

            if (!canMove) break;

            gridX = targetX;
            gridY = targetY;
            cellsMoved++;

            float moveTime = 1f / Mathf.Max(currentSpeed, 2f);
            moveTime = Mathf.Clamp(moveTime, 0.05f, 0.3f);

            float elapsed = 0;
            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentSpeed -= deceleration * moveTime;
            currentSpeed -= frictionPerCell;
            currentSpeed = Mathf.Max(currentSpeed, 0);
        }

        SetGridPosition(gridX, gridY, false);
        yield return new WaitForSeconds(smoothTime * 2f);

        bool merged = grid.CheckAndMergeAllPossibleRectangles();

        if (cellsMoved > 0 && !merged) grid.SpawnRandomBlocks(1);

        isMoving = false;
    }

    public void TriggerAdjacentMatch()
    {
        OnAdjacentMatchFound?.Invoke(this);
    }
}
