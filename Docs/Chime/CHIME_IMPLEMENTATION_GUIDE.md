# Chime 구현 가이드
**GDD 기반 전체 기능 구현 로드맵**

**Unity 6000.3.2f1**
**최종 업데이트: 2026.02.04**

---

## 목차

1. [개요](#개요)
2. [현재 구현 상태](#현재-구현-상태)
3. [구현 로드맵](#구현-로드맵)
4. [Phase 1: 핵심 메커니즘](#phase-1-핵심-메커니즘)
5. [Phase 2: 프래그먼트 & 커버리지](#phase-2-프래그먼트--커버리지)
6. [Phase 3: 게임 모드](#phase-3-게임-모드)
7. [Phase 4: 비주얼 & 오디오](#phase-4-비주얼--오디오)
8. [Phase 5: 폴리싱](#phase-5-폴리싱)
9. [테스트 가이드](#테스트-가이드)
10. [참조](#참조)

---

## 개요

이 가이드는 **Chime GDD**의 모든 기능을 Unity에서 구현하기 위한 단계별 로드맵입니다.

### 프로젝트 상태

| 항목 | 상태 |
|------|------|
| **프로토타입 이름** | Qube |
| **목표** | Chime (GDD 기반) |
| **현재 진행도** | ~30% (핵심 메커니즘만) |
| **예상 개발 기간** | 20-26주 |

### 빠른 참조: 구현 체크리스트

| 카테고리 | 완료 | 총 | 진행률 |
|---------|------|-----|--------|
| 핵심 메커니즘 | 5 | 8 | 62% |
| 프래그먼트 시스템 | 0 | 6 | 0% |
| 커버리지 시스템 | 0 | 5 | 0% |
| 게임 모드 | 0 | 5 | 0% |
| 애니메이션 | 1 | 10 | 10% |
| 효과음/BGM | 0 | 12 | 0% |
| UI/UX | 2 | 8 | 25% |
| **전체** | **8** | **54** | **15%** |

---

## 현재 구현 상태

### ✅ 구현 완료

#### 1. 턴 기반 시스템
- **파일**: `ChimePulseSystem.cs`
- **기능**: 피스 배치 = 1턴, 쿼드 타이머 감소
- **상태**: ✅ 완료 (타이머 간격만 8→4 수정 필요)

#### 2. 쿼드 감지 시스템
- **파일**: `ChimeQuadDetector.cs`
- **기능**: Flood Fill 알고리즘, 블록 ID 존중
- **상태**: ⚠️ 부분 완료 (최소 크기 2×2→3×3 수정 필요)

#### 3. 그리드 시스템
- **파일**: `ChimeGrid.cs`
- **기능**: 12×9 그리드, GridLayoutGroup
- **상태**: ✅ 완료

#### 4. 블록 시스템
- **파일**: `ChimeBlock.cs`, `ChimeBlockShape.cs`
- **기능**: 이동, 회전, 벽 킥
- **상태**: ⚠️ 부분 완료 (4종→12종 펜토미노 필요, 시각 피드백 필요)

#### 5. 게임 매니저
- **파일**: `ChimeGameManager.cs`
- **기능**: 싱글톤, 블록 생성, 게임 오버
- **상태**: ⚠️ 부분 완료 (게임 모드 시스템 필요)

### ❌ 미구현 기능 (GDD 요구사항)

#### 핵심 메커니즘
1. ❌ **펜토미노 12종** (현재 4종만)
2. ❌ **배치 검증 색상** (흰색/빨강 아웃라인)
3. ❌ **3×3 최소 쿼드** (현재 2×2)
4. ❌ **4턴 타이머** (현재 8턴)

#### 프래그먼트 시스템
1. ❌ **프래그먼트 생성** (쿼드 파쇄 시)
2. ❌ **5턴 생명주기** (색상 변화)
3. ❌ **소멸 경고** (깜빡임)
4. ❌ **전체 소멸** (5턴 후)
5. ❌ **콤보 리셋** (소멸 시)
6. ❌ **퍼펙트 쿼드 감지** (프래그먼트 0개)

#### 커버리지 시스템
1. ❌ **커버리지 추적** (쿼드 영역 칠하기)
2. ❌ **진행도 UI** (0-100%)
3. ❌ **90% 클리어** 조건
4. ❌ **100% 보너스** 점수
5. ❌ **섹션 진입** 감지 (25%, 50%, 75%, 90%)

#### 점수 시스템
1. ❌ **콤보 멀티플라이어** (x1.0 ~ x3.0)
2. ❌ **새 커버리지 점수** (중복 쿼드는 소량)
3. ❌ **퍼펙트 쿼드 보너스** (+500)
4. ❌ **큰 쿼드 보너스** (4×4+: +1000)
5. ❌ **클리어 보너스** (90%: +5000, 100%: +10000)

#### 게임 모드
1. ❌ **Practice 모드** (타이머 없음, 프래그먼트 영구)
2. ❌ **Standard 모드** (타이머, 타임 보너스)
3. ❌ **Sharp 모드** (라이프 시스템)
4. ❌ **Strike 모드** (90초 고정)
5. ❌ **Challenge 모드** (복잡한 그리드)

#### 애니메이션
1. ❌ **피스 배치 애니메이션** (0.15초 채우기)
2. ❌ **배치 불가능 흔들림** (0.2초)
3. ❌ **쿼드 형성 펄스** (0.3초)
4. ❌ **쿼드 파쇄 효과** (0.4초)
5. ❌ **커버리지 웨이브** (0.5초)
6. ❌ **프래그먼트 소멸** (0.6초 페이드)

#### 효과음 & BGM
1. ❌ **12가지 효과음** (이동, 회전, 배치, 쿼드 등)
2. ❌ **5개 레벨 BGM** (Chiptune, Electronic 등)

#### UI/UX
1. ❌ **메인 메뉴**
2. ❌ **모드 선택**
3. ❌ **인게임 HUD** (턴, 타이머, 콤보 등)
4. ❌ **클리어/게임오버 화면**
5. ❌ **업적 시스템**

---

## 구현 로드맵

### Phase 1: 핵심 메커니즘 (3주)
**목표**: GDD 핵심 메커니즘 정렬

- [x] 그리드 시스템
- [ ] 12개 펜토미노 블록
- [ ] 배치 검증 색상 (흰색/빨강)
- [ ] 3×3 최소 쿼드
- [ ] 4턴 타이머
- [ ] 쿼드 하이라이트
- [ ] 기본 점수 시스템

### Phase 2: 프래그먼트 & 커버리지 (3주)
**목표**: 진행 시스템 구현

- [ ] 프래그먼트 생성 로직
- [ ] 5턴 생명주기
- [ ] 커버리지 추적
- [ ] 진행도 UI
- [ ] 퍼펙트 쿼드 감지
- [ ] 콤보 멀티플라이어

### Phase 3: 게임 모드 (8주)
**목표**: 5가지 게임 모드 구현

- [ ] Practice 모드 (1주)
- [ ] Standard 모드 (2주)
- [ ] Sharp 모드 (2주)
- [ ] Strike 모드 (1주)
- [ ] Challenge 모드 (2주)

### Phase 4: 비주얼 & 오디오 (6주)
**목표**: 애니메이션, 효과음, BGM

- [ ] 피스 배치 애니메이션 (1주)
- [ ] 쿼드 파쇄 애니메이션 (1주)
- [ ] 효과음 통합 (2주)
- [ ] BGM 작곡/통합 (2주)

### Phase 5: 폴리싱 (6주)
**목표**: UI/UX, 레벨 시스템, 업적

- [ ] 메인 메뉴 (1주)
- [ ] 인게임 HUD (1주)
- [ ] 레벨 진행 (2주)
- [ ] 업적 시스템 (1주)
- [ ] QA & 밸런싱 (1주)

**총 기간**: 약 26주 (6.5개월)

---

## Phase 1: 핵심 메커니즘

### 1.1 펜토미노 12종 구현

#### 목표
- GDD 3.1에 정의된 12개 펜토미노 블록 생성
- 각 5칸, 고유한 색상

#### 구현 단계

**Step 1: ChimeBlockShape.cs에 정적 메서드 추가**

파일: `Assets/Scripts/Chime/ChimeBlockShape.cs`

```csharp
// F-Pentomino
public static Vector2Int[] GetFShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(1, 0), new Vector2Int(2, 0),
        new Vector2Int(1, 2)
    };
}

// I-Pentomino
public static Vector2Int[] GetIShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(0, 3),
        new Vector2Int(0, 4)
    };
}

// L-Pentomino
public static Vector2Int[] GetLShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(0, 3),
        new Vector2Int(1, 0)
    };
}

// N-Pentomino
public static Vector2Int[] GetNShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0), new Vector2Int(1, 1),
        new Vector2Int(2, 1), new Vector2Int(2, 2)
    };
}

// P-Pentomino
public static Vector2Int[] GetPShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(0, 2)
    };
}

// T-Pentomino
public static Vector2Int[] GetTShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
        new Vector2Int(1, 1),
        new Vector2Int(1, 2)
    };
}

// U-Pentomino
public static Vector2Int[] GetUShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(2, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
    };
}

// V-Pentomino
public static Vector2Int[] GetVShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
    };
}

// W-Pentomino
public static Vector2Int[] GetWShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0), new Vector2Int(1, 1),
        new Vector2Int(2, 1), new Vector2Int(2, 2)
    };
}

// X-Pentomino
public static Vector2Int[] GetXShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
        new Vector2Int(1, 2)
    };
}

// Y-Pentomino
public static Vector2Int[] GetYShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0), new Vector2Int(1, 1),
        new Vector2Int(1, 2),
        new Vector2Int(1, 3)
    };
}

// Z-Pentomino
public static Vector2Int[] GetZShape()
{
    return new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(1, 2), new Vector2Int(2, 2)
    };
}
```

**Step 2: Unity Editor에서 ScriptableObject 생성**

1. Project 창에서 `Assets/Data/BlockShapes` 폴더 생성
2. 각 펜토미노별로 ScriptableObject 생성:
   - 우클릭 → Create → Chime → Block Shape
   - 이름: `BlockShape_F`, `BlockShape_I`, ... `BlockShape_Z`

3. 각 에셋에 데이터 입력:

```
BlockShape_F:
  - Block Name: "F-Pentomino"
  - Cells: Size = 5
    - Element 0: (0, 1)
    - Element 1: (1, 1)
    - Element 2: (1, 0)
    - Element 3: (2, 0)
    - Element 4: (1, 2)
  - Block Color: Cyan (#00D9FF)
```

**권장 색상 (GDD 8.1)**:
- F: Cyan #00D9FF
- I: Purple #A855F7
- L: Pink #EC4899
- N: Green #10B981
- P: Yellow #F59E0B
- T: Blue #3B82F6
- U: Orange #F97316
- V: Red #EF4444
- W: Lime #84CC16
- X: Indigo #6366F1
- Y: Teal #14B8A6
- Z: Rose #F43F5E

**Step 3: GameManager에 할당**

1. Hierarchy에서 `GameManager` 선택
2. `ChimeGameManager` 컴포넌트의 `Block Shapes` 배열 크기를 12로 설정
3. 12개 ScriptableObject를 배열에 드래그

**Step 4: 랜덤 생성 로직 업데이트**

파일: `ChimeGameManager.cs:84`

```csharp
// 기존 코드 (4종만 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, Mathf.Min(4, blockShapes.Length))];

// 수정 코드 (전체 12종 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, blockShapes.Length)];
```

---

### 1.2 배치 검증 색상 구현

#### 목표
- 배치 가능: 흰색 아웃라인 (#FFFFFF, 50% 투명)
- 배치 불가능: 빨강 아웃라인 (#FF4444, 70% 투명)

#### 구현

**파일**: `ChimeBlock.cs:285`

```csharp
public void UpdatePlacementVisualFeedback()
{
    bool canPlace = CanPlace();

    Color visualColor;
    if (canPlace)
    {
        // 배치 가능: 흰색 아웃라인, 50% 투명 (GDD 5.1)
        visualColor = new Color(1f, 1f, 1f, 0.5f);
    }
    else
    {
        // 배치 불가능: 빨강 아웃라인, 30% 불투명 (GDD: 70% 투명 = 30% 불투명)
        visualColor = new Color(1f, 0.27f, 0.27f, 0.3f);
    }

    // 모든 셀 오브젝트에 색상 적용
    foreach (var cellObj in cellObjects)
    {
        Image image = cellObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = visualColor;
        }
    }
}
```

**호출 위치 추가**:

```csharp
// ChimeBlock.cs:182 (Move 메서드 끝)
public bool Move(Vector2Int direction)
{
    // ... 기존 코드 ...
    UpdateVisuals();
    UpdatePlacementVisualFeedback(); // ← 추가
    return true;
}

// ChimeBlock.cs:192 (Rotate 메서드 끝)
public bool Rotate(bool clockwise)
{
    // ... 기존 코드 ...
    CreateVisuals();
    UpdatePlacementVisualFeedback(); // ← 추가
    return true;
}
```

---

### 1.3 쿼드 최소 크기 3×3 변경

#### 목표
- 현재: 2×2 (4칸) 쿼드 감지
- 변경: 3×3 (9칸) 최소 쿼드

#### 구현

**파일**: `ChimeQuadDetector.cs:168`

```csharp
// 기존 코드
if (!isValid || quadCells.Count < 4)
    continue;

// 수정 코드
if (!isValid || quadCells.Count < 9)  // 3×3 minimum (GDD 3.3)
    continue;
```

---

### 1.4 타이머 간격 4턴 변경

#### 목표
- 현재: 8턴 후 쿼드 파쇄
- 변경: 4턴 후 파쇄 (GDD 3.2)

#### 구현

**파일**: `ChimePulseSystem.cs:11`

```csharp
// 기존 코드
private const int PULSE_INTERVAL = 8;

// 수정 코드
private const int PULSE_INTERVAL = 4;  // GDD 3.2: 4-turn timer
```

---

### 1.5 테스트

#### Phase 1 완료 체크리스트

- [ ] 펜토미노 12종이 모두 랜덤하게 생성됨
- [ ] 각 펜토미노가 5칸으로 구성됨
- [ ] 블록을 유효한 위치로 이동 시 흰색 아웃라인 표시
- [ ] 블록을 무효한 위치로 이동 시 빨강 아웃라인 표시
- [ ] 2×2 쿼드는 감지되지 않음
- [ ] 3×3 쿼드가 노란색 아웃라인으로 하이라이트됨
- [ ] 쿼드 중앙에 ④ 타이머 표시
- [ ] 4턴 후 쿼드가 자동 파쇄됨

---

## Phase 2: 프래그먼트 & 커버리지

### 2.1 프래그먼트 시스템

#### 목표
- 쿼드 파쇄 시 쿼드 외부 블록이 프래그먼트로 전환
- 5턴 생명주기 (색상 변화 → 깜빡임 → 소멸)
- 소멸 시 콤보 리셋

#### 데이터 구조

**새 파일 생성**: `Assets/Scripts/Chime/ChimeFragment.cs`

```csharp
using UnityEngine;

public class ChimeFragment
{
    public Vector2Int position;
    public int blockId;
    public Color originalColor;
    public int age; // 0-5턴 (5턴 후 소멸)

    public ChimeFragment(Vector2Int pos, int id, Color color)
    {
        position = pos;
        blockId = id;
        originalColor = color;
        age = 0;
    }

    // 턴마다 나이 증가
    public void IncrementAge()
    {
        age++;
    }

    // 현재 나이에 따른 색상 계산 (GDD 3.4)
    public Color GetCurrentColor()
    {
        if (age == 0)
        {
            // 1턴: 밝은 색
            return originalColor * 1.2f;
        }
        else if (age == 1)
        {
            // 2턴: 약간 어두워짐
            return originalColor * 1.0f;
        }
        else if (age == 2)
        {
            // 3턴: 더 어두워짐
            return originalColor * 0.8f;
        }
        else if (age == 3)
        {
            // 4턴: 매우 어두움
            return originalColor * 0.6f;
        }
        else // age >= 4
        {
            // 5턴: 깜빡임 상태 (외부에서 처리)
            return originalColor * 0.4f;
        }
    }

    // 소멸 경고 중인지 (5턴째)
    public bool IsAboutToVanish()
    {
        return age >= 4;
    }
}
```

#### 프래그먼트 생성 로직

**파일**: `ChimePulseSystem.cs`

```csharp
// 필드 추가
private List<ChimeFragment> fragments = new List<ChimeFragment>();

// 쿼드 파쇄 시 프래그먼트 생성
private void ShatterQuad(ChimeQuad quad)
{
    Debug.Log($"[ShatterQuad] Shattering quad {quad.width}x{quad.height}");

    // 쿼드 영역 외부의 모든 점유된 셀 찾기
    for (int x = 0; x < ChimeGrid.WIDTH; x++)
    {
        for (int y = 0; y < ChimeGrid.HEIGHT; y++)
        {
            ChimeCell cell = grid.GetCell(x, y);
            Vector2Int pos = new Vector2Int(x, y);

            // 점유되었지만 쿼드에 포함되지 않은 셀 = 프래그먼트
            if (cell != null && cell.isOccupied && !quad.Contains(pos))
            {
                // 프래그먼트 생성
                ChimeFragment fragment = new ChimeFragment(
                    pos,
                    cell.blockId,
                    cell.originalColor
                );
                fragments.Add(fragment);

                Debug.Log($"  Created fragment at ({x},{y}), blockId={cell.blockId}");
            }
        }
    }

    // 쿼드 영역은 커버리지로 전환 (섹션 2.2에서 구현)
    MarkQuadAsCoverage(quad);

    // 쿼드 영역 클리어
    foreach (var cellPos in quad.cells)
    {
        ChimeCell cell = grid.GetCell(cellPos);
        if (cell != null)
        {
            cell.Clear();
        }
    }

    Debug.Log($"[ShatterQuad] Total fragments: {fragments.Count}");
}
```

#### 프래그먼트 나이 증가

```csharp
// IncrementTurn() 메서드에 추가
public void IncrementTurn()
{
    globalTurnCounter++;

    // 기존 코드 (셀 클리어 타이머, 쿼드 타이머 등)
    // ...

    // 프래그먼트 나이 증가
    UpdateFragments();

    // 나머지 코드...
}

private void UpdateFragments()
{
    // 모든 프래그먼트 나이 증가
    foreach (var fragment in fragments)
    {
        fragment.IncrementAge();
    }

    // 5턴 이상 된 프래그먼트 체크
    bool shouldVanish = fragments.Any(f => f.age >= 5);

    if (shouldVanish)
    {
        VanishAllFragments();
    }
    else
    {
        // 색상 업데이트
        UpdateFragmentVisuals();
    }
}

private void UpdateFragmentVisuals()
{
    foreach (var fragment in fragments)
    {
        ChimeCell cell = grid.GetCell(fragment.position);
        if (cell != null)
        {
            Color currentColor = fragment.GetCurrentColor();

            // 깜빡임 효과 (4턴째)
            if (fragment.IsAboutToVanish())
            {
                // 코루틴으로 깜빡임 구현 (섹션 4에서 구현)
                StartCoroutine(BlinkCell(cell));
            }
            else
            {
                cell.SetColor(currentColor);
            }
        }
    }
}

private void VanishAllFragments()
{
    Debug.Log($"[VanishAllFragments] Vanishing {fragments.Count} fragments");

    // 모든 프래그먼트 셀 클리어
    foreach (var fragment in fragments)
    {
        ChimeCell cell = grid.GetCell(fragment.position);
        if (cell != null)
        {
            cell.Clear();
        }
    }

    // 프래그먼트 리스트 초기화
    fragments.Clear();

    // 콤보 리셋 (섹션 2.3에서 구현)
    ResetCombo();
}
```

---

### 2.2 커버리지 시스템

#### 목표
- 쿼드 파쇄 시 해당 영역을 커버리지로 칠하기
- 진행도 추적 (0-100%)
- 90% 달성 시 레벨 클리어
- 100% 달성 시 보너스 점수

#### 데이터 구조

**파일**: `ChimeGrid.cs`에 필드 추가

```csharp
// 커버리지 추적 (각 셀이 커버리지로 칠해졌는지)
private bool[,] coverageMap = new bool[WIDTH, HEIGHT];

public bool IsCovered(Vector2Int pos)
{
    if (!IsValidPosition(pos)) return false;
    return coverageMap[pos.x, pos.y];
}

public void SetCoverage(Vector2Int pos, bool covered)
{
    if (!IsValidPosition(pos)) return;
    coverageMap[pos.x, pos.y] = covered;
}

public void ClearCoverage()
{
    coverageMap = new bool[WIDTH, HEIGHT];
}

public float GetCoveragePercentage()
{
    int totalCells = WIDTH * HEIGHT;
    int coveredCells = 0;

    for (int x = 0; x < WIDTH; x++)
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            if (coverageMap[x, y])
            {
                coveredCells++;
            }
        }
    }

    return (float)coveredCells / totalCells * 100f;
}
```

#### 쿼드 파쇄 시 커버리지 마킹

**파일**: `ChimePulseSystem.cs`

```csharp
private void MarkQuadAsCoverage(ChimeQuad quad)
{
    foreach (var cellPos in quad.cells)
    {
        grid.SetCoverage(cellPos, true);

        // 시각적 표시 (GDD 8.1: 반투명 그린)
        ChimeCell cell = grid.GetCell(cellPos);
        if (cell != null)
        {
            Color coverageColor = new Color(0.29f, 0.87f, 0.5f, 0.3f); // #4ADE80, 30% 투명
            cell.SetCoverageVisual(coverageColor);
        }
    }

    Debug.Log($"[MarkQuadAsCoverage] Coverage: {grid.GetCoveragePercentage():F1}%");

    // 클리어 조건 체크
    CheckLevelClear();
}

private void CheckLevelClear()
{
    float coverage = grid.GetCoveragePercentage();

    if (coverage >= 100f)
    {
        // 100% 달성 (GDD 3.5)
        Debug.Log("100% Coverage! Bonus!");
        AddScore(10000); // 100% 보너스
        LevelClear(true);
    }
    else if (coverage >= 90f)
    {
        // 90% 달성 (GDD 3.5)
        Debug.Log("90% Coverage! Level Clear!");
        AddScore(5000); // 90% 보너스
        LevelClear(false);
    }
}

private void LevelClear(bool isPerfect)
{
    // 레벨 클리어 처리 (Phase 3에서 구현)
    ChimeGameManager.Instance.LevelClear(isPerfect);
}
```

#### 진행도 UI

**새 파일**: `Assets/Scripts/Chime/ChimeCoverageUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChimeCoverageUI : MonoBehaviour
{
    [Header("References")]
    public ChimeGrid grid;
    public Image progressFill;
    public TextMeshProUGUI percentageText;

    [Header("Sections")]
    public Image section25;
    public Image section50;
    public Image section75;
    public Image section90;

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        float coverage = grid.GetCoveragePercentage();

        // 진행 바 업데이트
        if (progressFill != null)
        {
            progressFill.fillAmount = coverage / 100f;
        }

        // 퍼센트 텍스트 업데이트
        if (percentageText != null)
        {
            percentageText.text = $"{coverage:F1}%";
        }

        // 섹션 마커 업데이트
        UpdateSectionMarkers(coverage);
    }

    private void UpdateSectionMarkers(float coverage)
    {
        Color achievedColor = new Color(0.29f, 0.87f, 0.5f); // Green
        Color notAchievedColor = Color.gray;

        if (section25 != null)
            section25.color = coverage >= 25f ? achievedColor : notAchievedColor;

        if (section50 != null)
            section50.color = coverage >= 50f ? achievedColor : notAchievedColor;

        if (section75 != null)
            section75.color = coverage >= 75f ? achievedColor : notAchievedColor;

        if (section90 != null)
            section90.color = coverage >= 90f ? achievedColor : notAchievedColor;
    }
}
```

---

### 2.3 콤보 멀티플라이어 시스템

#### 목표
- 프래그먼트를 떨어뜨리지 않고 연속 쿼드 생성 시 배율 증가
- x1.0 → x1.5 → x2.0 → x2.5 → x3.0
- 프래그먼트 소멸 시 리셋

#### 구현

**파일**: `ChimePulseSystem.cs`

```csharp
// 필드 추가
private int comboCount = 0;
private float currentMultiplier = 1.0f;

// 콤보 증가
private void IncrementCombo()
{
    comboCount++;
    currentMultiplier = GetMultiplier(comboCount);
    Debug.Log($"[Combo] Count: {comboCount}, Multiplier: x{currentMultiplier:F1}");
}

// 콤보 배율 계산 (GDD 3.7)
private float GetMultiplier(int combo)
{
    if (combo >= 5) return 3.0f;
    if (combo >= 4) return 2.5f;
    if (combo >= 3) return 2.0f;
    if (combo >= 2) return 1.5f;
    return 1.0f;
}

// 콤보 리셋
private void ResetCombo()
{
    comboCount = 0;
    currentMultiplier = 1.0f;
    Debug.Log("[Combo] Reset");
}

// 쿼드 점수 계산 시 배율 적용
private int CalculateQuadScore(ChimeQuad quad, bool isPerfect)
{
    int baseScore = quad.GetScore();

    // 퍼펙트 쿼드 보너스 (GDD 3.7)
    if (isPerfect)
    {
        baseScore += 500;
        Debug.Log($"[Score] Perfect Quad! +500 bonus");
    }

    // 큰 쿼드 보너스 (4×4+)
    if (quad.width >= 4 && quad.height >= 4)
    {
        baseScore += 1000;
        Debug.Log($"[Score] Large Quad! +1000 bonus");
    }

    // 콤보 배율 적용
    int finalScore = Mathf.RoundToInt(baseScore * currentMultiplier);

    Debug.Log($"[Score] Base: {baseScore}, Multiplier: x{currentMultiplier:F1}, Final: {finalScore}");

    return finalScore;
}

// 퍼펙트 쿼드 감지
private bool IsPerfectQuad(ChimeQuad quad)
{
    // 쿼드 외부에 블록이 없으면 퍼펙트
    for (int x = 0; x < ChimeGrid.WIDTH; x++)
    {
        for (int y = 0; y < ChimeGrid.HEIGHT; y++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            ChimeCell cell = grid.GetCell(pos);

            // 점유되었지만 쿼드에 포함되지 않은 셀이 있으면 NOT perfect
            if (cell != null && cell.isOccupied && !quad.Contains(pos))
            {
                return false;
            }
        }
    }

    return true;
}
```

---

### 2.4 Phase 2 테스트

#### 완료 체크리스트

- [ ] 쿼드 파쇄 시 프래그먼트 생성됨
- [ ] 프래그먼트가 턴마다 색상 변화 (밝음 → 어두움)
- [ ] 4턴째 프래그먼트가 깜빡임
- [ ] 5턴째 모든 프래그먼트가 동시 소멸
- [ ] 쿼드 파쇄 시 커버리지가 칠해짐 (반투명 그린)
- [ ] 진행도 UI가 0-100% 표시
- [ ] 90% 달성 시 "Level Clear" 메시지
- [ ] 100% 달성 시 보너스 점수 획득
- [ ] 연속 쿼드 생성 시 콤보 증가 (x1.0 → x1.5 → x2.0 등)
- [ ] 프래그먼트 소멸 시 콤보 리셋

---

## Phase 3: 게임 모드

### 3.1 게임 모드 시스템

#### 목표
- 5가지 게임 모드: Practice, Standard, Sharp, Strike, Challenge
- 각 모드별 고유 규칙 및 위협

#### 데이터 구조

**새 파일**: `Assets/Scripts/Chime/ChimeGameMode.cs`

```csharp
using UnityEngine;

public enum GameModeType
{
    Practice,   // 타이머 없음, 프래그먼트 영구
    Standard,   // 타이머, 타임 보너스
    Sharp,      // 라이프 시스템
    Strike,     // 90초 고정
    Challenge   // 복잡한 그리드
}

[System.Serializable]
public class GameModeConfig
{
    public GameModeType modeType;
    public string modeName;
    public string description;

    [Header("Timer Settings")]
    public bool hasTimer;
    public float startTime; // 초 단위
    public bool canExtendTime;

    [Header("Fragment Settings")]
    public bool fragmentsVanish; // false면 영구 유지

    [Header("Life Settings")]
    public bool hasLifeSystem;
    public int startingLives;

    [Header("Grid Settings")]
    public int gridWidth = 12;
    public int gridHeight = 9;
    public bool hasObstacles;
}
```

**새 파일**: `Assets/Scripts/Chime/ChimeGameModeManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ChimeGameModeManager : MonoBehaviour
{
    public static ChimeGameModeManager Instance { get; private set; }

    [Header("Mode Configurations")]
    public List<GameModeConfig> modeConfigs;

    private GameModeConfig currentMode;
    private float remainingTime;
    private int currentLives;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartMode(GameModeType modeType)
    {
        currentMode = modeConfigs.Find(m => m.modeType == modeType);

        if (currentMode == null)
        {
            Debug.LogError($"Mode config not found: {modeType}");
            return;
        }

        Debug.Log($"[GameMode] Starting {currentMode.modeName}");

        // 타이머 초기화
        if (currentMode.hasTimer)
        {
            remainingTime = currentMode.startTime;
        }

        // 라이프 초기화
        if (currentMode.hasLifeSystem)
        {
            currentLives = currentMode.startingLives;
        }

        // 게임 시작
        ChimeGameManager.Instance.NewGame();
    }

    private void Update()
    {
        if (currentMode == null) return;

        // 타이머 업데이트
        if (currentMode.hasTimer)
        {
            UpdateTimer();
        }
    }

    private void UpdateTimer()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            OnTimerExpired();
        }
    }

    private void OnTimerExpired()
    {
        // Standard 모드: 새 섹션 진입 시 타임 보너스 체크
        if (currentMode.modeType == GameModeType.Standard)
        {
            // 타임 보너스 체크 (섹션 3.2에서 구현)
            if (!TryApplyTimeBonus())
            {
                // 타임 보너스 없으면 게임 오버
                ChimeGameManager.Instance.GameOver();
            }
        }
        else
        {
            // Strike 모드: 즉시 게임 오버
            ChimeGameManager.Instance.GameOver();
        }
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public void AddTime(float seconds)
    {
        if (currentMode.hasTimer)
        {
            remainingTime += seconds;
            Debug.Log($"[Timer] +{seconds}s, Total: {remainingTime:F1}s");
        }
    }

    public int GetLives()
    {
        return currentLives;
    }

    public void LoseLife()
    {
        if (currentMode.hasLifeSystem)
        {
            currentLives--;
            Debug.Log($"[Life] Lost life, Remaining: {currentLives}");

            if (currentLives <= 0)
            {
                ChimeGameManager.Instance.GameOver();
            }
        }
    }

    public void GainLife()
    {
        if (currentMode.hasLifeSystem)
        {
            currentLives = Mathf.Min(currentLives + 1, currentMode.startingLives);
            Debug.Log($"[Life] Gained life, Total: {currentLives}");
        }
    }

    public bool ShouldFragmentsVanish()
    {
        return currentMode != null && currentMode.fragmentsVanish;
    }
}
```

---

### 3.2 Standard 모드 구현

#### 특징
- 시작 3분 타이머
- 섹션 진입 시 타임 보너스
- 90% 달성 시 클리어

#### 설정

**Unity Editor**에서 GameModeConfig 설정:

```
Standard Mode:
  - Mode Type: Standard
  - Mode Name: "Standard"
  - Description: "Classic Chime with timer and time bonuses"
  - Has Timer: true
  - Start Time: 180 (3분)
  - Can Extend Time: true
  - Fragments Vanish: true
  - Has Life System: false
```

#### 타임 보너스 시스템

**파일**: `ChimeGameModeManager.cs`

```csharp
// 마지막 섹션 추적
private int lastSection = 0; // 0, 25, 50, 75, 90

public bool TryApplyTimeBonus()
{
    float coverage = ChimeGrid.Instance.GetCoveragePercentage();
    int currentSection = GetCurrentSection(coverage);

    // 새 섹션 진입 시
    if (currentSection > lastSection)
    {
        lastSection = currentSection;
        ApplyTimeBonus(currentSection);
        return true;
    }

    return false;
}

private int GetCurrentSection(float coverage)
{
    if (coverage >= 90f) return 90;
    if (coverage >= 75f) return 75;
    if (coverage >= 50f) return 50;
    if (coverage >= 25f) return 25;
    return 0;
}

private void ApplyTimeBonus(int section)
{
    // GDD 3.6 타임 보너스
    float bonus = 0f;

    switch (section)
    {
        case 25: bonus = 30f; break;
        case 50: bonus = 45f; break;
        case 75: bonus = 60f; break;
        case 90: bonus = 30f; break;
    }

    if (bonus > 0f)
    {
        AddTime(bonus);
        Debug.Log($"[TimeBonus] Entered {section}% section! +{bonus}s");
    }
}
```

---

### 3.3 Sharp 모드 구현

#### 특징
- 타이머 없음
- 라이프 3개
- 프래그먼트 소멸 시 라이프 -1
- 퍼펙트 쿼드로 라이프 회복

#### 설정

```
Sharp Mode:
  - Mode Type: Sharp
  - Mode Name: "Sharp"
  - Description: "No timer, but life system - manage fragments carefully"
  - Has Timer: false
  - Fragments Vanish: true
  - Has Life System: true
  - Starting Lives: 3
```

#### 라이프 시스템 연동

**파일**: `ChimePulseSystem.cs`

```csharp
// 프래그먼트 소멸 시
private void VanishAllFragments()
{
    // ... 기존 코드 ...

    // Sharp 모드: 라이프 감소
    if (ChimeGameModeManager.Instance != null)
    {
        GameModeConfig mode = ChimeGameModeManager.Instance.GetCurrentMode();
        if (mode.modeType == GameModeType.Sharp)
        {
            ChimeGameModeManager.Instance.LoseLife();
        }
    }

    fragments.Clear();
    ResetCombo();
}

// 퍼펙트 쿼드 시
private void OnPerfectQuad()
{
    // Sharp 모드: 라이프 회복
    if (ChimeGameModeManager.Instance != null)
    {
        GameModeConfig mode = ChimeGameModeManager.Instance.GetCurrentMode();
        if (mode.modeType == GameModeType.Sharp)
        {
            ChimeGameModeManager.Instance.GainLife();
        }
    }
}
```

---

### 3.4 Practice 모드 구현

#### 특징
- 타이머 없음
- 프래그먼트 영구 유지 (소멸 안 함)
- 게임 오버 없음

#### 설정

```
Practice Mode:
  - Mode Type: Practice
  - Mode Name: "Practice"
  - Description: "No threats - learn the mechanics"
  - Has Timer: false
  - Fragments Vanish: false  ← 영구 유지
  - Has Life System: false
```

#### 프래그먼트 소멸 방지

**파일**: `ChimePulseSystem.cs`

```csharp
private void UpdateFragments()
{
    // Practice 모드면 프래그먼트 소멸 스킵
    if (ChimeGameModeManager.Instance != null &&
        !ChimeGameModeManager.Instance.ShouldFragmentsVanish())
    {
        // 색상 업데이트만 (소멸 안 함)
        UpdateFragmentVisuals();
        return;
    }

    // ... 기존 코드 (나이 증가, 소멸 체크) ...
}
```

---

### 3.5 Strike 모드 구현

#### 특징
- 90초 고정 타이머
- 타임 연장 불가
- 최대한 빠르게 점수 획득

#### 설정

```
Strike Mode:
  - Mode Type: Strike
  - Mode Name: "Strike"
  - Description: "90-second speed challenge"
  - Has Timer: true
  - Start Time: 90
  - Can Extend Time: false  ← 연장 불가
  - Fragments Vanish: true
```

---

### 3.6 Challenge 모드 구현

#### 특징
- 복잡한 그리드 (장애물, 비정형 형태)
- 제한된 셰이프셋
- 짧은 타이머 (2분)

#### 설정

```
Challenge Mode:
  - Mode Type: Challenge
  - Mode Name: "Challenge"
  - Description: "Complex grids with limited pieces"
  - Has Timer: true
  - Start Time: 120
  - Can Extend Time: true
  - Fragments Vanish: true
  - Has Obstacles: true
```

#### 장애물 시스템

**파일**: `ChimeGrid.cs`

```csharp
// 장애물 셀 (배치 불가능)
private bool[,] obstacleMap = new bool[WIDTH, HEIGHT];

public bool IsObstacle(Vector2Int pos)
{
    if (!IsValidPosition(pos)) return false;
    return obstacleMap[pos.x, pos.y];
}

public void SetObstacle(Vector2Int pos, bool isObstacle)
{
    if (!IsValidPosition(pos)) return;
    obstacleMap[pos.x, pos.y] = isObstacle;

    // 시각적 표시
    ChimeCell cell = GetCell(pos);
    if (cell != null)
    {
        cell.SetObstacleVisual(isObstacle);
    }
}

// 레벨별 장애물 패턴
public void GenerateObstacles(int level)
{
    // 예: L자 형태 그리드
    if (level == 1)
    {
        // 오른쪽 상단 모서리 장애물
        for (int x = 8; x < WIDTH; x++)
        {
            for (int y = 6; y < HEIGHT; y++)
            {
                SetObstacle(new Vector2Int(x, y), true);
            }
        }
    }
    // 더 많은 패턴 추가...
}
```

---

### 3.7 Phase 3 테스트

#### 완료 체크리스트

**Practice 모드**:
- [ ] 타이머 없음
- [ ] 프래그먼트가 소멸하지 않음
- [ ] 게임 오버 없음

**Standard 모드**:
- [ ] 3분 타이머 시작
- [ ] 25% 섹션 진입 시 +30초
- [ ] 50% 섹션 진입 시 +45초
- [ ] 75% 섹션 진입 시 +60초
- [ ] 90% 섹션 진입 시 +30초
- [ ] 90% 달성 시 레벨 클리어
- [ ] 타이머 0 도달 시 게임 오버

**Sharp 모드**:
- [ ] 타이머 없음
- [ ] 라이프 3개로 시작
- [ ] 프래그먼트 소멸 시 라이프 -1
- [ ] 퍼펙트 쿼드 시 라이프 +1
- [ ] 라이프 0 도달 시 게임 오버

**Strike 모드**:
- [ ] 90초 타이머
- [ ] 타임 연장 없음
- [ ] 90초 경과 시 게임 오버

**Challenge 모드**:
- [ ] 복잡한 그리드 (장애물)
- [ ] 2분 타이머
- [ ] 장애물 위치에 블록 배치 불가

---

## Phase 4: 비주얼 & 오디오

### 4.1 애니메이션 시스템

#### 피스 배치 애니메이션 (0.15초)

**파일**: `ChimeBlock.cs`

```csharp
public void Place()
{
    // ... 기존 코드 (그리드에 블록 정보 기록) ...

    // 배치 애니메이션 시작
    StartCoroutine(PlaceAnimation());
}

private IEnumerator PlaceAnimation()
{
    float duration = 0.15f; // GDD 8.2
    float elapsed = 0f;

    // 아웃라인 상태로 시작
    Color outlineColor = new Color(1f, 1f, 1f, 0.5f);
    Color targetColor = shape.blockColor; // 최종 색상

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        // 아웃라인 → 채워진 색상으로 보간
        Color currentColor = Color.Lerp(outlineColor, targetColor, t);

        foreach (var cellObj in cellObjects)
        {
            Image image = cellObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = currentColor;
            }
        }

        yield return null;
    }

    // 최종 색상 설정
    foreach (var cellObj in cellObjects)
    {
        Image image = cellObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = targetColor;
        }
    }

    // 배치 완료 - PlacedBlocks로 이동
    // ... 기존 코드 ...
}
```

#### 배치 불가능 흔들림 (0.2초)

**파일**: `ChimeBlock.cs`

```csharp
// 배치 시도 시 (ChimeGameManager.cs에서 호출)
if (!currentBlock.CanPlace())
{
    currentBlock.PlayInvalidPlacementAnimation();
    yield break;
}

public void PlayInvalidPlacementAnimation()
{
    StartCoroutine(ShakeAnimation());
}

private IEnumerator ShakeAnimation()
{
    float duration = 0.2f; // GDD 8.2
    float magnitude = 5f; // 흔들림 강도 (픽셀)
    Vector3 originalPos = transform.localPosition;

    float elapsed = 0f;

    while (elapsed < duration)
    {
        float x = Random.Range(-magnitude, magnitude);
        float y = Random.Range(-magnitude, magnitude);

        transform.localPosition = originalPos + new Vector3(x, y, 0f);

        elapsed += Time.deltaTime;
        yield return null;
    }

    // 원래 위치로 복귀
    transform.localPosition = originalPos;
}
```

#### 쿼드 형성 펄스 (0.3초)

**파일**: `ChimePulseSystem.cs`

```csharp
private void OnQuadFormed(ChimeQuad quad)
{
    StartCoroutine(QuadFormationAnimation(quad));
}

private IEnumerator QuadFormationAnimation(ChimeQuad quad)
{
    float duration = 0.3f; // GDD 8.2
    float elapsed = 0f;

    // 펄스 효과: 크기 1.0 → 1.2 → 1.0
    while (elapsed < duration)
    {
        float t = elapsed / duration;
        float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;

        foreach (var cellPos in quad.cells)
        {
            ChimeCell cell = grid.GetCell(cellPos);
            if (cell != null)
            {
                cell.transform.localScale = Vector3.one * scale;
            }
        }

        elapsed += Time.deltaTime;
        yield return null;
    }

    // 원래 크기로 복귀
    foreach (var cellPos in quad.cells)
    {
        ChimeCell cell = grid.GetCell(cellPos);
        if (cell != null)
        {
            cell.transform.localScale = Vector3.one;
        }
    }
}
```

#### 쿼드 파쇄 효과 (0.4초)

**파일**: `ChimePulseSystem.cs`

```csharp
private void ShatterQuad(ChimeQuad quad)
{
    StartCoroutine(ShatterAnimation(quad));
}

private IEnumerator ShatterAnimation(ChimeQuad quad)
{
    float duration = 0.4f; // GDD 8.2

    // 파편 효과 (각 셀이 랜덤 방향으로 날아감)
    Dictionary<Vector2Int, Vector3> originalPositions = new Dictionary<Vector2Int, Vector3>();
    Dictionary<Vector2Int, Vector3> targetOffsets = new Dictionary<Vector2Int, Vector3>();

    foreach (var cellPos in quad.cells)
    {
        ChimeCell cell = grid.GetCell(cellPos);
        if (cell != null)
        {
            originalPositions[cellPos] = cell.transform.localPosition;

            // 랜덤 방향으로 날아갈 오프셋
            Vector3 offset = Random.insideUnitCircle * 50f;
            targetOffsets[cellPos] = offset;
        }
    }

    float elapsed = 0f;

    while (elapsed < duration)
    {
        float t = elapsed / duration;

        foreach (var cellPos in quad.cells)
        {
            ChimeCell cell = grid.GetCell(cellPos);
            if (cell != null)
            {
                Vector3 offset = targetOffsets[cellPos];
                cell.transform.localPosition = originalPositions[cellPos] + offset * t;

                // 페이드 아웃
                Color color = cell.GetComponent<Image>().color;
                color.a = 1f - t;
                cell.GetComponent<Image>().color = color;
            }
        }

        elapsed += Time.deltaTime;
        yield return null;
    }

    // 쿼드 영역 클리어 및 커버리지 마킹
    // ... 기존 코드 ...
}
```

---

### 4.2 효과음 시스템

#### 사운드 매니저

**새 파일**: `Assets/Scripts/Chime/ChimeSoundManager.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ChimeSoundManager : MonoBehaviour
{
    public static ChimeSoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Sound Effects (GDD 9.1)")]
    public AudioClip pieceMove;        // "쉭"
    public AudioClip pieceRotate;      // "틱"
    public AudioClip piecePlaceValid;  // "퉁"
    public AudioClip piecePlaceInvalid; // "삑"
    public AudioClip quadForm;         // "팅" (차임벨)
    public AudioClip quadShatter;      // 크리스탈 깨지는 소리
    public AudioClip perfectQuad;      // 골든 차임
    public AudioClip fragmentWarning;  // 경고음
    public AudioClip fragmentVanish;   // 흩어지는 소리
    public AudioClip sectionEnter;     // 팡파레
    public AudioClip levelClear;       // 승리 음악
    public AudioClip gameOver;         // 하강 톤

    [Header("Background Music (GDD 9.2)")]
    public List<AudioClip> levelBGMs; // 레벨별 BGM

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayBGM(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelBGMs.Count)
        {
            Debug.LogWarning($"Invalid level index: {levelIndex}");
            return;
        }

        AudioClip bgm = levelBGMs[levelIndex];
        if (bgm != null && musicSource != null)
        {
            musicSource.clip = bgm;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StopBGM()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
}
```

#### 효과음 호출

**파일**: `ChimeBlock.cs`

```csharp
public bool Move(Vector2Int direction)
{
    // ... 기존 코드 ...

    // 이동 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.pieceMove, 0.3f);

    return true;
}

public bool Rotate(bool clockwise)
{
    // ... 기존 코드 ...

    // 회전 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.pieceRotate, 0.5f);

    return true;
}

public void Place()
{
    // 배치 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.piecePlaceValid, 0.7f);

    // ... 기존 코드 ...
}

public void PlayInvalidPlacementAnimation()
{
    // 배치 불가 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.piecePlaceInvalid, 0.6f);

    StartCoroutine(ShakeAnimation());
}
```

**파일**: `ChimePulseSystem.cs`

```csharp
private void OnQuadFormed(ChimeQuad quad)
{
    // 쿼드 형성 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.quadForm, 0.8f);

    // 퍼펙트 쿼드 체크
    if (IsPerfectQuad(quad))
    {
        ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.perfectQuad, 1f);
    }

    StartCoroutine(QuadFormationAnimation(quad));
}

private void ShatterQuad(ChimeQuad quad)
{
    // 쿼드 파쇄 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.quadShatter, 0.9f);

    StartCoroutine(ShatterAnimation(quad));
}

private void VanishAllFragments()
{
    // 프래그먼트 소멸 효과음
    ChimeSoundManager.Instance.PlaySFX(ChimeSoundManager.Instance.fragmentVanish, 0.7f);

    // ... 기존 코드 ...
}
```

---

### 4.3 BGM 시스템

#### 레벨별 BGM (GDD 9.2)

| 레벨 | 장르 | BPM | 특징 |
|------|------|-----|------|
| 1 | Chiptune | 120 | 8비트 스타일 |
| 2 | Electronic | 128 | 일렉트로닉 |
| 3 | Ambient | 90 | 차분한 분위기 |
| 4 | Drum & Bass | 174 | 빠른 리듬 |
| 5 | Orchestral | 110 | 오케스트라 |

#### BGM 재생

**파일**: `ChimeGameManager.cs`

```csharp
public void StartLevel(int levelIndex)
{
    // BGM 재생
    ChimeSoundManager.Instance.PlayBGM(levelIndex);

    // ... 레벨 시작 로직 ...
}
```

---

### 4.4 Phase 4 테스트

#### 완료 체크리스트

**애니메이션**:
- [ ] 피스 배치 시 0.15초 채우기 애니메이션
- [ ] 배치 불가능 시 0.2초 흔들림
- [ ] 쿼드 형성 시 0.3초 펄스 효과
- [ ] 쿼드 파쇄 시 0.4초 파편 효과

**효과음**:
- [ ] 피스 이동: "쉭" 소리
- [ ] 피스 회전: "틱" 소리
- [ ] 배치 성공: "퉁" 소리
- [ ] 배치 실패: "삑" 소리
- [ ] 쿼드 형성: "팅" (차임벨)
- [ ] 퍼펙트 쿼드: 골든 차임
- [ ] 쿼드 파쇄: 크리스탈 소리
- [ ] 프래그먼트 소멸: 흩어지는 소리

**BGM**:
- [ ] 레벨 1: Chiptune 재생
- [ ] 레벨 2: Electronic 재생
- [ ] 레벨 3: Ambient 재생
- [ ] 레벨 4: Drum & Bass 재생
- [ ] 레벨 5: Orchestral 재생

---

## Phase 5: 폴리싱

### 5.1 메인 메뉴 UI

#### UI 구조 (GDD 10.1)

```
MainMenu Canvas
├── Title (TextMeshProUGUI) - "C H I M E"
├── ModeSelection Panel
│   ├── PracticeButton
│   ├── StandardButton
│   ├── SharpButton
│   ├── StrikeButton
│   └── ChallengeButton
├── SettingsButton
└── CreditsButton
```

#### 구현

**새 파일**: `Assets/Scripts/Chime/ChimeMainMenu.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChimeMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button practiceButton;
    public Button standardButton;
    public Button sharpButton;
    public Button strikeButton;
    public Button challengeButton;

    private void Start()
    {
        practiceButton.onClick.AddListener(() => StartMode(GameModeType.Practice));
        standardButton.onClick.AddListener(() => StartMode(GameModeType.Standard));
        sharpButton.onClick.AddListener(() => StartMode(GameModeType.Sharp));
        strikeButton.onClick.AddListener(() => StartMode(GameModeType.Strike));
        challengeButton.onClick.AddListener(() => StartMode(GameModeType.Challenge));
    }

    private void StartMode(GameModeType modeType)
    {
        // 게임 씬 로드
        SceneManager.LoadScene("ChimeGame");

        // 모드 시작 (씬 로드 후 호출)
        StartCoroutine(StartModeAfterSceneLoad(modeType));
    }

    private System.Collections.IEnumerator StartModeAfterSceneLoad(GameModeType modeType)
    {
        yield return new WaitForSeconds(0.1f);

        if (ChimeGameModeManager.Instance != null)
        {
            ChimeGameModeManager.Instance.StartMode(modeType);
        }
    }
}
```

---

### 5.2 인게임 HUD

#### UI 구조 (GDD 10.2)

```
InGame HUD
├── Header
│   ├── LevelText - "♪ Level 1"
│   ├── TurnText - "Turn: 24"
│   ├── TimerText - "⏱ 02:34"
│   └── ScoreText - "💯 52,300"
├── Status Bar
│   ├── ComboText - "Combo: x2.5"
│   ├── PerfectCountText - "💎 Perfect x3"
│   └── FragmentCountText - "⚡ Fragment: 2"
└── Progress Bar
    ├── ProgressFill
    └── SectionMarkers (25%, 50%, 75%, 90%)
```

---

### 5.3 레벨 진행 시스템

#### 레벨 구조 (GDD 7.1)

**새 파일**: `Assets/Scripts/Chime/ChimeLevelData.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public string levelName;
    public ChimeBlockShape[] shapeset; // 7개 피스
    public int bgmIndex;
    public float difficulty; // 1-5
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Chime/Level Data")]
public class ChimeLevelData : ScriptableObject
{
    public List<LevelData> levels;

    public LevelData GetLevel(int index)
    {
        if (index < 0 || index >= levels.Count)
            return null;

        return levels[index];
    }
}
```

---

### 5.4 업적 시스템

#### 업적 정의 (GDD 7.2)

**새 파일**: `Assets/Scripts/Chime/ChimeAchievement.cs`

```csharp
using UnityEngine;

public enum AchievementType
{
    FirstQuad,        // 첫 쿼드 완성
    Perfect10,        // 퍼펙트 쿼드 10개
    ComboMaster,      // 콤보 x3.0 달성
    Complete100,      // 레벨 100% 완료
    SpeedDemon,       // Strike 모드 1분 안에 50,000점
    Survivor          // Sharp 모드 레벨 클리어
}

[System.Serializable]
public class Achievement
{
    public AchievementType type;
    public string title;
    public string description;
    public bool unlocked;
    public int progress;
    public int target;
}
```

---

## 테스트 가이드

### 단계별 테스트 체크리스트

#### Phase 1 테스트
```bash
□ 펜토미노 12종 랜덤 생성
□ 배치 검증 색상 (흰색/빨강)
□ 3×3 최소 쿼드 감지
□ 4턴 타이머
```

#### Phase 2 테스트
```bash
□ 프래그먼트 생성 및 생명주기
□ 커버리지 추적 (0-100%)
□ 퍼펙트 쿼드 감지
□ 콤보 멀티플라이어
```

#### Phase 3 테스트
```bash
□ Practice 모드 (타이머 없음, 프래그먼트 영구)
□ Standard 모드 (타이머, 타임 보너스)
□ Sharp 모드 (라이프 시스템)
□ Strike 모드 (90초 고정)
□ Challenge 모드 (장애물)
```

#### Phase 4 테스트
```bash
□ 피스 배치 애니메이션
□ 쿼드 파쇄 애니메이션
□ 12가지 효과음
□ 5개 레벨 BGM
```

#### Phase 5 테스트
```bash
□ 메인 메뉴
□ 인게임 HUD
□ 레벨 진행
□ 업적 시스템
```

---

## 참조

### 주요 파일 구조

```
Assets/
├── Scenes/
│   ├── MainMenu.unity
│   └── ChimeGame.unity
├── Scripts/
│   └── Chime/
│       ├── ChimeGameManager.cs
│       ├── ChimeGrid.cs
│       ├── ChimeCell.cs
│       ├── ChimeBlock.cs
│       ├── ChimeBlockShape.cs
│       ├── ChimeQuad.cs
│       ├── ChimeQuadDetector.cs
│       ├── ChimePulseSystem.cs
│       ├── ChimeFragment.cs ← NEW
│       ├── ChimeCoverageUI.cs ← NEW
│       ├── ChimeGameMode.cs ← NEW
│       ├── ChimeGameModeManager.cs ← NEW
│       ├── ChimeSoundManager.cs ← NEW
│       ├── ChimeLevelData.cs ← NEW
│       └── ChimeAchievement.cs ← NEW
├── Data/
│   ├── BlockShapes/
│   │   ├── BlockShape_F.asset
│   │   ├── ... (12개)
│   │   └── BlockShape_Z.asset
│   └── Levels/
│       └── LevelData.asset
├── Audio/
│   ├── SFX/
│   │   ├── piece_move.wav
│   │   ├── ... (12개)
│   │   └── game_over.wav
│   └── BGM/
│       ├── level1_chiptune.ogg
│       ├── ... (5개)
│       └── level5_orchestral.ogg
└── Sprites/
    └── Chime/
        └── Grid_12x9.svg

```

### GDD 구현 매핑

| GDD 섹션 | 구현 파일 | 상태 |
|---------|---------|------|
| 3.1 펜토미노 | ChimeBlockShape.cs | Phase 1 |
| 3.2 턴 시스템 | ChimePulseSystem.cs | Phase 1 |
| 3.3 쿼드 시스템 | ChimeQuadDetector.cs | Phase 1 |
| 3.4 프래그먼트 | ChimeFragment.cs | Phase 2 |
| 3.5 커버리지 | ChimeCoverageUI.cs | Phase 2 |
| 3.6 타이머 | ChimeGameModeManager.cs | Phase 3 |
| 3.7 점수 | ChimePulseSystem.cs | Phase 2 |
| 4. 게임 모드 | ChimeGameMode.cs | Phase 3 |
| 5. 컨트롤 | ChimeBlock.cs | Phase 1 |
| 8.2 애니메이션 | ChimeBlock.cs, ChimePulseSystem.cs | Phase 4 |
| 9. 오디오 | ChimeSoundManager.cs | Phase 4 |
| 10. UI/UX | ChimeMainMenu.cs 등 | Phase 5 |

### 우선순위 요약

**P1 (즉시)**: Phase 1 핵심 메커니즘
- 펜토미노 12종
- 검증 색상
- 3×3 쿼드
- 4턴 타이머

**P2 (단기)**: Phase 2 프래그먼트 & 커버리지
- 프래그먼트 시스템
- 커버리지 추적
- 콤보 시스템

**P3 (중기)**: Phase 3 게임 모드
- 5가지 게임 모드 구현

**P4 (장기)**: Phase 4-5 폴리싱
- 애니메이션, 효과음, BGM
- UI/UX, 레벨 시스템, 업적

---

**구현 가이드 종료**

추가 정보:
- `Chime_GDD.md` - 게임 디자인 사양
- `CHIME_SETUP_GUIDE.md` - 기본 설정 가이드 (Phase 1)
- Unity 인라인 주석 및 Debug.Log 문

**Version 1.0 | 2026.02.04**
