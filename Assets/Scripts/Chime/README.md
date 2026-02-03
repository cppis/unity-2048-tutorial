# Qube - 퍼즐 게임 프로토타입

Qube_GDD.md에 기반한 Unity 퍼즐 게임 프로토타입입니다. 블록을 배치하여 Quad를 만들고 펄스로 소거하는 게임입니다.

## 프로젝트 개요

- **게임 타입**: 퍼즐 게임
- **그리드**: 8x6
- **블록 타입**: L, I, T, O형 (기본 4종)
- **게임 모드**: Zen 모드 (전역 턴 기반 펄스)

## 핵심 메커니즘

### 1. 블록 배치
- L, I, T, O형 블록을 그리드에 배치
- 회전 가능 (Q: 반시계, E: 시계)
- 이동 가능 (WASD 또는 방향키)

### 2. Quad 시스템
- **Quad**: 2x2 이상의 직사각형 영역
- 블록 배치 후 자동으로 검출
- 완성된 Quad는 하이라이트 표시

### 3. 펄스 시스템 (Zen 모드)
- **전역 턴 기반**: 4턴마다 모든 Quad 동시 소거
- 턴 카운터가 4에 도달하면 펄스 발동
- 펄스 후 턴 카운터 리셋

### 4. 점수 시스템
- Quad 크기별 점수:
  - 2x2: 100점
  - 3x3: 300점
  - 4x4: 600점
  - 5x5 이상: 1000점+

- 동시 소거 보너스:
  - 2개 동시: 1.5배
  - 3개 이상 동시: 2.0배

## 스크립트 구조

### 핵심 스크립트

1. **QubeCell.cs** (Assets/Scripts/Qube/QubeCell.cs)
   - 그리드의 개별 셀 관리
   - 점유 상태 및 색상 관리

2. **QubeGrid.cs** (Assets/Scripts/Qube/QubeGrid.cs)
   - 8x6 그리드 생성 및 관리
   - 셀 접근 및 상태 업데이트

3. **QubeBlockShape.cs** (Assets/Scripts/Qube/QubeBlockShape.cs)
   - 블록 모양 정의 (ScriptableObject)
   - L, I, T, O, S, Z형 블록 템플릿

4. **QubeBlock.cs** (Assets/Scripts/Qube/QubeBlock.cs)
   - 블록 이동, 회전, 배치 로직
   - 충돌 감지 및 유효성 검사

5. **QubeQuad.cs** (Assets/Scripts/Qube/QubeQuad.cs)
   - Quad 데이터 구조
   - 점수 계산 및 범위 정보

6. **QubeQuadDetector.cs** (Assets/Scripts/Qube/QubeQuadDetector.cs)
   - Flood Fill 알고리즘으로 Quad 검출
   - 직사각형 유효성 검증
   - Quad 하이라이트

7. **QubePulseSystem.cs** (Assets/Scripts/Qube/QubePulseSystem.cs)
   - 턴 기반 펄스 시스템
   - Quad 소거 및 점수 계산
   - 펄스 이벤트 관리

8. **QubeGameManager.cs** (Assets/Scripts/Qube/QubeGameManager.cs)
   - 게임 전체 흐름 관리
   - 블록 생성 및 입력 처리
   - UI 업데이트

## 조작법

| 키 | 동작 |
|---|------|
| WASD / 방향키 | 블록 이동 |
| Q | 반시계방향 회전 |
| E | 시계방향 회전 |
| Space | 블록 배치 |

## Scene 설정

Scene 설정 방법은 `QUBE_SETUP_GUIDE.md`를 참조하세요.

## 주요 알고리즘

### Quad 검출 (Flood Fill)

```csharp
1. 그리드의 모든 셀 순회
2. 점유된 셀 발견 시:
   - Flood Fill로 연결된 영역 탐색
   - 영역이 직사각형인지 검증
   - 2x2 이상이면 Quad로 등록
```

### 펄스 시스템

```csharp
1. 블록 배치 → 턴 +1
2. Quad 검출 및 하이라이트
3. 턴 카운터 == 4:
   - 모든 Quad 소거
   - 점수 계산 (크기 + 동시 소거 보너스)
   - 턴 카운터 리셋
```

## 확장 기능 (미구현)

GDD에 정의되었지만 이 프로토타입에서 미구현된 기능:

- [ ] Chain 모드 (Quad별 타이머 + 연쇄 소거)
- [ ] S, Z형 블록 (고급 블록)
- [ ] 게임 오버 조건
- [ ] Undo 시스템
- [ ] 사운드 및 햅틱
- [ ] 애니메이션 효과
- [ ] 모바일 터치 입력

## 개발 환경

- Unity 6000.3.2f1 (또는 호환 버전)
- C# 스크립트
- UI Toolkit (Legacy UI 사용)

## 다음 단계

1. Unity Editor에서 Scene 설정 (QUBE_SETUP_GUIDE.md 참조)
2. 기본 테스트 및 디버깅
3. Chain 모드 구현 고려
4. 애니메이션 및 사운드 추가
5. 모바일 입력 지원

## 라이선스

이 프로토타입은 Qube_GDD.md에 기반하여 제작되었습니다.
