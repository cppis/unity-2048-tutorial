using UnityEngine;

[CreateAssetMenu(menuName = "Tile State")]
public class TileState : ScriptableObject
{
    public int number;
    public Color backgroundColor;
    public Color textColor;

    [Header("Sprite Settings")]
    [Tooltip("타일의 배경 sprite (null이면 backgroundColor 사용)")]
    public Sprite backgroundSprite;
}
