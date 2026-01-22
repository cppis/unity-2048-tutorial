using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    public BlockGrid blockGrid;
    public Camera targetCamera;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float zoomPadding = 2f;

    [Header("Pan Settings")]
    public bool enablePan = true;
    public float panSpeed = 0.5f;

    private Vector3 lastMousePosition;
    private float targetOrthographicSize;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null && !targetCamera.orthographic)
            Debug.LogWarning("CameraController works best with orthographic camera");

        targetOrthographicSize = targetCamera != null ? targetCamera.orthographicSize : 5f;

        if (targetCamera != null && targetCamera.GetComponent<Physics2DRaycaster>() == null)
            targetCamera.gameObject.AddComponent<Physics2DRaycaster>();
    }

    private void Update()
    {
        HandleZoom();
        if (enablePan) HandlePan();

        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                targetOrthographicSize,
                Time.deltaTime * zoomSpeed
            );
        }
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetOrthographicSize -= scrollInput * zoomSpeed;
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minZoom, maxZoom);
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 worldDelta = targetCamera.ScreenToWorldPoint(delta) - targetCamera.ScreenToWorldPoint(Vector3.zero);
            transform.position -= new Vector3(worldDelta.x, worldDelta.y, 0) * panSpeed;
            lastMousePosition = Input.mousePosition;
        }
    }

    public void FocusOnGrid()
    {
        if (blockGrid == null || targetCamera == null) return;

        Bounds gridBounds = blockGrid.GetGridBounds();
        float sizeForHeight = (gridBounds.size.y / 2f) + zoomPadding;
        float sizeForWidth = (gridBounds.size.x / (2f * targetCamera.aspect)) + zoomPadding;

        targetOrthographicSize = Mathf.Clamp(Mathf.Max(sizeForHeight, sizeForWidth), minZoom, maxZoom);
        transform.position = new Vector3(gridBounds.center.x, gridBounds.center.y, transform.position.z);
    }

    public void SetZoom(float zoom) => targetOrthographicSize = Mathf.Clamp(zoom, minZoom, maxZoom);
    public void ZoomIn() => targetOrthographicSize = Mathf.Clamp(targetOrthographicSize - 1f, minZoom, maxZoom);
    public void ZoomOut() => targetOrthographicSize = Mathf.Clamp(targetOrthographicSize + 1f, minZoom, maxZoom);
}
