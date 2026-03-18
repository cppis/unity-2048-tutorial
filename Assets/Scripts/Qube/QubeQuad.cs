using UnityEngine;
using System.Collections.Generic;

public class QubeQuad
{
    public List<Vector2Int> cells;
    public int minX, maxX, minY, maxY;
    public int width, height;
    public int size; // width * height

    public QubeQuad(List<Vector2Int> quadCells)
    {
        cells = new List<Vector2Int>(quadCells);
        CalculateBounds();
        size = width * height;
    }

    private void CalculateBounds()
    {
        if (cells.Count == 0) return;

        minX = cells[0].x;
        maxX = cells[0].x;
        minY = cells[0].y;
        maxY = cells[0].y;

        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        width = maxX - minX + 1;
        height = maxY - minY + 1;
    }

    public int GetScore()
    {
        int baseScore = size * size * 10;
        if (width == height)
        {
            baseScore = Mathf.RoundToInt(baseScore * 1.5f);
        }
        return Mathf.Max(baseScore, size * 25);
    }

    public bool Contains(Vector2Int cell)
    {
        return cells.Contains(cell);
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);
    }

    public Vector2 GetRectCenterFloat()
    {
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }

    public bool OverlapsWith(QubeQuad other)
    {
        foreach (var cell in cells)
        {
            if (other.Contains(cell))
                return true;
        }
        return false;
    }

    public bool IsAdjacentTo(QubeQuad other)
    {
        Vector2Int[] directions = {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        foreach (var cell in cells)
        {
            foreach (var dir in directions)
            {
                Vector2Int adjacentCell = cell + dir;
                if (other.Contains(adjacentCell))
                    return true;
            }
        }

        return false;
    }

    public bool CanMergeWith(QubeQuad other)
    {
        HashSet<Vector2Int> mergedCells = new HashSet<Vector2Int>(cells);
        foreach (var cell in other.cells)
        {
            mergedCells.Add(cell);
        }

        int newMinX = Mathf.Min(minX, other.minX);
        int newMaxX = Mathf.Max(maxX, other.maxX);
        int newMinY = Mathf.Min(minY, other.minY);
        int newMaxY = Mathf.Max(maxY, other.maxY);

        int newWidth = newMaxX - newMinX + 1;
        int newHeight = newMaxY - newMinY + 1;

        int expectedCells = newWidth * newHeight;
        return mergedCells.Count == expectedCells;
    }

    public QubeQuad MergeWith(QubeQuad other)
    {
        HashSet<Vector2Int> mergedCells = new HashSet<Vector2Int>(cells);
        foreach (var cell in other.cells)
        {
            mergedCells.Add(cell);
        }
        return new QubeQuad(new List<Vector2Int>(mergedCells));
    }
}
