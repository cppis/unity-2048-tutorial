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

## 게임 플레이 로직

### 6. 입력 처리

**TileBoard.Update()** (Assets/Scripts/TileBoard.cs:41-54):
- 매 프레임마다 실행
- `waiting` 플래그가 true면 입력 무시 (애니메이션 중)
- WASD 또는 방향키 입력 감지
- 각 방향별로 적절한 매개변수로 `Move()` 호출

```csharp
private void Update()
{
    if (waiting) return;

    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
        Move(Vector2Int.up, 0, 1, 1, 1);
    } else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
        Move(Vector2Int.left, 1, 1, 0, 1);
    } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
        Move(Vector2Int.down, 0, 1, grid.Height - 2, -1);
    } else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
        Move(Vector2Int.right, grid.Width - 2, -1, 0, 1);
    }
}
```

**방향별 매개변수 설명**:
- 위쪽: x는 0에서 시작하여 +1씩, y는 1에서 시작하여 +1씩 (위쪽 행부터)
- 아래쪽: x는 0에서 시작하여 +1씩, y는 Height-2에서 시작하여 -1씩 (아래쪽 행부터)
- 왼쪽: x는 1에서 시작하여 +1씩 (왼쪽 열부터), y는 0에서 시작하여 +1씩
- 오른쪽: x는 Width-2에서 시작하여 -1씩 (오른쪽 열부터), y는 0에서 시작하여 +1씩

**중요**: 이동 방향으로 순회하는 순서가 핵심! 예를 들어 위로 이동할 때는 위쪽 행부터 처리해야 타일이 올바르게 연쇄 이동합니다.

### 7. Move 메서드 - 전체 보드 이동

**Move()** (Assets/Scripts/TileBoard.cs:56-75):
- 지정된 순서로 모든 셀을 순회
- 각 타일에 대해 `MoveTile()` 호출
- 하나라도 이동이 발생했으면 `WaitForChanges()` 코루틴 시작

```csharp
private void Move(Vector2Int direction, int startX, int incrementX, int startY, int incrementY)
{
    bool changed = false;

    for (int x = startX; x >= 0 && x < grid.Width; x += incrementX)
    {
        for (int y = startY; y >= 0 && y < grid.Height; y += incrementY)
        {
            TileCell cell = grid.GetCell(x, y);

            if (cell.Occupied) {
                changed |= MoveTile(cell.tile, direction);
            }
        }
    }

    if (changed) {
        StartCoroutine(WaitForChanges());
    }
}
```

### 8. MoveTile - 개별 타일 이동 로직

**MoveTile()** (Assets/Scripts/TileBoard.cs:77-106):
1. 이동 방향으로 인접한 셀 확인
2. 빈 셀이 나오면 계속 전진 (최대 거리까지 이동)
3. 다른 타일과 만나면:
   - 병합 가능하면 병합 (같은 숫자 + locked 아님)
   - 병합 불가능하면 정지
4. 최종 위치로 타일 이동

```csharp
private bool MoveTile(Tile tile, Vector2Int direction)
{
    TileCell newCell = null;
    TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);

    while (adjacent != null)
    {
        if (adjacent.Occupied)
        {
            // 다른 타일과 만남
            if (CanMerge(tile, adjacent.tile))
            {
                MergeTiles(tile, adjacent.tile);
                return true;
            }

            break; // 병합 불가능하면 여기서 멈춤
        }

        // 빈 셀이면 계속 전진
        newCell = adjacent;
        adjacent = grid.GetAdjacentCell(adjacent, direction);
    }

    if (newCell != null)
    {
        tile.MoveTo(newCell);
        return true;
    }

    return false; // 이동하지 않음
}
```

**예시 (위쪽으로 이동)**:
```
초기 상태:
Row 0: [2] [ ] [ ] [2]
Row 1: [ ] [2] [ ] [ ]
Row 2: [ ] [ ] [4] [ ]
Row 3: [2] [ ] [ ] [ ]
```

위로 이동하면 (`Move(Vector2Int.up, 0, 1, 1, 1)`):
- y=1, 2, 3 순서로 순회 (위쪽 행부터 처리)
- 각 열(x=0,1,2,3)별로 처리

**Column 0 (x=0):**
- Row 3의 [2]: Row 0의 [2]와 병합 → [4]

**Column 1 (x=1):**
- Row 1의 [2]: Row 0(비어있음)으로 이동

**Column 2 (x=2):**
- Row 2의 [4]: Row 0(비어있음)으로 이동

**Column 3 (x=3):**
- Row 0의 [2]: 이미 최상단이므로 이동 없음

```
최종 결과:
Row 0: [4] [2] [4] [2]
Row 1: [ ] [ ] [ ] [ ]
Row 2: [ ] [ ] [ ] [ ]
Row 3: [ ] [ ] [ ] [ ]
```

**왜 위쪽 행부터 순회해야 하는가?**

만약 아래쪽 행(y=3)부터 처리한다면:
```
초기: [2] [2] [ ] [ ]
```
오른쪽으로 이동할 때 아래부터 처리하면 타일이 제대로 연쇄되지 않습니다.
위쪽부터(또는 이동 방향부터) 처리해야 각 타일이 최대한 멀리 이동할 수 있습니다

### 9. 타일 병합 로직

**CanMerge()** (Assets/Scripts/TileBoard.cs:108-111):
- 두 타일의 state가 동일한지 확인
- 대상 타일이 locked 상태가 아닌지 확인 (한 턴에 한 번만 병합)

```csharp
private bool CanMerge(Tile a, Tile b)
{
    return a.state == b.state && !b.locked;
}
```

**MergeTiles()** (Assets/Scripts/TileBoard.cs:113-123):
1. 병합되는 타일(a)을 리스트에서 제거
2. 타일 a를 타일 b의 위치로 이동 (애니메이션)
3. 타일 b를 다음 상태로 업그레이드 (2→4, 4→8, ...)
4. 타일 b를 locked 상태로 만들어 중복 병합 방지
5. GameManager에 점수 증가 알림

```csharp
private void MergeTiles(Tile a, Tile b)
{
    tiles.Remove(a);
    a.Merge(b.cell);

    int index = Mathf.Clamp(IndexOf(b.state) + 1, 0, tileStates.Length - 1);
    TileState newState = tileStates[index];

    b.SetState(newState);
    GameManager.Instance.IncreaseScore(newState.number);
}
```

**Tile.Merge()** (Assets/Scripts/Tile.cs:54-64):
- 현재 셀 참조 제거
- 대상 타일을 locked 상태로 설정
- 대상 위치로 애니메이션 후 자신을 파괴

```csharp
public void Merge(TileCell cell)
{
    if (this.cell != null) {
        this.cell.tile = null;
    }

    this.cell = null;
    cell.tile.locked = true;

    StartCoroutine(Animate(cell.transform.position, true));
}
```

### 10. 타일 이동 애니메이션

**Tile.MoveTo()** (Assets/Scripts/Tile.cs:42-52):
- 셀 참조 업데이트
- 애니메이션으로 부드럽게 이동

**Tile.Animate()** (Assets/Scripts/Tile.cs:66-85):
- 0.1초 동안 Lerp로 부드럽게 이동
- `merging`이 true면 애니메이션 후 GameObject 파괴

```csharp
private IEnumerator Animate(Vector3 to, bool merging)
{
    float elapsed = 0f;
    float duration = 0.1f;

    Vector3 from = transform.position;

    while (elapsed < duration)
    {
        transform.position = Vector3.Lerp(from, to, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    transform.position = to;

    if (merging) {
        Destroy(gameObject);
    }
}
```

### 11. 이동 후 처리

**WaitForChanges()** (Assets/Scripts/TileBoard.cs:137-156):
1. `waiting = true`로 설정하여 입력 차단
2. 0.1초 대기 (애니메이션 완료 시간)
3. 모든 타일의 `locked` 플래그 해제
4. 보드가 가득 차지 않았으면 새 타일 생성
5. 게임오버 체크

```csharp
private IEnumerator WaitForChanges()
{
    waiting = true;

    yield return new WaitForSeconds(0.1f);

    waiting = false;

    foreach (var tile in tiles) {
        tile.locked = false;
    }

    if (tiles.Count != grid.Size) {
        CreateTile();
    }

    if (CheckForGameOver()) {
        GameManager.Instance.GameOver();
    }
}
```

### 12. 게임오버 체크

**CheckForGameOver()** (Assets/Scripts/TileBoard.cs:158-189):
1. 보드가 가득 차지 않았으면 게임오버 아님
2. 모든 타일에 대해 상하좌우 인접 타일 확인
3. 하나라도 병합 가능한 타일이 있으면 게임오버 아님
4. 모든 타일이 병합 불가능하면 게임오버

```csharp
public bool CheckForGameOver()
{
    if (tiles.Count != grid.Size) {
        return false;
    }

    foreach (var tile in tiles)
    {
        TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
        TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
        TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
        TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

        if (up != null && CanMerge(tile, up.tile)) {
            return false;
        }

        if (down != null && CanMerge(tile, down.tile)) {
            return false;
        }

        if (left != null && CanMerge(tile, left.tile)) {
            return false;
        }

        if (right != null && CanMerge(tile, right.tile)) {
            return false;
        }
    }

    return true;
}
```

### 13. 점수 관리

**IncreaseScore()** (Assets/Scripts/GameManager.cs:81-84):
- 병합된 타일의 숫자만큼 점수 증가
- UI 업데이트
- 최고 점수 자동 저장

```csharp
public void IncreaseScore(int points)
{
    SetScore(score + points);
}

private void SetScore(int score)
{
    this.score = score;
    scoreText.text = score.ToString();

    SaveHiscore();
}

private void SaveHiscore()
{
    int hiscore = LoadHiscore();

    if (score > hiscore) {
        PlayerPrefs.SetInt("hiscore", score);
    }
}
```

### 14. 게임오버 처리

**GameOver()** (Assets/Scripts/GameManager.cs:55-61):
1. TileBoard 비활성화 (더 이상 입력 받지 않음)
2. 게임오버 UI를 interactable로 설정
3. 페이드 인 애니메이션 (1초 지연 후 0.5초 동안)

```csharp
public void GameOver()
{
    board.enabled = false;
    gameOver.interactable = true;

    StartCoroutine(Fade(gameOver, 1f, 1f));
}
```

## 전체 게임플레이 흐름

```
플레이어 입력 (방향키)
  ↓
Update() - 입력 감지
  ↓
Move() - 방향별 적절한 순서로 셀 순회
  ↓
MoveTile() - 각 타일에 대해:
  ├─ 빈 셀까지 이동
  ├─ 같은 숫자 타일 만나면 병합
  └─ 다른 타일 만나면 정지
  ↓
Animate() - 0.1초 동안 부드럽게 이동/병합
  ↓
WaitForChanges() - 애니메이션 완료 대기
  ↓
locked 플래그 해제
  ↓
새 타일 생성 (보드가 가득 차지 않은 경우)
  ↓
CheckForGameOver()
  ├─ 게임오버면 → GameOver() → UI 표시
  └─ 게임오버 아니면 → 다음 입력 대기
```

## 주요 메커니즘

### Locked 플래그
- 한 턴에 타일이 여러 번 병합되는 것을 방지
- 병합된 타일은 즉시 `locked = true`
- 모든 이동이 끝나면 `locked = false`로 리셋

**예시**:
```
[2] [2] [2] [ ]
```
오른쪽으로 이동하면:
1. 첫 번째 [2]가 두 번째 [2]와 병합 → [4] (locked)
2. 세 번째 [2]는 locked된 [4]와 병합 불가
3. 결과: `[ ] [ ] [4] [2]` (올바름)

만약 locked가 없다면: `[ ] [ ] [ ] [8]` (잘못됨)

### 이동 순서의 중요성
- 이동 방향으로 먼저 순회해야 타일이 올바르게 연쇄 이동
- 위로 이동: 위쪽 행부터
- 아래로 이동: 아래쪽 행부터
- 왼쪽 이동: 왼쪽 열부터
- 오른쪽 이동: 오른쪽 열부터

### Y축 반전
`GetAdjacentCell()` (Assets/Scripts/TileGrid.cs:36-43)에서 `coordinates.y -= direction.y`를 사용하는 이유:
- Unity UI는 위쪽이 +Y 방향
- 배열 인덱스는 위쪽이 0 (작은 값)
- 따라서 위로 이동(+Y)할 때 배열 인덱스는 감소해야 함

게임은 이렇게 초기화되고 플레이어의 입력을 기다리며 게임 루프가 시작됩니다.
