# Chime Quick Start 가이드

**Unity 6000.3.2f1**
**최종 업데이트: 2026.02.04**

---

## 목차

1. [개요](#개요)
2. [빠른 시작](#빠른-시작)
3. [현재 구현 상태](#현재-구현-상태)
4. [필수 수정 사항 (Phase 1)](#필수-수정-사항-phase-1)
5. [Scene 설정](#scene-설정)
6. [테스트 및 디버깅](#테스트-및-디버깅)
7. [다음 단계](#다음-단계)

---

## 개요

이 가이드는 Chime 프로토타입을 빠르게 설정하고 **Phase 1 핵심 메커니즘**을 구현하는 Quick Start 가이드입니다.

### 📚 관련 문서

| 문서 | 용도 |
|------|------|
| **CHIME_SETUP_GUIDE.md** (현재) | Quick Start - 즉시 시작 가능한 기본 설정 |
| **CHIME_IMPLEMENTATION_GUIDE.md** | 전체 구현 로드맵 (Phase 1-5, 26주) |
| **Chime_GDD.md** | 게임 디자인 문서 (전체 사양) |

### 이 가이드의 범위

- ✅ **즉시 구현 가능**: Phase 1 핵심 메커니즘 (3주)
- ⏭️ **이후 구현**: 전체 기능은 CHIME_IMPLEMENTATION_GUIDE.md 참조

---

## 빠른 시작

### 30초 체크리스트

현재 구현("Qube" 프로토타입)을 Chime GDD 사양에 맞추기 위한 최소 수정사항:

```bash
□ 1. 쿼드 최소 크기 변경: 2×2 → 3×3 (1줄)
□ 2. 타이머 간격 변경: 8턴 → 4턴 (1줄)
□ 3. 검증 색상 추가: 흰색/빨강 아웃라인 (1 메서드)
□ 4. 펜토미노 12종 생성: ScriptableObject 12개
```

**예상 시간**: 30분 - 1시간

---

## 현재 구현 상태

### ✅ 이미 구현된 기능

| 기능 | 파일 | 상태 |
|------|------|------|
| 12×9 그리드 | ChimeGrid.cs | ✅ 완료 |
| 턴 기반 시스템 | ChimePulseSystem.cs | ⚠️ 8턴 → 4턴 수정 필요 |
| 쿼드 감지 | ChimeQuadDetector.cs | ⚠️ 2×2 → 3×3 수정 필요 |
| 블록 이동/회전 | ChimeBlock.cs | ⚠️ 시각 피드백 필요 |
| 쿼드 하이라이트 | ChimeQuadDetector.cs | ✅ 완료 |
| 블록 생성 | ChimeGameManager.cs | ⚠️ 4종 → 12종 필요 |

### 현재 프로토타입 vs. GDD 목표

| 항목 | 현재 (Qube) | 목표 (Chime GDD) | 수정 |
|--------|----------------|-------------------|------|
| 그리드 크기 | 12×9 | 12×9 | ✅ 동일 |
| 블록 타입 | L, I, T, O (3-4칸) | 12 펜토미노 (각 5칸) | 🔧 필요 |
| 쿼드 크기 | 2×2+ 직사각형 | 3×3+ 직사각형 | 🔧 필요 |
| 턴 타이머 | 8턴 | 4턴 | 🔧 필요 |
| 시각 피드백 | 없음 | 흰색/빨강 아웃라인 | 🔧 필요 |

---

## 필수 수정 사항 (Phase 1)

### 수정 1: 쿼드 최소 크기 3×3 변경

**파일**: `Assets/Scripts/Chime/ChimeQuadDetector.cs:168`

```csharp
// 기존
if (!isValid || quadCells.Count < 4)
    continue;

// 수정
if (!isValid || quadCells.Count < 9)  // 3×3 minimum
    continue;
```

---

### 수정 2: 타이머 간격 4턴 변경

**파일**: `Assets/Scripts/Chime/ChimePulseSystem.cs:11`

```csharp
// 기존
private const int PULSE_INTERVAL = 8;

// 수정
private const int PULSE_INTERVAL = 4;
```

---

### 수정 3: 검증 색상 피드백 추가

**파일**: `Assets/Scripts/Chime/ChimeBlock.cs:285`

기존 `UpdatePlacementVisualFeedback()` 메서드를 다음과 같이 수정:

```csharp
public void UpdatePlacementVisualFeedback()
{
    bool canPlace = CanPlace();

    Color visualColor;
    if (canPlace)
    {
        // 배치 가능: 흰색 아웃라인, 50% 투명
        visualColor = new Color(1f, 1f, 1f, 0.5f);
    }
    else
    {
        // 배치 불가능: 빨강 아웃라인, 30% 불투명
        visualColor = new Color(1f, 0.27f, 0.27f, 0.3f);
    }

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

그리고 **Move/Rotate 메서드 끝에 호출 추가**:

```csharp
// ChimeBlock.cs:182 (Move 메서드)
public bool Move(Vector2Int direction)
{
    // ... 기존 코드 ...
    UpdateVisuals();
    UpdatePlacementVisualFeedback(); // ← 추가
    return true;
}

// ChimeBlock.cs:192 (Rotate 메서드)
public bool Rotate(bool clockwise)
{
    // ... 기존 코드 ...
    CreateVisuals();
    UpdatePlacementVisualFeedback(); // ← 추가
    return true;
}
```

---

### 수정 4: 펜토미노 12종 생성

#### Step 1: Unity Editor에서 ScriptableObject 생성

1. Project 창에서 우클릭 → Create → Chime → Block Shape
2. 이름: `BlockShape_F`
3. Inspector에서 데이터 입력:

**F-Pentomino**:
```
Block Name: "F-Pentomino"
Cells: Size = 5
  [0] (0, 1)
  [1] (1, 1)
  [2] (1, 0)
  [3] (2, 0)
  [4] (1, 2)
Block Color: Cyan (#00D9FF)
```

#### 12개 펜토미노 좌표

<details>
<summary>📋 전체 펜토미노 좌표 (클릭하여 펼치기)</summary>

**F**: (0,1), (1,1), (1,0), (2,0), (1,2)
**I**: (0,0), (0,1), (0,2), (0,3), (0,4)
**L**: (0,0), (0,1), (0,2), (0,3), (1,0)
**N**: (0,0), (1,0), (1,1), (2,1), (2,2)
**P**: (0,0), (1,0), (0,1), (1,1), (0,2)
**T**: (0,0), (1,0), (2,0), (1,1), (1,2)
**U**: (0,0), (2,0), (0,1), (1,1), (2,1)
**V**: (0,0), (0,1), (0,2), (1,2), (2,2)
**W**: (0,0), (1,0), (1,1), (2,1), (2,2)
**X**: (1,0), (0,1), (1,1), (2,1), (1,2)
**Y**: (0,0), (1,0), (1,1), (1,2), (1,3)
**Z**: (0,0), (1,0), (1,1), (1,2), (2,2)

**권장 색상**:
F: Cyan #00D9FF
I: Purple #A855F7
L: Pink #EC4899
N: Green #10B981
P: Yellow #F59E0B
T: Blue #3B82F6
U: Orange #F97316
V: Red #EF4444
W: Lime #84CC16
X: Indigo #6366F1
Y: Teal #14B8A6
Z: Rose #F43F5E

</details>

#### Step 2: GameManager에 할당

1. Hierarchy에서 `GameManager` GameObject 선택
2. `ChimeGameManager` 컴포넌트의 `Block Shapes` 배열 크기를 **12**로 설정
3. 12개 ScriptableObject를 모두 배열에 드래그

#### Step 3: 랜덤 생성 로직 업데이트

**파일**: `Assets/Scripts/Chime/ChimeGameManager.cs:85`

```csharp
// 기존 (4종만 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, Mathf.Min(4, blockShapes.Length))];

// 수정 (전체 12종 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, blockShapes.Length)];
```

---

## Scene 설정

### 현재 Scene 구조

```
Chime (Scene)
├── Canvas
│   ├── GameManager (ChimeGameManager)
│   ├── Grid (ChimeGrid) - 12×9 = 108 cells
│   ├── PlacedBlocks (Container)
│   ├── QuadDetector (ChimeQuadDetector)
│   ├── PulseSystem (ChimePulseSystem)
│   └── UI
│       ├── ScoreText
│       ├── TurnCounterText
│       └── GameOverText
└── EventSystem
```

### 필요한 수정사항

**없음** - 현재 Scene 구조는 Phase 1에 충분합니다.

---

## 테스트 및 디버깅

### Phase 1 완료 체크리스트

#### 기본 테스트

```bash
□ Unity Editor에서 Play 버튼 누르기
□ 펜토미노 블록이 랜덤하게 생성됨 (12종 중 하나)
□ 각 블록이 5칸으로 구성됨
□ WASD로 블록 이동 가능
□ Q/E로 블록 회전 가능
```

#### 검증 색상 테스트

```bash
□ 블록을 빈 공간으로 이동 → 흰색 아웃라인 표시
□ 블록을 겹치는 위치로 이동 → 빨강 아웃라인 표시
□ 그리드 밖으로 이동 → 빨강 아웃라인 표시
```

#### 쿼드 감지 테스트

```bash
□ 블록을 배치해 2×2 형성 → 쿼드 감지 안 됨 (정상)
□ 블록을 배치해 3×3 형성 → 노란색 아웃라인으로 하이라이트
□ 쿼드 중앙에 ④ 숫자 표시
```

#### 타이머 테스트

```bash
□ 3×3 쿼드 형성 → 타이머 ④ 표시
□ 블록 1개 더 배치 → 타이머 ③ 표시
□ 블록 2개 더 배치 → 타이머 ② 표시
□ 블록 3개 더 배치 → 타이머 ① 표시
□ 블록 4개 더 배치 → 쿼드 파쇄 (총 4턴)
```

### 디버그 팁

#### 문제 1: 쿼드가 감지되지 않음

**확인 사항**:
1. `ChimeQuadDetector.cs:168`에 `quadCells.Count < 9` 있는지 확인
2. Console 창에서 "Total Quads detected: X" 로그 확인
3. 모든 셀이 점유되었는지 확인 (빈틈 없음)

#### 문제 2: 타이머가 4턴이 아님

**확인 사항**:
1. `ChimePulseSystem.cs:11`에 `PULSE_INTERVAL = 4` 있는지 확인
2. Console 창에서 "turnTimer=" 로그 확인

#### 문제 3: 검증 색상이 작동하지 않음

**확인 사항**:
1. `Move()` 및 `Rotate()` 메서드 끝에 `UpdatePlacementVisualFeedback()` 호출 있는지 확인
2. Console 창에서 에러 메시지 확인

---

## 다음 단계

### Phase 1 완료 후

✅ Phase 1 완료! 다음 단계로 이동:

**Phase 2: 프래그먼트 & 커버리지 시스템** (3주)
- 프래그먼트 생성 및 5턴 생명주기
- 커버리지 추적 및 진행도 UI
- 퍼펙트 쿼드 감지
- 콤보 멀티플라이어

**상세 가이드**: `CHIME_IMPLEMENTATION_GUIDE.md` → Phase 2 섹션 참조

### 전체 로드맵

| Phase | 목표 | 기간 | 가이드 |
|-------|------|------|--------|
| **Phase 1** | 핵심 메커니즘 | 3주 | CHIME_SETUP_GUIDE.md (현재) |
| Phase 2 | 프래그먼트 & 커버리지 | 3주 | CHIME_IMPLEMENTATION_GUIDE.md |
| Phase 3 | 5가지 게임 모드 | 8주 | CHIME_IMPLEMENTATION_GUIDE.md |
| Phase 4 | 애니메이션 & 오디오 | 6주 | CHIME_IMPLEMENTATION_GUIDE.md |
| Phase 5 | UI/UX & 폴리싱 | 6주 | CHIME_IMPLEMENTATION_GUIDE.md |

**총 기간**: 약 26주 (6.5개월)

---

## 참조

### 주요 파일

| 파일 | 목적 | 핵심 수정 위치 |
|------|------|--------------|
| ChimeQuadDetector.cs | 쿼드 감지 | :168 (최소 크기) |
| ChimePulseSystem.cs | 턴 & 타이머 | :11 (타이머 간격) |
| ChimeBlock.cs | 블록 제어 | :285 (검증 색상), :182/:192 (호출) |
| ChimeGameManager.cs | 블록 생성 | :85 (랜덤 생성) |

### 관련 문서

- **Chime_GDD.md**: 게임 디자인 문서 (전체 사양)
- **CHIME_IMPLEMENTATION_GUIDE.md**: 전체 구현 로드맵 (Phase 1-5)
- **Assets/Sprites/Chime/README.md**: 그리드 스프라이트 가이드

---

**Quick Start 가이드 종료**

궁금한 사항이 있으면:
1. Console 창의 Debug.Log 확인
2. CHIME_IMPLEMENTATION_GUIDE.md의 상세 가이드 참조
3. Chime_GDD.md의 게임 사양 참조

**Version 2.0 | 2026.02.04**
