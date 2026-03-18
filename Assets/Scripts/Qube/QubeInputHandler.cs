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
    private const float SWIPE_ROTATE_INTERVAL = 120f; // 이 거리마다 1회 회전
    private const float SWIPE_DIRECTION_RATIO = 1.5f; // 수평 > 수직 × 1.5 (시작 판정용)
    private const float SWIPE_START_THRESHOLD = 30f; // 스와이프 시작 판정 최소 거리
    private const float SWIPE_SCREEN_LOWER_RATIO = 0.4f; // 화면 하단 40%

    private bool isSwipeTracking = false;
    private Vector2 swipeStartPos;
    private Vector2 swipePrevPos; // 이전 프레임 위치
    private float swipeAccumulated = 0f; // 누적 수평 이동 거리
    private bool swipeActive = false; // 스와이프 방향 확정됨
    private bool swipeConsumed = false;

    public int lastSwipeDirection { get; private set; } // 1=우(시계), -1=좌(반시계)

    public enum InputAction { None, Place, CancelDrag, ClickedQuad, RotatePreview }

    // Phase 1에서 클릭된 그리드 좌표 (ClickedQuad 시 유효)
    public Vector2Int lastClickedCell { get; private set; }

    /// <summary>
    /// Phase 1: Quad 클릭 감지 (블록 없는 상태)
    /// </summary>
    public InputAction UpdateQubeControl()
    {
        bool isTouchDown = false;
        bool isTouchHeld = false;
        bool isTouchUp = false;
        Vector2 screenPos = Vector2.zero;

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
            screenPos = Input.mousePosition;
            isTouchDown = Input.GetMouseButtonDown(0);
            isTouchHeld = Input.GetMouseButton(0) && !isTouchDown;
            isTouchUp = Input.GetMouseButtonUp(0);
        }

        // 하단 영역에서 터치 시작 → 스와이프 추적 시작
        if (isTouchDown && IsInLowerScreen(screenPos))
        {
            Vector2Int gridPos;
            if (!ScreenToGridPosition(screenPos, out gridPos))
            {
                isSwipeTracking = true;
                swipeActive = false;
                swipeStartPos = screenPos;
                swipePrevPos = screenPos;
                swipeAccumulated = 0f;
                swipeConsumed = false;
            }
        }

        // 스와이프 누적 판정 (부호 있는 누적: +우, -좌)
        if (isSwipeTracking && (isTouchHeld || isTouchUp))
        {
            float frameDeltaX = screenPos.x - swipePrevPos.x;
            swipePrevPos = screenPos;

            if (!swipeActive)
            {
                // 방향 확정 전: 시작점에서 충분히 이동했는지 확인
                Vector2 totalDelta = screenPos - swipeStartPos;
                float absX = Mathf.Abs(totalDelta.x);
                float absY = Mathf.Abs(totalDelta.y);

                if (absX >= SWIPE_START_THRESHOLD && absX > absY * SWIPE_DIRECTION_RATIO)
                {
                    swipeActive = true;
                    swipeAccumulated = totalDelta.x; // 부호 포함 누적
                }
            }
            else
            {
                // 방향 확정 후: 부호 있는 수평 이동 누적
                swipeAccumulated += frameDeltaX;
            }

            // +방향 회전 간격 도달 → 시계방향
            if (swipeActive && swipeAccumulated >= SWIPE_ROTATE_INTERVAL)
            {
                swipeAccumulated -= SWIPE_ROTATE_INTERVAL;
                lastSwipeDirection = 1;
                swipeConsumed = true;
                return InputAction.RotatePreview;
            }
            // -방향 회전 간격 도달 → 반시계방향
            if (swipeActive && swipeAccumulated <= -SWIPE_ROTATE_INTERVAL)
            {
                swipeAccumulated += SWIPE_ROTATE_INTERVAL;
                lastSwipeDirection = -1;
                swipeConsumed = true;
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

        // 드래그 시작 후 3프레임 이내에는 릴리즈 판정하지 않음
        if (elapsed <= 3)
        {
            UpdateBlockPosition(screenPos, isOnGrid, gridPos);
            return InputAction.None;
        }

        if (isTouchUp || !isTouchHeld)
        {
            isDragFromPreview = false;
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
