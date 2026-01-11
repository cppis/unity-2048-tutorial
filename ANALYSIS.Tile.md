# TileGrid, TileRow, TileCell 컴포넌트 생성 방법

이 문서는 Unity 2048 게임에서 TileGrid, TileRow, TileCell 컴포넌트들이 어떻게 생성되고 설정되었는지 설명합니다.

## 1. Unity 에디터에서 수동으로 계층 구조 생성

이 프로젝트에서 TileGrid, TileRow, TileCell은 **프로그래밍 방식으로 생성되지 않고**, Unity 에디터에서 수동으로 GameObject 계층 구조를 만들어 설정했습니다.

### 계층 구조

```
Grid (GameObject)
├── TileGrid (Component)
└── Children:
    ├── Row (GameObject) [0]
    │   ├── TileRow (Component)
    │   └── Children:
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       └── Cell (GameObject)  [TileCell Component]
    ├── Row (GameObject) [1]
    │   ├── TileRow (Component)
    │   └── Children:
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       └── Cell (GameObject)  [TileCell Component]
    ├── Row (GameObject) [2]
    │   ├── TileRow (Component)
    │   └── Children:
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       ├── Cell (GameObject)  [TileCell Component]
    │       └── Cell (GameObject)  [TileCell Component]
    └── Row (GameObject) [3]
        ├── TileRow (Component)
        └── Children:
            ├── Cell (GameObject)  [TileCell Component]
            ├── Cell (GameObject)  [TileCell Component]
            ├── Cell (GameObject)  [TileCell Component]
            └── Cell (GameObject)  [TileCell Component]
```

## 2. Unity 에디터에서의 생성 과정

### 단계별 생성 방법

#### Step 1: Grid GameObject 생성
1. Unity 에디터의 Hierarchy 창에서 우클릭
2. `UI > Empty` 선택하여 빈 GameObject 생성
3. 이름을 "Grid"로 변경
4. Inspector에서 `Add Component` 클릭
5. `TileGrid` 스크립트 컴포넌트 추가

#### Step 2: Row GameObject들 생성
1. Grid GameObject를 선택한 상태에서 우클릭
2. `Create Empty` 선택 (4번 반복)
3. 각각의 이름을 "Row"로 설정
4. 각 Row GameObject에 `TileRow` 스크립트 컴포넌트 추가

#### Step 3: Cell GameObject들 생성
1. 각 Row GameObject를 선택한 상태에서 우클릭
2. `Create Empty` 선택 (각 Row마다 4번 반복)
3. 각각의 이름을 "Cell"로 설정
4. 각 Cell GameObject에:
   - `TileCell` 스크립트 컴포넌트 추가
   - `Image` 컴포넌트 추가 (UI 배경 표시용)
   - RectTransform 크기 조정 (약 105x105)
   - 적절한 위치 배치

#### Step 4: 레이아웃 설정
1. Grid의 RectTransform 설정
2. 각 Row의 위치 및 크기 조정
3. 각 Cell의 간격 및 위치 조정

## 3. 자동 초기화 과정

수동으로 계층 구조를 만들면, 런타임에 각 컴포넌트의 `Awake()` 메서드가 자동으로 참조를 연결합니다.

### TileGrid.Awake() (Assets/Scripts/TileGrid.cs:12-20)

```csharp
private void Awake()
{
    // 자식 오브젝트에서 모든 TileRow 컴포넌트를 찾음
    rows = GetComponentsInChildren<TileRow>();

    // 자식 오브젝트에서 모든 TileCell 컴포넌트를 찾음
    cells = GetComponentsInChildren<TileCell>();

    // 각 셀에 좌표(x, y)를 자동으로 할당
    for (int i = 0; i < cells.Length; i++) {
        cells[i].coordinates = new Vector2Int(i % Width, i / Width);
    }
}
```

**동작 원리:**
- `GetComponentsInChildren<T>()`: Unity의 내장 함수로, 현재 GameObject와 모든 자식 GameObject에서 T 타입의 컴포넌트를 찾음
- Grid GameObject 아래의 모든 Row와 Cell을 자동으로 배열에 저장
- 셀의 인덱스를 기반으로 2D 좌표 계산:
  - `x = i % Width` (0, 1, 2, 3 반복)
  - `y = i / Width` (0, 0, 0, 0, 1, 1, 1, 1, ...)

### TileRow.Awake() (Assets/Scripts/TileRow.cs:7-10)

```csharp
private void Awake()
{
    // 자식 오브젝트에서 모든 TileCell 컴포넌트를 찾음
    cells = GetComponentsInChildren<TileCell>();
}
```

**동작 원리:**
- 각 Row는 자신의 자식 Cell들만 찾아서 배열에 저장
- 이를 통해 각 Row가 4개의 Cell 참조를 가지게 됨

## 4. 왜 수동 생성 방식을 사용했나?

### 장점:
1. **시각적 편집**: Unity 에디터에서 직접 위치와 크기를 조정할 수 있음
2. **디자인 유연성**: 각 Cell의 간격, 색상, 크기를 자유롭게 조정
3. **간단한 구조**: 4x4 고정 그리드이므로 동적 생성이 불필요
4. **디버깅 용이**: Hierarchy 창에서 구조를 한눈에 확인 가능

### 코드 생성 방식과의 비교:

만약 코드로 생성한다면 다음과 같은 코드가 필요했을 것입니다:

```csharp
// 코드로 생성하는 예시 (이 프로젝트에서는 사용하지 않음)
void CreateGrid()
{
    for (int y = 0; y < 4; y++) {
        GameObject rowObj = new GameObject("Row");
        rowObj.transform.SetParent(gridTransform);
        TileRow row = rowObj.AddComponent<TileRow>();

        for (int x = 0; x < 4; x++) {
            GameObject cellObj = new GameObject("Cell");
            cellObj.transform.SetParent(rowObj.transform);
            TileCell cell = cellObj.AddComponent<TileCell>();
            // 위치, 크기 설정 등...
        }
    }
}
```

하지만 이 프로젝트에서는 **단순하고 고정된 4x4 그리드**이므로, Unity 에디터에서 수동으로 만드는 것이 더 효율적입니다.

## 5. Cell의 구조

각 Cell GameObject는 다음 컴포넌트들을 가집니다:

1. **RectTransform**: UI 위치 및 크기
2. **TileCell**: 셀의 논리적 상태 관리
3. **Image**: 셀의 배경 이미지 (회색 사각형)
4. **CanvasRenderer**: UI 렌더링

```yaml
Cell GameObject:
  - RectTransform
    - Size: 105 x 105
    - Anchor: Top Left (그리드 내 정렬용)
  - TileCell Component
    - coordinates: Vector2Int (자동 할당)
    - tile: Tile (런타임에 설정)
  - Image Component
    - Color: RGB(204, 193, 180) - 회색
    - Type: Sliced (모서리가 둥근 사각형)
  - CanvasRenderer
```

## 6. 씬 파일에서의 구조

`Assets/Scenes/2048.unity` 파일에 다음과 같이 저장되어 있습니다:

```yaml
GameObject: Grid (ID: 1761606469)
  Component: TileGrid (GUID: 8e58752ab4469474d8d7adb1a91e565f)
  Children:
    - Row (ID: 74210738)
        Component: TileRow (GUID: 85a06c4367d8bca4e8e221290e23d1b0)
        Children: 4개의 Cell
    - Row (ID: 2117100432)
        Component: TileRow
        Children: 4개의 Cell
    - Row (ID: 856523463)
        Component: TileRow
        Children: 4개의 Cell
    - Row (ID: 1857828989)
        Component: TileRow
        Children: 4개의 Cell
```

각 Cell은 `TileCell` 컴포넌트 (GUID: 1335b6a2298860847bd72644fb64f146)를 가집니다.

## 요약

- **생성 방법**: Unity 에디터에서 수동으로 GameObject 계층 구조 생성
- **컴포넌트 추가**: 각 GameObject에 해당 스크립트 컴포넌트를 수동으로 추가
- **초기화**: `Awake()` 메서드에서 `GetComponentsInChildren`으로 자동 연결
- **좌표 설정**: TileGrid.Awake()에서 인덱스 기반으로 자동 계산
- **장점**: 시각적 편집 가능, 간단한 구조에 적합

이 방식은 고정 크기의 그리드(4x4)를 가진 2048 게임에 최적화된 접근 방법입니다.
