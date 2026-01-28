using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-1)]
public class QubeGameManager : MonoBehaviour
{
    public static QubeGameManager Instance { get; private set; }

    [Header("References")]
    public QubeGrid grid;
    public QubeQuadDetector quadDetector;
    public QubePulseSystem pulseSystem;
    public GameObject blockPrefab;

    [Header("Block Shapes")]
    public QubeBlockShape[] blockShapes;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI turnCounterText;

    private QubeBlock currentBlock;
    private int score = 0;
    private bool isGameOver = false;

    // 회전 반복 입력 처리
    private float rotateRepeatDelay = 0.15f; // 회전 반복 간격 (초)
    private float lastRotateTime = 0f;

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
        // 펄스 이벤트 구독
        pulseSystem.OnPulse += OnPulseTriggered;

        // 게임 시작
        NewGame();
    }

    private void OnDestroy()
    {
        if (pulseSystem != null)
        {
            pulseSystem.OnPulse -= OnPulseTriggered;
        }
    }

    public void NewGame()
    {
        score = 0;
        isGameOver = false;
        grid.ClearGrid();
        pulseSystem.ClearAllQuads(); // 모든 Quad 리셋

        UpdateUI();
        SpawnNewBlock();
    }

    private void SpawnNewBlock()
    {
        if (blockShapes == null || blockShapes.Length == 0)
        {
            Debug.LogError("No block shapes defined!");
            return;
        }

        // 랜덤 블록 선택 (기본 4종만 사용: L, I, T, 1x1)
        QubeBlockShape randomShape = blockShapes[Random.Range(0, Mathf.Min(4, blockShapes.Length))];

        // 블록 생성 (Canvas의 자식으로 생성)
        GameObject blockObj = Instantiate(blockPrefab, grid.transform.parent);
        currentBlock = blockObj.GetComponent<QubeBlock>();

        if (currentBlock != null)
        {
            // 스케일을 명시적으로 1로 설정
            blockObj.transform.localScale = Vector3.one;

            currentBlock.Initialize(randomShape, grid);
            Debug.Log($"Block parent: {blockObj.transform.parent.name}, localScale: {blockObj.transform.localScale}");
        }
        else
        {
            Debug.LogError("QubeBlock component not found on blockPrefab!");
        }
    }

    private void Update()
    {
        if (isGameOver || currentBlock == null) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // 이동 (WASD 또는 방향키)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentBlock.Move(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentBlock.Move(Vector2Int.right);
        }
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentBlock.Move(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentBlock.Move(Vector2Int.down);
        }

        // 회전 (Q: 반시계, E: 시계) - 누르고 있으면 계속 회전
        bool shouldRotate = false;
        bool clockwise = false;

        if (Input.GetKey(KeyCode.Q))
        {
            shouldRotate = true;
            clockwise = false;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            shouldRotate = true;
            clockwise = true;
        }

        if (shouldRotate)
        {
            // 첫 입력이거나 딜레이 시간이 지났으면 회전
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) ||
                Time.time - lastRotateTime >= rotateRepeatDelay)
            {
                currentBlock.Rotate(clockwise);
                lastRotateTime = Time.time;
            }
        }

        // 배치 (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlaceBlock();
        }
    }

    private void PlaceBlock()
    {
        if (currentBlock == null) return;

        // 배치 가능한지 확인
        if (!currentBlock.CanPlace())
        {
            Debug.Log("Cannot place block - cells are occupied!");
            return;
        }

        // 블록 배치
        currentBlock.Place();
        currentBlock = null;

        // 턴 증가
        pulseSystem.IncrementTurn();

        UpdateUI();

        // 다음 블록 생성
        Invoke(nameof(SpawnNewBlock), 0.2f);
    }

    private void OnPulseTriggered(int pulseScore)
    {
        score += pulseScore;
        UpdateUI();
        Debug.Log($"Pulse! Score: +{pulseScore}, Total: {score}");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }

        if (turnCounterText != null)
        {
            int currentTurn = pulseSystem.GetTurnCounter();
            int activeQuads = pulseSystem.GetActiveQuads().Count;
            turnCounterText.text = $"Turn: {currentTurn} | Quads: {activeQuads}";
        }
    }

    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!");
    }
}
