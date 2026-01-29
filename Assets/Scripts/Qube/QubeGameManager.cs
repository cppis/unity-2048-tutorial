using System.Collections;
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
    private bool isPlacingBlock = false; // 블록 배치 중 플래그

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
        isPlacingBlock = false; // 플래그 초기화
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

        // 회전 (Q: 반시계, E: 시계) - 한 번 클릭 시 한 번만 회전
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentBlock.Rotate(clockwise: false);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentBlock.Rotate(clockwise: true);
        }

        // 배치 (Space)
        if (Input.GetKeyDown(KeyCode.Space) && !isPlacingBlock)
        {
            StartCoroutine(PlaceBlockCoroutine());
        }
    }

    private IEnumerator PlaceBlockCoroutine()
    {
        if (currentBlock == null || isPlacingBlock) yield break;

        // 배치 가능한지 확인
        if (!currentBlock.CanPlace())
        {
            Debug.Log("Cannot place block - cells are occupied!");
            yield break;
        }

        // 블록 배치 시작
        isPlacingBlock = true;

        // 블록 배치
        currentBlock.Place();
        currentBlock = null;

        // 한 프레임 대기 - grid 상태가 완전히 갱신될 때까지 대기
        yield return null;

        // 턴 증가 및 quad 감지
        pulseSystem.IncrementTurn();

        UpdateUI();

        // 다음 블록 생성 (0.2초 후)
        yield return new WaitForSeconds(0.2f);
        SpawnNewBlock();

        // 블록 배치 완료
        isPlacingBlock = false;
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
