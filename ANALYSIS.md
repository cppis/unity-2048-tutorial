# 프로젝트 시작 시 동작 분석

이 문서는 Unity 2048 게임이 시작될 때 어떻게 동작하는지 설명합니다.

## 1. GameManager 초기화 (가장 먼저 실행)

`GameManager`는 `[DefaultExecutionOrder(-1)]` 속성 때문에 다른 스크립트보다 먼저 실행됩니다.

**Awake()** (Assets/Scripts/GameManager.cs:17-24):
- 싱글톤 패턴으로 인스턴스 설정
- 이미 인스턴스가 있으면 현재 객체를 파괴

```csharp
private void Awake()
{
    if (Instance != null) {
        DestroyImmediate(gameObject);
    } else {
        Instance = this;
    }
}
```

## 2. 그리드 시스템 초기화

**TileGrid.Awake()** (Assets/Scripts/TileGrid.cs:12-20):
- 자식 오브젝트에서 모든 `TileRow`를 찾음
- 모든 `TileCell`을 찾아 배열에 저장
- 각 셀에 좌표(x, y) 할당

```csharp
private void Awake()
{
    rows = GetComponentsInChildren<TileRow>();
    cells = GetComponentsInChildren<TileCell>();

    for (int i = 0; i < cells.Length; i++) {
        cells[i].coordinates = new Vector2Int(i % Width, i / Width);
    }
}
```

**TileBoard.Awake()** (Assets/Scripts/TileBoard.cs:14-18):
- `TileGrid` 컴포넌트 참조 가져오기
- 타일 리스트 초기화 (최대 16개)

## 3. 게임 시작

**GameManager.Start()** (Assets/Scripts/GameManager.cs:33-36):
- `NewGame()` 호출

**NewGame()** (Assets/Scripts/GameManager.cs:38-53):
1. 점수를 0으로 리셋
2. PlayerPrefs에서 최고 점수 로드하여 표시
3. 게임오버 화면 숨김 (alpha = 0, interactable = false)
4. `board.ClearBoard()` - 보드의 모든 타일 제거
5. `board.CreateTile()` - 첫 번째 타일 생성 (무작위 빈 셀에)
6. `board.CreateTile()` - 두 번째 타일 생성
7. `board.enabled = true` - 입력 받기 시작

```csharp
public void NewGame()
{
    // reset score
    SetScore(0);
    hiscoreText.text = LoadHiscore().ToString();

    // hide game over screen
    gameOver.alpha = 0f;
    gameOver.interactable = false;

    // update board state
    board.ClearBoard();
    board.CreateTile();
    board.CreateTile();
    board.enabled = true;
}
```

## 4. 타일 생성 과정

**CreateTile()** (Assets/Scripts/TileBoard.cs:33-39):
1. `tilePrefab`을 인스턴스화
2. `tileStates[0]` (숫자 2)로 상태 설정
3. 무작위 빈 셀을 찾아서 타일을 그곳에 스폰
4. 타일을 리스트에 추가

```csharp
public void CreateTile()
{
    Tile tile = Instantiate(tilePrefab, grid.transform);
    tile.SetState(tileStates[0]);
    tile.Spawn(grid.GetRandomEmptyCell());
    tiles.Add(tile);
}
```

**Tile.Spawn()** (Assets/Scripts/Tile.cs:30-40):
- 타일과 셀 간의 양방향 참조 설정
- 타일의 위치를 셀의 위치로 즉시 이동

```csharp
public void Spawn(TileCell cell)
{
    if (this.cell != null) {
        this.cell.tile = null;
    }

    this.cell = cell;
    this.cell.tile = this;

    transform.position = cell.transform.position;
}
```

## 5. 무작위 빈 셀 선택

**GetRandomEmptyCell()** (Assets/Scripts/TileGrid.cs:45-65):
- 무작위 인덱스에서 시작
- 빈 셀을 찾을 때까지 순회
- 모든 셀이 차있으면 null 반환

```csharp
public TileCell GetRandomEmptyCell()
{
    int index = Random.Range(0, cells.Length);
    int startingIndex = index;

    while (cells[index].Occupied)
    {
        index++;

        if (index >= cells.Length) {
            index = 0;
        }

        // all cells are occupied
        if (index == startingIndex) {
            return null;
        }
    }

    return cells[index];
}
```

## 실행 흐름 요약

```
Unity 시작
  ↓
GameManager.Awake() (싱글톤 설정)
  ↓
TileGrid.Awake() (그리드 구조 초기화, 셀 좌표 설정)
TileBoard.Awake() (보드 초기화, 타일 리스트 생성)
  ↓
GameManager.Start()
  ↓
NewGame()
  ↓
- 점수 리셋 (0으로 설정)
- 최고 점수 로드 및 표시
- 게임오버 화면 숨김
- 보드 초기화 (ClearBoard)
- 타일 2개 생성 (무작위 빈 셀에, 각각 숫자 2)
- 보드 활성화
  ↓
게임 플레이 시작 (WASD/방향키 입력 대기)
  ↓
TileBoard.Update() (매 프레임마다 입력 감지)
```

## 게임 플레이 시작 후

이후 플레이어가 키를 누르면:
1. `TileBoard.Update()`에서 입력 감지 (Assets/Scripts/TileBoard.cs:41-54)
2. `Move()` 메서드 호출로 타일 이동 로직 실행
3. 타일 이동/병합 후 새로운 타일 생성
4. 게임오버 체크

게임은 이렇게 초기화되고 플레이어의 입력을 기다리며 게임 루프가 시작됩니다.
