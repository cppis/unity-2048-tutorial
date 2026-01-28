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

                    // 디버그: 감지된 영역 정보 출력
                    string regionDebug = $"Region at ({x},{y}): {region.Count} cells - ";
                    foreach (var cell in region)
                    {
                        regionDebug += $"({cell.x},{cell.y}) ";
                    }
                    Debug.Log(regionDebug);

                    if (IsValidQuad(region))
                    {
                        QubeQuad quad = new QubeQuad(region, 0); // turnTimer는 나중에 설정
                        Debug.Log($"✓ Valid Quad: {quad.width}x{quad.height} at ({quad.minX},{quad.minY})");
                        quads.Add(quad);
                    }
                    else
                    {
                        Debug.Log($"✗ Invalid Quad: region has {region.Count} cells");
                    }
                }
            }
        }

        Debug.Log($"Total Quads detected: {quads.Count}");
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
        if (region.Count < 4)
        {
            Debug.Log($"  → Rejected: Only {region.Count} cells (minimum is 4)");
            return false;
        }

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

        Debug.Log($"  → Bounds: ({minX},{minY}) to ({maxX},{maxY}), Size: {width}x{height}");

        // 직사각형이고 최소 2x2 이상인지 확인
        if (width < 2 || height < 2)
        {
            Debug.Log($"  → Rejected: {width}x{height} is too small (minimum 2x2)");
            return false;
        }

        // 영역 내 모든 셀이 채워져 있는지 확인
        int expectedCells = width * height;
        if (region.Count != expectedCells)
        {
            Debug.Log($"  → Rejected: Has {region.Count} cells but expected {expectedCells} for {width}x{height} rectangle (has holes)");
            return false;
        }

        Debug.Log($"  → Accepted: Valid {width}x{height} rectangle with {region.Count} cells");
        return true;
    }

    public void HighlightQuads(List<QubeQuad> quads, int pulseInterval = 4)
    {
        // 모든 셀의 하이라이트 및 외곽선 해제
        for (int x = 0; x < QubeGrid.WIDTH; x++)
        {
            for (int y = 0; y < QubeGrid.HEIGHT; y++)
            {
                QubeCell cell = grid.GetCell(x, y);
                if (cell != null && cell.isOccupied)
                {
                    // 원본 색상 복원 (그대로 유지)
                    cell.SetColor(cell.originalColor);
                    // 외곽선 비활성화
                    cell.SetOutline(false);
                    // 턴 타이머 텍스트 숨김
                    cell.SetTurnTimer(-1);
                }
            }
        }

        // Quad 셀 하이라이트 및 외곽선 표시
        foreach (var quad in quads)
        {
            // 남은 턴 수 계산
            int remainingTurns = pulseInterval - quad.turnTimer;

            // Quad의 중앙 위치 계산
            Vector2Int centerPos = quad.GetCenter();

            foreach (var cellPos in quad.cells)
            {
                QubeCell cell = grid.GetCell(cellPos);
                if (cell != null)
                {
                    // 밝게 표시 (원본 색상 기준)
                    Color highlightColor = cell.originalColor * 1.3f;
                    highlightColor.a = 1f;
                    cell.SetColor(highlightColor);

                    // 외곽선 활성화 (노란색)
                    cell.SetOutline(true, Color.yellow);

                    // 중앙 셀만 큰 폰트로 턴 수 표시
                    bool isCenter = (cellPos == centerPos);
                    cell.SetTurnTimer(isCenter ? remainingTurns : -1, isCenter);
                }
            }
        }
    }
}
