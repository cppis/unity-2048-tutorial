using UnityEngine;
using System.Collections.Generic;

public class QubeQuad
{
    public List<Vector2Int> cells;
    public int minX, maxX, minY, maxY;
    public int width, height;
    public int size; // width * height
    public int turnTimer; // 생성 후 경과 턴
    public int creationTurn; // 생성된 턴 (디버그용)

    public QubeQuad(List<Vector2Int> quadCells, int currentTurn = 0)
    {
        cells = new List<Vector2Int>(quadCells);
        CalculateBounds();
        size = width * height;
        turnTimer = 0;
        creationTurn = currentTurn;
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
        // 셀 수에 비례한 점수 계산
        // 기본 점수: size^2 * 10 (면적에 비례)
        int baseScore = size * size * 10;

        // 정사각형 보너스 (width == height인 경우 1.5배)
        if (width == height)
        {
            baseScore = Mathf.RoundToInt(baseScore * 1.5f);
        }

        // 최소 점수 보장
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

    // Rect의 실제 중앙 좌표 (float)
    public Vector2 GetRectCenterFloat()
    {
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }

    // 다른 Quad와 겹치는지 확인
    public bool OverlapsWith(QubeQuad other)
    {
        foreach (var cell in cells)
        {
            if (other.Contains(cell))
                return true;
        }
        return false;
    }

    // 다른 Quad와 병합 가능한지 확인 (합쳤을 때 직사각형이 되는가?)
    public bool CanMergeWith(QubeQuad other)
    {
        // 두 Quad의 모든 셀을 합침
        HashSet<Vector2Int> mergedCells = new HashSet<Vector2Int>(cells);
        foreach (var cell in other.cells)
        {
            mergedCells.Add(cell);
        }

        // 합친 영역의 경계 계산
        int newMinX = Mathf.Min(minX, other.minX);
        int newMaxX = Mathf.Max(maxX, other.maxX);
        int newMinY = Mathf.Min(minY, other.minY);
        int newMaxY = Mathf.Max(maxY, other.maxY);

        int newWidth = newMaxX - newMinX + 1;
        int newHeight = newMaxY - newMinY + 1;

        // 직사각형인지 확인
        int expectedCells = newWidth * newHeight;
        return mergedCells.Count == expectedCells;
    }

    // 다른 Quad와 병합
    public QubeQuad MergeWith(QubeQuad other, int currentTurn)
    {
        HashSet<Vector2Int> mergedCells = new HashSet<Vector2Int>(cells);
        foreach (var cell in other.cells)
        {
            mergedCells.Add(cell);
        }

        return new QubeQuad(new List<Vector2Int>(mergedCells), currentTurn);
    }
}
