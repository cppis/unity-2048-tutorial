using UnityEngine;

[CreateAssetMenu(menuName = "Grid Settings")]
public class GridSettings : ScriptableObject
{
    [Header("Cell Appearance")]
    [Tooltip("그리드 셀의 배경 sprite (null이면 색상 사용)")]
    public Sprite cellSprite;

    [Tooltip("그리드 셀의 배경 색상 (sprite가 null일 때 사용)")]
    public Color cellColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Tile Appearance")]
    [Tooltip("타일의 기본 sprite (TileState에 sprite가 없을 때 사용)")]
    public Sprite defaultTileSprite;
}
