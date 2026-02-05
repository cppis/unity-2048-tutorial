# Chime Quick Start 가이드

**Unity 6000.3.2f1**
**최종 업데이트: 2026.02.05**

---

## 목차

1. [개요](#개요)
2. [현재 Scene 분석](#현재-scene-분석)
3. [현재 구현 상태](#현재-구현-상태)
4. [필수 수정 사항](#필수-수정-사항)
5. [블록 추가 가이드](#블록-추가-가이드)
6. [Scene 설정 가이드](#scene-설정-가이드)
7. [테스트 및 디버깅](#테스트-및-디버깅)
8. [다음 단계](#다음-단계)

---

## 개요

이 가이드는 Chime 프로토타입의 현재 상태를 분석하고 **GDD 사양에 맞추기 위한 작업 가이드**입니다.

### 📚 관련 문서

| 문서 | 용도 |
|------|------|
| **CHIME_SETUP_GUIDE.md** (현재) | Quick Start - 즉시 시작 가능한 기본 설정 |
| **Chime_GDD.md** | 게임 디자인 문서 (전체 사양) |

---

## 현재 Scene 분석

### ⚠️ 중요: Scene 컴포넌트 불일치 발견

**Scene 파일**: `Assets/Scenes/Chime.unity`

현재 씬이 **Qube 컴포넌트**를 사용하고 있습니다. Chime 스크립트가 별도로 존재하지만 씬에 연결되지 않았습니다.

### 현재 Scene 계층 구조

```
Chime.unity
├── Main Camera (Camera, AudioListener)
├── Canvas (Canvas, CanvasScaler, GraphicRaycaster)
│   ├── Background (Image - Color: #0D1B2A)
│   ├── GameManager ← ❌ QubeGameManager 사용 중
│   ├── Grid ← ❌ QubeGrid, QubePulseSystem, QubeQuadDetector 사용 중
│   ├── ScoreText (TextMeshProUGUI - "Score: 0")
│   └── TurnCounterText (TextMeshProUGUI - "Turn: 0/4")
└── EventSystem (EventSystem, StandaloneInputModule)
```

### Scene 컴포넌트 상세

| GameObject | 현재 컴포넌트 | 변경 필요 |
|------------|--------------|----------|
| GameManager | `QubeGameManager` | → `ChimeGameManager` |
| Grid | `QubeGrid` | → `ChimeGrid` |
| Grid | `QubeQuadDetector` | → `ChimeQuadDetector` |
| Grid | `QubePulseSystem` | → `ChimePulseSystem` |

### 참조 설정 (Inspector)

**GameManager (QubeGameManager)**:
- grid: Grid의 QubeGrid
- quadDetector: Grid의 QubeQuadDetector
- pulseSystem: Grid의 QubePulseSystem
- blockPrefab: `Assets/Prefabs/Qube/QubeBlock.prefab`
- blockShapes: 4개 ScriptableObject (L, I, T, O 형태)
- scoreText: ScoreText
- turnCounterText: TurnCounterText

**Grid (QubeGrid)**:
- cellPrefab: `Assets/Prefabs/Qube/QubeCell.prefab`
- cellSize: 80
- spacing: 5

---

## 현재 구현 상태

### Chime 스크립트 파일 (구현 완료)

| 파일 | 경로 | 상태 |
|------|------|------|
| ChimeGameManager.cs | Assets/Scripts/Chime/ | ✅ 구현됨 |
| ChimeGrid.cs | Assets/Scripts/Chime/ | ✅ 12×9 그리드 |
| ChimeCell.cs | Assets/Scripts/Chime/ | ✅ 구현됨 |
| ChimeBlock.cs | Assets/Scripts/Chime/ | ✅ 시각 피드백 구현됨 |
| ChimeBlockShape.cs | Assets/Scripts/Chime/ | ✅ 펜토미노 12종 구현됨 |
| ChimeQuad.cs | Assets/Scripts/Chime/ | ✅ 구현됨 |
| ChimeQuadDetector.cs | Assets/Scripts/Chime/ | ⚠️ 3×3 최소 크기 수정 필요 |
| ChimePulseSystem.cs | Assets/Scripts/Chime/ | ⚠️ 4턴 간격 수정 필요 |

### 현재 vs. GDD 사양 비교

| 항목 | 현재 상태 | GDD 목표 | 수정 필요 |
|------|----------|---------|----------|
| Scene 컴포넌트 | Qube* 사용 | Chime* 사용 | 🔴 필수 |
| 그리드 크기 | 12×9 | 12×9 | ✅ 일치 |
| 셀 크기 | 80px | 80px | ✅ 일치 |
| 셀 간격 | 5px | 5px | ✅ 일치 |
| 블록 타입 | 12 펜토미노 (5칸) | 12 펜토미노 (5칸) | ✅ 구현됨 |
| 쿼드 최소 크기 | 2×2 (4셀) | 3×3 (9셀) | 🔴 필수 |
| 펄스 간격 | 8턴 | 4턴 | 🔴 필수 |
| 배치 피드백 | 흰색/빨강 아웃라인 | 흰색/빨강 아웃라인 | ✅ 구현됨 |

---

## 필수 수정 사항

### Phase 0: Scene 컴포넌트 교체 (선행 작업)

> ⚠️ **핵심 원칙**: Chime Scene은 Qube Scene 설정을 **복사하여 새 구조로 수정**합니다.
> Qube의 검증된 구조를 기반으로 Chime 전용 컴포넌트로 교체하는 방식입니다.

#### Step 0: Qube Scene 구조 복사 (권장)

**방법 A: Scene 복제 후 수정** (권장)
1. `Assets/Scenes/Qube.unity` 파일 복제
2. 이름 변경: `Chime.unity`
3. Scene 열고 아래 Step 1~2 진행

**방법 B: 기존 Chime Scene에 Qube 참조값 적용**
1. Qube.unity와 Chime.unity를 동시에 열기
2. Qube의 Inspector 값들을 참조하여 Chime에 적용

#### Step 1: Chime Prefab 생성 (Qube 복사 → 수정)

1. **ChimeCell Prefab 생성**:
   - `Assets/Prefabs/Qube/QubeCell.prefab` 복제 (Ctrl+D)
   - 이름 변경: `ChimeCell.prefab`
   - 경로: `Assets/Prefabs/Chime/ChimeCell.prefab`
   - **컴포넌트 교체**:
     - `QubeCell` 제거 (Remove Component)
     - `ChimeCell` 추가 (Add Component)
   - Qube Prefab의 RectTransform, Image 설정은 그대로 유지

2. **ChimeBlock Prefab 생성**:
   - `Assets/Prefabs/Qube/QubeBlock.prefab` 복제 (Ctrl+D)
   - 이름 변경: `ChimeBlock.prefab`
   - 경로: `Assets/Prefabs/Chime/ChimeBlock.prefab`
   - **컴포넌트 교체**:
     - `QubeBlock` 제거 (Remove Component)
     - `ChimeBlock` 추가 (Add Component)
   - Qube Prefab의 기본 구조는 그대로 유지

#### Step 2: Scene 컴포넌트 교체 (Qube → Chime)

> 💡 **중요**: Qube 컴포넌트의 Inspector 값을 **먼저 메모**한 후 제거하세요.
> 특히 `cellPrefab`, `blockPrefab`, `blockShapes` 참조 경로를 기록해두세요.

**Grid GameObject** (Qube 컴포넌트 제거 → Chime 컴포넌트 추가):

| 순서 | 작업 | Qube에서 복사할 값 |
|------|------|-------------------|
| 1 | `QubeGrid` 제거 | cellSize: 80, spacing: 5 메모 |
| 2 | `QubeQuadDetector` 제거 | - |
| 3 | `QubePulseSystem` 제거 | - |
| 4 | `ChimeGrid` 추가 | cellSize: 80, spacing: 5 적용 |
| 5 | `ChimeQuadDetector` 추가 | - |
| 6 | `ChimePulseSystem` 추가 | - |

**참조 연결**:
- ChimeGrid.cellPrefab → `Assets/Prefabs/Chime/ChimeCell.prefab`
- ChimeQuadDetector.grid → ChimeGrid
- ChimePulseSystem.grid → ChimeGrid
- ChimePulseSystem.quadDetector → ChimeQuadDetector

**GameManager GameObject** (Qube 컴포넌트 제거 → Chime 컴포넌트 추가):

| 순서 | 작업 | Qube에서 복사할 참조 |
|------|------|---------------------|
| 1 | `QubeGameManager` 제거 | blockShapes 배열 구조 메모 |
| 2 | `ChimeGameManager` 추가 | - |

**참조 연결** (Qube 설정 기반):
- grid → ChimeGrid
- quadDetector → ChimeQuadDetector
- pulseSystem → ChimePulseSystem
- blockPrefab → `Assets/Prefabs/Chime/ChimeBlock.prefab`
- blockShapes → (새로운 펜토미노 ScriptableObject들)
- scoreText → ScoreText (Qube와 동일)
- turnCounterText → TurnCounterText (Qube와 동일)

#### Step 3: 설정값 검증 (Qube 값과 비교)

| 항목 | Qube 설정 | Chime 설정 (복사 후) |
|------|----------|---------------------|
| Grid 크기 | 12×9 | 12×9 (동일) |
| Cell 크기 | 80px | 80px (동일) |
| Cell 간격 | 5px | 5px (동일) |
| 블록 종류 | L, I, T, O (4종) | 펜토미노 12종 (변경) |
| Quad 최소 | 2×2 | 3×3 (변경) |
| 펄스 간격 | 8턴 | 4턴 (변경) |

---

### Phase 1: 핵심 메커니즘 수정

#### 수정 1: 쿼드 최소 크기 3×3

**파일**: `Assets/Scripts/Chime/ChimeQuadDetector.cs:168`

```csharp
// 기존 (2×2 = 4셀)
if (!isValid || quadCells.Count < 4)
    continue;

// 수정 (3×3 = 9셀)
if (!isValid || quadCells.Count < 9)
    continue;
```

---

#### 수정 2: 펄스 간격 4턴

**파일**: `Assets/Scripts/Chime/ChimePulseSystem.cs:11`

```csharp
// 기존
private const int PULSE_INTERVAL = 8;

// 수정
private const int PULSE_INTERVAL = 4;
```

---

#### 수정 3: 배치 검증 시각 피드백 ✅ 구현 완료

**파일**: `Assets/Scripts/Chime/ChimeBlock.cs:285-309`

`UpdatePlacementVisualFeedback()` 메서드가 GDD 사양대로 구현되어 있습니다:

```csharp
public void UpdatePlacementVisualFeedback()
{
    bool canPlace = CanPlace();
    Color visualColor;

    if (canPlace)
    {
        // 배치 가능: 흰색 아웃라인 느낌 (밝게)
        visualColor = new Color(1f, 1f, 1f, 0.5f);
    }
    else
    {
        // 배치 불가능: 빨강 아웃라인 느낌
        visualColor = new Color(1f, 0.27f, 0.27f, 0.7f);
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

---

### 블록 추가 가이드

이 섹션에서는 Chime에 새로운 블록을 추가하는 방법을 설명합니다.

#### 블록 시스템 구조

```
ChimeBlock (MonoBehaviour)
├── shape: ChimeBlockShape (ScriptableObject 참조)
├── position: Vector2Int (그리드 상 위치)
├── currentCells: Vector2Int[] (블록을 구성하는 셀 좌표)
└── cellObjects: List<GameObject> (시각적 UI 요소)

ChimeBlockShape (ScriptableObject)
├── blockName: string (블록 이름)
├── cells: Vector2Int[] (상대 좌표 배열)
└── blockColor: Color (블록 색상)
```

#### Step 1: ChimeBlockShape ScriptableObject 생성

**방법 1: Unity Editor에서 직접 생성**

1. Project 창에서 우클릭 → **Create → Chime → Block Shape**
2. 생성된 ScriptableObject 이름 변경 (예: `Pentomino_L`)
3. 저장 경로: `Assets/Data/Chime/Pentominoes/`

**방법 2: 기존 블록 복제 후 수정**

1. 기존 ScriptableObject 복제 (Ctrl+D)
2. 이름 및 속성 수정

#### Step 2: Inspector에서 블록 속성 설정

| 속성 | 설명 | 예시 |
|------|------|------|
| **Block Name** | 블록 식별 이름 | `L_Pentomino` |
| **Cells** | 블록을 구성하는 셀의 상대 좌표 배열 | 아래 참조 |
| **Block Color** | 블록 색상 (RGB/Hex) | `#EC4899` (Pink) |

**Cells 좌표 입력 규칙**:
- (0,0)을 기준점으로 상대 좌표 입력
- X: 오른쪽 방향 (+), Y: 위쪽 방향 (+)
- 배열 크기 = 블록을 구성하는 셀 수 (펜토미노는 항상 5)

#### 펜토미노 12종 좌표 시각화

```
┌───────────────────────────────────────────────────────────────────────────┐
│ F 펜토미노              I 펜토미노              L 펜토미노                │
│                                                                           │
│    [4]                  [4]                     [4]                       │
│ [0][1]                  [3]                     [3]                       │
│    [2][3]               [2]                     [2]                       │
│                         [1]                     [1]                       │
│                         [0]                     [0][4]                    │
│                                                                           │
│ (0,1),(1,1),(1,0),      (0,0),(0,1),(0,2),      (0,0),(0,1),(0,2),        │
│ (2,0),(1,2)             (0,3),(0,4)             (0,3),(1,0)               │
│ Color: #00D9FF          Color: #A855F7          Color: #EC4899            │
└───────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────┐
│ N 펜토미노              P 펜토미노              T 펜토미노                │
│                                                                           │
│       [4]               [4]                     [0][1][2]                 │
│    [3][2]               [2][3]                     [3]                    │
│ [0][1]                  [0][1]                     [4]                    │
│                                                                           │
│ (0,0),(1,0),(1,1),      (0,0),(1,0),(0,1),      (0,0),(1,0),(2,0),        │
│ (2,1),(2,2)             (1,1),(0,2)             (1,1),(1,2)               │
│ Color: #10B981          Color: #F59E0B          Color: #3B82F6            │
└───────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────┐
│ U 펜토미노              V 펜토미노              W 펜토미노                │
│                                                                           │
│ [2][3][4]               [2][3][4]                  [4]                    │
│ [0]   [1]               [1]                     [2][3]                    │
│                         [0]                     [0][1]                    │
│                                                                           │
│ (0,0),(2,0),(0,1),      (0,0),(0,1),(0,2),      (0,0),(1,0),(1,1),        │
│ (1,1),(2,1)             (1,2),(2,2)             (2,1),(2,2)               │
│ Color: #F97316          Color: #EF4444          Color: #84CC16            │
└───────────────────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────┐
│ X 펜토미노              Y 펜토미노              Z 펜토미노                │
│                                                                           │
│    [4]                     [4]                  [0][1]                    │
│ [1][2][3]                  [3]                     [2]                    │
│    [0]                  [0][1]                     [3][4]                 │
│                            [2]                                            │
│                                                                           │
│ (1,0),(0,1),(1,1),      (0,0),(1,0),(1,1),      (0,0),(1,0),(1,1),        │
│ (2,1),(1,2)             (1,2),(1,3)             (1,2),(2,2)               │
│ Color: #6366F1          Color: #14B8A6          Color: #F43F5E            │
└───────────────────────────────────────────────────────────────────────────┘
```

> 💡 **좌표 읽는 법**: 숫자는 Cells 배열의 인덱스입니다. `[0]`이 Element 0의 위치를 나타냅니다.

#### Step 3: GameManager에 블록 등록

**파일**: Chime Scene → GameManager → ChimeGameManager

1. Inspector에서 **Block Shapes** 배열 확인
2. Size 증가 (예: 4 → 5)
3. 새 Element에 생성한 ScriptableObject 드래그 앤 드롭

```
Block Shapes (Array)
├── Element 0: Pentomino_F
├── Element 1: Pentomino_I
├── Element 2: Pentomino_L
├── ...
└── Element 11: Pentomino_Z  ← 새로 추가
```

#### Step 4: 블록 테스트

1. Unity Editor에서 Play
2. 새 블록이 랜덤으로 생성되는지 확인
3. 회전 테스트 (Q/E 키)
4. 배치 테스트 (Space 키)

---

#### 수정 4: 펜토미노 12종 생성 ✅ 구현 완료

**파일**: `Assets/Scripts/Chime/ChimeBlockShape.cs`

펜토미노 12종 정적 메서드가 구현되어 있습니다:

```csharp
// === 펜토미노 (5칸) ===

public static Vector2Int[] GetF_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 2)
    };
}

public static Vector2Int[] GetI_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4)
    };
}

public static Vector2Int[] GetL_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(1, 0)
    };
}

public static Vector2Int[] GetN_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2)
    };
}

public static Vector2Int[] GetP_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2)
    };
}

public static Vector2Int[] GetT_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(1, 2)
    };
}

public static Vector2Int[] GetU_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(2, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1)
    };
}

public static Vector2Int[] GetV_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(0, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
    };
}

public static Vector2Int[] GetW_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2)
    };
}

public static Vector2Int[] GetX_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(1, 0), new Vector2Int(0, 1),
        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2)
    };
}

public static Vector2Int[] GetY_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3)
    };
}

public static Vector2Int[] GetZ_Pentomino()
{
    return new Vector2Int[] {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 2)
    };
}
```

#### ScriptableObject 생성

Unity Editor에서 12개의 펜토미노 ScriptableObject 생성:

| 이름 | 색상 (Hex) | 셀 좌표 |
|------|-----------|---------|
| Pentomino_F | #00D9FF (Cyan) | (0,1), (1,1), (1,0), (2,0), (1,2) |
| Pentomino_I | #A855F7 (Purple) | (0,0), (0,1), (0,2), (0,3), (0,4) |
| Pentomino_L | #EC4899 (Pink) | (0,0), (0,1), (0,2), (0,3), (1,0) |
| Pentomino_N | #10B981 (Green) | (0,0), (1,0), (1,1), (2,1), (2,2) |
| Pentomino_P | #F59E0B (Yellow) | (0,0), (1,0), (0,1), (1,1), (0,2) |
| Pentomino_T | #3B82F6 (Blue) | (0,0), (1,0), (2,0), (1,1), (1,2) |
| Pentomino_U | #F97316 (Orange) | (0,0), (2,0), (0,1), (1,1), (2,1) |
| Pentomino_V | #EF4444 (Red) | (0,0), (0,1), (0,2), (1,2), (2,2) |
| Pentomino_W | #84CC16 (Lime) | (0,0), (1,0), (1,1), (2,1), (2,2) |
| Pentomino_X | #6366F1 (Indigo) | (1,0), (0,1), (1,1), (2,1), (1,2) |
| Pentomino_Y | #14B8A6 (Teal) | (0,0), (1,0), (1,1), (1,2), (1,3) |
| Pentomino_Z | #F43F5E (Rose) | (0,0), (1,0), (1,1), (1,2), (2,2) |

**생성 방법**:
1. Project 창에서 우클릭 → Create → Chime → Block Shape
2. Inspector에서 이름, 색상, 셀 좌표 입력
3. `Assets/Data/Chime/Pentominoes/` 폴더에 저장

---

#### 수정 5: 랜덤 블록 생성 로직

**파일**: `Assets/Scripts/Chime/ChimeGameManager.cs:85`

```csharp
// 기존 (4종만 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, Mathf.Min(4, blockShapes.Length))];

// 수정 (전체 펜토미노 사용)
ChimeBlockShape randomShape = blockShapes[Random.Range(0, blockShapes.Length)];
```

---

## Scene 설정 가이드

### 최종 Scene 구조 (목표)

```
Chime.unity
├── Main Camera
│   └── Camera, AudioListener
├── Canvas
│   ├── Background
│   │   └── Image (Color: #0D1B2A)
│   ├── GameManager
│   │   └── ChimeGameManager
│   │       ├── grid → Grid/ChimeGrid
│   │       ├── quadDetector → Grid/ChimeQuadDetector
│   │       ├── pulseSystem → Grid/ChimePulseSystem
│   │       ├── blockPrefab → ChimeBlock.prefab
│   │       ├── blockShapes → [12개 펜토미노]
│   │       ├── scoreText → ScoreText
│   │       └── turnCounterText → TurnCounterText
│   ├── Grid
│   │   ├── ChimeGrid
│   │   │   └── cellPrefab → ChimeCell.prefab
│   │   ├── ChimeQuadDetector
│   │   │   └── grid → ChimeGrid
│   │   └── ChimePulseSystem
│   │       ├── grid → ChimeGrid
│   │       └── quadDetector → ChimeQuadDetector
│   ├── PlacedBlocks (런타임 생성)
│   ├── ScoreText
│   │   └── TextMeshProUGUI
│   └── TurnCounterText
│       └── TextMeshProUGUI
└── EventSystem
```

### Inspector 설정 값

**ChimeGrid**:
| 속성 | 값 |
|------|-----|
| Cell Prefab | ChimeCell.prefab |
| Cell Size | 80 |
| Spacing | 5 |

**ChimeGameManager**:
| 속성 | 값 |
|------|-----|
| Block Prefab | ChimeBlock.prefab |
| Block Shapes | 12개 펜토미노 ScriptableObject |

---

## 테스트 및 디버깅

### Phase 0 완료 체크리스트 (Qube 복사 → Chime 수정)

```
□ Step 0: Qube Scene 구조 확인/복사
  □ Qube.unity의 계층 구조 확인
  □ Qube Inspector 값 메모 (cellSize, spacing 등)

□ Step 1: Prefab 복사 및 수정
  □ QubeCell.prefab → ChimeCell.prefab 복제
  □ QubeCell 컴포넌트 제거 → ChimeCell 추가
  □ QubeBlock.prefab → ChimeBlock.prefab 복제
  □ QubeBlock 컴포넌트 제거 → ChimeBlock 추가

□ Step 2: Scene 컴포넌트 교체
  □ Grid: QubeGrid/QubeQuadDetector/QubePulseSystem 제거
  □ Grid: ChimeGrid/ChimeQuadDetector/ChimePulseSystem 추가
  □ Grid: cellPrefab → ChimeCell.prefab 연결
  □ GameManager: QubeGameManager 제거
  □ GameManager: ChimeGameManager 추가
  □ GameManager: 모든 참조 연결 완료

□ Step 3: 검증
  □ Qube 설정값과 비교 (cellSize: 80, spacing: 5)
  □ Unity Editor에서 Play 시 에러 없음
  □ 그리드 12×9 정상 생성
```

### Phase 1 완료 체크리스트 (핵심 메커니즘)

```
□ 쿼드 최소 크기 3×3 변경 완료
□ 펄스 간격 4턴 변경 완료
✓ 배치 검증 피드백 구현 완료 (ChimeBlock.cs)
✓ 펜토미노 12종 정적 메서드 구현 완료 (ChimeBlockShape.cs)
□ 펜토미노 12종 ScriptableObject 생성 (Unity Editor에서)
□ 랜덤 생성 로직 수정 완료
```

### 기능 테스트

#### 블록 이동/회전 테스트
```
□ WASD로 블록 이동 가능
□ Q/E로 블록 회전 가능
□ 그리드 경계에서 이동 제한됨
□ Wall kick 동작 확인
```

#### 배치 피드백 테스트
```
□ 배치 가능 위치 → 흰색/밝은 색상 표시
□ 배치 불가 위치 (겹침) → 빨간색 표시
□ 배치 불가 위치 (그리드 밖) → 빨간색 표시
```

#### 쿼드 감지 테스트
```
□ 2×2 배치 → 쿼드 감지 안 됨 (정상)
□ 3×3 배치 → 노란색 아웃라인 하이라이트
□ 쿼드 중앙에 남은 턴 수 표시 (④)
```

#### 펄스 시스템 테스트
```
□ 쿼드 형성 → 타이머 ④ 시작
□ 블록 1개 배치 → 타이머 ③
□ 블록 2개 배치 → 타이머 ②
□ 블록 3개 배치 → 타이머 ①
□ 블록 4개 배치 → 쿼드 파쇄 + 점수 획득
```

### 디버그 로그 확인

Console 창에서 다음 로그 확인:

```
=== Turn X Started ===
Detected Y potential new quads
Total Quads detected: Z
=== Turn X Ended: Z active quads ===
```

---

## 다음 단계

### Phase 2: 프래그먼트 & 커버리지 시스템

- [ ] 프래그먼트 생성 로직 (쿼드 파쇄 후 남은 블록)
- [ ] 프래그먼트 5턴 생명주기
- [ ] 프래그먼트 색상 변화 (5단계)
- [ ] 커버리지 추적 시스템
- [ ] 진행도 UI (0% ~ 100%)
- [ ] 퍼펙트 쿼드 감지 및 보너스

### Phase 3: 게임 모드

- [ ] Practice 모드 (타이머 없음)
- [ ] Standard 모드 (타이머 + 타임 보너스)
- [ ] Sharp 모드 (라이프 시스템)
- [ ] Strike 모드 (90초 스피드런)
- [ ] Challenge 모드 (복잡한 그리드)

---

## 참조

### 주요 파일 경로

| 용도 | 경로 |
|------|------|
| Scene | Assets/Scenes/Chime.unity |
| 스크립트 | Assets/Scripts/Chime/*.cs |
| Prefab | Assets/Prefabs/Chime/*.prefab |
| 데이터 | Assets/Data/Chime/Pentominoes/*.asset |

### 핵심 수정 위치

| 수정 내용 | 파일:라인 | 상태 |
|----------|----------|------|
| 쿼드 최소 크기 | ChimeQuadDetector.cs:168 | 🔴 수정 필요 |
| 펄스 간격 | ChimePulseSystem.cs:11 | 🔴 수정 필요 |
| 배치 피드백 | ChimeBlock.cs:285-309 | ✅ 구현됨 |
| 펜토미노 정의 | ChimeBlockShape.cs:10-105 | ✅ 구현됨 |
| 랜덤 생성 | ChimeGameManager.cs:85 | 🔴 수정 필요 |

---

**Version 3.1 | 2026.02.06**
