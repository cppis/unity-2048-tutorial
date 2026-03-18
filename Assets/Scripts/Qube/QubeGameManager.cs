using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-1)]
public class QubeGameManager : MonoBehaviour
{
    public enum GamePhase
    {
        QubeControl,     // Phase 1: Quad 클릭 소거 + 프리뷰 드래그 대기
        DraggingBlock,   // Phase 2: 프리뷰에서 블록 드래그 중
        PlacingBlock,    // Phase 3: 터치 릴리즈 → 블록 배치
        UpdatingQube     // Phase 4: Quad 감지/하이라이트 → Phase 1 복귀
    }

    public static QubeGameManager Instance { get; private set; }

    [Header("References")]
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;
    public QubePulseSystem pulseSystem;
    public GameObject blockPrefab;

    [Header("Difficulty")]
    public QubeDifficulty difficulty;

    [Header("Block Shapes")]
    public QubeBlockShape[] blockShapes;

    [Header("Block Queue")]
    public QubeBlockQueue blockQueue;
    public QubeBlockPreviewUI previewUI;

    [Header("Input")]
    public QubeInputHandler inputHandler;

    [Header("Feedback")]
    public QubeScoreFeedback scoreFeedback;

    [Header("Ghost Preview")]
    public bool ghostEnabled = true;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI turnCounterText;
    public TextMeshProUGUI gameOverText;

    private QubeBlock currentBlock;
    private int score = 0;
    private bool isGameOver = false;
    private GamePhase currentPhase = GamePhase.QubeControl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        pulseSystem.OnPulse += OnPulseTriggered;

        // InputHandler 자동 탐색 (Inspector에서 연결 안 되어있을 경우)
        if (inputHandler == null)
        {
            inputHandler = FindObjectOfType<QubeInputHandler>();
            if (inputHandler != null)
                Debug.Log($"[GameManager] InputHandler auto-found: {inputHandler.gameObject.name}");
            else
                Debug.LogWarning("[GameManager] InputHandler not found!");
        }

        if (previewUI != null)
        {
            previewUI.OnSlotDragStarted += OnPreviewSlotDragStarted;
        }

        NewGame();
    }

    private void OnDestroy()
    {
        if (pulseSystem != null)
            pulseSystem.OnPulse -= OnPulseTriggered;
        if (previewUI != null)
            previewUI.OnSlotDragStarted -= OnPreviewSlotDragStarted;
    }

    public void NewGame()
    {
        score = 0;
        isGameOver = false;

        if (difficulty != null)
        {
            grid.SetSize(difficulty.gridWidth, difficulty.gridHeight);
            grid.RebuildGrid();
        }
        else
        {
            grid.ClearGrid();
        }
        pulseSystem.ClearAllQuads();

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        int shapeCount = difficulty != null ? difficulty.blockShapeCount : Mathf.Min(5, blockShapes.Length);
        if (blockQueue != null)
            blockQueue.Initialize(blockShapes, Mathf.Min(shapeCount, blockShapes.Length));

        UpdateUI();
        EnterQubeControl();
    }

    // ==================== Phase Transitions ====================

    private void EnterQubeControl()
    {
        currentPhase = GamePhase.QubeControl;

        // 프리뷰 슬롯 터치 활성화
        if (previewUI != null)
            previewUI.SetSlotsInteractable(true);

        // 미리보기 갱신
        if (previewUI != null && blockQueue != null)
            previewUI.UpdatePreview(blockQueue.GetPreview());

        // 게임오버 체크
        if (blockQueue != null && !CanAnyQueuedBlockPlace())
        {
            GameOver();
        }
    }

    private void EnterDraggingBlock()
    {
        currentPhase = GamePhase.DraggingBlock;

        // 프리뷰 슬롯 터치 비활성화 (EventSystem 간섭 방지)
        if (previewUI != null)
            previewUI.SetSlotsInteractable(false);

        // 큐에서 첫 번째 블록 꺼내 생성
        QubeBlockEntry entry = blockQueue != null
            ? blockQueue.Dequeue()
            : new QubeBlockEntry(blockShapes[0], (Vector2Int[])blockShapes[0].cells.Clone());

        if (previewUI != null && blockQueue != null)
            previewUI.UpdatePreview(blockQueue.GetPreview());

        // 클릭 위치를 그리드 좌표로 변환하여 초기 위치로 설정
        Vector2Int spawnPos = new Vector2Int(-100, -100);
        if (inputHandler != null)
        {
            Vector2 screenPos = Input.touchCount > 0
                ? (Vector2)Input.GetTouch(0).position
                : (Vector2)Input.mousePosition;
            Vector2Int gridPos;
            if (inputHandler.ScreenToGridPosition(screenPos, out gridPos))
            {
                spawnPos = gridPos;
            }
        }

        GameObject blockObj = Instantiate(blockPrefab, grid.transform.parent);
        currentBlock = blockObj.GetComponent<QubeBlock>();

        if (currentBlock != null)
        {
            blockObj.transform.localScale = Vector3.one;
            currentBlock.SetSpawnPosition(spawnPos);
            currentBlock.Initialize(entry.shape, entry.rotatedCells, grid);
            currentBlock.SetGhostEnabled(ghostEnabled);

            if (inputHandler != null)
                inputHandler.StartDragFromPreview(currentBlock);
        }
    }

    private void EnterPlacingBlock()
    {
        currentPhase = GamePhase.PlacingBlock;
        StartCoroutine(PlaceBlockCoroutine());
    }

    private void EnterUpdatingQube()
    {
        currentPhase = GamePhase.UpdatingQube;
        StartCoroutine(UpdateQubeCoroutine());
    }

    // ==================== Update Loop ====================

    private void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                NewGame();
            return;
        }

        switch (currentPhase)
        {
            case GamePhase.QubeControl:
                UpdateQubeControlPhase();
                break;

            case GamePhase.DraggingBlock:
                UpdateDraggingPhase();
                break;

            // PlacingBlock, UpdatingQube는 코루틴이 처리
        }
    }

    private void UpdateQubeControlPhase()
    {
        if (inputHandler == null) return;

        var action = inputHandler.UpdateQubeControl();
        if (action == QubeInputHandler.InputAction.ClickedQuad)
        {
            // Quad 클릭 → 소거
            QubeQuad quad = pulseSystem.GetQuadAtCell(inputHandler.lastClickedCell);
            if (quad != null)
            {
                pulseSystem.RemoveQuad(quad);
                UpdateUI();
            }
        }
    }

    private void UpdateDraggingPhase()
    {
        if (inputHandler == null || currentBlock == null)
        {
            Debug.Log($"[UpdateDraggingPhase] Null detected - inputHandler={inputHandler}, currentBlock={currentBlock}. Returning to QubeControl.");
            EnterQubeControl();
            return;
        }

        var action = inputHandler.UpdateDragging();

        if (action == QubeInputHandler.InputAction.Place || action == QubeInputHandler.InputAction.CancelDrag)
        {
            // 현재 위치에서 배치 가능하면 즉시 배치
            if (currentBlock.CanPlace())
            {
                EnterPlacingBlock();
                return;
            }

            // 근처에서 배치 가능한 위치 탐색
            Vector2Int nearestPos;
            if (currentBlock.FindNearestPlaceablePosition(currentBlock.position, out nearestPos))
            {
                currentBlock.SetPosition(nearestPos);
                EnterPlacingBlock();
            }
            else
            {
                CancelCurrentBlock();
            }
        }
    }

    // ==================== Events ====================

    private void OnPreviewSlotDragStarted(int slotIndex)
    {
        if (currentPhase != GamePhase.QubeControl) return;
        if (isGameOver || currentBlock != null) return;
        if (slotIndex != 0) return; // 첫 번째 슬롯만 허용

        EnterDraggingBlock();
    }

    // ==================== Coroutines ====================

    private IEnumerator PlaceBlockCoroutine()
    {
        currentBlock.Place();
        currentBlock = null;
        if (inputHandler != null) inputHandler.ClearBlock();

        yield return null; // grid 상태 갱신 대기

        EnterUpdatingQube();
    }

    private IEnumerator UpdateQubeCoroutine()
    {
        // Quad 감지 + 하이라이트 (소거는 Phase 1에서 클릭으로)
        pulseSystem.ProcessTurn();
        UpdateUI();

        yield return new WaitForSeconds(0.2f);

        EnterQubeControl();
    }

    // ==================== Helpers ====================

    private void CancelCurrentBlock()
    {
        if (currentBlock != null)
        {
            Destroy(currentBlock.gameObject);
            currentBlock = null;
        }
        if (inputHandler != null) inputHandler.ClearBlock();

        EnterQubeControl();
    }

    private void OnPulseTriggered(int pulseScore, System.Collections.Generic.List<QubeQuad> removedQuads)
    {
        score += pulseScore;
        UpdateUI();

        if (scoreFeedback != null && removedQuads != null && removedQuads.Count > 0)
        {
            float cellStep = grid.cellSize + grid.spacing;
            Vector2 center = removedQuads[0].GetRectCenterFloat();
            float gw = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
            float gh = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;
            Vector2 popupPos = new Vector2(
                -gw / 2f + center.x * cellStep + grid.cellSize / 2f,
                -gh / 2f + center.y * cellStep + grid.cellSize / 2f
            );
            scoreFeedback.ShowScorePopup(pulseScore, popupPos);

            if (removedQuads.Count >= 2)
            {
                float multiplier = removedQuads.Count >= 3 ? 2.0f : 1.5f;
                scoreFeedback.ShowComboText(removedQuads.Count, multiplier);
            }

            int maxSize = 0;
            foreach (var q in removedQuads) { if (q.size > maxSize) maxSize = q.size; }
            if (maxSize >= 9 || removedQuads.Count >= 3)
            {
                float intensity = Mathf.Clamp(maxSize * 1.5f, 5f, 20f);
                scoreFeedback.ShakeScreen(intensity);
            }
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (turnCounterText != null)
        {
            int activeQuads = pulseSystem.GetActiveQuads().Count;
            turnCounterText.text = $"Quads: {activeQuads}";
        }
    }

    private bool CanAnyQueuedBlockPlace()
    {
        if (blockQueue == null) return true;

        QubeBlockEntry[] preview = blockQueue.GetPreview();
        foreach (var entry in preview)
        {
            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                for (int x = 0; x < QubeGrid.WIDTH; x++)
                {
                    bool valid = true;
                    foreach (var cell in entry.rotatedCells)
                    {
                        Vector2Int checkPos = new Vector2Int(x + cell.x, y + cell.y);
                        if (!grid.IsValidPosition(checkPos) || grid.IsCellOccupied(checkPos))
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid) return true;
                }
            }
        }
        return false;
    }

    public void ToggleGhost()
    {
        ghostEnabled = !ghostEnabled;
        if (currentBlock != null)
            currentBlock.SetGhostEnabled(ghostEnabled);
    }

    public void SetGhostEnabled(bool enabled)
    {
        ghostEnabled = enabled;
        if (currentBlock != null)
            currentBlock.SetGhostEnabled(ghostEnabled);
    }

    public void GameOver()
    {
        isGameOver = true;

        if (gameOverText != null)
        {
            gameOverText.text = $"GAME OVER!\nFinal Score: {score}\n\nPress R to Restart";
            gameOverText.gameObject.SetActive(true);
        }

        if (currentBlock != null)
        {
            Destroy(currentBlock.gameObject);
            currentBlock = null;
            if (inputHandler != null) inputHandler.ClearBlock();
        }
    }
}
