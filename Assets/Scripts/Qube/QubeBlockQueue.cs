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

    /// <summary>
    /// 블록을 큐 맨 앞에 다시 넣고, 맨 뒤의 새로 추가된 블록을 제거합니다.
    /// 취소 시 원래 상태로 복원하는 데 사용됩니다.
    /// </summary>
    public void PushFront(QubeBlockEntry entry)
    {
        QubeBlockEntry[] arr = GetPreview();
        queue.Clear();
        queue.Enqueue(entry);
        // 마지막 하나(Dequeue 시 추가된 것)를 버리고 나머지 복원
        for (int i = 0; i < arr.Length - 1; i++)
        {
            queue.Enqueue(arr[i]);
        }
    }

    public QubeBlockEntry[] GetPreview()
    {
        QubeBlockEntry[] preview = new QubeBlockEntry[queue.Count];
        queue.CopyTo(preview, 0);
        return preview;
    }

    public QubeBlockEntry Peek(int index)
    {
        QubeBlockEntry[] arr = GetPreview();
        return arr[index];
    }

    public QubeBlockEntry DequeueAt(int index)
    {
        // 큐를 배열로 변환, 해당 인덱스 제거 후 재구성
        QubeBlockEntry[] arr = GetPreview();
        QubeBlockEntry entry = arr[index];

        queue.Clear();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i != index) queue.Enqueue(arr[i]);
        }
        // 새 블록 추가하여 큐 크기 유지
        queue.Enqueue(CreateRandomEntry());

        return entry;
    }

    /// <summary>
    /// 첫 번째(활성) 블록만 회전합니다. direction: 1=시계, -1=반시계
    /// </summary>
    public void RotateFirst(int direction)
    {
        if (queue.Count == 0) return;

        QubeBlockEntry[] arr = GetPreview();
        Vector2Int[] rotated = QubeBlock.ApplyRotation(arr[0].rotatedCells, direction > 0 ? 1 : 3);
        arr[0] = new QubeBlockEntry(arr[0].shape, rotated);

        queue.Clear();
        for (int i = 0; i < arr.Length; i++)
        {
            queue.Enqueue(arr[i]);
        }
    }

    private QubeBlockEntry CreateRandomEntry()
    {
        QubeBlockShape shape = availableShapes[Random.Range(0, shapeCount)];
        int rotationCount = Random.Range(0, 4);
        Vector2Int[] rotatedCells = QubeBlock.ApplyRotation(shape.cells, rotationCount);
        return new QubeBlockEntry(shape, rotatedCells);
    }
}
