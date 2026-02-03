# 직사각형 감지 및 병합 로직 통합 가이드

이 문서는 프로젝트의 두 게임(2048, Qube)에서 사용되는 직사각형 감지 및 병합 시스템을 설명합니다.

## 목차
- [개요](#개요)
- [2048 게임: 블록 병합 시스템](#2048-게임-블록-병합-시스템)
- [비교 분석](#비교-분석)
- [참고 자료](#참고-자료)

---

## 개요

두 게임 모두 **직사각형 패턴 감지**를 핵심 메커니즘으로 사용하지만, 구현 방식과 목적이 다릅니다.

| 게임 | 시스템 | 목적 | 트리거 | 파일 위치 |
|------|--------|------|--------|-----------|
| **2048** | 블록 병합 | 여러 1×1 블록 → 하나의 큰 블록 | 블록 드래그 종료 | `Assets/Scripts/BlockGrid.cs` |
| **Qube** | Quad 추적 | 직사각형 영역 감지 및 턴 기반 소거 | 블록 배치 후 턴 증가 | `Assets/Scripts/Qube/` |

### 공통점
- ✓ BFS(너비 우선 탐색) 알고리즘 사용
- ✓ 경계 박스 계산 (minX, maxX, minY, maxY)
- ✓ 면적 검증 (width × height == 셀 개수)
- ✓ 빈 공간 체크 (완전한 직사각형만 인정)

### 차이점
| 요소 | 2048 | Qube |
|------|------|------|
| **실행 시점** | 즉시 (드래그 종료) | 턴 기반 (매 턴마다) |
| **처리 방식** | 병합 (여러 블록 → 1개) | 추적 (영역 감지 + 라이프사이클) |
| **색상 조건** | 같은 색만 병합 | 색상 무관 (현재) |
| **블록 크기** | 1×1만 병합 대상 | 모든 점유 셀 |
| **지속성** | 즉시 병합 후 종료 | 8턴간 추적 후 소거 |
| **병합 기능** | ✓ (기존 블록 제거) | ✓ (Quad 병합, turnTimer 리셋) |

---

## 2048 게임: 블록 병합 시스템

블록 드래그 완료 시 같은 색의 1×1 블록들이 직사각형을 형성하면 자동으로 하나의 큰 블록으로 병합됩니다.

### 실행 시점 및 우선순위

**트리거**: `Block.cs:OnEndDrag` → `BlockGrid.CheckAdjacentBlocks()`

**우선순위**:
1. 직사각형 병합 시도 (우선)
2. 기존 이벤트 로직 (대체) - `OnAdjacentMatchFound` 발생

---

### 핵심 알고리즘

#### 1. FindRectangle (BlockGrid.cs:723-784)

같은 색의 연결된 1×1 블록들을 찾아 직사각형을 형성하는지 확인합니다.

**조건**:
- 시작 블록이 1×1이어야 함
- 모든 연결된 블록이 같은 색이어야 함
- 모든 블록이 1×1이어야 함
- 최소 2개 이상의 블록

**BFS 탐색**:
```csharp
Queue<Block> queue = new Queue<Block>();
HashSet<Block> visited = new HashSet<Block>();
queue.Enqueue(startBlock);
visited.Add(startBlock);

while (queue.Count > 0)
{
    Block current = queue.Dequeue();

    // 4방향 (상/하/좌/우) 탐색
    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;  // 자기 자신 제외
            if (dx != 0 && dy != 0) continue;  // 대각선 제외

            Block neighbor = grid[x + dx, y + dy];

            // 조건: 같은 색 + 1×1 + 미방문
            if (neighbor != null &&
                neighbor.IsSameColor(startBlock) &&
                neighbor.width == 1 && neighbor.height == 1 &&
                !visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }
    }
}
```

---

#### 2. IsRectangle (BlockGrid.cs:789-833)

블록들이 유효한 직사각형을 형성하는지 검증합니다.

**검증 로직**:

**단계 1: 경계 박스 계산**
```csharp
int minX = int.MaxValue, maxX = int.MinValue;
int minY = int.MaxValue, maxY = int.MinValue;

foreach (Block block in blocks)
{
    if (block.gridX < minX) minX = block.gridX;
    if (block.gridX > maxX) maxX = block.gridX;
    if (block.gridY < minY) minY = block.gridY;
    if (block.gridY > maxY) maxY = block.gridY;
}

int rectWidth = maxX - minX + 1;
int rectHeight = maxY - minY + 1;
```

**단계 2: 면적 일치 확인**
```csharp
if (rectWidth * rectHeight != blocks.Count)
    return false;  // 구멍이 있거나 L자형 등
```

**단계 3: 빈 공간 확인**
```csharp
for (int x = minX; x <= maxX; x++)
{
    for (int y = minY; y <= maxY; y++)
    {
        Block blockAtPos = grid[x, y];
        if (blockAtPos == null || !blocks.Contains(blockAtPos))
            return false;  // 빈 공간 발견
    }
}
return true;
```

**예시**:
```
✓ 유효한 직사각형 (2×3):     ✗ 무효 (L자):              ✗ 무효 (빈 공간):
O O O                        O O .                      O O O
O O O                        O . .                      O . O
```

---

#### 3. MergeBlocksToRectangle (BlockGrid.cs:838-881)

여러 1×1 블록을 하나의 큰 블록으로 병합합니다.

**병합 과정**:

```csharp
public void MergeBlocksToRectangle(List<Block> blocks)
{
    // 1. 경계 박스 계산
    int minX = blocks.Min(b => b.gridX);
    int maxX = blocks.Max(b => b.gridX);
    int minY = blocks.Min(b => b.gridY);
    int maxY = blocks.Max(b => b.gridY);

    int rectWidth = maxX - minX + 1;
    int rectHeight = maxY - minY + 1;
    Color mergeColor = blocks[0].GetComponent<Image>().color;

    // 2. 기존 블록 제거
    foreach (Block block in blocks)
    {
        activeBlocks.Remove(block);
        Destroy(block.gameObject);
    }

    // 3. 그리드 셀 정리
    for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
            grid[x, y] = null;

    // 4. 새로운 큰 블록 생성
    SpawnBlock(minX, minY, mergeColor, rectWidth, rectHeight);

    // 5. 디버그 로그
    Debug.Log($"Merging {blocks.Count} blocks into {rectWidth}×{rectHeight} rectangle at ({minX},{minY})");
}
```

---

#### 4. CheckAdjacentBlocks 수정 (BlockGrid.cs:668-698)

블록 이동 후 자동으로 실행되는 메인 로직입니다.

```csharp
public void CheckAdjacentBlocks(Block block)
{
    // 1. 직사각형 병합 시도 (우선)
    List<Block> rectangleBlocks = FindRectangle(block);
    if (rectangleBlocks != null && rectangleBlocks.Count > 1)
    {
        MergeBlocksToRectangle(rectangleBlocks);
        return; // 병합 성공 - 종료
    }

    // 2. 직사각형이 안되면 기존 이벤트 로직
    // 직선 매칭 확인 및 OnAdjacentMatchFound 이벤트 발생
    // ... (기존 코드)
}
```

---

### 작동 예시

#### 예시 1: 2×2 정사각형 병합
```
이동 전:        이동 후:              결과:
R . .          R R .                [2×2]
R . .    →     R R .    →           [  R  ]
. . B          . . B
```
→ 4개의 빨간색 1×1 블록 → 하나의 2×2 빨간 블록

#### 예시 2: 1×3 세로 직사각형 병합
```
이동 전:        이동 후:              결과:
B . .          B . .                [1×3]
. . .    →     B . .    →           [ B ]
B . .          B . .
```
→ 3개의 파란색 1×1 블록 → 하나의 1×3 파란 블록

#### 예시 3: 3×2 가로 직사각형 병합
```
이동 전:        이동 후:              결과:
G G .          G G G                [3×2]
. . G    →     G G G    →           [  G  ]
. . .          . . .
```
→ 6개의 녹색 1×1 블록 → 하나의 3×2 녹색 블록

#### 예시 4: L자 형태 (병합 안됨)
```
이동 전:        이동 후:              결과:
G . .          G G .                병합 안됨
. . .    →     G . .    →           (직사각형 아님)
G . .          . . .                이벤트 발생
```
→ 면적 체크 실패: 2×2=4 ≠ 3개 블록

#### 예시 5: 빈 공간 있음 (병합 안됨)
```
이동 전:        이동 후:              결과:
R R .          R R R                병합 안됨
. . .    →     R . R    →           (빈 공간 있음)
R . R          . . .                이벤트 발생
```
→ 중간에 빈 공간 존재

---

### 구현 위치

#### BlockGrid.cs
- **Line 668-698**: `CheckAdjacentBlocks()` - 메인 로직
- **Line 723-784**: `FindRectangle()` - 직사각형 탐색
- **Line 789-833**: `IsRectangle()` - 직사각형 검증
- **Line 838-881**: `MergeBlocksToRectangle()` - 병합 실행

#### Block.cs
- **Line 337**: OnEndDrag에서 `grid.CheckAdjacentBlocks(this)` 호출
- **Line 361**: OnEndDrag에서 `grid.CheckAdjacentBlocks(this)` 호출

---

### 제한사항

#### 병합 조건
- **1×1 블록만 병합**: 이미 병합된 블록(2×1, 1×2 등)은 추가 병합 안됨
- **완전한 직사각형만**: 빈 공간이 하나라도 있으면 병합 안됨
- **같은 색만**: 다른 색 블록이 섞이면 병합 안됨
- **연결된 블록만**: BFS로 연결되지 않은 블록은 제외

#### 성능 고려사항
- BFS 탐색: O(width × height)
- 직사각형 검증: O(블록 개수)
- 매 블록 이동마다 실행

---

## 비교 분석

### 알고리즘 비교

| 요소 | 2048 블록 병합 | Qube Quad 시스템 |
|------|----------------|------------------|
| **탐색 알고리즘** | BFS (Block 객체) | BFS (Vector2Int 좌표) |
| **탐색 시작점** | 이동한 블록 | 전체 그리드 스캔 |
| **색상 조건** | 필수 (같은 색만) | 선택 (현재 무관) |
| **크기 조건** | 1×1만 병합 | 모든 점유 셀 |
| **최소 크기** | 2개 이상 | 2×2 이상 (4셀) |
| **병합 방식** | 즉시 병합 (블록 제거 + 생성) | 지능적 병합 (turnTimer 리셋) |
| **지속성** | 없음 (즉시 종료) | 8턴 추적 |
| **시각 효과** | 없음 | outline + 턴 타이머 |

---

### 사용 사례 비교

#### 2048 블록 병합: 즉시 반응형

**장점**:
- ✓ 직관적인 피드백 (드래그 즉시 병합)
- ✓ 간단한 게임플레이
- ✓ 색상 매칭 전략

**단점**:
- ✗ 1×1 블록만 병합 가능
- ✗ 한 번 병합하면 추가 병합 불가

**적합한 경우**:
- 퍼즐 게임 (색상 매칭)
- 빠른 피드백이 중요한 게임
- 단순한 병합 메커니즘

---

#### Qube Quad 시스템: 턴 기반 전략형

**장점**:
- ✓ 전략적 게임플레이 (8턴 계획)
- ✓ 병합으로 생존 연장 가능
- ✓ 큰 직사각형 형성 유도
- ✓ 면적 기반 점수 (기하급수적 증가)

**단점**:
- ✗ 복잡한 시스템 (turnTimer, 병합 로직)
- ✗ 매 턴 전체 스캔 (성능)

**적합한 경우**:
- 전략 퍼즐 게임
- 턴 기반 게임
- 시간에 따른 위험/보상 메커니즘

---

### 공통 패턴 추출

두 시스템에서 공통으로 사용되는 패턴을 재사용 가능한 유틸리티로 만들 수 있습니다.

#### RectangleDetector 유틸리티 (가상 예시)

```csharp
public static class RectangleDetector
{
    // BFS로 연결된 영역 탐색
    public static List<T> FloodFill<T>(
        T start,
        Func<T, IEnumerable<T>> getNeighbors,
        Func<T, bool> isValid)
    {
        HashSet<T> visited = new HashSet<T>();
        Queue<T> queue = new Queue<T>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            foreach (var neighbor in getNeighbors(current))
            {
                if (!visited.Contains(neighbor) && isValid(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return new List<T>(visited);
    }

    // 직사각형 검증
    public static bool IsRectangle<T>(
        List<T> items,
        Func<T, Vector2Int> getPosition)
    {
        if (items.Count < 2) return false;

        var positions = items.Select(getPosition).ToList();

        int minX = positions.Min(p => p.x);
        int maxX = positions.Max(p => p.x);
        int minY = positions.Min(p => p.y);
        int maxY = positions.Max(p => p.y);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // 면적 일치 + 모든 셀 채움
        return width * height == items.Count;
    }
}
```

**사용 예시 (2048)**:
```csharp
List<Block> connectedBlocks = RectangleDetector.FloodFill(
    startBlock,
    block => GetNeighbors(block),
    neighbor => neighbor.IsSameColor(startBlock) && neighbor.width == 1
);

bool isRect = RectangleDetector.IsRectangle(
    connectedBlocks,
    block => new Vector2Int(block.gridX, block.gridY)
);
```

**사용 예시 (Qube)**:
```csharp
List<Vector2Int> region = RectangleDetector.FloodFill(
    startPos,
    pos => GetAdjacentPositions(pos),
    pos => grid.IsCellOccupied(pos)
);

bool isRect = RectangleDetector.IsRectangle(
    region,
    pos => pos
);
```

---

## 디버그 및 테스트

### 2048 테스트 케이스

- [x] 2×2 정사각형
- [x] 1×N 세로 직사각형 (N=2,3,4...)
- [x] N×1 가로 직사각형 (N=2,3,4...)
- [x] N×M 직사각형 (다양한 크기)
- [x] L자/T자 형태 (병합되지 않아야 함)
- [x] 빈 공간이 있는 형태 (병합되지 않아야 함)
- [x] 다른 색 블록 혼합 (병합되지 않아야 함)
- [x] 이미 병합된 블록 (추가 병합 안됨)

### Qube 테스트 케이스

#### 기본 감지
- [ ] 2×2 정사각형 감지
- [ ] 3×3, 4×4 정사각형 감지
- [ ] 직사각형 (2×3, 3×4 등) 감지
- [ ] 불완전한 직사각형 (구멍) 거부

#### 병합 로직
- [ ] 겹침 병합: 기존 Quad와 겹치면 확장
- [ ] 인접 확장: 기존 Quad 옆에 블록 배치 시 확장 (신규)
- [ ] 다중 Quad 통합: 여러 Quad를 하나로 병합 (신규)
- [ ] 병합으로 turnTimer 리셋
- [ ] 병합 불가 시 기존 Quad 유지

#### 점수 및 소거
- [ ] 정사각형이 직사각형보다 높은 점수 획득
- [ ] 8턴마다 Quad 소거
- [ ] 동시 소거 보너스 적용

#### 시각화
- [ ] outline 및 턴 타이머 표시
- [ ] Quad 확장 시 turnTimer 리셋 표시

---

## 향후 개선 사항

### 2048 블록 병합
1. **다단계 병합**: 큰 블록끼리도 병합 가능하도록
2. **애니메이션**: 병합 시 부드러운 시각 효과
3. **점수 시스템**: 병합 크기에 따른 점수
4. **사운드/파티클 효과**
5. **최대 크기 제한**
6. **특수 블록**: 특정 크기 달성 시 특수 능력

### Qube Quad 시스템
1. **색상 기반 매칭**: 같은 색만 Quad 형성 (난이도 증가)
2. **펄스 타이밍 조정**: 8턴 → 4/6/10턴 등
3. **점수 밸런스**: 정사각형 보너스 증가 (2.0배 등)
4. **소거 애니메이션**: 페이드 아웃, 파티클
5. **병합 효과**: 플래시, 스케일 애니메이션
6. **경고 시스템**: 남은 턴 1-2일 때 색상 변경
7. **게임 오버 조건**: 배치 불가 시 종료

---

## 참고 자료

### 관련 문서
- [QUBE_GUIDE.md](./QUBE_GUIDE.md): Qube 게임 전체 개요
- [CLAUDE.md](./CLAUDE.md): 2048 게임 프로젝트 가이드

### 파일 위치
- **2048 블록 병합**: `Assets/Scripts/BlockGrid.cs`, `Assets/Scripts/Block.cs`
- **Qube Quad 시스템**: `Assets/Scripts/Qube/` (QubeQuadDetector, QubePulseSystem, QubeQuad, QubeCell)

### 알고리즘 참고
- **BFS (Breadth-First Search)**: https://en.wikipedia.org/wiki/Breadth-first_search
- **Flood Fill**: https://en.wikipedia.org/wiki/Flood_fill
- **Bounding Box**: https://en.wikipedia.org/wiki/Minimum_bounding_box

---

**마지막 업데이트**: 2026-01-29
**작성자**: Claude Code
