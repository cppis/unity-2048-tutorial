using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TileBoard board;
    [SerializeField] private CanvasGroup gameOver;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;

    public int score { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        // Only start new game if board is assigned (i.e., in 2048 game scene)
        if (board != null)
        {
            NewGame();
        }
    }

    public void NewGame()
    {
        // reset score
        SetScore(0);

        if (hiscoreText != null)
        {
            hiscoreText.text = LoadHiscore().ToString();
        }

        // hide game over screen
        if (gameOver != null)
        {
            gameOver.alpha = 0f;
            gameOver.interactable = false;
        }

        // update board state
        if (board != null)
        {
            board.ClearBoard();
            board.CreateTile();
            board.CreateTile();
            board.enabled = true;
        }
    }

    public void GameOver()
    {
        if (board != null)
        {
            board.enabled = false;
        }

        if (gameOver != null)
        {
            gameOver.interactable = true;
            StartCoroutine(Fade(gameOver, 1f, 1f));
        }
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    public void IncreaseScore(int points)
    {
        SetScore(score + points);
    }

    private void SetScore(int score)
    {
        this.score = score;

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        SaveHiscore();
    }

    private void SaveHiscore()
    {
        int hiscore = LoadHiscore();

        if (score > hiscore) {
            PlayerPrefs.SetInt("hiscore", score);
        }
    }

    private int LoadHiscore()
    {
        return PlayerPrefs.GetInt("hiscore", 0);
    }

}
