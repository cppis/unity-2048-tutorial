# 드래그 동작 이질감 분석 및 개선 방안

## 현재 동작 방식

### 1. 그리드 기반 스냅 이동
```csharp
// OnDrag (Line 321-323, 331-333)
int steps = Mathf.RoundToInt(dragDistanceX);
steps = Mathf.Clamp(steps, -1, 1); // ❌ 한 번에 1칸만 이동
targetGrid = new Vector2Int(actualGridX + steps, actualGridY);
```

**문제점:**
- 빠르게 드래그해도 1칸씩만 이동
- 블록이 마우스를 따라오지 못함
- "끌어당기는" 느낌이 아닌 "명령하는" 느낌

### 2. 그리드 위치로만 이동
```csharp
// OnDrag (Line 371-375)
Vector3 targetWorldPos = grid.GetWorldPosition(moveInfo.toX, moveInfo.toY);
// 항상 그리드 중앙으로 스냅됨
```

**문제점:**
- 드래그 중에도 블록이 그리드에 고정됨
- 자유로운 이동 불가
- 실제 물체를 잡은 느낌이 없음

### 3. 임계값 제한
```csharp
// OnDrag (Line 319, 329)
if (Mathf.Abs(dragDistanceX) >= dragThreshold) // 0.05 셀
```

**문제점:**
- 미세한 드래그가 무시됨
- 즉각적인 반응 부족

---

## 이질감의 원인

### ⚠️ 주요 이슈

1. **간접 조작감**
   - 현재: 그리드 셀을 선택 → 이동 명령
   - 기대: 블록을 직접 잡고 → 자유롭게 드래그

2. **속도 제한**
   - 현재: 초당 여러 프레임이 발생해도 1칸씩만 이동
   - 기대: 드래그 속도만큼 즉시 이동

3. **위치 제약**
   - 현재: 항상 그리드 위치에만 존재
   - 기대: 드래그 중에는 자유 위치, 드롭 시 스냅

---

## 개선 방안

### 옵션 A: 자유 드래그 + 드롭 시 스냅 (권장)

**개념:**
```
드래그 시작
    ↓
블록이 마우스/손가락 위치를 직접 따라감 (자유 이동)
    ↓
드래그 중
    - 그리드 제약 없음
    - 마우스 위치 = 블록 위치
    - 유효성 체크만 수행 (시각 피드백)
    ↓
드롭
    ↓
가장 가까운 유효한 그리드 위치로 스냅
    ↓
유효하면 이동 확정, 아니면 원위치
```

**구현 방법:**
```csharp
public void OnDrag(PointerEventData eventData)
{
    // 1. 마우스 위치를 직접 따라감 (그리드 무시)
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
    worldPos.z = 0;

    // 2. 드래그 오프셋 적용하여 자연스러운 잡기
    Vector3 targetPos = worldPos + dragOffset;

    // 3. 즉시 이동 (그리드 스냅 없음)
    transform.position = targetPos;

    // 4. 가장 가까운 그리드 위치 계산 (시각 피드백용)
    Vector2Int nearestGrid = grid.WorldToGrid(targetPos);

    // 5. 유효성 체크 및 시각 피드백
    if (grid.CanPlaceBlock(nearestGrid.x, nearestGrid.y, this))
    {
        spriteRenderer.color = blockColor; // 유효 - 원색
        // 옵션: 유효한 그리드 셀 하이라이트
    }
    else
    {
        spriteRenderer.color = blockColor * 0.5f; // 무효 - 어둡게
    }
}

public void OnEndDrag(PointerEventData eventData)
{
    // 가장 가까운 유효한 그리드 위치 찾기
    Vector2Int targetGrid = FindNearestValidGrid(transform.position);

    if (targetGrid != Vector2Int.zero)
    {
        // 유효한 위치로 스냅 및 이동
        MoveToGrid(targetGrid);
    }
    else
    {
        // 유효한 위치 없음 - 원위치
        ReturnToOriginalPosition();
    }
}
```

**장점:**
- ✅ 실제 블록을 잡은 것처럼 자연스러움
- ✅ 즉각적인 반응
- ✅ 직관적인 조작감
- ✅ 드롭 시 자동으로 정렬

**단점:**
- 현재 그리드 시스템 대폭 수정 필요
- 다른 블록 밀기 로직 재설계 필요

---

### 옵션 B: 멀티 셀 이동 (간단한 개선)

**개념:**
```
드래그 거리에 따라 여러 칸 한번에 이동
```

**구현 방법:**
```csharp
public void OnDrag(PointerEventData eventData)
{
    // 기존 코드에서 Clamp 제거
    int steps = Mathf.RoundToInt(dragDistanceX);
    // steps = Mathf.Clamp(steps, -1, 1); // ❌ 제거

    // 여러 칸 이동 가능
    targetGrid = new Vector2Int(actualGridX + steps, actualGridY);
}
```

**장점:**
- ✅ 빠른 드래그 시 빠르게 이동
- ✅ 최소한의 코드 변경

**단점:**
- ⚠️ 여전히 그리드에 스냅됨
- ⚠️ 자유로운 이동 불가
- ⚠️ 근본적인 이질감 해결 못함

---

### 옵션 C: 하이브리드 (추천)

**개념:**
```
일정 속도 이하: 그리드 기반 이동 (현재 방식)
빠른 드래그: 자유 드래그 모드로 전환
```

**구현 방법:**
```csharp
private bool freeFormDrag = false;
private const float FREE_DRAG_VELOCITY_THRESHOLD = 5f; // cells/s

public void OnDrag(PointerEventData eventData)
{
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
    float dragSpeed = (worldPos - lastDragPosition).magnitude / Time.deltaTime;

    // 빠른 드래그면 자유 모드로 전환
    if (dragSpeed > FREE_DRAG_VELOCITY_THRESHOLD)
    {
        freeFormDrag = true;
    }

    if (freeFormDrag)
    {
        // 자유 드래그
        transform.position = worldPos + dragOffset;
        // 시각 피드백
    }
    else
    {
        // 그리드 기반 드래그 (기존 로직)
    }
}
```

**장점:**
- ✅ 정밀한 조작: 천천히 = 그리드 스냅
- ✅ 빠른 이동: 빠르게 = 자유 드래그
- ✅ 기존 동작 유지 + 새 기능 추가

**단점:**
- 두 가지 모드 전환이 혼란스러울 수 있음

---

## 추가 개선 사항

### 1. 시각적 피드백 강화

```csharp
// 드래그 중 대상 그리드 셀 하이라이트
private void HighlightTargetCell(Vector2Int gridPos)
{
    // 반투명 사각형 표시
    // 유효: 초록색, 무효: 빨간색
}
```

### 2. 촉감 피드백 (모바일)

```csharp
// 유효한 위치에 도달 시 햅틱
if (canPlace && !wasValid)
{
    Handheld.Vibrate(); // 짧은 진동
}
```

### 3. 오디오 피드백

```csharp
// 그리드 셀 변경 시 사운드
if (targetGrid != lastTargetGrid)
{
    AudioSource.PlayOneShot(snapSound);
}
```

### 4. 드래그 오프셋 유지

```csharp
// OnPointerDown에서
dragOffset = transform.position - worldPos;

// OnDrag에서
transform.position = worldPos + dragOffset;
```
- 클릭한 위치에서 블록이 점프하지 않음
- 자연스러운 잡기 느낌

---

## 권장 구현 순서

### 단계 1: 즉시 개선 (현재 코드 기반)
1. ✅ `Clamp(-1, 1)` 제거 → 여러 칸 이동 허용
2. ✅ `dragSmoothTime` 더 낮추기 (0.01초)
3. ✅ 시각 피드백 강화

### 단계 2: 중기 개선 (옵션 C)
1. 자유 드래그 모드 추가
2. 속도 기반 전환
3. 테스트 및 조정

### 단계 3: 장기 개선 (옵션 A)
1. 완전한 자유 드래그 시스템
2. 그리드 로직 재설계
3. 전체 리팩토링

---

## 결론

**즉시 적용 가능한 개선:**
1. `Mathf.Clamp(steps, -1, 1)` 제거
2. `dragSmoothTime = 0.005f` (더 빠른 반응)
3. 드래그 오프셋 정확히 유지

**근본적인 해결:**
- 옵션 A (자유 드래그 + 드롭 스냅) 구현
- 실제 물리적 블록을 잡는 느낌 제공

**시간 투자 대비 효과:**
- 옵션 B (멀티 셀): ⭐⭐ (30분, 부분 개선)
- 옵션 C (하이브리드): ⭐⭐⭐⭐ (2시간, 큰 개선)
- 옵션 A (완전 재설계): ⭐⭐⭐⭐⭐ (1일, 완벽한 UX)

**추천:**
먼저 **옵션 B**로 빠른 개선 후, 사용자 피드백에 따라 **옵션 A** 또는 **C** 선택
