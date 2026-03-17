using UnityEngine;
using System.Collections.Generic;

public struct QubeBlockEntry
{
    public QubeBlockShape shape;
    public Vector2Int[] rotatedCells;

    public QubeBlockEntry(QubeBlockShape shape, Vector2Int[] rotatedCells)
    {
        this.shape = shape;
        this.rotatedCells = rotatedCells;
    }
}

public class QubeBlockQueue : MonoBehaviour
{
    public const int QUEUE_SIZE = 5;

    private Queue<QubeBlockEntry> queue = new Queue<QubeBlockEntry>();
    private QubeBlockShape[] availableShapes;
    private int shapeCount = 4; // 기본 블록 4종 사용

    public void Initialize(QubeBlockShape[] shapes, int useShapeCount = 4)
    {
        availableShapes = shapes;
        shapeCount = Mathf.Min(useShapeCount, shapes.Length);
        queue.Clear();

        for (int i = 0; i < QUEUE_SIZE; i++)
        {
            queue.Enqueue(CreateRandomEntry());
        }
    }

    public QubeBlockEntry Dequeue()
    {
        QubeBlockEntry entry = queue.Dequeue();
        queue.Enqueue(CreateRandomEntry());
        return entry;
    }

    public QubeBlockEntry[] GetPreview()
    {
        QubeBlockEntry[] preview = new QubeBlockEntry[queue.Count];
        queue.CopyTo(preview, 0);
        return preview;
    }

    private QubeBlockEntry CreateRandomEntry()
    {
        QubeBlockShape shape = availableShapes[Random.Range(0, shapeCount)];
        int rotationCount = Random.Range(0, 4);
        Vector2Int[] rotatedCells = QubeBlock.ApplyRotation(shape.cells, rotationCount);
        return new QubeBlockEntry(shape, rotatedCells);
    }
}
