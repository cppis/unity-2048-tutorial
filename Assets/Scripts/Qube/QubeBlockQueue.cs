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
    public const int QUEUE_SIZE = 3;

    private Queue<QubeBlockEntry> queue = new Queue<QubeBlockEntry>();
    private QubeBlockShape[] availableShapes;
    private int shapeCount;

    public void Initialize(QubeBlockShape[] shapes, int useShapeCount = -1)
    {
        availableShapes = shapes;
        shapeCount = useShapeCount > 0 ? Mathf.Min(useShapeCount, shapes.Length) : shapes.Length;
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
        InsertAt(0, entry);
    }

    /// <summary>
    /// 블록을 큐의 지정 위치에 삽입하고, 맨 뒤의 블록을 제거합니다.
    /// 취소 시 원래 슬롯 위치로 복원하는 데 사용됩니다.
    /// </summary>
    public void InsertAt(int index, QubeBlockEntry entry)
    {
        QubeBlockEntry[] arr = GetPreview();
        queue.Clear();

        // 마지막 하나(DequeueAt 시 추가된 것)를 버리고, 지정 위치에 삽입
        int insertCount = arr.Length - 1; // 원래 크기로 복원
        int srcIdx = 0;
        for (int i = 0; i <= insertCount; i++)
        {
            if (i == index)
            {
                queue.Enqueue(entry);
            }
            else
            {
                if (srcIdx < arr.Length)
                    queue.Enqueue(arr[srcIdx++]);
            }
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
    /// 모든 블록을 회전합니다. direction: 1=시계, -1=반시계
    /// </summary>
    public void RotateAll(int direction)
    {
        if (queue.Count == 0) return;

        QubeBlockEntry[] arr = GetPreview();
        int rotAmount = direction > 0 ? 1 : 3;

        queue.Clear();
        for (int i = 0; i < arr.Length; i++)
        {
            Vector2Int[] rotated = QubeBlock.ApplyRotation(arr[i].rotatedCells, rotAmount);
            queue.Enqueue(new QubeBlockEntry(arr[i].shape, rotated));
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
