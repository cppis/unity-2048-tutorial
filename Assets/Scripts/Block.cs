using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class Block : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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
    [Tooltip("Deceleration rate (higher = stops faster)")]
    public float deceleration = 10f;

    [Tooltip("Minimum speed to keep moving")]
    public float minSpeed = 0.5f;

    [Tooltip("Friction per cell (speed lost when crossing each cell boundary)")]
    [Range(0f, 5f)]
    public float frictionPerCell = 2.5f;

    [Tooltip("Smooth time for animation")]
    public float smoothTime = 0.1f;

    [Tooltip("Minimum drag distance to determine direction (in Unity units)")]
    public float minDragDistanceForDirection = 0.2f;

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
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Create selection indicator if not assigned
        if (selectionIndicator == null && spriteRenderer != null)
        {
            GameObject indicatorObj = new GameObject("SelectionIndicator");
            indicatorObj.transform.SetParent(transform);
            indicatorObj.transform.localPosition = Vector3.zero;
            indicatorObj.transform.localScale = Vector3.one * 1.1f;
            selectionIndicator = indicatorObj.AddComponent<SpriteRenderer>();
            selectionIndicator.sprite = spriteRenderer.sprite;
            selectionIndicator.color = new Color(1, 1, 1, 0.3f);
            selectionIndicator.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        SetSelected(false);

        // Initialize target position
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
            // Calculate total size with spacing
            float totalWidth = width * grid.cellSize + (width - 1) * grid.cellSpacing;
            float totalHeight = height * grid.cellSize + (height - 1) * grid.cellSpacing;

            // Make block 2 pixels smaller than cell size (in Unity units, assuming 100 pixels per unit)
            float pixelSize = 0.02f; // 2 pixels at 100 PPU (pixels per unit)
            float scaleX = totalWidth - pixelSize;
            float scaleY = totalHeight - pixelSize;

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Update collider size
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = Vector2.one;
            }
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

        // Determine primary direction (horizontal or vertical only) - only once when moved enough distance
        if (lastDragGrid.x == originalGridX && lastDragGrid.y == originalGridY)
        {
            // Only determine direction when moved enough distance
            float dragDistance = dragDelta.magnitude;
            if (dragDistance >= minDragDistanceForDirection)
            {
                // Moved enough - determine direction
                isDraggingHorizontal = Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y);
            }
            else
            {
                // Not moved enough - don't process movement yet
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
            // Limit to one cell movement from original position
            int moveDistance = isDraggingHorizontal
                ? Mathf.Abs(currentDragGrid.x - originalGridX)
                : Mathf.Abs(currentDragGrid.y - originalGridY);

            // Only allow movement if we haven't exceeded one cell from original position
            if (moveDistance <= 1)
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
                // Already moved one cell - don't allow further movement
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

        // Limit to one cell movement only
        // If already moved during drag, no additional movement
        if (cellsAlreadyMoved > 0)
        {
            // Already moved - just finalize position
            SetGridPosition(gridX, gridY, false);
            bool merged = grid.CheckAdjacentBlocks(this);

            // Spawn a new random block only if no merge occurred
            if (!merged)
            {
                grid.SpawnRandomBlocks(1);
            }

            grid.ResetDragSession(); // Reset drag session for all blocks
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
                // Try to move one cell
                int currentGridX = gridX;
                int currentGridY = gridY;
                bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetX, targetY, originalGridX, originalGridY);

                if (canMove)
                {
                    gridX = targetX;
                    gridY = targetY;
                    bool merged = grid.CheckAdjacentBlocks(this);

                    // Spawn a new random block only if no merge occurred
                    if (!merged)
                    {
                        grid.SpawnRandomBlocks(1);
                    }
                }
                else
                {
                    // Cannot move - stay at current position
                    SetGridPosition(gridX, gridY, false);
                }
            }
            else
            {
                // Out of bounds - stay at current position
                SetGridPosition(gridX, gridY, false);
            }
        }
        else
        {
            // No movement - just finalize position
            SetGridPosition(gridX, gridY, false);
        }

        // Reset drag session for all blocks when drag ends
        grid.ResetDragSession();
    }

    private void SnapToNearestGrid()
    {
        Vector2Int targetGrid = grid.GetGridPosition(transform.position);
        targetGrid.x = Mathf.Clamp(targetGrid.x, 0, grid.width - width);
        targetGrid.y = Mathf.Clamp(targetGrid.y, 0, grid.height - height);

        // If target is different from current position, try to move (with pushing)
        if (targetGrid.x != gridX || targetGrid.y != gridY)
        {
            // IMPORTANT: Save current position before calling TryMoveBlockWithPushingPublic
            int currentGridX = gridX;
            int currentGridY = gridY;
            bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetGrid.x, targetGrid.y, originalGridX, originalGridY);

            if (canMove)
            {
                // TryMoveBlockWithPushingPublic already updated grid data and called SetGridPosition
                gridX = targetGrid.x;
                gridY = targetGrid.y;
                grid.CheckAdjacentBlocks(this);
            }
            else
            {
                // Cannot move - return to original position
                gridX = originalGridX;
                gridY = originalGridY;
                SetGridPosition(originalGridX, originalGridY, false); // Smooth animation
            }
        }
        else
        {
            // Same position - just snap
            SetGridPosition(gridX, gridY, false); // Smooth animation
        }
    }

    private IEnumerator SlideWithDeceleration(int dx, int dy, float initialSpeed, int maxCells)
    {
        isMoving = true;
        float currentSpeed = initialSpeed;
        int cellsMoved = 0;

        while (currentSpeed > minSpeed && cellsMoved < maxCells)
        {
            // Try to move one cell in the direction
            int targetX = gridX + dx;
            int targetY = gridY + dy;

            // Check bounds
            if (targetX < 0 || targetX + width > grid.width ||
                targetY < 0 || targetY + height > grid.height)
            {
                break;
            }

            // Try to move block (pushing adjacent blocks if needed)
            // IMPORTANT: Use current gridX/gridY as fromX/fromY before they're updated
            int currentGridX = gridX;
            int currentGridY = gridY;
            bool canMove = grid.TryMoveBlockWithPushingPublic(this, currentGridX, currentGridY, targetX, targetY, originalGridX, originalGridY);

            if (!canMove)
            {
                // Cannot move (blocked by immovable blocks or boundary)
                break;
            }

            // Update block position (grid data and visual position already updated by TryMoveBlockWithPushingPublic)
            gridX = targetX;
            gridY = targetY;
            cellsMoved++;

            // Calculate wait time based on current speed
            float moveTime = 1f / Mathf.Max(currentSpeed, 2f);
            moveTime = Mathf.Clamp(moveTime, 0.05f, 0.3f); // Clamp for reasonable animation speed

            // Wait for animation to complete
            float elapsed = 0;
            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Apply deceleration (time-based)
            currentSpeed -= deceleration * moveTime;

            // Apply friction (per-cell friction when crossing cell boundary)
            currentSpeed -= frictionPerCell;
            currentSpeed = Mathf.Max(currentSpeed, 0); // Prevent negative speed
        }

        // Final position snap
        SetGridPosition(gridX, gridY, false);

        // Wait for final animation to complete
        yield return new WaitForSeconds(smoothTime * 2f);

        bool merged = grid.CheckAdjacentBlocks(this);

        // Spawn a new random block only if moved and no merge occurred
        if (cellsMoved > 0 && !merged)
        {
            grid.SpawnRandomBlocks(1);
        }

        isMoving = false;
    }

    public void TriggerAdjacentMatch()
    {
        OnAdjacentMatchFound?.Invoke(this);
    }
}
