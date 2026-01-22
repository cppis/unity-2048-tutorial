using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("References")]
    public BlockGrid blockGrid;
    public CameraController cameraController;

    [Header("UI Elements")]
    public Toggle gravityToggle;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button createGridButton;
    public Button spawnBlocksButton;
    public TMP_InputField blockCountInput;

    [Header("Default Values")]
    public int defaultWidth = 7;
    public int defaultHeight = 7;
    public int defaultBlockCount = 10;

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Force default values to 7x7
        defaultWidth = 7;
        defaultHeight = 7;

        // Set default values
        if (widthInput != null)
        {
            widthInput.text = defaultWidth.ToString();
        }

        if (heightInput != null)
        {
            heightInput.text = defaultHeight.ToString();
        }

        if (blockCountInput != null)
        {
            blockCountInput.text = defaultBlockCount.ToString();
        }

        // Setup button listeners
        if (createGridButton != null)
        {
            createGridButton.onClick.AddListener(OnCreateGridClicked);
        }

        if (spawnBlocksButton != null)
        {
            spawnBlocksButton.onClick.AddListener(OnSpawnBlocksClicked);
        }

        // Setup toggle listener
        if (gravityToggle != null)
        {
            gravityToggle.isOn = false;
            gravityToggle.onValueChanged.AddListener(OnGravityToggled);
        }

        // Create initial grid
        OnCreateGridClicked();
    }

    /// <summary>
    /// Creates a new grid with dimensions from input fields
    /// </summary>
    public void OnCreateGridClicked()
    {
        if (blockGrid == null)
        {
            Debug.LogError("BlockGrid reference is missing!");
            return;
        }

        int width = defaultWidth;
        int height = defaultHeight;

        // Parse width
        if (widthInput != null && !string.IsNullOrEmpty(widthInput.text))
        {
            if (int.TryParse(widthInput.text, out int parsedWidth))
            {
                width = Mathf.Clamp(parsedWidth, 2, 20);
            }
        }

        // Parse height
        if (heightInput != null && !string.IsNullOrEmpty(heightInput.text))
        {
            if (int.TryParse(heightInput.text, out int parsedHeight))
            {
                height = Mathf.Clamp(parsedHeight, 2, 20);
            }
        }

        // Create grid
        blockGrid.InitializeGrid(width, height);

        // Update camera to fit grid
        if (cameraController != null)
        {
            cameraController.FocusOnGrid();
        }
    }

    /// <summary>
    /// Clears existing blocks and spawns new random blocks
    /// </summary>
    public void OnSpawnBlocksClicked()
    {
        if (blockGrid == null)
        {
            Debug.LogError("BlockGrid reference is missing!");
            return;
        }

        int blockCount = defaultBlockCount;

        // Parse block count
        if (blockCountInput != null && !string.IsNullOrEmpty(blockCountInput.text))
        {
            if (int.TryParse(blockCountInput.text, out int parsedCount))
            {
                blockCount = Mathf.Max(1, parsedCount);
            }
        }

        // Clear existing blocks first
        blockGrid.ClearBlocks();

        // Spawn new blocks
        blockGrid.SpawnRandomBlocks(blockCount);
    }

    /// <summary>
    /// Toggles gravity on/off for the grid
    /// </summary>
    public void OnGravityToggled(bool isOn)
    {
        if (blockGrid == null)
        {
            Debug.LogError("BlockGrid reference is missing!");
            return;
        }

        blockGrid.SetGravity(isOn);
    }
}
