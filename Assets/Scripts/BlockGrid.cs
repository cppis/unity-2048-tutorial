using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class BlockGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 7;
    public int height = 7;
    public float cellSize = 1f;
    public float cellSpacing = 0.1f;

    [Header("Block Settings")]
    public GameObject blockPrefab;
    public Color[] availableColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    // Available block sizes (width, height)
    private Vector2Int[] blockSizes = new Vector2Int[]
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, 2),
        new Vector2Int(1, 3),
        new Vector2Int(2, 1),
        new Vector2Int(3, 1)
    };

    [Header("Gravity Settings")]
    public bool gravityEnabled = false;
    public float gravitySpeed = 5f;

    [Header("Visual")]
    public GameObject cellPrefab;
    public Color cellColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

    private Block[,] grid;
    private GameObject[,] cells;
    private Block selectedBlock;
    private List<Block> activeBlocks = new List<Block>();

    private void Awake()
    {
        // Initialize will be called from UI
    }

    public bool TryMoveBlockWithPushingPublic(Block block, int fromX, int fromY, int toX, int toY, int dragStartX = -1, int dragStartY = -1)
    {
        return TryMoveBlockWithPushing(block, fromX, fromY, toX, toY, dragStartX, dragStartY);
    }

    /// <summary>
    /// Resets drag session positions for all blocks (call when drag ends)
    /// </summary>
    public void ResetDragSession()
    {
        foreach (Block b in activeBlocks)
        {
            if (b != null)
            {
                b.dragSessionStartX = -1;
                b.dragSessionStartY = -1;
            }
        }
    }

    // Check if move is possible without actually moving (for momentum movement)
    /// <summary>
    /// Checks if a block can move from one position to another without actually moving it
    /// </summary>
    public bool CanMoveBlock(Block block, int fromX, int fromY, int toX, int toY)
    {
        // First, check basic boundary conditions
        if (!IsValidPosition(toX, toY) || !IsValidPosition(toX + block.width - 1, toY + block.height - 1))
        {
            return false;
        }

        // Calculate movement direction
        int dx = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int dy = toY > fromY ? 1 : (toY < fromY ? -1 : 0);

        // Collect all blocks that need to be pushed
        List<BlockMoveInfo> blocksToMove = new List<BlockMoveInfo>();

        // Track original positions for all blocks
        Dictionary<Block, Vector2Int> originalPositions = new Dictionary<Block, Vector2Int>();
        foreach (Block b in activeBlocks)
        {
            if (b != null)
            {
                originalPositions[b] = new Vector2Int(b.gridX, b.gridY);
            }
        }

        return CanMoveBlockRecursive(block, fromX, fromY, toX, toY, dx, dy, blocksToMove, new HashSet<Block>(), originalPositions);
    }

    /// <summary>
    /// Manually updates grid data without calling SetGridPosition (used for momentum movement)
    /// </summary>
    public void UpdateGridData(Block block, int fromX, int fromY, int toX, int toY)
    {
        // Safety check: validate target position
        if (!IsValidPosition(toX, toY) || !IsValidPosition(toX + block.width - 1, toY + block.height - 1))
        {
            Debug.LogError($"UpdateGridData: Invalid target position ({toX}, {toY}) for block size {block.width}x{block.height}. Grid: {width}x{height}");
            return;
        }

        // Safety check: validate source position
        if (!IsValidPosition(fromX, fromY) || !IsValidPosition(fromX + block.width - 1, fromY + block.height - 1))
        {
            Debug.LogError($"UpdateGridData: Invalid source position ({fromX}, {fromY}) for block size {block.width}x{block.height}. Grid: {width}x{height}");
            return;
        }

        // Free old cells
        FreeCells(fromX, fromY, block.width, block.height);

        // Occupy new cells
        OccupyCells(block, toX, toY, block.width, block.height);
    }

    /// <summary>
    /// Gets a list of all blocks that would be pushed for a visual preview (doesn't actually move them)
    /// </summary>
    public List<BlockMoveInfo> GetBlocksToPushVisual(Block block, int fromX, int fromY, int toX, int toY)
    {
        // Calculate movement direction
        int dx = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int dy = toY > fromY ? 1 : (toY < fromY ? -1 : 0);

        // Collect all blocks that need to be pushed
        List<BlockMoveInfo> blocksToMove = new List<BlockMoveInfo>();

        // Track original positions for all blocks
        Dictionary<Block, Vector2Int> originalPositions = new Dictionary<Block, Vector2Int>();
        foreach (Block b in activeBlocks)
        {
            if (b != null)
            {
                originalPositions[b] = new Vector2Int(b.gridX, b.gridY);
            }
        }

        if (!CanMoveBlockRecursive(block, fromX, fromY, toX, toY, dx, dy, blocksToMove, new HashSet<Block>(), originalPositions))
        {
            return null; // Cannot move
        }

        return blocksToMove;
    }

    private bool TryMoveBlockWithPushing(Block block, int fromX, int fromY, int toX, int toY, int dragStartX = -1, int dragStartY = -1)
    {
        // Calculate movement direction
        int dx = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int dy = toY > fromY ? 1 : (toY < fromY ? -1 : 0);

        // Collect all blocks that need to be pushed
        List<BlockMoveInfo> blocksToMove = new List<BlockMoveInfo>();

        // Initialize drag session positions for all blocks (first time only)
        foreach (Block b in activeBlocks)
        {
            if (b != null)
            {
                if (b.dragSessionStartX < 0 || b.dragSessionStartY < 0)
                {
                    // First time this block is involved in the drag session
                    b.dragSessionStartX = b.gridX;
                    b.dragSessionStartY = b.gridY;
                }
            }
        }

        // Track original positions for all blocks using drag session start positions
        Dictionary<Block, Vector2Int> originalPositions = new Dictionary<Block, Vector2Int>();
        foreach (Block b in activeBlocks)
        {
            if (b != null)
            {
                if (b == block && dragStartX >= 0 && dragStartY >= 0)
                {
                    // Use the provided drag start position for the dragged block
                    originalPositions[b] = new Vector2Int(dragStartX, dragStartY);
                }
                else
                {
                    // For other blocks, use their drag session start position
                    originalPositions[b] = new Vector2Int(b.dragSessionStartX, b.dragSessionStartY);
                }
            }
        }

        if (!CanMoveBlockRecursive(block, fromX, fromY, toX, toY, dx, dy, blocksToMove, new HashSet<Block>(), originalPositions))
        {
            return false;
        }

        // Move all blocks (in reverse order to avoid conflicts)
        for (int i = blocksToMove.Count - 1; i >= 0; i--)
        {
            BlockMoveInfo moveInfo = blocksToMove[i];
            FreeCells(moveInfo.fromX, moveInfo.fromY, moveInfo.block.width, moveInfo.block.height);
        }

        for (int i = blocksToMove.Count - 1; i >= 0; i--)
        {
            BlockMoveInfo moveInfo = blocksToMove[i];
            OccupyCells(moveInfo.block, moveInfo.toX, moveInfo.toY, moveInfo.block.width, moveInfo.block.height);
            moveInfo.block.SetGridPosition(moveInfo.toX, moveInfo.toY, false); // Smooth animation
        }

        return true;
    }

    private bool CanMoveBlockRecursive(Block block, int fromX, int fromY, int toX, int toY, int dx, int dy, List<BlockMoveInfo> blocksToMove, HashSet<Block> processed, Dictionary<Block, Vector2Int> originalPositions)
    {
        if (processed.Contains(block))
        {
            return true;
        }

        processed.Add(block);

        // Check if target position is valid
        if (!IsValidPosition(toX, toY) || !IsValidPosition(toX + block.width - 1, toY + block.height - 1))
        {
            return false;
        }

        // Check if this block would move more than 1 cell from its original position
        if (originalPositions.ContainsKey(block))
        {
            Vector2Int originalPos = originalPositions[block];
            int moveDistanceX = Mathf.Abs(toX - originalPos.x);
            int moveDistanceY = Mathf.Abs(toY - originalPos.y);

            // Allow only 1 cell movement in either direction
            if (moveDistanceX > 1 || moveDistanceY > 1)
            {
                return false;
            }
        }

        // Find blocks that would be pushed
        HashSet<Block> blockingBlocks = new HashSet<Block>();
        for (int bx = 0; bx < block.width; bx++)
        {
            for (int by = 0; by < block.height; by++)
            {
                int checkX = toX + bx;
                int checkY = toY + by;

                if (IsValidPosition(checkX, checkY))
                {
                    Block otherBlock = grid[checkX, checkY];
                    if (otherBlock != null && otherBlock != block && !processed.Contains(otherBlock))
                    {
                        blockingBlocks.Add(otherBlock);
                    }
                }
            }
        }

        // If there are blocking blocks, try to push them
        foreach (Block blockingBlock in blockingBlocks)
        {
            int newX = blockingBlock.gridX + dx;
            int newY = blockingBlock.gridY + dy;

            if (!CanMoveBlockRecursive(blockingBlock, blockingBlock.gridX, blockingBlock.gridY, newX, newY, dx, dy, blocksToMove, processed, originalPositions))
            {
                return false;
            }
        }

        // Add this block to the move list
        blocksToMove.Add(new BlockMoveInfo(block, fromX, fromY, toX, toY));
        return true;
    }

    public class BlockMoveInfo
    {
        public Block block;
        public int fromX, fromY;
        public int toX, toY;

        public BlockMoveInfo(Block block, int fromX, int fromY, int toX, int toY)
        {
            this.block = block;
            this.fromX = fromX;
            this.fromY = fromY;
            this.toX = toX;
            this.toY = toY;
        }
    }

    /// <summary>
    /// Initializes a new grid with the specified dimensions
    /// </summary>
    public void InitializeGrid(int w, int h)
    {
        width = w;
        height = h;

        // Clear existing grid
        ClearGrid();

        // Create new grid
        grid = new Block[width, height];
        cells = new GameObject[width, height];

        // Create cell visuals
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateCell(x, y);
            }
        }

        // Center the grid
        CenterGrid();
    }

    private void CreateCell(int x, int y)
    {
        GameObject cell;

        if (cellPrefab != null)
        {
            cell = Instantiate(cellPrefab, transform);
        }
        else
        {
            // Create default cell
            cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cell.transform.SetParent(transform);
            cell.GetComponent<Renderer>().material.color = cellColor;
        }

        cell.name = $"Cell_{x}_{y}";
        cell.transform.localPosition = GetLocalPosition(x, y);
        cell.transform.localScale = Vector3.one * cellSize;

        cells[x, y] = cell;
    }

    public void ClearBlocks()
    {
        // Clear only blocks, keep the grid
        foreach (Block block in activeBlocks)
        {
            if (block != null)
            {
                Destroy(block.gameObject);
            }
        }
        activeBlocks.Clear();

        // Clear block references in grid
        if (grid != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = null;
                }
            }
        }

        selectedBlock = null;
    }

    private void ClearGrid()
    {
        // Clear blocks
        foreach (Block block in activeBlocks)
        {
            if (block != null)
            {
                Destroy(block.gameObject);
            }
        }
        activeBlocks.Clear();

        // Clear cells
        if (cells != null)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    if (cells[x, y] != null)
                    {
                        Destroy(cells[x, y]);
                    }
                }
            }
        }

        grid = null;
        cells = null;
        selectedBlock = null;
    }

    private Vector3 GetLocalPosition(int x, int y)
    {
        float totalSize = cellSize + cellSpacing;
        return new Vector3(x * totalSize, y * totalSize, 0);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return transform.TransformPoint(GetLocalPosition(x, y));
    }

    private void CenterGrid()
    {
        float totalSize = cellSize + cellSpacing;
        Vector3 offset = new Vector3(
            -(width - 1) * totalSize * 0.5f,
            -(height - 1) * totalSize * 0.5f,
            0
        );
        transform.localPosition = offset;
    }

    public void SpawnRandomBlocks(int count)
    {
        if (grid == null) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 10; // Prevent infinite loops

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            // Random position
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // Random size
            Vector2Int size = blockSizes[Random.Range(0, blockSizes.Length)];

            // Random color
            Color randomColor = availableColors[Random.Range(0, availableColors.Length)];

            // Try to spawn block
            if (CanPlaceBlock(x, y, size.x, size.y))
            {
                SpawnBlock(x, y, randomColor, size.x, size.y);
                spawned++;
            }
        }

        // Silently continue if all blocks couldn't be spawned (grid might be too full)
    }

    private List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    emptyCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return emptyCells;
    }

    /// <summary>
    /// Spawns a new block at the specified position with given size and color
    /// </summary>
    public void SpawnBlock(int x, int y, Color color, int w = 1, int h = 1)
    {
        if (!CanPlaceBlock(x, y, w, h))
        {
            return;
        }

        GameObject blockObj;

        if (blockPrefab != null)
        {
            blockObj = Instantiate(blockPrefab, transform);
        }
        else
        {
            // Create default block (2D sprite)
            blockObj = new GameObject("Block");
            blockObj.transform.SetParent(transform);

            // Add SpriteRenderer for 2D visuals
            SpriteRenderer sr = blockObj.AddComponent<SpriteRenderer>();

            // Create a simple sprite (sharp rectangle)
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Fill entire texture with white (solid rectangle)
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            sr.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            // Add BoxCollider2D for click detection
            BoxCollider2D collider = blockObj.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            // Add necessary components
            if (blockObj.GetComponent<Block>() == null)
            {
                blockObj.AddComponent<Block>();
            }
        }

        Block block = blockObj.GetComponent<Block>();
        if (block == null)
        {
            block = blockObj.AddComponent<Block>();
        }

        // Ensure BoxCollider2D exists (for prefab case)
        if (blockObj.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = blockObj.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        block.Initialize(x, y, color, this, w, h);
        OccupyCells(block, x, y, w, h);
        activeBlocks.Add(block);

        blockObj.name = $"Block_{x}_{y}_{w}x{h}";
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsCellEmpty(int x, int y)
    {
        if (!IsValidPosition(x, y)) return false;
        return grid[x, y] == null;
    }

    public bool CanPlaceBlock(int x, int y, int w, int h, Block ignoreBlock = null)
    {
        // Check if all cells needed for this block are valid and empty
        // ignoreBlock: allows checking if a block can move to a new position
        for (int bx = 0; bx < w; bx++)
        {
            for (int by = 0; by < h; by++)
            {
                int checkX = x + bx;
                int checkY = y + by;

                if (!IsValidPosition(checkX, checkY))
                {
                    return false;
                }

                Block occupyingBlock = grid[checkX, checkY];
                if (occupyingBlock != null && occupyingBlock != ignoreBlock)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void OccupyCells(Block block, int x, int y, int w, int h)
    {
        // Mark all cells as occupied by this block
        for (int bx = 0; bx < w; bx++)
        {
            for (int by = 0; by < h; by++)
            {
                if (IsValidPosition(x + bx, y + by))
                {
                    grid[x + bx, y + by] = block;
                }
            }
        }
    }

    private void FreeCells(int x, int y, int w, int h)
    {
        // Free all cells occupied by a block
        for (int bx = 0; bx < w; bx++)
        {
            for (int by = 0; by < h; by++)
            {
                if (IsValidPosition(x + bx, y + by))
                {
                    grid[x + bx, y + by] = null;
                }
            }
        }
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float totalSize = cellSize + cellSpacing;

        int x = Mathf.RoundToInt(localPos.x / totalSize);
        int y = Mathf.RoundToInt(localPos.y / totalSize);

        return new Vector2Int(x, y);
    }

    public void SelectBlock(Block block)
    {
        if (selectedBlock != null && selectedBlock != block)
        {
            selectedBlock.SetSelected(false);
        }

        selectedBlock = block;
    }

    public void MoveBlock(Block block, int fromX, int fromY, int toX, int toY)
    {
        if (block == null) return;

        // Free old cells
        FreeCells(fromX, fromY, block.width, block.height);

        // Occupy new cells
        OccupyCells(block, toX, toY, block.width, block.height);

        if (gravityEnabled)
        {
            StartCoroutine(ApplyGravityRoutine());
        }
    }

    /// <summary>
    /// Checks for adjacent blocks with matching colors in all 4 directions
    /// </summary>
    public void CheckAdjacentBlocks(Block block)
    {
        if (block == null) return;

        List<Block> matches = new List<Block>();
        matches.Add(block);

        // Check all 4 directions
        CheckDirection(block, matches, 1, 0);   // Right
        CheckDirection(block, matches, -1, 0);  // Left
        CheckDirection(block, matches, 0, 1);   // Up
        CheckDirection(block, matches, 0, -1);  // Down

        if (matches.Count > 1)
        {
            // Found adjacent blocks with same color
            foreach (Block match in matches)
            {
                match.TriggerAdjacentMatch();
            }
        }
    }

    private void CheckDirection(Block block, List<Block> matches, int dx, int dy)
    {
        int checkX = block.gridX + dx;
        int checkY = block.gridY + dy;

        if (!IsValidPosition(checkX, checkY)) return;

        Block adjacentBlock = grid[checkX, checkY];

        if (adjacentBlock != null &&
            !matches.Contains(adjacentBlock) &&
            ColorsMatch(block.blockColor, adjacentBlock.blockColor))
        {
            matches.Add(adjacentBlock);
            // Recursively check in the same direction
            CheckDirection(adjacentBlock, matches, dx, dy);
        }
    }

    private bool ColorsMatch(Color a, Color b)
    {
        float threshold = 0.1f;
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }

    public void ApplyGravity()
    {
        if (!gravityEnabled) return;

        StartCoroutine(ApplyGravityRoutine());
    }

    private IEnumerator ApplyGravityRoutine()
    {
        bool blocksMoving = true;

        while (blocksMoving)
        {
            blocksMoving = false;

            // Process from bottom to top
            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height; y++)
                {
                    if (grid[x, y] != null && grid[x, y - 1] == null)
                    {
                        // Move block down
                        Block block = grid[x, y];
                        grid[x, y] = null;
                        grid[x, y - 1] = block;

                        block.SetGridPosition(x, y - 1, false); // Smooth animation
                        blocksMoving = true;
                    }
                }
            }

            if (blocksMoving)
            {
                yield return new WaitForSeconds(1f / gravitySpeed);
            }
        }

        // Check for matches after gravity settles
        foreach (Block block in activeBlocks)
        {
            if (block != null)
            {
                CheckAdjacentBlocks(block);
            }
        }
    }

    public void SetGravity(bool enabled)
    {
        gravityEnabled = enabled;

        if (enabled)
        {
            ApplyGravity();
        }
    }

    // Get grid bounds for camera
    public Bounds GetGridBounds()
    {
        float totalSize = cellSize + cellSpacing;
        Vector3 size = new Vector3(
            width * totalSize,
            height * totalSize,
            0
        );

        return new Bounds(transform.position, size);
    }
}
