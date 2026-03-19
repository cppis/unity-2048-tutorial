using UnityEngine;

[CreateAssetMenu(menuName = "Qube/Block Shape")]
public class QubeBlockShape : ScriptableObject
{
    public string blockName;
    public Vector2Int[] cells; // 블록을 구성하는 셀의 상대 좌표
    public Color blockColor;

    // ==================== 7색 팔레트 ====================

    public static readonly Color COLOR_CYAN    = new Color(0.00f, 0.82f, 1.00f); // #00D2FF
    public static readonly Color COLOR_MAGENTA = new Color(1.00f, 0.18f, 0.47f); // #FF2D78
    public static readonly Color COLOR_LIME    = new Color(0.49f, 0.83f, 0.13f); // #7ED321
    public static readonly Color COLOR_AMBER   = new Color(1.00f, 0.72f, 0.00f); // #FFB800
    public static readonly Color COLOR_PURPLE  = new Color(0.61f, 0.35f, 0.71f); // #9B59B6
    public static readonly Color COLOR_CORAL   = new Color(1.00f, 0.42f, 0.42f); // #FF6B6B
    public static readonly Color COLOR_TEAL    = new Color(0.18f, 0.85f, 0.64f); // #2ED8A3
    public static readonly Color COLOR_GOLD    = new Color(1.00f, 0.84f, 0.00f); // #FFD700
    public static readonly Color COLOR_SKY     = new Color(0.40f, 0.69f, 1.00f); // #66B0FF
    public static readonly Color COLOR_ROSE    = new Color(0.91f, 0.35f, 0.65f); // #E859A6
    public static readonly Color COLOR_MINT    = new Color(0.40f, 0.90f, 0.80f); // #66E6CC
    public static readonly Color COLOR_PEACH   = new Color(1.00f, 0.60f, 0.40f); // #FF9966
    public static readonly Color COLOR_INDIGO  = new Color(0.40f, 0.35f, 0.80f); // #6659CC
    public static readonly Color COLOR_LEMON   = new Color(0.90f, 0.90f, 0.20f); // #E6E633
    public static readonly Color COLOR_SALMON  = new Color(0.98f, 0.50f, 0.45f); // #FA807A
    public static readonly Color COLOR_AZURE   = new Color(0.24f, 0.60f, 0.85f); // #3D99D9
    public static readonly Color COLOR_OLIVE   = new Color(0.60f, 0.73f, 0.35f); // #99BA59
    public static readonly Color COLOR_PLUM    = new Color(0.73f, 0.33f, 0.60f); // #BA5499

    private static readonly Color[] PALETTE = new Color[]
    {
        COLOR_CYAN, COLOR_MAGENTA, COLOR_LIME, COLOR_AMBER,
        COLOR_PURPLE, COLOR_CORAL, COLOR_TEAL,
        COLOR_GOLD, COLOR_SKY, COLOR_ROSE,
        COLOR_MINT, COLOR_PEACH, COLOR_INDIGO, COLOR_LEMON,
        COLOR_SALMON, COLOR_AZURE, COLOR_OLIVE, COLOR_PLUM
    };

    /// <summary>
    /// blockShapes 배열에 팔레트 색상을 순서대로 적용합니다.
    /// </summary>
    public static void ApplyPalette(QubeBlockShape[] shapes)
    {
        if (shapes == null) return;
        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i].blockColor = PALETTE[i % PALETTE.Length];
        }
    }

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
        // 1x1 단일 블록
        return new Vector2Int[]
        {
            new Vector2Int(0, 0)
        };
    }

    public static Vector2Int[] GetDShape()
    {
        // 1x2 도미노 블록
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
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

    public static Vector2Int[] GetJShape()
    {
        // L의 거울상
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1)
        };
    }

    public static Vector2Int[] GetPShape()
    {
        // 2x2 정사각형
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }

    public static Vector2Int[] GetXShape()
    {
        // 십자(+) 모양
        return new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1),
            new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetFShape()
    {
        // 비대칭 계단형
        //  ##.
        //  .#.
        //  .#
        return new Vector2Int[]
        {
            new Vector2Int(0, 2),
            new Vector2Int(1, 2),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0)
        };
    }

    public static Vector2Int[] GetUShape()
    {
        // U자형
        //  #.#
        //  ###
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1),
            new Vector2Int(2, 1)
        };
    }

    public static Vector2Int[] GetWShape()
    {
        // 지그재그 계단
        //  .#
        //  ##
        //  #.
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetYShape()
    {
        // 비대칭 T 변형
        //  .#
        //  ##
        //  .#
        return new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2)
        };
    }

    public static Vector2Int[] GetFmShape()
    {
        // F의 거울상
        //  .##
        //  .#.
        //  #.
        return new Vector2Int[]
        {
            new Vector2Int(1, 2),
            new Vector2Int(2, 2),
            new Vector2Int(1, 1),
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        };
    }

    public static Vector2Int[] GetWmShape()
    {
        // W의 거울상
        //  #.
        //  ##
        //  .#
        return new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(0, 2)
        };
    }

    public static Vector2Int[] GetL2Shape()
    {
        // 큰 L자
        //  #.
        //  #.
        //  ##
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, 2)
        };
    }

    public static Vector2Int[] GetL2mShape()
    {
        // 큰 L자 거울상
        //  .#
        //  .#
        //  ##
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2)
        };
    }
}
