using UnityEngine;

[CreateAssetMenu(menuName = "Chime/Block Shape")]
public class ChimeBlockShape : ScriptableObject
{
    public string blockName;
    public Vector2Int[] cells; // 블록을 구성하는 셀의 상대 좌표
    public Color blockColor;

    // 펜토미노 (5칸)
    public static Vector2Int[] GetF_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetI_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4)
        };
    }

    public static Vector2Int[] GetL_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(1, 0)
        };
    }

    public static Vector2Int[] GetN_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2)
        };
    }

    public static Vector2Int[] GetP_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2)
        };
    }

    public static Vector2Int[] GetT_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetU_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(2, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
        };
    }

    public static Vector2Int[] GetV_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1),
            new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
        };
    }

    public static Vector2Int[] GetW_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2)
        };
    }

    public static Vector2Int[] GetX_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetY_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3)
        };
    }

    public static Vector2Int[] GetZ_Pentomino()
    {
        return new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 2)
        };
    }
}
