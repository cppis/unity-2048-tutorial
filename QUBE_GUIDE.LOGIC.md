# 직사각형 감지 및 병합 로직 통합 가이드

이 문서는 프로젝트의 두 게임(2048, Qube)에서 사용되는 직사각형 감지 및 병합 시스템을 설명합니다.

## 목차
- [개요](#개요)
- [Quad 라이프사이클 시스템](#quad-라이프사이클-시스템)
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

## Quad 라이프사이클 시스템

블록 배치 후 2×2 이상의 직사각형 영역을 감지하여 8턴간 추적하고, 만료 시 자동 소거하는 시스템입니다.

### 실행 시점 및 흐름

**트리거**: 블록 배치 (Space 키) → `QubePulseSystem.IncrementTurn()`

**전체 흐름**:
```
블록 배치 (Space 키)
    ↓
QubePulseSystem.IncrementTurn()
    ↓
QubeQuadDetector.DetectQuads()  ← Flood Fill로 영역 감지
    ↓
QubePulseSystem.ProcessNewQuad()  ← 병합 로직
    ↓
QubeQuadDetector.HighlightQuads()  ← 시각화
```

---

### 핵심 알고리즘

#### 1. DetectQuads (QubeQuadDetector.cs:8-45)

전체 그리드를 스캔하여 직사각형 영역을 감지합니다.

```csharp
public List<QubeQuad> DetectQuads()
{
    List<QubeQuad> quads = new List<QubeQuad>();
    bool[,] visited = new bool[QubeGrid.WIDTH, QubeGrid.HEIGHT];

    // 전체 그리드 순회 (12×9 = 108셀)
    for (int x = 0; x < QubeGrid.WIDTH; x++)
    {
        for (int y = 0; y < QubeGrid.HEIGHT; y++)
        {
            // 조건: 미방문 + 블록 있음
            if (!visited[x, y] && grid.IsCellOccupied(new Vector2Int(x, y)))
            {
                // Flood Fill로 연결된 영역 탐색
                List<Vector2Int> region = FloodFill(x, y, visited);

                // 유효한 Quad인지 검증
                if (IsValidQuad(region))
                {
                    QubeQuad quad = new QubeQuad(region, 0);
                    quads.Add(quad);
                }
            }
        }
    }

    return quads;
}
```

**특징**:
- **색상 무관**: 현재는 점유 여부만 확인 (색상 매칭은 선택 사항)
- **전체 스캔**: 매 턴마다 그리드 전체 탐색
- **visited 배열**: 중복 탐색 방지

---

#### 2. FloodFill (QubeQuadDetector.cs:47-82)

BFS로 연결된 영역을 탐색합니다.

```csharp
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

        // 4방향 탐색 (상하좌우)
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // 위
            new Vector2Int(0, -1),  // 아래
            new Vector2Int(1, 0),   // 오른쪽
            new Vector2Int(-1, 0)   // 왼쪽
        };

        foreach (var dir in directions)
        {
            Vector2Int next = current + dir;

            if (grid.IsValidPosition(next) &&      // 그리드 범위 내
                !visited[next.x, next.y] &&        // 미방문
                grid.IsCellOccupied(next))         // 블록 존재
            {
                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }
    }

    return region;
}
```

**2048과의 차이점**:
| 요소 | 2048 | Qube |
|------|------|------|
| 탐색 대상 | Block 객체 | Vector2Int 좌표 |
| 색상 체크 | 필수 (같은 색만) | 선택 (현재 무관) |
| 크기 제한 | 1×1만 | 모든 점유 셀 |

---

#### 3. IsValidQuad (QubeQuadDetector.cs:84-126)

영역이 유효한 Quad인지 검증합니다.

```csharp
private bool IsValidQuad(List<Vector2Int> region)
{
    // 조건 1: 최소 4개 셀 (2×2)
    if (region.Count < 4)
        return false;

    // 조건 2: 경계 박스 계산
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

    // 조건 3: 최소 2×2 이상
    if (width < 2 || height < 2)
        return false;

    // 조건 4: 완전한 직사각형 (구멍 없음)
    int expectedCells = width * height;
    if (region.Count != expectedCells)
        return false;

    return true;
}
```

**검증 조건**:
| 조건 | 설명 | 예시 |
|------|------|------|
| 최소 4개 셀 | 2×2 이상만 허용 | 1×3(3셀) ✗, 2×2(4셀) ✓ |
| 최소 2×2 크기 | width ≥ 2 && height ≥ 2 | 1×5 ✗, 2×3 ✓ |
| 직사각형 형태 | region.Count == width × height | L자형 ✗, 직사각형 ✓ |

---

### Quad 라이프사이클 관리

#### QubeQuad 데이터 구조 (QubeQuad.cs:4-121)

```csharp
public class QubeQuad
{
    public List<Vector2Int> cells;      // 구성 셀 좌표 목록
    public int minX, maxX, minY, maxY;  // 경계 박스
    public int width, height;           // 크기
    public int size;                    // width × height
    public int turnTimer;               // 생성 후 경과 턴 (8 도달 시 소거)
    public int creationTurn;            // 생성된 턴 번호
}
```

---

#### IncrementTurn (QubePulseSystem.cs:22-68)

매 턴마다 실행되는 메인 로직입니다.

```csharp
public void IncrementTurn()
{
    globalTurnCounter++;

    // 0. clearTimer 감소 (소거된 셀 복구)
    for (int x = 0; x < QubeGrid.WIDTH; x++)
        for (int y = 0; y < QubeGrid.HEIGHT; y++)
            grid.GetCell(x, y)?.UpdateClearTimer();

    // 1. 기존 Quad들의 turnTimer 증가
    foreach (var quad in trackedQuads)
        quad.turnTimer++;

    // 2. 새로운 영역 감지
    List<QubeQuad> detectedQuads = quadDetector.DetectQuads();

    // 3. 병합 또는 추가
    foreach (var newQuad in detectedQuads)
        ProcessNewQuad(newQuad);

    // 4. turnTimer ≥ 8인 Quad 소거
    List<QubeQuad> quadsToRemove =
        trackedQuads.Where(q => q.turnTimer >= PULSE_INTERVAL).ToList();

    if (quadsToRemove.Count > 0)
        StartCoroutine(RemoveQuads(quadsToRemove));

    // 5. 하이라이트 업데이트
    quadDetector.HighlightQuads(trackedQuads, PULSE_INTERVAL);
}
```

**단계별 설명**:

| 단계 | 작업 | 목적 |
|------|------|------|
| 0 | clearTimer 감소 | 소거된 셀 시각 효과 복구 (8턴 후) |
| 1 | turnTimer 증가 | 생존 시간 추적 |
| 2 | 새 영역 감지 | 블록 배치로 생긴 직사각형 탐색 |
| 3 | 병합 또는 추가 | 기존 Quad와 합칠지 결정 |
| 4 | 만료 Quad 소거 | turnTimer ≥ 8인 Quad 제거 + 점수 |
| 5 | 하이라이트 업데이트 | 현재 활성 Quad 시각화 |

---

#### ProcessNewQuad (QubePulseSystem.cs:70-127)

새로 감지된 영역을 처리합니다. **겹치거나 인접한** Quad와 병합을 시도합니다.

```csharp
private void ProcessNewQuad(QubeQuad newQuad)
{
    // 1. 기존 Quad들과 겹치거나 인접한지 확인
    List<QubeQuad> relatedQuads = new List<QubeQuad>();

    foreach (var existingQuad in trackedQuads)
    {
        // 겹치거나 인접한 Quad 찾기
        if (newQuad.OverlapsWith(existingQuad) || newQuad.IsAdjacentTo(existingQuad))
        {
            relatedQuads.Add(existingQuad);
        }
    }

    if (relatedQuads.Count == 0)
    {
        // 2-A. 완전히 새로운 Quad (겹치지도 인접하지도 않음)
        newQuad.creationTurn = globalTurnCounter;
        trackedQuads.Add(newQuad);
        Debug.Log($"→ New Quad added: {newQuad.width}x{newQuad.height}");
    }
    else
    {
        // 2-B. 겹치거나 인접한 Quad가 있음 → 병합 시도
        QubeQuad mergedQuad = newQuad;
        bool canMergeAll = true;

        // 모든 관련 Quad들을 하나로 병합 시도
        foreach (var quad in relatedQuads)
        {
            if (!mergedQuad.CanMergeWith(quad))
            {
                canMergeAll = false;
                break;
            }
            mergedQuad = mergedQuad.MergeWith(quad, globalTurnCounter);
        }

        if (canMergeAll && mergedQuad.size > newQuad.size)
        {
            // 3-A. 병합 성공 → 기존 제거 + 새 Quad 추가 (turnTimer 리셋)
            foreach (var quad in relatedQuads)
            {
                trackedQuads.Remove(quad);
                Debug.Log($"→ Removed old Quad: {quad.width}x{quad.height}");
            }

            trackedQuads.Add(mergedQuad);
            Debug.Log($"→ Merged into larger Quad: {mergedQuad.width}x{mergedQuad.height} (turnTimer reset)");
        }
        else
        {
            // 3-B. 병합 불가 → 기존 Quad 유지
            Debug.Log($"→ Cannot merge - keeping existing Quads");
        }
    }
}
```

**핵심 개선사항**:
- `OverlapsWith` 뿐만 아니라 `IsAdjacentTo`도 확인
- 겹치지 않고 인접한 블록들도 병합 대상에 포함
- 더 큰 직사각형을 만들 수 있으면 자동으로 확장

---

### 병합 메커니즘

#### OverlapsWith & IsAdjacentTo - 관계 확인 메서드

**OverlapsWith** (QubeQuad.cs:76-84) - 겹침 확인:
```csharp
public bool OverlapsWith(QubeQuad other)
{
    foreach (var cell in cells)
    {
        if (other.Contains(cell))
            return true;
    }
    return false;
}
```

**IsAdjacentTo** (QubeQuad.cs:87-109) - 인접 확인:
```csharp
public bool IsAdjacentTo(QubeQuad other)
{
    // 4방향 (상하좌우)
    Vector2Int[] directions = {
        new Vector2Int(0, 1),   // 위
        new Vector2Int(0, -1),  // 아래
        new Vector2Int(1, 0),   // 오른쪽
        new Vector2Int(-1, 0)   // 왼쪽
    };

    // 이 Quad의 각 셀에 대해 인접 셀이 다른 Quad에 포함되는지 확인
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
```

**용도**:
- `OverlapsWith`: 두 Quad가 최소 1개 이상의 셀을 공유하는지 확인
- `IsAdjacentTo`: 두 Quad가 상하좌우로 맞닿아 있는지 확인 (겹치지는 않음)

---

#### CanMergeWith (QubeQuad.cs:112-133)

```csharp
public bool CanMergeWith(QubeQuad other)
{
    // 1. 두 Quad의 모든 셀 합치기
    HashSet<Vector2Int> mergedCells = new HashSet<Vector2Int>(cells);
    foreach (var cell in other.cells)
        mergedCells.Add(cell);

    // 2. 합친 영역의 경계 계산
    int newMinX = Mathf.Min(minX, other.minX);
    int newMaxX = Mathf.Max(maxX, other.maxX);
    int newMinY = Mathf.Min(minY, other.minY);
    int newMaxY = Mathf.Max(maxY, other.maxY);

    int newWidth = newMaxX - newMinX + 1;
    int newHeight = newMaxY - newMinY + 1;

    // 3. 직사각형인지 확인
    int expectedCells = newWidth * newHeight;
    return mergedCells.Count == expectedCells;  // 구멍 없으면 true
}
```

**병합 시나리오**:

**시나리오 1: 겹침 병합** (기존)
```
턴 1: [■][■]     → Quad A 생성 (2×2, turnTimer=0)
      [■][■]

턴 2: [■][■][■]  → 새 블록 배치
      [■][■][ ]

      감지: 2×3 (6셀)
      겹침: Quad A (2×2)
      병합 가능: Yes (완전한 2×3 직사각형)
      → Quad A 제거, Quad B(2×3, turnTimer=0) 추가 (8턴 연장!)
```

**시나리오 2: 인접 확장** (신규)
```
턴 1: [■][■][ ]  → Quad A 생성 (2×2, turnTimer=0)
      [■][■][ ]

턴 2: [ ][ ][■]  → 새 블록 배치 (인접)
      [ ][ ][■]

      감지: 새 영역 2×1 (2셀)
      겹침: No
      인접: Yes (Quad A 오른쪽에 맞닿음)
      병합 가능: Yes (2+2=4셀, 완전한 2×3 직사각형)
      → Quad A 제거, 새 Quad(2×3, turnTimer=0) 추가 (8턴 연장!)
```

**시나리오 3: 다중 Quad 통합**
```
턴 1: [■][■][ ]  → Quad A 생성 (2×2, turnTimer=2)
      [■][■][ ]

턴 2: [ ][ ][■]  → Quad B 생성 (2×1, turnTimer=0)
      [ ][ ][■]

턴 3: [ ][■][ ]  → 새 블록 배치 (중간 연결)
      [ ][■][ ]

      감지: 2×1 (2셀) - 중간 블록
      인접: Quad A (왼쪽), Quad B (오른쪽)
      병합 가능: Yes (A+중간+B = 완전한 2×3)
      → Quad A, B 제거, 새 Quad(2×3, turnTimer=0) 추가!
```

---

### 점수 시스템

#### GetScore (QubeQuad.cs:43-57)

```csharp
public int GetScore()
{
    // 기본 점수: size² × 10 (면적의 제곱)
    int baseScore = size * size * 10;

    // 정사각형 보너스 (1.5배)
    if (width == height)
    {
        baseScore = Mathf.RoundToInt(baseScore * 1.5f);
    }

    // 최소 점수 보장
    return Mathf.Max(baseScore, size * 25);
}
```

**점수 표**:

| 크기 | 타입 | 계산식 | 점수 |
|------|------|--------|------|
| 2×2 | 정사각형 | 4² × 10 × 1.5 | **240점** |
| 2×3 | 직사각형 | 6² × 10 | **360점** |
| 2×4 | 직사각형 | 8² × 10 | **640점** |
| 3×3 | 정사각형 | 9² × 10 × 1.5 | **1,215점** |
| 3×4 | 직사각형 | 12² × 10 | **1,440점** |
| 4×4 | 정사각형 | 16² × 10 × 1.5 | **3,840점** |

**동시 소거 보너스**:
- 2개 Quad: ×1.5
- 3개 이상 Quad: ×2.0

---

### 시각화 시스템

#### HighlightQuads (QubeQuadDetector.cs:128-188)

```csharp
public void HighlightQuads(List<QubeQuad> quads, int pulseInterval = 4)
{
    // 1. 모든 셀 원상복구
    for (int x = 0; x < QubeGrid.WIDTH; x++)
    {
        for (int y = 0; y < QubeGrid.HEIGHT; y++)
        {
            QubeCell cell = grid.GetCell(x, y);
            if (cell != null && cell.isOccupied)
            {
                cell.SetColor(cell.originalColor);
                cell.SetOutline(false);
                cell.SetTurnTimer(-1);
            }
        }
    }

    // 2. Quad 셀 하이라이트
    foreach (var quad in quads)
    {
        int remainingTurns = pulseInterval - quad.turnTimer;
        Vector2Int centerCell = quad.GetCenter();

        foreach (var cellPos in quad.cells)
        {
            QubeCell cell = grid.GetCell(cellPos);
            if (cell != null)
            {
                // 밝게 표시 (원본 × 1.3)
                Color highlightColor = cell.originalColor * 1.3f;
                cell.SetColor(highlightColor);

                // 노란색 outline
                cell.SetOutline(true, Color.yellow);

                // 중앙 셀만 턴 수 표시
                bool isCenter = (cellPos == centerCell);
                cell.SetTurnTimer(isCenter ? remainingTurns : -1, isCenter);
            }
        }
    }
}
```

**시각적 요소**:

| 상태 | 색상 | outline | 텍스트 |
|------|------|---------|--------|
| 일반 블록 | 원본 | 없음 | 없음 |
| Quad 블록 | 원본×1.3 | 노란색 3px | 중앙 셀만 턴 수 |
| 빈 셀 | #2D3436 | 없음 | 없음 |
| 소거된 셀 | #141A1C (8턴) | 없음 | 없음 |

---

### 구현 위치

#### Qube 폴더 (Assets/Scripts/Qube/)
- **QubeQuadDetector.cs:8-45**: `DetectQuads()` - 영역 감지
- **QubeQuadDetector.cs:47-82**: `FloodFill()` - BFS 탐색
- **QubeQuadDetector.cs:84-126**: `IsValidQuad()` - 검증
- **QubeQuadDetector.cs:128-188**: `HighlightQuads()` - 시각화
- **QubePulseSystem.cs:22-68**: `IncrementTurn()` - 턴 처리
- **QubePulseSystem.cs:70-127**: `ProcessNewQuad()` - 병합 로직 (겹침 + 인접 검사)
- **QubePulseSystem.cs:129-160**: `RemoveQuads()` - 소거 처리
- **QubeQuad.cs:43-57**: `GetScore()` - 점수 계산
- **QubeQuad.cs:76-84**: `OverlapsWith()` - 겹침 확인
- **QubeQuad.cs:87-109**: `IsAdjacentTo()` - 인접 확인 (신규)
- **QubeQuad.cs:112-133**: `CanMergeWith()` - 병합 가능 여부
- **QubeQuad.cs:136-145**: `MergeWith()` - 병합 실행

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
