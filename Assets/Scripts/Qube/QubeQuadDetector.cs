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

        // Region을 HashSet으로 변환 (빠른 검색용)
        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(region);

        // 4. Cell 기반으로 직사각형 찾기 (큰 직사각형부터, greedy)
        for (int height = maxY - minY + 1; height >= 2; height--)
        {
            for (int width = maxX - minX + 1; width >= 2; width--)
            {
                for (int startY = minY; startY + height - 1 <= maxY; startY++)
                {
                    for (int startX = minX; startX + width - 1 <= maxX; startX++)
                    {
                        // 직사각형 내의 모든 셀 수집
                        List<Vector2Int> quadCells = new List<Vector2Int>();
                        HashSet<int> involvedBlocks = new HashSet<int>();
                        bool isValid = true;

                        for (int y = startY; y < startY + height; y++)
                        {
                            for (int x = startX; x < startX + width; x++)
                            {
                                Vector2Int cell = new Vector2Int(x, y);

                                // 중요: region에 포함된 셀만 처리
                                if (!regionSet.Contains(cell))
                                {
                                    isValid = false;
                                    break;
                                }

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

                        // 최소 4셀 필요
                        if (!isValid || quadCells.Count < 4)
                            continue;

                        // 이미 사용된 블록이 포함되어 있으면 스킵
                        bool hasUsedBlock = false;
                        foreach (int blockId in involvedBlocks)
                        {
                            if (usedBlocks.Contains(blockId))
                            {
                                hasUsedBlock = true;
                                break;
                            }
                        }
                        if (hasUsedBlock) continue;

                        // 블록 완전 포함 체크: 직사각형에 포함된 각 블록이 완전히 포함되는지 확인
                        bool allBlocksComplete = true;
                        foreach (int blockId in involvedBlocks)
                        {
                            List<Vector2Int> blockCells = blockGroups[blockId];

                            // 이 블록의 모든 셀이 quadCells에 포함되는지 확인
                            foreach (var blockCell in blockCells)
                            {
                                if (!quadCells.Contains(blockCell))
                                {
                                    allBlocksComplete = false;
                                    break;
                                }
                            }
                            if (!allBlocksComplete) break;
                        }

                        if (allBlocksComplete)
                        {
                            // 유효한 quad 발견!
                            QubeQuad quad = new QubeQuad(quadCells);
                            quads.Add(quad);

                            // 사용된 블록 표시
                            foreach (int blockId in involvedBlocks)
                            {
                                usedBlocks.Add(blockId);
                            }

                            string blockIdsStr = string.Join(", ", involvedBlocks);
                            Debug.Log($"    ✓ Found quad: {width}x{height} at ({startX},{startY}) with {involvedBlocks.Count} complete block(s) [BlockIDs: {blockIdsStr}]");
                        }
                    }
                }
            }
        }

        return quads;
    }

    public void HighlightQuads(List<QubeQuad> quads)
    {
        QubeGridLines gridLines = grid.GetGridLines();
        if (gridLines == null) return;

        // 모든 외곽선 해제
        gridLines.ClearAllOutlines();

        // Quad 단위로 크기별 색상 아웃라인 표시
        foreach (var quad in quads)
        {
            Color outlineColor = QubeGridLines.GetOutlineColorBySize(quad.size);
            gridLines.AddQuadOutline(quad.minX, quad.minY, quad.maxX, quad.maxY, outlineColor);
        }

        // 펄스 애니메이션 시작
        gridLines.StartPulse();
    }

}
