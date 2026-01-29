using UnityEngine;
using UnityEngine.UI;

public class TileCell : MonoBehaviour
{
    public Vector2Int coordinates { get; set; }
    public Tile tile { get; set; }

    public bool Empty => tile == null;
    public bool Occupied => tile != null;

    private Image background;

    private void Awake()
    {
        background = GetComponent<Image>();
    }

    public void SetAppearance(Sprite sprite, Color color)
    {
        if (background == null) return;

        if (sprite != null)
        {
            background.sprite = sprite;
            background.color = Color.white; // sprite에 색상 적용하지 않음
        }
        else
        {
            background.sprite = null;
            background.color = color;
        }
    }
}
