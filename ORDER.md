# Unity 2048 개발 순서

이 문서는 Unity 2048 프로젝트를 처음부터 개발할 때 권장되는 작업 순서를 정리합니다.

## 1단계: 프로젝트 초기 설정

### 1.1 Unity 프로젝트 생성
- Unity 6000.3.2f1 (또는 2021.3 LTS 이상) 버전으로 새 2D 프로젝트 생성
- 프로젝트 이름: unity-2048-tutorial

### 1.2 필수 패키지 설정
- TextMesh Pro 임포트 (Window > TextMesh Pro > Import TMP Essential Resources)
- 필요시 Clear Sans 폰트 다운로드 및 임포트

### 1.3 폴더 구조 생성
```
Assets/
├── Scenes/
├── Scripts/
├── Prefabs/
├── Tiles/
└── Fonts/
```

## 2단계: 기초 데이터 구조 개발

### 2.1 TileState.cs 작성
**위치**: Assets/Scripts/TileState.cs
**의존성**: 없음
**설명**: 타일의 숫자 값과 색상을 저장하는 ScriptableObject

**핵심 구현 사항**:
- `[CreateAssetMenu]` 어트리뷰트로 에셋 생성 가능하게 설정
- `number` (int): 타일 숫자 (2, 4, 8, ...)
- `backgroundColor` (Color): 배경색
- `textColor` (Color): 텍스트 색상

**이유**: 다른 모든 클래스의 기반이 되는 데이터 구조이며, 의존성이 전혀 없어 가장 먼저 작성해야 합니다.

## 3단계: 그리드 시스템 개발 (하위 → 상위)

### 3.1 TileCell.cs 작성
**위치**: Assets/Scripts/TileCell.cs
**의존성**: Tile (순환 참조)
**설명**: 그리드의 개별 셀을 나타내는 컴포넌트

**핵심 구현 사항**:
- `coordinates` (Vector2Int): 셀의 좌표
- `tile` (Tile): 현재 셀을 차지하는 타일 참조
- `Empty`, `Occupied` 프로퍼티: 셀 상태 확인

**이유**: 그리드의 최소 단위이므로 Row와 Grid보다 먼저 작성해야 합니다.

### 3.2 TileRow.cs 작성
**위치**: Assets/Scripts/TileRow.cs
**의존성**: TileCell
**설명**: 셀들의 가로 행을 묶는 컨테이너

**핵심 구현 사항**:
- `Awake()`에서 자식 TileCell들을 `GetComponentsInChildren`으로 수집
- `cells` 배열로 행의 모든 셀 관리

**이유**: TileCell을 사용하므로 TileCell 이후에 작성합니다.

### 3.3 TileGrid.cs 작성
**위치**: Assets/Scripts/TileGrid.cs
**의존성**: TileRow, TileCell
**설명**: 전체 그리드를 관리하는 최상위 컨테이너

**핵심 구현 사항**:
- `Awake()`에서 모든 rows와 cells 초기화
- 각 셀의 좌표를 계산하여 설정 (`i % Width`, `i / Width`)
- `GetCell(x, y)`: 좌표로 셀 가져오기
- `GetAdjacentCell()`: 방향으로 인접 셀 가져오기 (Y축 반전 주의!)
- `GetRandomEmptyCell()`: 빈 셀 무작위 선택

**주의사항**:
- Unity UI 좌표계에 맞춰 `GetAdjacentCell`에서 Y축을 반전 (`coordinates.y -= direction.y`)

**이유**: 그리드 시스템의 최상위 관리자로, TileRow와 TileCell을 모두 사용합니다.

## 4단계: 타일 시스템 개발

### 4.1 Tile.cs 작성
**위치**: Assets/Scripts/Tile.cs
**의존성**: TileState, TileCell, TextMesh Pro
**설명**: 시각적 타일 GameObject와 애니메이션 관리

**핵심 구현 사항**:
- `state` (TileState): 타일의 현재 상태
- `cell` (TileCell): 타일이 위치한 셀
- `locked` (bool): 한 턴에 중복 병합 방지 플래그
- `SetState()`: 타일 상태 설정 및 시각 업데이트
- `Spawn()`: 셀에 즉시 생성
- `MoveTo()`: 셀로 이동 (애니메이션)
- `Merge()`: 다른 타일과 병합 (애니메이션 후 파괴)
- `Animate()`: 코루틴을 사용한 부드러운 이동 (0.1초 Lerp)

**이유**: TileState와 TileCell을 사용하므로 이들 이후에 작성합니다.

## 5단계: 게임 로직 개발

### 5.1 TileBoard.cs 작성
**위치**: Assets/Scripts/TileBoard.cs
**의존성**: Tile, TileGrid, TileState, GameManager
**설명**: 핵심 게임플레이 로직과 입력 처리

**핵심 구현 사항**:
- `tilePrefab` (Tile): 타일 프리팹 참조
- `tileStates` (TileState[]): 모든 타일 상태 배열
- `waiting` (bool): 애니메이션 중 입력 차단 플래그
- `tiles` (List<Tile>): 현재 보드의 모든 타일
- `Update()`: WASD/방향키 입력 처리
- `Move()`: 방향별 이동 로직 (올바른 순서로 셀 반복)
- `MoveTile()`: 개별 타일 이동 처리
- `CanMerge()`: 두 타일 병합 가능 여부 확인
- `MergeTiles()`: 타일 병합 및 점수 증가
- `WaitForChanges()`: 애니메이션 대기 후 새 타일 생성
- `CheckForGameOver()`: 게임 오버 조건 확인

**주요 로직**:
1. 입력 방향에 따라 올바른 순서로 셀 순회 (예: 위로 이동 시 위쪽부터)
2. 각 타일을 방향으로 이동 가능한 최대 거리까지 이동
3. 같은 숫자 타일 만나면 병합 (locked 플래그로 중복 방지)
4. 모든 이동 완료 후 0.1초 대기
5. 새 타일 생성 (보드가 가득 차지 않은 경우)
6. 게임 오버 확인 (보드 가득 + 병합 불가능)

**이유**: 게임의 핵심 로직으로, Tile, TileGrid, GameManager를 모두 사용합니다.

### 5.2 GameManager.cs 작성
**위치**: Assets/Scripts/GameManager.cs
**의존성**: TileBoard, TextMesh Pro
**설명**: 게임 전체 상태 관리 (싱글톤 패턴)

**핵심 구현 사항**:
- `[DefaultExecutionOrder(-1)]`: 다른 스크립트보다 먼저 초기화
- 싱글톤 패턴 구현 (Instance)
- `board` (TileBoard): 게임 보드 참조
- `score`: 현재 점수
- `scoreText`, `hiscoreText`: UI 텍스트 참조
- `gameOver` (CanvasGroup): 게임 오버 UI
- `NewGame()`: 게임 초기화 (점수 리셋, 보드 클리어, 타일 2개 생성)
- `GameOver()`: 게임 오버 처리 (보드 비활성화, UI 페이드인)
- `IncreaseScore()`: 점수 증가 및 최고 점수 저장 (PlayerPrefs)
- `Fade()`: 코루틴으로 UI 페이드 애니메이션

**이유**: TileBoard를 사용하므로 TileBoard 이후에 작성하거나, 동시에 작성하면서 서로 참조하도록 구현합니다.

## 6단계: Unity 에디터 작업

### 6.1 TileState 에셋 생성
**위치**: Assets/Tiles/
**개수**: 17개 (2, 4, 8, 16, 32, ..., 131072)

**작업 순서**:
1. Assets/Tiles 폴더 생성
2. 우클릭 > Create > Tile State
3. 각 타일 번호별로 에셋 생성
4. Inspector에서 number, backgroundColor, textColor 설정

**색상 가이드** (원작 2048 참고):
- 2, 4: 밝은 색상
- 8~64: 주황색 계열
- 128~2048: 노란색/금색 계열
- 4096 이상: 진한 색상

### 6.2 기본 씬 구조 설계
**위치**: Assets/Scenes/2048.unity

**씬 계층 구조**:
```
Canvas
├── Game Manager (GameManager 컴포넌트)
├── Board (TileBoard 컴포넌트)
│   ├── Grid (TileGrid 컴포넌트)
│   │   ├── Row 0 (TileRow 컴포넌트)
│   │   │   ├── Cell 0 (TileCell 컴포넌트) - Image
│   │   │   ├── Cell 1 (TileCell 컴포넌트) - Image
│   │   │   ├── Cell 2 (TileCell 컴포넌트) - Image
│   │   │   └── Cell 3 (TileCell 컴포넌트) - Image
│   │   ├── Row 1 (TileRow 컴포넌트)
│   │   │   └── ... (4개 Cell)
│   │   ├── Row 2 (TileRow 컴포넌트)
│   │   │   └── ... (4개 Cell)
│   │   └── Row 3 (TileRow 컴포넌트)
│   │       └── ... (4개 Cell)
│   └── Tiles (빈 Transform, 타일이 생성될 부모)
├── Score Panel
│   ├── Score Text (TextMeshProUGUI)
│   └── Hiscore Text (TextMeshProUGUI)
└── Game Over (CanvasGroup)
    ├── Background
    ├── Text
    └── New Game Button
```

**작업 순서**:
1. Canvas 생성 (UI > Canvas)
2. Grid Layout Group을 사용하여 4x4 그리드 구조 생성
3. 각 오브젝트에 적절한 컴포넌트 추가
4. Layout 설정 (Grid Layout: Cell Size, Spacing 등)

### 6.3 타일 프리팹 생성
**위치**: Assets/Prefabs/Tile.prefab

**구조**:
```
Tile (Image 컴포넌트 + Tile 스크립트)
└── Text (TextMeshProUGUI)
```

**작업 순서**:
1. Hierarchy에서 UI > Image 생성
2. Tile 스크립트 추가
3. 자식으로 TextMeshProUGUI 추가
4. Layout 설정 (크기, 폰트 등)
5. Prefabs 폴더로 드래그하여 프리팹 생성

### 6.4 씬 연결 및 설정

**GameManager 설정**:
- Board: TileBoard 오브젝트 드래그
- Game Over: Game Over CanvasGroup 드래그
- Score Text: Score Text 드래그
- Hiscore Text: Hiscore Text 드래그

**TileBoard 설정**:
- Tile Prefab: Tile 프리팹 드래그
- Tile States: 17개의 TileState 에셋을 순서대로 추가 (2, 4, 8, ..., 131072)

**중요**: TileStates 배열은 반드시 오름차순으로 정렬되어야 합니다!

### 6.5 UI 스타일링
- 배경 색상 설정
- 폰트 적용 (Clear Sans)
- 버튼 스타일링
- 레이아웃 조정

## 7단계: 테스트 및 디버깅

### 7.1 기본 기능 테스트
1. 게임 시작 시 타일 2개 생성 확인
2. 방향키 입력으로 타일 이동 확인
3. 같은 숫자 타일 병합 확인
4. 점수 증가 확인
5. 새 타일 생성 확인

### 7.2 엣지 케이스 테스트
1. 보드 가득 찬 상태에서 게임 오버 확인
2. 여러 타일 연속 병합 확인
3. 한 턴에 같은 타일이 두 번 병합되지 않는지 확인
4. 최고 점수 저장/로드 확인

### 7.3 버그 수정
- 타일 이동 순서 문제
- 병합 로직 오류
- 애니메이션 타이밍 문제
- UI 업데이트 오류

## 8단계: 최적화 및 완성

### 8.1 성능 최적화
- 오브젝트 풀링 (필요시)
- 불필요한 GetComponent 호출 제거
- 애니메이션 최적화

### 8.2 최종 점검
- 모든 기능 재테스트
- 빌드 설정 확인
- 플랫폼별 빌드 테스트

## 핵심 개발 원칙

### 의존성 순서 준수
스크립트는 의존성이 적은 것부터 작성:
1. TileState (의존성 없음)
2. TileCell (Tile과 순환 참조, 간단하므로 먼저)
3. TileRow (TileCell 사용)
4. TileGrid (TileRow, TileCell 사용)
5. Tile (TileState, TileCell 사용)
6. TileBoard (모든 타일 시스템 사용)
7. GameManager (TileBoard 사용)

### 컴포넌트 기반 설계
Unity의 컴포넌트 시스템을 활용하여:
- 각 클래스는 단일 책임 원칙 준수
- MonoBehaviour 컴포넌트로 Unity 기능 활용
- ScriptableObject로 데이터 분리

### 테스트 주도 개발
각 단계마다:
1. 스크립트 작성
2. Unity 에디터에서 간단한 테스트 씬 구성
3. 기능 동작 확인
4. 다음 단계로 진행

### 코드 품질 유지
- 명확한 변수명 사용
- 적절한 접근 제어자 (`public`, `private`, `[SerializeField]`)
- 코루틴을 활용한 부드러운 애니메이션
- 주석으로 복잡한 로직 설명

## 참고 자료

- 원작: https://play2048.co/
- 튜토리얼: https://youtu.be/4NFZwPhqeRs
- Unity TextMesh Pro: https://docs.unity3d.com/Manual/com.unity.textmeshpro.html
- Unity UI: https://docs.unity3d.com/Manual/UISystem.html

## 예상 개발 시간

- **초급 개발자**: 8-12시간
- **중급 개발자**: 4-6시간
- **고급 개발자**: 2-3시간

*UI 디자인 시간은 별도*
