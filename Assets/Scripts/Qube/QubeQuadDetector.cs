using UnityEngine;
using System.Collections.Generic;

public class QubeQuadDetector : MonoBehaviour
{
    public QubeGrid grid;

    public List<QubeQuad> DetectQuads()
    {
        List<QubeQuad> quads = new List<QubeQuad>();
        bool[,] visited = new bool[QubeGrid.WIDTH, QubeGrid.HEIGHT];

        for (int x = 0; x < QubeGrid.WIDTH; x++)
        {
            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                if (!visited[x, y] && grid.IsCellOccupied(new Vector2Int(x, y)))
                {
                    List<Vector2Int> region = FloodFill(x, y, visited);
                    if (IsValidQuad(region))
                    {
                        QubeQuad quad = new QubeQuad(region);
                        quads.Add(quad);
                    }
                }
            }
        }

        return quads;
    }

    private List<Vector2Int> FloodFill(int startX, int startY, bool[,] visited)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            region.Add(current);

            // 4방향 탐색
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  // 위
                new Vector2Int(0, -1), // 아래
                new Vector2Int(1, 0),  // 오른쪽
                new Vector2Int(-1, 0)  // 왼쪽
            };

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (grid.IsValidPosition(next) &&
                    !visited[next.x, next.y] &&
                    grid.IsCellOccupied(next))
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return region;
    }

    private bool IsValidQuad(List<Vector2Int> region)
    {
        if (region.Count < 4) return false; // 최소 2x2

        // 영역이 직사각형인지 확인
        int minX = region[0].x, maxX = region[0].x;
        int minY = region[0].y, maxY = region[0].y;

        foreach (var cell in region)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // 직사각형이고 최소 2x2 이상인지 확인
        if (width < 2 || height < 2) return false;

        // 영역 내 모든 셀이 채워져 있는지 확인
        if (region.Count != width * height) return false;

        return true;
    }

    public void HighlightQuads(List<QubeQuad> quads)
    {
        // 모든 셀의 하이라이트 해제
        for (int x = 0; x < QubeGrid.WIDTH; x++)
        {
            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                QubeCell cell = grid.GetCell(x, y);
                if (cell != null && cell.isOccupied)
                {
                    // 원래 색상 복원 (약간 어둡게)
                    Color originalColor = cell.cellColor;
                    cell.SetColor(originalColor * 0.8f);
                }
            }
        }

        // Quad 셀 하이라이트
        foreach (var quad in quads)
        {
            foreach (var cellPos in quad.cells)
            {
                QubeCell cell = grid.GetCell(cellPos);
                if (cell != null)
                {
                    // 밝게 표시
                    Color highlightColor = cell.cellColor * 1.3f;
                    highlightColor.a = 1f;
                    cell.SetColor(highlightColor);
                }
            }
        }
    }
}
