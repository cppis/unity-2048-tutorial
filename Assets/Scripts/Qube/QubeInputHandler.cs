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

    public enum InputAction { None, Place, CancelDrag, ClickedQuad }

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

        if (isTouchUp)
        {
            Vector2Int gridPos;
            if (ScreenToGridPosition(screenPos, out gridPos))
            {
                lastClickedCell = gridPos;
                return InputAction.ClickedQuad;
            }
        }

        return InputAction.None;
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
