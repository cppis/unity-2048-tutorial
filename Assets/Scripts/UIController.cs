using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    private const int DEFAULT_WIDTH = 7;
    private const int DEFAULT_HEIGHT = 7;
    private const int DEFAULT_BLOCK_COUNT = 10;
    private const int MIN_GRID_SIZE = 2;
    private const int MAX_GRID_SIZE = 20;

    [Header("References")]
    public BlockGrid blockGrid;
    public CameraController cameraController;

    [Header("UI Elements")]
    public Toggle gravityToggle;
    public Toggle autoSpawnToggle;
    public TMP_Dropdown moveModeDropdown;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Button createGridButton;
    public Button spawnBlocksButton;
    public TMP_InputField blockCountInput;

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        SetupInputFields();
        SetupButtons();
        SetupToggles();
        SetupDropdowns();
        OnCreateGridClicked();
    }

    private void SetupInputFields()
    {
        if (widthInput != null) widthInput.text = DEFAULT_WIDTH.ToString();
        if (heightInput != null) heightInput.text = DEFAULT_HEIGHT.ToString();
        if (blockCountInput != null) blockCountInput.text = DEFAULT_BLOCK_COUNT.ToString();
    }

    private void SetupButtons()
    {
        if (createGridButton != null) createGridButton.onClick.AddListener(OnCreateGridClicked);
        if (spawnBlocksButton != null) spawnBlocksButton.onClick.AddListener(OnSpawnBlocksClicked);
    }

    private void SetupToggles()
    {
        if (gravityToggle != null)
        {
            gravityToggle.isOn = false;
            gravityToggle.onValueChanged.AddListener(OnGravityToggled);
            OnGravityToggled(false);
        }

        TryFindAutoSpawnToggle();
        if (autoSpawnToggle != null)
        {
            bool currentState = autoSpawnToggle.isOn;
            autoSpawnToggle.onValueChanged.AddListener(OnAutoSpawnToggled);
            OnAutoSpawnToggled(currentState);
        }
        else if (blockGrid != null)
        {
            blockGrid.SetAutoSpawn(true);
        }
    }

    private void SetupDropdowns()
    {
        if (moveModeDropdown != null)
        {
            moveModeDropdown.ClearOptions();
            moveModeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "1",
                "Free",
                "End"
            });
            moveModeDropdown.value = 0; // Default: OneCell
            moveModeDropdown.onValueChanged.AddListener(OnMoveModeChanged);
            OnMoveModeChanged(0); // Initialize
        }
    }

    private void TryFindAutoSpawnToggle()
    {
        if (autoSpawnToggle != null) return;

        foreach (GameObject obj in FindObjectsOfType<GameObject>())
        {
            if (obj.name.Contains("AutoSpawn") && obj.name.Contains("Toggle"))
            {
                autoSpawnToggle = obj.GetComponent<Toggle>();
                if (autoSpawnToggle != null) break;
            }
        }
    }

    public void OnCreateGridClicked()
    {
        if (blockGrid == null) return;

        int width = ParseInput(widthInput, DEFAULT_WIDTH);
        int height = ParseInput(heightInput, DEFAULT_HEIGHT);

        blockGrid.InitializeGrid(width, height);
        if (cameraController != null) cameraController.FocusOnGrid();
    }

    public void OnSpawnBlocksClicked()
    {
        if (blockGrid == null) return;

        int blockCount = ParseInput(blockCountInput, DEFAULT_BLOCK_COUNT);
        blockGrid.ClearBlocks();
        blockGrid.SpawnRandomBlocks(blockCount, forceSpawn: true);
    }

    public void OnGravityToggled(bool isOn)
    {
        if (blockGrid != null) blockGrid.SetGravity(isOn);
    }

    public void OnAutoSpawnToggled(bool isOn)
    {
        if (blockGrid != null) blockGrid.SetAutoSpawn(isOn);
    }

    public void OnMoveModeChanged(int index)
    {
        if (blockGrid == null) return;

        BlockMoveMode mode = (BlockMoveMode)index;
        blockGrid.SetMoveMode(mode);
    }

    private int ParseInput(TMP_InputField inputField, int defaultValue)
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text))
            return defaultValue;

        if (int.TryParse(inputField.text, out int value))
            return Mathf.Clamp(value, MIN_GRID_SIZE, MAX_GRID_SIZE);

        return defaultValue;
    }
}
