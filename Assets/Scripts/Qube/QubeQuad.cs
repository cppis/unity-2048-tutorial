using UnityEngine;
using System.Collections.Generic;

public class QubeQuad
{
    public List<Vector2Int> cells;
    public int minX, maxX, minY, maxY;
    public int width, height;
    public int size; // width * height
    public int turnTimer; // Chain 모드용 타이머

    public QubeQuad(List<Vector2Int> quadCells)
    {
        cells = new List<Vector2Int>(quadCells);
        CalculateBounds();
        size = width * height;
        turnTimer = 0;
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
        // 크기별 점수
        if (width == 2 && height == 2) return 100;
        if (width == 3 && height == 3) return 300;
        if (width == 4 && height == 4) return 600;
        return 1000 + (size - 16) * 100;
    }

    public bool Contains(Vector2Int cell)
    {
        return cells.Contains(cell);
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int((minX + maxX) / 2, (minY + maxY) / 2);
    }
}
