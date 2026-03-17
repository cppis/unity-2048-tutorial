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

    [Header("Block Queue")]
    public QubeBlockQueue blockQueue;
    public QubeBlockPreviewUI previewUI;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI turnCounterText;
    public TextMeshProUGUI gameOverText;

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

        // 게임 오버 텍스트 숨김
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        // 블록 큐 초기화
        if (blockQueue != null)
        {
            blockQueue.Initialize(blockShapes, Mathf.Min(5, blockShapes.Length));
        }

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

        // 큐에서 다음 블록 가져오기 (사전 회전 적용됨)
        QubeBlockEntry entry;
        if (blockQueue != null)
        {
            entry = blockQueue.Dequeue();
        }
        else
        {
            // 폴백: 큐 없이 랜덤 생성
            QubeBlockShape shape = blockShapes[Random.Range(0, Mathf.Min(4, blockShapes.Length))];
            entry = new QubeBlockEntry(shape, (Vector2Int[])shape.cells.Clone());
        }

        // 미리보기 UI 갱신
        if (previewUI != null && blockQueue != null)
        {
            previewUI.UpdatePreview(blockQueue.GetPreview());
        }

        // 블록 생성 (Canvas의 자식으로 생성)
        GameObject blockObj = Instantiate(blockPrefab, grid.transform.parent);
        currentBlock = blockObj.GetComponent<QubeBlock>();

        if (currentBlock != null)
        {
            blockObj.transform.localScale = Vector3.one;
            currentBlock.Initialize(entry.shape, entry.rotatedCells, grid);

            if (!currentBlock.CanPlaceAnywhere())
            {
                Debug.Log("Cannot place block anywhere on the grid - Game Over!");
                GameOver();
            }
        }
        else
        {
            Debug.LogError("QubeBlock component not found on blockPrefab!");
        }
    }

    private void Update()
    {
        // 게임 오버 시 R 키로 재시작
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                NewGame();
            }
            return;
        }

        if (currentBlock == null) return;

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

        // 게임 오버 텍스트 표시
        if (gameOverText != null)
        {
            gameOverText.text = $"GAME OVER!\nFinal Score: {score}\n\nPress R to Restart";
            gameOverText.gameObject.SetActive(true);
        }

        // 현재 블록 제거
        if (currentBlock != null)
        {
            Destroy(currentBlock.gameObject);
            currentBlock = null;
        }
    }
}
