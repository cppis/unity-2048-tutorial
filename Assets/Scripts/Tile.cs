using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    private const float ANIMATION_DURATION = 0.1f;

    public TileState state { get; private set; }
    public TileCell cell { get; private set; }
    public bool locked { get; set; }

    private Image background;
    private TextMeshProUGUI text;

    private void Awake()
    {
        background = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetState(TileState state)
    {
        this.state = state;
        background.color = state.backgroundColor;
        text.color = state.textColor;
        text.text = state.number.ToString();
    }

    public void Spawn(TileCell cell)
    {
        SetCell(cell);
        transform.position = cell.transform.position;
    }

    public void MoveTo(TileCell cell)
    {
        SetCell(cell);
        StartCoroutine(Animate(cell.transform.position, false));
    }

    public void Merge(TileCell cell)
    {
        if (this.cell != null) this.cell.tile = null;
        this.cell = null;
        cell.tile.locked = true;
        StartCoroutine(Animate(cell.transform.position, true));
    }

    private void SetCell(TileCell cell)
    {
        if (this.cell != null) this.cell.tile = null;
        this.cell = cell;
        this.cell.tile = this;
    }

    private IEnumerator Animate(Vector3 to, bool merging)
    {
        float elapsed = 0f;
        Vector3 from = transform.position;

        while (elapsed < ANIMATION_DURATION)
        {
            transform.position = Vector3.Lerp(from, to, elapsed / ANIMATION_DURATION);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        if (merging) Destroy(gameObject);
    }
}
