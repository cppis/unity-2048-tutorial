using UnityEngine;

[CreateAssetMenu(menuName = "Qube/Block Shape")]
public class QubeBlockShape : ScriptableObject
{
    public string blockName;
    public Vector2Int[] cells; // 블록을 구성하는 셀의 상대 좌표
    public Color blockColor;

    // 기본 블록 모양 정의
    public static Vector2Int[] GetLShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1)
        };
    }

    public static Vector2Int[] GetIShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0)
        };
    }

    public static Vector2Int[] GetTShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0),
            new Vector2Int(1, 1)
        };
    }

    public static Vector2Int[] GetOShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }

    public static Vector2Int[] GetSShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }

    public static Vector2Int[] GetZShape()
    {
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        };
    }
}
