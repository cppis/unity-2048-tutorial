# Qube Game Scene 설정 가이드

이 가이드는 Unity Editor에서 Qube 게임 Scene을 설정하는 방법을 설명합니다.

## 1. 새 Scene 생성

1. Unity Editor에서 `File > New Scene` 선택
2. Scene을 `Assets/Scenes/Qube.unity`로 저장

## 2. Canvas 설정

### Canvas 생성
1. Hierarchy에서 우클릭 > `UI > Canvas`
2. Canvas의 Canvas Scaler 컴포넌트 설정:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920` (세로 모드)
   - Screen Match Mode: `Match Width Or Height`
   - Match: `0.5`

### Canvas 배경색 설정
1. Canvas에 `UI > Image` 추가 (이름: Background)
2. Background 설정:
   - Color: `#0D1B2A` (R: 13/255, G: 27/255, B: 42/255)
   - Anchor: Stretch-Stretch
   - Left, Top, Right, Bottom: 0

## 3. UI 구조 생성

### GameManager GameObject
1. Canvas 하위에 빈 GameObject 생성 (이름: GameManager)
2. QubeGameManager 컴포넌트 추가

### Grid GameObject
1. Canvas 하위에 빈 GameObject 생성 (이름: Grid)
2. QubeGrid 컴포넌트 추가
3. RectTransform 설정:
   - Width: 760
   - Height: 570
   - Anchor Preset: Middle-Center

### Cell Prefab 생성
1. Hierarchy에서 우클릭 > `Create Empty` (이름: QubeCell)
2. QubeCell에 우클릭 > `UI > Image` 추가
3. Add Component > QubeCell 스크립트 추가
4. RectTransform 설정:
   - Width: 80
   - Height: 80
5. Image 설정:
   - Color: `#2D3436` (빈 셀 색상)
6. Hierarchy의 QubeCell GameObject를 Project 창의 Assets 폴더로 드래그하여 Prefab 생성
7. Hierarchy의 원본 QubeCell 삭제 (Prefab은 Assets에 보관됨)

### QubeQuadDetector
1. Grid GameObject에 QubeQuadDetector 컴포넌트 추가
2. Grid 레퍼런스를 Grid 자신으로 연결

### QubePulseSystem
1. Grid GameObject에 QubePulseSystem 컴포넌트 추가
2. 레퍼런스 연결:
   - Grid: Grid GameObject
   - Quad Detector: Grid GameObject (QubeQuadDetector 컴포넌트)

## 4. Block Prefab 생성

1. Hierarchy에서 우클릭 > `Create Empty` (이름: QubeBlock)
2. Add Component > RectTransform (UI GameObject로 변환)
3. Add Component > QubeBlock 스크립트 추가
4. Hierarchy의 QubeBlock GameObject를 Project 창의 Assets 폴더로 드래그하여 Prefab 생성
5. Hierarchy의 원본 QubeBlock 삭제 (Prefab은 Assets에 보관됨)

## 5. Block Shapes ScriptableObject 생성

각 블록 타입(L, I, T, O)에 대해:

1. Assets 폴더에서 우클릭 > `Create > Qube > Block Shape`
2. 이름: `BlockShape_L` (I, T, O도 동일하게)
3. Inspector에서 설정:

### L-Block
- Block Name: "L"
- Cells: Size = 3
  - Element 0: (0, 0)
  - Element 1: (1, 0)
  - Element 2: (0, 1)
- Block Color: `#00CEC9` (청록색)

### I-Block
- Block Name: "I"
- Cells: Size = 3
  - Element 0: (0, 0)
  - Element 1: (1, 0)
  - Element 2: (2, 0)
- Block Color: `#6C5CE7` (보라색)

### T-Block
- Block Name: "T"
- Cells: Size = 4
  - Element 0: (0, 0)
  - Element 1: (1, 0)
  - Element 2: (2, 0)
  - Element 3: (1, 1)
- Block Color: `#00CEC9` (청록색)

### O-Block
- Block Name: "O"
- Cells: Size = 4
  - Element 0: (0, 0)
  - Element 1: (1, 0)
  - Element 2: (0, 1)
  - Element 3: (1, 1)
- Block Color: `#6C5CE7` (보라색)

## 6. UI Text 추가

### Score Text
1. Canvas 하위에 `UI > Text` 추가 (이름: ScoreText)
2. 설정:
   - Text: "Score: 0"
   - Font Size: 48
   - Color: 흰색
   - Alignment: Left-Top
   - Anchor: Top-Left
   - Position: X=100, Y=-100

### Turn Counter Text
1. Canvas 하위에 `UI > Text` 추가 (이름: TurnCounterText)
2. 설정:
   - Text: "Turn: 0/4"
   - Font Size: 36
   - Color: 흰색
   - Alignment: Right-Top
   - Anchor: Top-Right
   - Position: X=-100, Y=-100

## 7. GameManager 레퍼런스 연결

GameManager GameObject의 QubeGameManager 컴포넌트에서:

1. **Grid**: Grid GameObject 드래그
2. **Quad Detector**: Grid GameObject (QubeQuadDetector)
3. **Pulse System**: Grid GameObject (QubePulseSystem)
4. **Block Prefab**: QubeBlock Prefab 드래그
5. **Block Shapes**: Size = 4
   - Element 0: BlockShape_L
   - Element 1: BlockShape_I
   - Element 2: BlockShape_T
   - Element 3: BlockShape_O
6. **Score Text**: ScoreText GameObject
7. **Turn Counter Text**: TurnCounterText GameObject

## 8. Grid 레퍼런스 연결

Grid GameObject의 QubeGrid 컴포넌트에서:

1. **Cell Prefab**: QubeCell Prefab 드래그
2. **Cell Size**: 80
3. **Spacing**: 5

## 9. 테스트

1. Play 버튼 클릭
2. 조작법:
   - WASD 또는 방향키: 블록 이동
   - Q: 반시계방향 회전
   - E: 시계방향 회전
   - Space: 블록 배치

## 10. 게임 규칙

- 블록을 배치하여 2x2 이상의 직사각형 영역(Quad)을 만듭니다
- 4턴마다 모든 Quad가 동시에 소거됩니다 (펄스)
- Quad 크기별 점수:
  - 2x2: 100점
  - 3x3: 300점
  - 4x4: 600점
- 동시 소거 보너스:
  - 2개: 1.5배
  - 3개 이상: 2.0배

## 문제 해결

### 블록이 보이지 않음
- Block Prefab에 QubeBlock 스크립트가 추가되었는지 확인
- Block Shapes가 올바르게 설정되었는지 확인

### 그리드가 보이지 않음
- Cell Prefab이 QubeGrid에 연결되었는지 확인
- Canvas가 Screen Space - Overlay 모드인지 확인

### 스크립트 에러
- 모든 스크립트가 Assets/Scripts/Qube 폴더에 있는지 확인
- Unity Console에서 에러 메시지 확인
