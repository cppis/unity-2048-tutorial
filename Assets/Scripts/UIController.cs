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
    public Toggle autoSpawnToggle;
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

        // Setup toggle listeners
        if (gravityToggle != null)
        {
            gravityToggle.isOn = false;
            gravityToggle.onValueChanged.AddListener(OnGravityToggled);
            // Apply initial state
            OnGravityToggled(gravityToggle.isOn);
        }

        // Auto-find Auto Spawn Toggle if not assigned
        if (autoSpawnToggle == null)
        {
            // Try to find by name in the scene
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("AutoSpawn") && obj.name.Contains("Toggle"))
                {
                    autoSpawnToggle = obj.GetComponent<Toggle>();
                    if (autoSpawnToggle != null)
                    {
                        Debug.Log($"<color=cyan>[AUTO-FOUND]</color> Auto Spawn Toggle found automatically: {obj.name}");
                        break;
                    }
                }
            }
        }

        if (autoSpawnToggle != null)
        {
            // Read current toggle state (don't force it to true)
            bool currentState = autoSpawnToggle.isOn;
            autoSpawnToggle.onValueChanged.AddListener(OnAutoSpawnToggled);
            // Apply current toggle state to sync with BlockGrid
            Debug.Log($"<color=cyan>[SETUP]</color> Auto Spawn Toggle connected - current state: {(currentState ? "ON" : "OFF")}");
            OnAutoSpawnToggled(currentState);
        }
        else
        {
            Debug.LogWarning("<color=yellow>[SETUP WARNING]</color> Auto Spawn Toggle not found. " +
                          "Please create a Toggle named 'AutoSpawnToggle' or assign it in Inspector. " +
                          "Auto spawn will remain enabled by default.");
            // Set default state in BlockGrid even if toggle is missing
            if (blockGrid != null)
            {
                blockGrid.SetAutoSpawn(true);
            }
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

        // Spawn new blocks (force spawn even if auto-spawn is disabled)
        blockGrid.SpawnRandomBlocks(blockCount, forceSpawn: true);
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

    /// <summary>
    /// Toggles auto spawn of random blocks after moves
    /// </summary>
    public void OnAutoSpawnToggled(bool isOn)
    {
        if (blockGrid == null)
        {
            Debug.LogError("BlockGrid reference is missing! Cannot toggle auto-spawn.");
            return;
        }

        Debug.Log($"<color=cyan>[UI TOGGLE]</color> Auto Spawn Toggle clicked: {isOn}");
        blockGrid.SetAutoSpawn(isOn);
    }
}
