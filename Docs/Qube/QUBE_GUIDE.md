# Qube 게임 개발 가이드

## 프로젝트 개요

Qube는 블록 배치와 정사각형 소거 메커니즘을 결합한 퍼즐 게임입니다. 플레이어는 테트리스 스타일의 블록을 12x9 그리드에 배치하고, nxn 정사각형을 만들어 소거하며 점수를 획득합니다.

## 핵심 게임 메커니즘

### 1. 블록 배치 시스템
- **입력 방식**:
  - WASD/방향키: 블록 이동
  - Q/E: 반시계/시계 방향 회전 (누르고 있으면 연속 회전)
  - Space: 블록 배치
- **블록 종류**: 테트리스 스타일 (L, I, T, 1x1, S, Z)
- **배치 규칙**:
  - 그리드 범위 내에서만 이동 가능
  - 빈 셀에만 배치 가능 (이미 점유된 셀과 겹치면 배치 불가)

### 2. Quad 감지 및 소거 시스템

#### 새로운 Quad 라이프사이클 시스템
- **Quad 생성**: 2x2 이상의 직사각형이 감지되면 Quad로 등록
- **Quad 추적**: 한 번 생성된 Quad는 독립적으로 추적됨 (turnTimer 보유)
- **Quad 병합**:
  - 새로운 블록 배치 시, 기존 Quad와 합쳐 더 큰 직사각형을 만들 수 있다면 병합
  - 병합 시 turnTimer 리셋 (새 Quad로 간주)
  - 병합 불가능하면 각각 독립적으로 유지
- **Quad 소거**:
  - 각 Quad는 생성 후 **8턴**이 지나면 자동 소거 (테스트용)
  - 여러 Quad가 동시에 소거되면 보너스 점수

#### Quad 감지 알고리즘 (QubeQuadDetector.cs)
현재 구현은 **Flood Fill** 알고리즘 사용:
1. 그리드를 순회하며 점유된 셀 탐색
2. 점유된 셀에서 시작하여 인접한(4방향) 점유 셀을 재귀적으로 탐색
3. 연결된 영역이 유효한 Quad인지 검증

#### Quad 유효성 검증 (QubeQuadDetector.cs:69-95)
현재 조건:
- 최소 4개 셀 (2x2)
- **직사각형 또는 정사각형 형태** (2x3, 3x4 등 모두 허용)
- 영역 내 모든 셀이 채워져 있어야 함

#### 점수 시스템 (QubeQuad.cs:41-55)
**새로운 점수 계산 방식**:
- 기본 점수: `size² × 10` (면적의 제곱에 비례)
- 정사각형 보너스: 1.5배 (width == height)
- 최소 점수: `size × 25`

**예시**:
- 2x2 (size=4): 4² × 10 × 1.5 = 240점
- 2x3 (size=6): 6² × 10 = 360점
- 3x3 (size=9): 9² × 10 × 1.5 = 1,215점
- 2x4 (size=8): 8² × 10 = 640점
- 4x4 (size=16): 16² × 10 × 1.5 = 3,840점

**동시 소거 보너스**:
- 2개 Quad: 1.5배
- 3개 이상 Quad: 2.0배

## 코드 아키텍처

### 핵심 컴포넌트

#### 1. QubeGameManager (Assets/Scripts/Qube/QubeGameManager.cs)
- 게임 전체 흐름 관리
- 블록 생성 및 입력 처리
- 점수 및 UI 업데이트
- 싱글톤 패턴 사용

#### 2. QubeGrid (Assets/Scripts/Qube/QubeGrid.cs)
- 12x9 그리드 관리
- 셀 생성 및 상태 관리
- 좌표 유효성 검증
- 셀 점유 상태 제어

#### 3. QubeBlock (Assets/Scripts/Qube/QubeBlock.cs)
- 블록 이동/회전/배치 로직
- 시각적 렌더링 (UI Image 기반)
- 충돌 감지 (범위 및 점유 검사)

#### 4. QubeQuadDetector (Assets/Scripts/Qube/QubeQuadDetector.cs)
- Flood Fill로 연결된 영역 탐색
- Quad 유효성 검증
- 하이라이트 시각 효과

#### 5. QubePulseSystem (Assets/Scripts/Qube/QubePulseSystem.cs)
- **Quad 라이프사이클 관리**: 생성된 모든 Quad 추적 (trackedQuads)
- **턴별 처리**:
  - 기존 Quad의 turnTimer 증가
  - 새로운 Quad 감지 및 병합 로직
  - turnTimer 4 도달 시 자동 소거
- **병합 로직**: 기존 Quad와 새 영역을 합쳐 더 큰 직사각형이 되는지 검증
- **점수 계산**: 소거 시 점수 이벤트 발생

#### 6. QubeCell (Assets/Scripts/Qube/QubeCell.cs)
- 개별 셀 데이터 (좌표, 점유 상태, 색상)
- **clearTimer**: 소거 후 경과 턴 추적 (8턴 동안 어두운 색 유지)
- UI Image 색상 제어
- **Outline 컴포넌트**: 외곽선 표시/숨김
- **TextMeshProUGUI**: Quad의 남은 턴 수 표시 (흰색 숫자)
- `UpdateClearTimer()`: 매 턴마다 호출되어 clearTimer 감소 및 색상 복구

#### 7. QubeBlockShape (Assets/Scripts/Qube/QubeBlockShape.cs)
- ScriptableObject 기반 블록 정의
- 블록 모양(셀 좌표)과 색상 정의
- **사용되는 블록**: L, I, T, 1x1 (총 4종)

## 현재 구현 상태 및 개선 과제

### ✅ 완료된 기능
- [x] 그리드 시스템 (12x9)
- [x] 블록 이동/회전/배치 (Wall Kick 지원)
- [x] 연속 회전 입력 (Q/E 키 홀드)
- [x] Flood Fill 기반 영역 감지
- [x] **Quad 라이프사이클 시스템**:
  - 각 Quad는 생성 후 8턴간 유지 (테스트용)
  - 독립적인 turnTimer 추적
  - 인접 블록 배치 시 병합 가능
  - 병합 시 turnTimer 리셋
- [x] **직사각형/정사각형 소거** (2x2, 2x3, 3x4 등 모두 인정)
- [x] **Quad 외곽선 표시** (노란색 테두리)
- [x] **턴 타이머 텍스트**: Quad 중앙 셀에 2배 크기로 남은 턴 수 표시
- [x] **소거된 셀 표시**: 소거 후 8턴 동안 더 어두운 색으로 흔적 표시 (테스트용)
- [x] 면적 기반 점수 시스템 (정사각형 1.5배 보너스)
- [x] 동시 소거 보너스 (2개: 1.5배, 3개 이상: 2.0배)

### ⚠️ 개선 필요 사항

#### 1. 색상 기반 영역 감지 (우선순위: 중간)
**파일**: `Assets/Scripts/Qube/QubeQuadDetector.cs:32-67`

**현재 상태**:
- 점유 여부만 확인 (`grid.IsCellOccupied(next)`)
- 색상과 관계없이 모든 점유 셀을 하나의 영역으로 인식

**개선 옵션**:
1. **같은 색상만 묶기**:
   - 더 전략적인 게임플레이 (색상 매칭 필요)
   - 난이도 증가
2. **색상 무관하게 묶기** (현재):
   - 더 쉬운 Quad 형성
   - 초보자 친화적

**구현 예시 (색상 매칭)**:
```csharp
private List<Vector2Int> FloodFill(int startX, int startY, bool[,] visited)
{
    QubeCell startCell = grid.GetCell(startX, startY);
    Color targetColor = startCell.cellColor;

    // ... (기존 코드)

    foreach (var dir in directions)
    {
        Vector2Int next = current + dir;
        QubeCell nextCell = grid.GetCell(next);

        if (grid.IsValidPosition(next) &&
            !visited[next.x, next.y] &&
            grid.IsCellOccupied(next) &&
            ColorsMatch(nextCell.cellColor, targetColor)) // 색상 비교 추가
        {
            visited[next.x, next.y] = true;
            queue.Enqueue(next);
        }
    }
}

private bool ColorsMatch(Color a, Color b, float tolerance = 0.01f)
{
    return Mathf.Abs(a.r - b.r) < tolerance &&
           Mathf.Abs(a.g - b.g) < tolerance &&
           Mathf.Abs(a.b - b.b) < tolerance;
}
```

#### 2. 중력/블록 낙하 시스템 (우선순위: 낮음)
현재는 소거 후 빈 공간이 그대로 남음. 선택적으로 구현 가능:
- 위쪽 블록이 아래로 떨어지는 메커니즘
- 구현 시 `QubePulseSystem.PulseCoroutine` 수정 필요

#### 3. 게임 오버 조건 (우선순위: 중간)
현재 `GameOver()` 메서드가 있지만 호출되지 않음.

**구현 필요 사항**:
- 블록을 배치할 공간이 없을 때 게임 오버 판정
- 새 블록 생성 시 배치 가능 여부 체크

**구현 위치**: `QubeGameManager.SpawnNewBlock()`
```csharp
private void SpawnNewBlock()
{
    // ... (블록 생성 코드)

    if (!currentBlock.CanPlace())
    {
        // 초기 위치에 배치 불가면 게임 오버
        GameOver();
        Destroy(currentBlock.gameObject);
        currentBlock = null;
    }
}
```

#### 4. 블록 시각 크기 불일치 (우선순위: 낮음)
**파일**: `Assets/Scripts/Qube/QubeBlock.cs:66`

현재 블록 셀 크기가 하드코딩됨 (50x50):
```csharp
float blockCellSize = 50f;
```

**권장 수정**:
```csharp
float blockCellSize = grid.cellSize; // 그리드와 동일한 크기 사용
```

## 작업 우선순위 로드맵

### Phase 1: 핵심 로직 개선 (선택)
1. 게임 오버 조건 구현
2. 블록 크기 그리드와 일치시키기
3. 색상 기반 매칭 도입 여부 결정

### Phase 2: 게임플레이 개선 (선택)
4. 중력 시스템 추가 여부 결정
5. 추가 블록 모양 정의 (현재 L, I, T, 1x1 4종 사용)
6. 점수 밸런스 조정 및 테스트

### Phase 3: 시각/피드백 강화 (선택)
7. 소거 애니메이션 추가
8. 파티클 효과 (Quad 소거 시)
9. 사운드 효과 추가

## 테스트 체크리스트

### 기본 동작 테스트
- [ ] 블록이 정상적으로 이동/회전/배치되는가?
- [ ] 그리드 경계를 벗어나지 않는가?
- [ ] 점유된 셀에는 배치가 불가능한가?

### Quad 감지 테스트
- [ ] 2x2 정사각형이 정상 감지되는가?
- [ ] 3x3, 4x4도 정상 감지되는가?
- [ ] 직사각형(2x3, 3x4 등)도 정상 감지되는가?
- [ ] 불완전한 직사각형(구멍 있음)은 감지되지 않는가?
- [ ] 정사각형이 직사각형보다 높은 점수를 받는가?

### 펄스 시스템 테스트
- [ ] 8턴마다 펄스가 발동하는가? (테스트용)
- [ ] 소거 후 턴 카운터가 리셋되는가?
- [ ] 점수가 올바르게 계산되는가?
- [ ] 동시 소거 보너스가 적용되는가?

### 게임 오버 테스트
- [ ] 배치할 공간이 없을 때 게임 오버가 발생하는가?
- [ ] 게임 오버 후 입력이 차단되는가?

## 참고 사항

### Unity UI 좌표계
- Qube는 UI Canvas 기반 (2048과 달리)
- RectTransform.anchoredPosition 사용
- 중앙 앵커 기준 좌표 계산

### 색상 정보
- **빈 셀**: #2D3436 (어두운 회색)
- **소거된 셀 (8턴 동안)**: #141A1C (더 어두운 회색) - clearTimer로 관리 (테스트용)
- 일반 블록: 원본 색상 그대로 유지 (QubeBlockShape에서 정의)
- **Quad 블록**: 원본 색상 × 1.3 (밝게 표시)
- **Quad 외곽선**: 노란색 (Color.yellow), 두께 3px
- **턴 타이머 텍스트**: 흰색 (Color.white), Quad 중앙 셀만 폰트 크기 72 (2배), Bold

### 성능 고려사항
- Flood Fill은 O(n) 시간 복잡도 (n = 그리드 셀 수)
- 12x9 = 108셀이므로 성능 문제 없음
- 매 턴마다 전체 그리드 스캔 (최적화 불필요)

## 최신 기능: Quad 라이프사이클 & 병합 시스템

### 구현된 기능

#### 1. Quad 생성 및 추적
- 블록 배치 시 2x2 이상 직사각형 자동 감지
- 각 Quad는 생성 턴과 turnTimer 보유
- **노란색 외곽선** + 밝은 색상으로 표시

#### 2. Quad 병합 메커니즘
**시나리오 예시 (8턴 시스템)**:
```
턴 1: [■][■]     ← 2x2 Quad 생성 (turnTimer = 0, 남은 턴: 8)
      [■][■]

턴 2: [■][■][■]  ← 새 블록 배치
      [■][■][ ]

      → 2x3 Quad로 병합! (turnTimer 리셋 → 0, 남은 턴: 8)
```

**병합 조건**:
- 기존 Quad와 새로운 영역이 겹침
- 합쳤을 때 완벽한 직사각형 형성
- 병합 시 turnTimer 리셋 (새 Quad로 간주)

**병합 불가 시**:
- 구멍이 생기거나 L자형 등 직사각형이 아닌 경우
- 기존 Quad는 그대로 유지 (turnTimer 계속 증가)

#### 3. 8턴 후 자동 소거 (테스트용)
- 각 Quad는 **생성 후 8턴**이 지나면 소거
- 여러 Quad가 동시 소거 시 보너스 점수
- 병합된 Quad는 turnTimer가 리셋되어 8턴 연장

### 시각적 피드백
- **일반 블록**: 원본 색상 그대로 유지
- **빈 셀**: 어두운 회색 (#2D3436)
- **소거된 셀 (8턴 동안)**: 더 어두운 회색 (#141A1C) - 소거 흔적 표시 (테스트용)
- **Quad (turnTimer < 8)**:
  - 노란색 외곽선 + 밝은 색상 (1.3배)
  - **흰색 숫자**: Quad 중앙 셀에만 **2배 크기(72pt)**로 남은 턴 수 표시 (8 → 7 → 6 → ... → 1) (테스트용)
- **UI 표시**: "Turn: N | Quads: M" (전체 턴 수 | 활성 Quad 수)

### 커스터마이징

#### 외곽선 설정 (QubeCell.cs:28-30)
```csharp
outline.effectColor = Color.yellow;  // 색상 변경 가능
outline.effectDistance = new Vector2(3, 3);  // 두께 조정 가능
```

#### 턴 타이머 텍스트 설정 (QubeCell.cs:52-57, 148)
```csharp
// 기본 설정 (Awake에서)
turnTimerText.fontSize = 36;  // 기본 폰트 크기
turnTimerText.fontStyle = FontStyles.Bold;  // 굵기
turnTimerText.alignment = TextAlignmentOptions.Center;  // 정렬
turnTimerText.color = Color.white;  // 색상 (흰색)

// SetTurnTimer에서 동적 크기 조정
turnTimerText.fontSize = isCenter ? 72 : 36;  // 중앙: 72pt, 기타: 36pt
```

## 다음 작업 제안

현재 시스템은 **직사각형/정사각형 모두 소거 가능**하며, 정사각형에 1.5배 보너스가 적용됩니다.

권장 작업 순서:
1. Unity에서 테스트: 다양한 크기의 직사각형/정사각형이 올바르게 감지되고 외곽선이 표시되는지 확인
2. 게임 오버 조건 구현 (블록 배치 불가 시)
3. 블록 시각 크기를 그리드와 일치시키기 (선택)
4. 색상 매칭 도입 여부 결정
5. 외곽선 색상/효과 커스터마이징 (선택)

**점수 밸런스 참고**:
- 2x2 정사각형: 240점
- 2x3 직사각형: 360점 (더 큼)
- 3x3 정사각형: 1,215점
- 2x4 직사각형: 640점

현재 점수 시스템은 **면적이 클수록 기하급수적으로 높은 점수**를 주므로, 큰 직사각형이 작은 정사각형보다 유리할 수 있습니다.

추가 질문이나 특정 기능 구현이 필요하시면 말씀해주세요!
