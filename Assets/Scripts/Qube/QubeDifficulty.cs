using UnityEngine;

[CreateAssetMenu(menuName = "Qube/Difficulty")]
public class QubeDifficulty : ScriptableObject
{
    public string difficultyName = "Normal";
    public int gridWidth = 10;
    public int gridHeight = 8;
    public int blockShapeCount = 5;
}
