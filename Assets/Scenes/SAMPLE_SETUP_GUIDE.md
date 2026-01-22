# Sample Scene 설정 가이드

이 가이드는 Unity Editor에서 Sample.unity scene을 설정하는 방법을 설명합니다.

## 필수 스크립트

다음 스크립트들이 `Assets/Scripts/`에 생성되어 있습니다:
- `Block.cs` - 블럭 오브젝트 (드래그, 선택, 속성)
- `BlockGrid.cs` - 그리드 시스템 (n×m 그리드, 중력, 인접 블럭 체크)
- `CameraController.cs` - 카메라 줌 인/아웃
- `UIController.cs` - UI 컨트롤 패널

## Scene 설정 단계

### 1. Main Camera 설정

1. Hierarchy에서 Main Camera 선택
2. Inspector에서 다음 설정:
   - Projection: **Orthographic**
   - Size: **10**
   - Background: 원하는 배경색
3. Main Camera에 **CameraController** 컴포넌트 추가 (Add Component → Camera Controller)

### 2. BlockGrid 오브젝트 생성

1. Hierarchy에서 우클릭 → Create Empty
2. 이름을 **BlockGrid**로 변경
3. Inspector에서 **Block Grid** 컴포넌트 추가
4. BlockGrid 설정:
   - Width: 5
   - Height: 5
   - Cell Size: 1
   - Cell Spacing: 0.1
   - Available Colors: 원하는 색상 배열 설정 (예: Red, Blue, Green, Yellow, Magenta, Cyan)
   - Gravity Enabled: false (초기값)
   - Gravity Speed: 5

### 3. Block Prefab 생성 (선택사항)

기본 블럭을 사용하지 않고 커스텀 블럭을 만들고 싶다면:

1. Hierarchy에서 우클릭 → 3D Object → Sphere (또는 원하는 모양)
2. 이름을 **BlockPrefab**로 변경
3. Inspector에서 **Block** 컴포넌트 추가
4. Sprite Renderer 또는 원하는 Renderer 설정
5. BlockPrefab을 `Assets/Prefabs/` 폴더로 드래그하여 Prefab 생성
6. Hierarchy에서 BlockPrefab 삭제
7. BlockGrid Inspector에서 Block Prefab 필드에 생성한 Prefab 할당

### 4. UI Canvas 생성

1. Hierarchy에서 우클릭 → UI → Canvas
2. Canvas 설정:
   - Render Mode: **Screen Space - Overlay**
   - Canvas Scaler → UI Scale Mode: **Scale with Screen Size**
   - Reference Resolution: 1920 x 1080

### 5. UI Panel 생성

Canvas 하위에 Panel 생성:

1. Canvas 우클릭 → UI → Panel
2. 이름을 **ControlPanel**로 변경
3. RectTransform 설정:
   - Anchor: Top-Left
   - Width: 300
   - Height: 400
   - Pos X: 170
   - Pos Y: -220

### 6. UI 요소 추가

ControlPanel 하위에 다음 UI 요소들을 추가:

#### 6.1 Gravity Toggle
1. ControlPanel 우클릭 → UI → Toggle
2. 이름: **GravityToggle**
3. Text (자식 Label): "Gravity"

#### 6.2 Grid Size Input Fields
1. ControlPanel 우클릭 → UI → Input Field (TMP)
2. 이름: **WidthInput**
3. Placeholder: "Width (2-20)"
4. 반복하여 **HeightInput** 생성

#### 6.3 Create Grid Button
1. ControlPanel 우클릭 → UI → Button (TMP)
2. 이름: **CreateGridButton**
3. Text: "Create Grid"

#### 6.4 Block Count Input
1. ControlPanel 우클릭 → UI → Input Field (TMP)
2. 이름: **BlockCountInput**
3. Placeholder: "Block Count"

#### 6.5 Spawn Blocks Button
1. ControlPanel 우클릭 → UI → Button (TMP)
2. 이름: **SpawnBlocksButton**
3. Text: "Spawn Random Blocks"

### 7. UIController 설정

1. Hierarchy에서 빈 GameObject 생성 (Create Empty)
2. 이름을 **GameController**로 변경
3. **UIController** 컴포넌트 추가
4. Inspector에서 참조 연결:
   - Block Grid: Hierarchy의 BlockGrid 드래그
   - Camera Controller: Main Camera의 CameraController 컴포넌트 드래그
   - Gravity Toggle: GravityToggle 드래그
   - Width Input: WidthInput 드래그
   - Height Input: HeightInput 드래그
   - Create Grid Button: CreateGridButton 드래그
   - Spawn Blocks Button: SpawnBlocksButton 드래그
   - Block Count Input: BlockCountInput 드래그

### 8. CameraController 참조 설정

1. Main Camera 선택
2. CameraController 컴포넌트에서:
   - Block Grid: Hierarchy의 BlockGrid 드래그
   - Target Camera: Main Camera 드래그 (또는 자동)

## 기능 테스트

1. Play 버튼 클릭
2. "Create Grid" 버튼으로 그리드 생성
3. "Spawn Random Blocks" 버튼으로 블럭 생성
4. 블럭을 클릭하고 드래그하여 이동
5. Gravity Toggle로 중력 ON/OFF
6. 마우스 스크롤로 줌 인/아웃
7. 우클릭 드래그로 카메라 팬

## 작동 원리

### 좌표 시스템
- X축: 왼쪽 → 오른쪽 증가
- Y축: 아래 → 위 증가

### 블럭 이동

#### 자유 드래그 모드 (기본, Free Form Drag = ON)
1. 블럭 클릭 → 선택
2. 드래그 → 블럭이 마우스/손가락을 직접 따라감
   - 그리드 제약 없음
   - 실시간 유효성 체크 (색상 피드백)
3. 드롭 → 가장 가까운 유효한 그리드로 자동 스냅
   - 유효한 위치: 밝은 색상
   - 무효한 위치: 어두운 색상 (60%)

#### 그리드 기반 모드 (Free Form Drag = OFF)
1. 블럭 클릭 → 선택
2. 드래그 → 그리드 단위 이동
3. 드롭 → 드래그 속도에 따라 모멘텀 적용

### 모멘텀 물리
블럭을 빠르게 드래그하면 속도에 비례하여 더 멀리 이동합니다:
- **드래그 중**: Exponential smoothing으로 일관된 속도 유지
  - 속도 변화 없이 부드럽게 추적
  - 가속/감속 없음
- **드래그 릴리스 후**: 물리 기반 모멘텀 적용
  - **초기 속도**: 드래그 속도 × Velocity Multiplier
  - **최대 거리**: (속도² - 최소속도²) / (2 × 감속도)
  - **감속**: 물리 기반 감속으로 점진적으로 느려짐

#### 조정 가능한 파라미터 (Block 컴포넌트)
- **Drag Threshold**: 이동 시작 최소 거리 (기본: 0.05 셀)
- **Deceleration**: 감속도 (기본: 8 cells/s²)
  - 높을수록 빠르게 멈춤
- **Min Velocity Threshold**: 모멘텀 지속 최소 속도 (기본: 0.8 cells/s)
- **Velocity Multiplier**: 드래그 속도 배율 (기본: 1.5, 범위: 0.5-3)
  - 높을수록 민감하고 더 멀리 이동

### 중력
- 중력 활성화 시 블럭이 아래로 떨어짐
- 빈 공간이 없을 때까지 반복

### 인접 블럭 체크
- 블럭 이동 후 같은 색상의 인접 블럭 자동 감지
- Console에 로그 출력
- `Block.OnAdjacentMatchFound` 이벤트 발생

## 커스터마이징

### 색상 변경
BlockGrid Inspector에서 Available Colors 배열 수정

### 그리드 크기
- Cell Size: 셀 크기
- Cell Spacing: 셀 간격

### 카메라
- Min/Max Zoom: 줌 범위
- Zoom Speed: 줌 속도
- Pan Speed: 팬 속도

### 중력
- Gravity Speed: 중력 속도 (높을수록 빠름)

### 블럭 모멘텀 (Block 컴포넌트)
블럭을 선택하여 Inspector에서 다음 파라미터 조정:

**Animation Settings**
- Momentum Smooth Time: 모멘텀 이동 시 부드러움 (기본: 0.08초, 범위: 0.01-0.2)
  - 낮을수록 (예: 0.03) → 빠르고 딱딱한 이동
  - 높을수록 (예: 0.15) → 느리고 부드러운 이동
- Drag Smooth Time: 드래그 중 응답 속도 (기본: 0.02초, 범위: 0.005-0.1)
  - **Exponential smoothing 사용 - 일관된 속도 유지**
  - 낮을수록 (예: 0.01) → 즉각 반응
  - 높을수록 (예: 0.05) → 약간 부드러움
  - **중요**: 이동 중 속도가 재가속하지 않음

**Drag Settings**
- Free Form Drag: 자유 드래그 모드 활성화 (기본: ON)
  - ON: 블럭이 마우스를 직접 따라감 (추천!)
  - OFF: 그리드 기반 이동
- Drag Threshold: 드래그 감지 최소 거리 (0.05 = 셀의 5%, 그리드 모드에서만 사용)

**Momentum Physics**
- Deceleration: 감속도 (cells/s²)
  - 낮을수록 (예: 4) → 더 멀리 이동
  - 높을수록 (예: 12) → 빨리 멈춤
- Min Velocity Threshold: 모멘텀 유지 최소 속도 (cells/s)
- Velocity Multiplier: 속도 배율 (0.5~3)
  - 낮을수록 (예: 0.8) → 덜 민감, 짧게 이동
  - 높을수록 (예: 2.5) → 매우 민감, 멀리 이동

**추천 설정**

**드래그 모드:**
- 자유 드래그 (추천): Free Form Drag=ON
  - 직관적이고 자연스러운 조작감
  - 실제 물체를 잡는 느낌
- 그리드 기반: Free Form Drag=OFF
  - 정밀한 제어
  - 모멘텀 물리 사용

**애니메이션:**
- 즉각 반응형: Drag Smooth Time=0.01, Momentum Smooth Time=0.05
- 기본 (균형): Drag Smooth Time=0.02, Momentum Smooth Time=0.08
- 부드러운 느낌: Drag Smooth Time=0.04, Momentum Smooth Time=0.12

**모멘텀 (그리드 모드):**
- 느린 이동: Deceleration=12, Velocity Multiplier=0.8
- 빠른 이동: Deceleration=5, Velocity Multiplier=2.5

## 문제 해결

### 블럭이 드래그되지 않음
- EventSystem이 Scene에 있는지 확인 (Canvas 생성 시 자동 생성됨)
- Block에 Collider가 있는지 확인

### 카메라가 줌되지 않음
- Camera가 Orthographic 모드인지 확인
- CameraController가 활성화되어 있는지 확인

### UI 버튼이 작동하지 않음
- UIController의 참조가 모두 연결되어 있는지 확인
- EventSystem이 활성화되어 있는지 확인
