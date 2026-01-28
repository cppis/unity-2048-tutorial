using UnityEngine;
using UnityEngine.UI;

public class QubeCell : MonoBehaviour
{
    public Vector2Int coordinates;
    public bool isOccupied;
    public Color cellColor;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        cellColor = color;
        if (image != null)
        {
            image.color = color;
        }
    }

    public void SetOccupied(bool occupied, Color color)
    {
        isOccupied = occupied;
        if (occupied)
        {
            SetColor(color);
        }
        else
        {
            // 빈 셀은 어두운 회색 (#2D3436)
            SetColor(new Color(0.176f, 0.204f, 0.212f, 1f));
        }
    }
}
