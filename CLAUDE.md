# CLAUDE.md

이 파일은 Claude Code (claude.ai/code)가 이 저장소의 코드 작업 시 참고하는 가이드입니다.

## 프로젝트 개요

이것은 Gabriele Cirulli가 원작한 2048 퍼즐 게임의 Unity 구현입니다. 그리드 위의 숫자 타일을 슬라이드하여 2048에 도달하는 것이 목표입니다.

- **Unity 버전**: Unity 6000.3.2f1 (원래는 2021.3 LTS로 시작)
- **주제**: UI, 그리드, 애니메이션
- **튜토리얼**: https://youtu.be/4NFZwPhqeRs

## 빌드 및 실행

이것은 Unity 프로젝트입니다. 작업 방법:

1. **Unity에서 열기**: Unity Editor(버전 6000.3.2f1 또는 호환 버전)에서 프로젝트 폴더 열기
2. **빌드**: Unity의 Build Settings(File > Build Settings)를 사용하여 대상 플랫폼용으로 빌드
3. **에디터에서 플레이**: Unity Editor에서 Play 버튼을 눌러 테스트

이 프로젝트는 Unity Editor를 통해 완전히 관리되므로 CLI 빌드 명령은 없습니다.

## 코드 아키텍처

### 핵심 게임 루프

게임은 `GameManager` (Assets/Scripts/GameManager.cs:6)를 중심으로 한 싱글톤 패턴을 사용합니다:
- 게임 상태 관리 (점수, 최고 점수, 게임 오버)
- 보드 초기화 및 제어
- `DefaultExecutionOrder(-1)`로 다른 스크립트보다 먼저 초기화되도록 보장

### 그리드 시스템

그리드 시스템은 계층적이고 컴포넌트 기반입니다:

1. **TileGrid** (Assets/Scripts/TileGrid.cs): 그리드 구조를 관리하는 컨테이너
   - 모든 행과 셀에 대한 참조 보유
   - 좌표 또는 방향으로 셀을 가져오는 메서드 제공
   - 무작위 빈 셀 선택 처리

2. **TileRow** (Assets/Scripts/TileRow.cs): 셀의 가로 행을 나타냄

3. **TileCell** (Assets/Scripts/TileCell.cs): 개별 그리드 위치
   - 좌표와 어떤 타일(있는 경우)이 차지하는지 추적
   - 간단한 점유/비어있음 상태

### 타일 시스템

1. **Tile** (Assets/Scripts/Tile.cs): 시각적 타일 GameObject
   - 현재 셀을 참조
   - 코루틴을 사용하여 이동 및 병합 애니메이션 처리
   - 한 번의 이동에서 중복 병합을 방지하는 `locked` 플래그 보유

2. **TileState** (Assets/Scripts/TileState.cs): 타일 외형을 정의하는 ScriptableObject
   - 숫자 값과 색상 저장
   - Unity의 Create Asset Menu를 통해 생성

### 게임 로직

**TileBoard** (Assets/Scripts/TileBoard.cs)는 핵심 게임플레이 컨트롤러입니다:
- 입력 처리 (WASD/방향키)
- 방향 기반 반복으로 이동 로직 구현
- 타일 병합 관리 (Assets/Scripts/TileBoard.cs:113)
- 게임 오버 조건 확인 (Assets/Scripts/TileBoard.cs:158)
- 애니메이션 중 입력을 방지하는 `waiting` 플래그 사용

주요 메커니즘:
- 이동은 이동 방향으로 셀을 반복
- 타일은 이동당 한 번만 병합 가능 (`locked` 플래그로)
- 각 성공적인 이동 후 새 타일 생성
- 보드가 가득 차고 병합이 불가능할 때 게임 오버 발생

## 주요 디자인 패턴

- **싱글톤**: GameManager는 적절한 정리와 함께 싱글톤 패턴 사용
- **컴포넌트 기반**: 그리드 구조는 Unity의 컴포넌트 계층 사용
- **ScriptableObjects**: 타일 상태는 ScriptableObjects를 통해 데이터 주도적
- **코루틴**: 모든 애니메이션은 부드러운 전환을 위해 코루틴 사용

## 타일 병합 로직

타일 병합은 2048 게임의 핵심 메커니즘으로, 다음과 같이 구현됩니다:

### 병합 조건 (Assets/Scripts/TileBoard.cs:114-117)

`CanMerge` 메서드는 두 타일이 병합 가능한지 확인합니다:
```csharp
private bool CanMerge(Tile a, Tile b)
{
    return a.state == b.state && !b.locked;
}
```

두 조건을 모두 충족해야 병합 가능:
1. **같은 상태**: 두 타일의 숫자 값이 동일해야 함 (예: 2+2, 4+4)
2. **잠금 해제**: 대상 타일(`b`)이 `locked` 상태가 아니어야 함

### 병합 실행 (Assets/Scripts/TileBoard.cs:119-129)

`MergeTiles` 메서드는 실제 병합을 처리합니다:

1. **타일 제거**: 이동한 타일(`a`)을 타일 리스트에서 제거하고 병합 애니메이션 시작
2. **상태 업그레이드**: 대상 타일(`b`)의 상태를 다음 단계로 업그레이드 (예: 2 → 4)
3. **점수 증가**: 새로운 타일 값만큼 점수 증가
4. **인덱스 클램핑**: `Mathf.Clamp`로 최대 타일 값(일반적으로 2048) 초과 방지

### 중복 병합 방지 메커니즘

한 번의 이동에서 같은 타일이 여러 번 병합되는 것을 방지하기 위해 `locked` 플래그를 사용:

1. **병합 시**: 타일이 병합되면 자동으로 `locked` 상태가 됨 (Assets/Scripts/Tile.cs:58)
2. **이동 완료 후**: `WaitForChanges` 코루틴에서 0.1초 대기 후 모든 타일의 `locked` 해제 (Assets/Scripts/TileBoard.cs:151-153)

이 메커니즘이 없다면 다음과 같은 문제가 발생할 수 있습니다:
- 예: `[2][2][2]`을 오른쪽으로 이동 시
- 잘못된 결과: `[8]` (2→4→8로 연쇄 병합)
- 올바른 결과: `[2][4]` (첫 두 개만 병합, 세 번째는 독립)

### 이동 중 병합 흐름

`MoveTile` 메서드(Assets/Scripts/TileBoard.cs:83-112)에서:

1. 타일을 이동 방향으로 한 칸씩 이동
2. 인접한 셀이 점유된 경우:
   - 병합 가능하면(`CanMerge`): 병합 실행 후 `true` 반환
   - 병합 불가능하면: 이동 중단 (현재 위치 유지)
3. 빈 셀이면 계속 이동
4. 더 이상 이동할 수 없으면 최종 위치로 이동

## 중요한 구현 참고사항

- Unity의 UI 좌표 시스템에 맞추기 위해 `GetAdjacentCell`에서 Y축이 반전됨 (Assets/Scripts/TileGrid.cs:40)
- 타일은 셀과 양방향 참조 유지
- 최고 점수는 PlayerPrefs를 통해 영구 저장 (Assets/Scripts/GameManager.cs:99)
- 이동 순서가 중요: 타일을 올바르게 연쇄시키려면 이동 방향에서 반복 시작
- 병합 후 새 타일 값은 인덱스 기반으로 결정되므로 `tileStates` 배열 순서가 중요
