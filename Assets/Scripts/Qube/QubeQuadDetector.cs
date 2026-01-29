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
                        QubeCell qCell = grid.GetCell(cell);
                        regionDebug += $"({cell.x},{cell.y}:b{qCell?.blockId}) ";
                    }
                    Debug.Log(regionDebug);

                    // 영역 내에서 완전한 블록들로 구성된 직사각형 찾기
                    List<QubeQuad> regionQuads = FindQuadsRespectingBlocks(region);
                    quads.AddRange(regionQuads);

                    Debug.Log($"  → Found {regionQuads.Count} quad(s) in this region");
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

    private List<QubeQuad> FindQuadsRespectingBlocks(List<Vector2Int> region)
    {
        List<QubeQuad> quads = new List<QubeQuad>();

        // 1. 영역 내의 셀들을 블록 ID별로 그룹화
        Dictionary<int, List<Vector2Int>> blockGroups = new Dictionary<int, List<Vector2Int>>();
        foreach (var cell in region)
        {
            QubeCell qCell = grid.GetCell(cell);
            if (qCell != null)
            {
                int blockId = qCell.blockId;
                if (!blockGroups.ContainsKey(blockId))
                {
                    blockGroups[blockId] = new List<Vector2Int>();
                }
                blockGroups[blockId].Add(cell);
            }
        }

        Debug.Log($"  Found {blockGroups.Count} distinct blocks in region:");
        foreach (var kvp in blockGroups)
        {
            string cellsStr = string.Join(", ", kvp.Value.ConvertAll(c => $"({c.x},{c.y})"));
            Debug.Log($"    Block {kvp.Key}: {kvp.Value.Count} cells - {cellsStr}");
        }

        // 2. 사용된 블록 추적 (블록은 하나의 quad에만 속해야 함)
        HashSet<int> usedBlocks = new HashSet<int>();

        // 3. region의 bounding box 계산
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

        // 4. 큰 직사각형부터 찾기 (greedy)
        for (int height = maxY - minY + 1; height >= 2; height--)
        {
            for (int width = maxX - minX + 1; width >= 2; width--)
            {
                for (int startY = minY; startY + height - 1 <= maxY; startY++)
                {
                    for (int startX = minX; startX + width - 1 <= maxX; startX++)
                    {
                        // 이 직사각형이 완전한 블록들로 구성되는지 확인
                        List<Vector2Int> quadCells = new List<Vector2Int>();
                        HashSet<int> involvedBlocks = new HashSet<int>();
                        bool isValid = true;

                        // 직사각형 내의 모든 셀 수집
                        for (int y = startY; y < startY + height; y++)
                        {
                            for (int x = startX; x < startX + width; x++)
                            {
                                Vector2Int cell = new Vector2Int(x, y);
                                QubeCell qCell = grid.GetCell(cell);

                                if (qCell == null || !qCell.isOccupied)
                                {
                                    isValid = false;
                                    break;
                                }

                                quadCells.Add(cell);
                                involvedBlocks.Add(qCell.blockId);
                            }
                            if (!isValid) break;
                        }

                        if (!isValid || quadCells.Count < 4)
                            continue;

                        // 관련된 블록 중 이미 사용된 블록이 있는지 확인
                        bool hasUsedBlock = false;
                        foreach (int blockId in involvedBlocks)
                        {
                            if (usedBlocks.Contains(blockId))
                            {
                                hasUsedBlock = true;
                                break;
                            }
                        }

                        if (hasUsedBlock)
                            continue;

                        // 각 관련 블록이 완전히 포함되는지 확인 (블록 분할 방지)
                        bool allBlocksComplete = true;
                        string blockCheckLog = "";
                        List<string> missingCellsLog = new List<string>();

                        foreach (int blockId in involvedBlocks)
                        {
                            List<Vector2Int> blockCells = blockGroups[blockId];
                            int cellsInQuad = 0;
                            int cellsOutsideQuad = 0;
                            List<Vector2Int> outsideCells = new List<Vector2Int>();

                            foreach (var blockCell in blockCells)
                            {
                                if (quadCells.Contains(blockCell))
                                {
                                    cellsInQuad++;
                                }
                                else
                                {
                                    cellsOutsideQuad++;
                                    outsideCells.Add(blockCell);
                                }
                            }

                            blockCheckLog += $" Block{blockId}({cellsInQuad}/{blockCells.Count})";

                            if (cellsOutsideQuad > 0)
                            {
                                allBlocksComplete = false;
                                string outsideStr = string.Join(", ", outsideCells.ConvertAll(c => $"({c.x},{c.y})"));
                                missingCellsLog.Add($"Block{blockId} missing: {outsideStr}");
                            }
                        }

                        // 3x3 이상인 경우 상세 로그 출력
                        if (width >= 3 && height >= 3)
                        {
                            string quadCellsStr = string.Join(", ", quadCells.ConvertAll(c => $"({c.x},{c.y})"));
                            Debug.Log($"  Checking {width}x{height} at ({startX},{startY}):");
                            Debug.Log($"    Cells: {quadCellsStr}");
                            Debug.Log($"    Blocks:{blockCheckLog}");
                            Debug.Log($"    allBlocksComplete={allBlocksComplete}");
                            if (!allBlocksComplete && missingCellsLog.Count > 0)
                            {
                                foreach (var log in missingCellsLog)
                                {
                                    Debug.Log($"      ✗ {log}");
                                }
                            }
                        }

                        if (allBlocksComplete)
                        {
                            // 유효한 quad 발견!
                            QubeQuad quad = new QubeQuad(quadCells, 0);
                            quads.Add(quad);

                            // 사용된 블록 표시
                            foreach (int blockId in involvedBlocks)
                            {
                                usedBlocks.Add(blockId);
                            }

                            Debug.Log($"    ✓ Found quad: {width}x{height} at ({startX},{startY}) with {involvedBlocks.Count} complete block(s)");
                        }
                    }
                }
            }
        }

        return quads;
    }

    public void HighlightQuads(List<QubeQuad> quads, int pulseInterval = 4)
    {
        Debug.Log($"[HighlightQuads] ===== START ===== quads.Count={quads.Count}, pulseInterval={pulseInterval}");

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
                QubeCell cell = grid.GetCell(cellPos);
                if (cell != null)
                {
                    // 밝게 표시 (원본 색상 기준)
                    Color highlightColor = cell.originalColor * 1.3f;
                    highlightColor.a = 1f;
                    cell.SetColor(highlightColor);

                    // 외곽선 활성화 (노란색)
                    cell.SetOutline(true, Color.yellow);

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
