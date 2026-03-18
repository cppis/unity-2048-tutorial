using UnityEngine;

public class QubeInputHandler : MonoBehaviour
{
    public QubeGrid grid;
    public RectTransform gridRectTransform;
    public RectTransform canvasRectTransform;
    public Camera uiCamera;

    private QubeBlock currentBlock;
    private bool isDragFromPreview = false;
    private int dragStartFrame = -1;

    private void Awake()
    {
        // 미할당 참조 자동 탐색
        if (grid == null)
            grid = FindObjectOfType<QubeGrid>();
        if (gridRectTransform == null && grid != null)
            gridRectTransform = grid.GetComponent<RectTransform>();
        if (canvasRectTransform == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
    }

    public void SetBlock(QubeBlock block)
    {
        currentBlock = block;
    }

    public void ClearBlock()
    {
        currentBlock = null;
        isDragFromPreview = false;
        dragStartFrame = -1;
    }

    public void StartDragFromPreview(QubeBlock block)
    {
        currentBlock = block;
        isDragFromPreview = true;
        dragStartFrame = Time.frameCount;
    }

    // 스와이프 회전 감지용
    private const float SWIPE_MIN_DISTANCE = 80f;
    private const float SWIPE_DIRECTION_RATIO = 1.5f; // 수평 > 수직 × 1.5
    private const float SWIPE_SCREEN_LOWER_RATIO = 0.4f; // 화면 하단 40%

    private bool isSwipeTracking = false;
    private Vector2 swipeStartPos;
    private bool swipeConsumed = false; // 한 터치에서 스와이프 1회만

    public int lastSwipeDirection { get; private set; } // 1=우(시계), -1=좌(반시계)

    public enum InputAction { None, Place, CancelDrag, ClickedQuad, RotatePreview }

    // Phase 1에서 클릭된 그리드 좌표 (ClickedQuad 시 유효)
    public Vector2Int lastClickedCell { get; private set; }

    /// <summary>
    /// Phase 1: Quad 클릭 감지 (블록 없는 상태)
    /// </summary>
    public InputAction UpdateQubeControl()
    {
        bool isTouchUp = false;
        Vector2 screenPos = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPos = touch.position;
            isTouchUp = (touch.phase == TouchPhase.Ended);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            screenPos = Input.mousePosition;
            isTouchUp = true;
        }

        // 하단 영역 스와이프 회전 감지
        bool isTouchDown = false;
        bool isTouchHeld = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPos = touch.position;
            isTouchDown = (touch.phase == TouchPhase.Began);
            isTouchHeld = (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary);
            isTouchUp = (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                screenPos = Input.mousePosition;
                isTouchDown = true;
            }
            else if (Input.GetMouseButton(0))
            {
                screenPos = Input.mousePosition;
                isTouchHeld = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                screenPos = Input.mousePosition;
                isTouchUp = true;
            }
        }

        // 하단 영역에서 터치 시작 → 스와이프 추적 시작
        if (isTouchDown && IsInLowerScreen(screenPos))
        {
            // 프리뷰 슬롯 위가 아닌 경우에만 스와이프 추적
            Vector2Int gridPos;
            if (!ScreenToGridPosition(screenPos, out gridPos))
            {
                isSwipeTracking = true;
                swipeStartPos = screenPos;
                swipeConsumed = false;
            }
        }

        // 스와이프 판정 (50px마다 연속 회전)
        if (isSwipeTracking && (isTouchHeld || isTouchUp))
        {
            Vector2 delta = screenPos - swipeStartPos;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absX >= SWIPE_MIN_DISTANCE && absX > absY * SWIPE_DIRECTION_RATIO)
            {
                lastSwipeDirection = delta.x > 0 ? 1 : -1;
                swipeConsumed = true;
                swipeStartPos = screenPos; // 기준점 리셋 → 다음 50px에서 다시 회전
                return InputAction.RotatePreview;
            }
        }

        // 터치 종료 → 추적 리셋
        if (isTouchUp)
        {
            if (isSwipeTracking && !swipeConsumed)
            {
                // 스와이프 없이 릴리즈 → Quad 클릭 판정
                isSwipeTracking = false;
            }

            Vector2Int gridPos2;
            if (ScreenToGridPosition(screenPos, out gridPos2))
            {
                lastClickedCell = gridPos2;
                return InputAction.ClickedQuad;
            }

            isSwipeTracking = false;
        }

        return InputAction.None;
    }

    private bool IsInLowerScreen(Vector2 screenPos)
    {
        return screenPos.y < Screen.height * SWIPE_SCREEN_LOWER_RATIO;
    }

    /// <summary>
    /// Phase 2: 프리뷰에서 드래그 중인 블록 처리
    /// </summary>
    public InputAction UpdateDragging()
    {
        if (currentBlock == null) return InputAction.None;

        Vector2 screenPos = Input.touchCount > 0
            ? (Vector2)Input.GetTouch(0).position
            : (Vector2)Input.mousePosition;

        bool isTouchHeld = Input.touchCount > 0 || Input.GetMouseButton(0);
        bool isTouchUp = Input.touchCount > 0
            ? (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)
            : Input.GetMouseButtonUp(0);

        int elapsed = Time.frameCount - dragStartFrame;

        Vector2Int gridPos;
        bool isOnGrid = ScreenToGridPosition(screenPos, out gridPos);

        Debug.Log($"[UpdateDragging] frame={Time.frameCount}, elapsed={elapsed}, isTouchHeld={isTouchHeld}, isTouchUp={isTouchUp}, isOnGrid={isOnGrid}, gridPos={gridPos}");

        // 드래그 시작 후 3프레임 이내에는 릴리즈 판정하지 않음
        if (elapsed <= 3)
        {
            UpdateBlockPosition(screenPos, isOnGrid, gridPos);
            return InputAction.None;
        }

        if (isTouchUp || !isTouchHeld)
        {
            isDragFromPreview = false;
            Debug.Log($"[UpdateDragging] RELEASE - isOnGrid={isOnGrid}, canPlace={currentBlock?.CanPlace()}");
            if (isOnGrid && currentBlock.CanPlace())
            {
                return InputAction.Place;
            }
            else
            {
                return InputAction.CancelDrag;
            }
        }

        // 드래그 중 — 블록 위치 업데이트
        UpdateBlockPosition(screenPos, isOnGrid, gridPos);

        return InputAction.None;
    }

    private void UpdateBlockPosition(Vector2 screenPos, bool isOnGrid, Vector2Int gridPos)
    {
        if (isOnGrid)
        {
            // 그리드 위: 그리드 좌표로 스냅
            currentBlock.SetPosition(gridPos);
        }
        else
        {
            // 그리드 밖: 화면 좌표를 따라다님
            currentBlock.SetScreenPosition(screenPos, canvasRectTransform, uiCamera);
        }
    }

    public bool ScreenToGridPosition(Vector2 screenPos, out Vector2Int gridPos)
    {
        gridPos = Vector2Int.zero;
        if (gridRectTransform == null) return false;

        Vector2 localPoint;
        Camera cam = uiCamera != null ? uiCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, screenPos, cam, out localPoint))
            return false;

        float cellStep = grid.cellSize + grid.spacing;
        float gridWidth = QubeGrid.WIDTH * grid.cellSize + (QubeGrid.WIDTH - 1) * grid.spacing;
        float gridHeight = QubeGrid.HEIGHT * grid.cellSize + (QubeGrid.HEIGHT - 1) * grid.spacing;

        float relX = localPoint.x + gridWidth / 2f;
        float relY = localPoint.y + gridHeight / 2f;

        int gx = Mathf.FloorToInt(relX / cellStep);
        int gy = Mathf.FloorToInt(relY / cellStep);

        gridPos = new Vector2Int(gx, gy);
        return gx >= 0 && gx < QubeGrid.WIDTH && gy >= 0 && gy < QubeGrid.HEIGHT;
    }
}
