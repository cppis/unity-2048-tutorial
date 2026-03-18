using System.Collections;
using System.Collections.Generic;
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

    [Header("Visual FX")]
    public bool applyColorPalette = true;

    [Header("Ads")]
    public QubeAdBanner adBanner;

    private QubeBlock currentBlock;
    private QubeBlockEntry? currentDragEntry; // 드래그 중인 블록의 큐 엔트리 (취소 시 복원용)
    private int score = 0;
    private bool isGameOver = false;
    private GamePhase currentPhase = GamePhase.QubeControl;
    private QubeUIParticles uiParticles;
    private QubeAudio qubeAudio;
    private int qubeControlEnterFrame = -1; // 입력 쿨다운용

    // 배치 바운스 상수
    private const float BOUNCE_DURATION = 0.15f;
    private const float BOUNCE_SCALE = 1.15f;

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
            if (inputHandler == null)
                Debug.LogWarning("[GameManager] InputHandler not found!");
        }

        if (previewUI != null)
        {
            previewUI.OnSlotDragStarted += OnPreviewSlotDragStarted;
        }

        // Phase 1 FX: 색상 팔레트 적용
        if (applyColorPalette && blockShapes != null && blockShapes.Length > 0)
        {
            QubeBlockShape.ApplyPalette(blockShapes);
        }

        // Phase 1 FX: 배경 그라데이션 + 비네트
        InitBackground();

        // 광고 초기화
        InitAds();

        // Phase 2 FX: UI 파티클 시스템 초기화
        InitUIParticles();

        // 오디오 초기화
        InitAudio();

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

        // 게임오버 오버레이 제거
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform overlay = canvas.transform.Find("GameOverOverlay");
            if (overlay != null) Destroy(overlay.gameObject);
        }

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

        int shapeCount = difficulty != null ? difficulty.blockShapeCount : blockShapes.Length;
        if (blockQueue != null)
            blockQueue.Initialize(blockShapes, Mathf.Min(shapeCount, blockShapes.Length));

        UpdateUI();
        EnterQubeControl();
    }

    // ==================== Phase Transitions ====================

    private void EnterQubeControl()
    {
        currentPhase = GamePhase.QubeControl;
        qubeControlEnterFrame = Time.frameCount;

        // 프리뷰 슬롯 터치 활성화
        if (previewUI != null)
            previewUI.SetSlotsInteractable(true);

        // 미리보기 갱신
        if (previewUI != null && blockQueue != null)
            previewUI.UpdatePreview(blockQueue.GetPreview());

        // 배치 불가 체크 → 쿼드 폭파 또는 게임오버
        if (blockQueue != null && !CanAnyQueuedBlockPlace())
        {
            if (pulseSystem.HasActiveQuads())
            {
                // 쿼드가 있으면 전부 폭파하여 공간 확보
                pulseSystem.RemoveAllQuads();
                UpdateUI();
                // 폭파 후 재검사는 다음 EnterQubeControl에서 수행
                StartCoroutine(RecheckAfterBlast());
            }
            else
            {
                GameOver();
            }
        }
    }

    private void EnterDraggingBlock()
    {
        currentPhase = GamePhase.DraggingBlock;

        // 프리뷰 슬롯 터치 비활성화 (EventSystem 간섭 방지)
        if (previewUI != null)
            previewUI.SetSlotsInteractable(false);

        // 큐에서 첫 번째 블록 꺼내 생성 (프리뷰 갱신은 배치 성공 후에)
        QubeBlockEntry entry = blockQueue != null
            ? blockQueue.Dequeue()
            : new QubeBlockEntry(blockShapes[0], (Vector2Int[])blockShapes[0].cells.Clone());
        currentDragEntry = entry;

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
            currentBlock.StartDragVisual();

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
            // 터치 또는 클릭으로 재시작
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
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

        // 배치/취소 직후 입력 무시 (이전 터치 이벤트 소비 대기)
        if (Time.frameCount - qubeControlEnterFrame <= 5) return;

        var action = inputHandler.UpdateQubeControl();
        if (action == QubeInputHandler.InputAction.RotatePreview)
        {
            // 하단 스와이프 → 첫 번째 블록 회전
            if (blockQueue != null)
            {
                blockQueue.RotateFirst(inputHandler.lastSwipeDirection);
                if (previewUI != null)
                    previewUI.AnimateRotateFirst(blockQueue.GetPreview(), inputHandler.lastSwipeDirection);
            }
        }
        else if (action == QubeInputHandler.InputAction.ClickedQuad)
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
            EnterQubeControl();
            return;
        }

        var action = inputHandler.UpdateDragging();

        if (action == QubeInputHandler.InputAction.CancelDrag)
        {
            // 그리드 밖 릴리즈 → 즉시 취소
            CancelCurrentBlock();
        }
        else if (action == QubeInputHandler.InputAction.Place)
        {
            // 그리드 위 릴리즈 → 배치 시도
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
                return;
            }

            // 배치 불가 → 취소
            CancelCurrentBlock();
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
        // 배치 전 위치 기록 (파티클/바운스용)
        List<Vector2Int> placedPositions = currentBlock.GetPlacedPositions();
        Color blockColor = currentBlock.shape.blockColor;

        currentBlock.EndDragVisual();
        currentBlock.Place();
        currentBlock = null;
        currentDragEntry = null;
        if (inputHandler != null) inputHandler.ClearBlock();

        // 배치 성공 → 프리뷰 갱신
        if (previewUI != null && blockQueue != null)
            previewUI.UpdatePreview(blockQueue.GetPreview());

        yield return null; // grid 상태 갱신 대기

        // 배치 파티클 + 바운스 애니메이션
        EmitPlaceEffects(placedPositions, blockColor);

        EnterUpdatingQube();
    }

    private IEnumerator RecheckAfterBlast()
    {
        // 폭파 애니메이션 완료 대기
        yield return new WaitForSeconds(1.0f);

        if (blockQueue != null && !CanAnyQueuedBlockPlace())
        {
            GameOver();
        }
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
            currentBlock.EndDragVisual();
            Destroy(currentBlock.gameObject);
            currentBlock = null;
        }
        if (inputHandler != null) inputHandler.ClearBlock();

        // 큐에 블록 복원
        if (currentDragEntry.HasValue && blockQueue != null)
        {
            blockQueue.PushFront(currentDragEntry.Value);
            currentDragEntry = null;
        }

        // 프리뷰 갱신 (복원된 상태)
        if (previewUI != null && blockQueue != null)
            previewUI.UpdatePreview(blockQueue.GetPreview());

        EnterQubeControl();
    }

    private void OnPulseTriggered(int pulseScore, System.Collections.Generic.List<QubeQuad> removedQuads)
    {
        score += pulseScore;
        UpdateUI();

        if (scoreFeedback != null && removedQuads != null && removedQuads.Count > 0)
        {
            Vector2 gridOffset = grid.GetComponent<RectTransform>().anchoredPosition;
            float cellStep = grid.cellSize + grid.spacing;
            Vector2 center = removedQuads[0].GetRectCenterFloat();
            float gw = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
            float gh = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;
            Vector2 popupPos = new Vector2(
                -gw / 2f + center.x * cellStep + grid.cellSize / 2f + gridOffset.x,
                -gh / 2f + center.y * cellStep + grid.cellSize / 2f + gridOffset.y
            );
            scoreFeedback.ShowScorePopup(pulseScore, popupPos);

            // 쿼드 파티클
            if (uiParticles != null)
            {
                Color quadColor = QubeGridLines.GetOutlineColorBySize(removedQuads[0].size);
                uiParticles.EmitQuadParticles(popupPos, quadColor);
            }

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

        // 첫 번째(활성) 블록의 4가지 회전 모두 검사
        QubeBlockEntry entry = blockQueue.Peek(0);
        bool isGridEmpty = grid.IsGridEmpty();

        for (int rot = 0; rot < 4; rot++)
        {
            Vector2Int[] cells = QubeBlock.ApplyRotation(entry.rotatedCells, rot);

            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                for (int x = 0; x < QubeGrid.WIDTH; x++)
                {
                    bool valid = true;
                    bool hasAdjacent = false;

                    foreach (var cell in cells)
                    {
                        Vector2Int checkPos = new Vector2Int(x + cell.x, y + cell.y);
                        if (!grid.IsValidPosition(checkPos) || grid.IsCellOccupied(checkPos))
                        {
                            valid = false;
                            break;
                        }
                        if (!isGridEmpty && grid.HasAdjacentOccupied(checkPos))
                        {
                            hasAdjacent = true;
                        }
                    }

                    if (valid && (isGridEmpty || hasAdjacent))
                        return true;
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

    private bool IsBlockPositionOnGrid(Vector2Int blockPos)
    {
        // 블록 위치의 기준점이 그리드 범위 근처(±2셀)에 있는지 확인
        return blockPos.x >= -2 && blockPos.x < QubeGrid.WIDTH + 2 &&
               blockPos.y >= -2 && blockPos.y < QubeGrid.HEIGHT + 2;
    }

    private void InitUIParticles()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        uiParticles = canvas.gameObject.GetComponent<QubeUIParticles>();
        if (uiParticles == null)
            uiParticles = canvas.gameObject.AddComponent<QubeUIParticles>();

        uiParticles.Initialize(canvas.transform);
    }

    private void EmitPlaceEffects(List<Vector2Int> placedPositions, Color blockColor)
    {
        if (placedPositions == null || placedPositions.Count == 0) return;

        Vector2 gridOffset = grid.GetComponent<RectTransform>().anchoredPosition;
        float cellStep = grid.cellSize + grid.spacing;
        float gw = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
        float gh = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;

        // 블록 중심 계산
        Vector2 centerSum = Vector2.zero;
        Transform placedContainer = grid.GetPlacedBlocksContainer();

        foreach (var pos in placedPositions)
        {
            float xPos = -gw / 2f + pos.x * cellStep + grid.cellSize / 2f + gridOffset.x;
            float yPos = -gh / 2f + pos.y * cellStep + grid.cellSize / 2f + gridOffset.y;
            centerSum += new Vector2(xPos, yPos);

            // 각 셀에 바운스 애니메이션
            string cellName = $"PlacedCell_{pos.x}_{pos.y}";
            Transform cellTransform = placedContainer.Find(cellName);
            if (cellTransform != null)
            {
                StartCoroutine(BounceCell(cellTransform.GetComponent<RectTransform>()));
            }
        }

        // 파티클 버스트
        Vector2 particleCenter = centerSum / placedPositions.Count;
        if (uiParticles != null)
        {
            uiParticles.EmitPlaceParticles(particleCenter, blockColor);
        }
    }

    private IEnumerator BounceCell(RectTransform cellRect)
    {
        if (cellRect == null) yield break;

        float elapsed = 0f;
        while (elapsed < BOUNCE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / BOUNCE_DURATION;

            // 스퀴시: 1.0 → 1.15 → 1.0
            float scale;
            if (t < 0.4f)
                scale = Mathf.Lerp(1f, BOUNCE_SCALE, t / 0.4f);
            else
                scale = Mathf.Lerp(BOUNCE_SCALE, 1f, (t - 0.4f) / 0.6f);

            cellRect.localScale = Vector3.one * scale;
            yield return null;
        }

        cellRect.localScale = Vector3.one;
    }

    private void InitAudio()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        qubeAudio = canvas.gameObject.GetComponent<QubeAudio>();
        if (qubeAudio == null)
            qubeAudio = canvas.gameObject.AddComponent<QubeAudio>();

        qubeAudio.Initialize(canvas.transform);

        if (previewUI != null)
            previewUI.SetAudio(qubeAudio);
    }

    private void InitAds()
    {
        if (adBanner == null)
        {
            adBanner = gameObject.GetComponent<QubeAdBanner>();
            if (adBanner == null)
                adBanner = gameObject.AddComponent<QubeAdBanner>();
        }
        adBanner.Initialize();
    }

    private void InitBackground()
    {
        // Canvas를 찾아 배경 적용
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        QubeBackground bg = canvas.gameObject.GetComponent<QubeBackground>();
        if (bg == null)
            bg = canvas.gameObject.AddComponent<QubeBackground>();

        bg.Apply(canvas.transform);
    }

    public void GameOver()
    {
        isGameOver = true;

        if (currentBlock != null)
        {
            Destroy(currentBlock.gameObject);
            currentBlock = null;
            if (inputHandler != null) inputHandler.ClearBlock();
        }

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // Canvas 최상위에 흑백 오버레이 생성
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();

        GameObject overlayObj = null;
        Image overlayImage = null;
        GameObject textObj = null;
        TextMeshProUGUI gameOverTmp = null;

        if (canvas != null)
        {
            // 어두운 오버레이
            overlayObj = new GameObject("GameOverOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = true;

            // Game Over 텍스트
            textObj = new GameObject("GameOverText");
            textObj.transform.SetParent(overlayObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.3f);
            textRect.anchorMax = new Vector2(1f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            gameOverTmp = textObj.AddComponent<TextMeshProUGUI>();
            gameOverTmp.text = $"GAME OVER\n\n<size=70%>Score: {score}</size>\n<size=50%>Tap to Restart</size>";
            gameOverTmp.fontSize = 72;
            gameOverTmp.fontStyle = FontStyles.Bold;
            gameOverTmp.alignment = TextAlignmentOptions.Center;
            gameOverTmp.color = new Color(1f, 1f, 1f, 0f);
            gameOverTmp.raycastTarget = false;
            gameOverTmp.outlineWidth = 0.3f;
            gameOverTmp.outlineColor = new Color(0f, 0f, 0f, 0.6f);

            Canvas textCanvas = textObj.AddComponent<Canvas>();
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 3000;
        }

        // 페이드 인 애니메이션 (0.8초)
        float fadeDuration = 0.8f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float ease = t * t * (3f - 2f * t); // SmoothStep

            if (overlayImage != null)
                overlayImage.color = new Color(0.05f, 0.05f, 0.1f, ease * 0.75f);

            if (gameOverTmp != null)
                gameOverTmp.color = new Color(1f, 1f, 1f, ease);

            yield return null;
        }

        // 기존 gameOverText도 업데이트 (R키 재시작용)
        if (gameOverText != null)
        {
            gameOverText.text = "";
            gameOverText.gameObject.SetActive(false);
        }
    }
}
