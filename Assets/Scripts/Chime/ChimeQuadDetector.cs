using UnityEngine;
using System.Collections.Generic;

public class ChimeQuadDetector : MonoBehaviour
{
    public ChimeGrid grid;

    public List<ChimeQuad> DetectQuads()
    {
        return DetectQuads(new HashSet<Vector2Int>());
    }

    public List<ChimeQuad> DetectQuads(HashSet<Vector2Int> excludedCells)
    {
        List<ChimeQuad> quads = new List<ChimeQuad>();
        bool[,] visited = new bool[ChimeGrid.WIDTH, ChimeGrid.HEIGHT];

        for (int x = 0; x < ChimeGrid.WIDTH; x++)
        {
            for (int y = 0; y < ChimeGrid.HEIGHT; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                // 이미 다른 quad에 속한 셀은 제외
                if (!visited[x, y] && grid.IsCellOccupied(cellPos) && !excludedCells.Contains(cellPos))
                {
                    List<Vector2Int> region = FloodFill(x, y, visited, excludedCells);

                    if (region.Count == 0) continue;

                    // 디버그: 감지된 영역 정보 출력
                    string regionDebug = $"Region at ({x},{y}): {region.Count} cells - ";
                    foreach (var cell in region)
                    {
                        ChimeCell qCell = grid.GetCell(cell);
                        regionDebug += $"({cell.x},{cell.y}:b{qCell?.blockId}) ";
                    }
                    Debug.Log(regionDebug);

                    // 영역 내에서 직사각형 찾기 (셀 단위로 중복 방지)
                    List<ChimeQuad> regionQuads = FindQuadsInRegion(region, excludedCells);
                    quads.AddRange(regionQuads);

                    // 새로 찾은 quad의 셀들을 excludedCells에 추가
                    foreach (var quad in regionQuads)
                    {
                        foreach (var cell in quad.cells)
                        {
                            excludedCells.Add(cell);
                        }
                    }

                    Debug.Log($"  → Found {regionQuads.Count} quad(s) in this region");
                }
            }
        }

        Debug.Log($"Total Quads detected: {quads.Count}");
        return quads;
    }

    private List<Vector2Int> FloodFill(int startX, int startY, bool[,] visited, HashSet<Vector2Int> excludedCells)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Vector2Int start = new Vector2Int(startX, startY);
        if (excludedCells.Contains(start))
        {
            visited[startX, startY] = true;
            return region;
        }

        queue.Enqueue(start);
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
                    grid.IsCellOccupied(next) &&
                    !excludedCells.Contains(next))
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return region;
    }

    private List<ChimeQuad> FindQuadsInRegion(List<Vector2Int> region, HashSet<Vector2Int> excludedCells)
    {
        List<ChimeQuad> quads = new List<ChimeQuad>();

        if (region.Count < 9) return quads; // 최소 3x3 = 9셀 필요

        // 1. region의 bounding box 계산
        int minX = region[0].x, maxX = region[0].x;
        int minY = region[0].y, maxY = region[0].y;
        foreach (var cell in region)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        Debug.Log($"  Region bounds: ({minX},{minY}) to ({maxX},{maxY})");

        // Region을 HashSet으로 변환 (빠른 검색용)
        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(region);

        // 2. 사용된 셀 추적 (셀은 하나의 quad에만 속해야 함)
        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>(excludedCells);

        // 3. Cell 기반으로 직사각형 찾기 (큰 직사각형부터, greedy)
        // 최소 3x3 이상만 허용
        for (int height = maxY - minY + 1; height >= 3; height--)
        {
            for (int width = maxX - minX + 1; width >= 3; width--)
            {
                for (int startY = minY; startY + height - 1 <= maxY; startY++)
                {
                    for (int startX = minX; startX + width - 1 <= maxX; startX++)
                    {
                        // 직사각형 내의 모든 셀 수집
                        List<Vector2Int> quadCells = new List<Vector2Int>();
                        bool isValid = true;

                        for (int y = startY; y < startY + height && isValid; y++)
                        {
                            for (int x = startX; x < startX + width && isValid; x++)
                            {
                                Vector2Int cell = new Vector2Int(x, y);

                                // 이미 사용된 셀이면 스킵
                                if (usedCells.Contains(cell))
                                {
                                    isValid = false;
                                    break;
                                }

                                // region에 포함된 셀만 처리
                                if (!regionSet.Contains(cell))
                                {
                                    isValid = false;
                                    break;
                                }

                                ChimeCell qCell = grid.GetCell(cell);
                                if (qCell == null || !qCell.isOccupied)
                                {
                                    isValid = false;
                                    break;
                                }

                                quadCells.Add(cell);
                            }
                        }

                        if (!isValid || quadCells.Count != width * height)
                            continue;

                        // 유효한 quad 발견!
                        ChimeQuad quad = new ChimeQuad(quadCells, 0);
                        quads.Add(quad);

                        // 사용된 셀 표시
                        foreach (var cell in quadCells)
                        {
                            usedCells.Add(cell);
                        }

                        Debug.Log($"    ✓ Found quad: {width}x{height} at ({startX},{startY}), {quadCells.Count} cells");
                    }
                }
            }
        }

        return quads;
    }

    public void HighlightQuads(List<ChimeQuad> quads, int pulseInterval = 4)
    {
        Debug.Log($"[HighlightQuads] ===== START ===== quads.Count={quads.Count}, pulseInterval={pulseInterval}");

        // 모든 셀의 하이라이트 및 외곽선 해제
        for (int x = 0; x < ChimeGrid.WIDTH; x++)
        {
            for (int y = 0; y < ChimeGrid.HEIGHT; y++)
            {
                ChimeCell cell = grid.GetCell(x, y);
                if (cell != null && cell.isOccupied)
                {
                    // 원본 색상 복원 (그대로 유지)
                    cell.SetColor(cell.originalColor);
                    // 외곽선 비활성화 (모든 변)
                    cell.SetOutline(false, false, false, false);
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
            Debug.Log($"[HighlightQuads] Highlighting Quad {quad.width}x{quad.height}: turnTimer={quad.turnTimer}, pulseInterval={pulseInterval}, remainingTurns={remainingTurns}");

            // Quad의 rect 실제 중앙 위치 계산 (float)
            Vector2 rectCenter = quad.GetRectCenterFloat();

            // 중앙에 가장 가까운 셀 찾기
            Vector2Int centerCell = quad.GetCenter();
            Debug.Log($"[HighlightQuads] Center cell: ({centerCell.x},{centerCell.y})");

            // cellSize + spacing (QubeGrid에서 85)
            float cellStep = 80f + 5f;

            // 중앙 셀로부터 rect 중앙까지의 오프셋 계산
            Vector2 offset = new Vector2(
                (rectCenter.x - centerCell.x) * cellStep,
                (rectCenter.y - centerCell.y) * cellStep
            );

            foreach (var cellPos in quad.cells)
            {
                ChimeCell cell = grid.GetCell(cellPos);
                if (cell != null)
                {
                    // 밝게 표시 (원본 색상 기준)
                    Color highlightColor = cell.originalColor * 1.3f;
                    highlightColor.a = 1f;
                    cell.SetColor(highlightColor);

                    // 이 셀이 quad의 어느 경계에 있는지 확인
                    bool isTopEdge = (cellPos.y == quad.maxY);
                    bool isBottomEdge = (cellPos.y == quad.minY);
                    bool isLeftEdge = (cellPos.x == quad.minX);
                    bool isRightEdge = (cellPos.x == quad.maxX);

                    // 외곽 변만 활성화
                    cell.SetOutline(isTopEdge, isBottomEdge, isLeftEdge, isRightEdge, Color.yellow);

                    // 중앙 셀인지 확인
                    bool isCenter = (cellPos == centerCell);

                    // 중앙 셀에만 턴 수 표시 (오프셋 적용하여 rect 중앙에 위치)
                    int timerValue = isCenter ? remainingTurns : -1;

                    if (isCenter)
                    {
                        Debug.Log($"[HighlightQuads] Setting timer on CENTER cell ({cellPos.x},{cellPos.y}): value={timerValue}, offset={offset}");
                    }

                    cell.SetTurnTimer(timerValue, isCenter, isCenter ? offset : (Vector2?)null);
                }
            }
        }

        Debug.Log($"[HighlightQuads] ===== END ===== Highlighted {quads.Count} Quad(s)");
    }
}
