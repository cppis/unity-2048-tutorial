# QUBE 컨트롤 가이드

이 문서는 스마트폰 터치 및 햅틱 피드백을 활용한 게임 컨트롤 방법을 정리합니다.

## 1. 표준 2048 게임용 (슬라이드만)

### 스와이프 제스처 (가장 권장)

**동작**
- 화면을 상하좌우로 스와이프하여 타일 이동

**구현 방법**
- Unity의 `Touch` API로 드래그 방향 감지
- 터치 시작 위치와 끝 위치의 벡터 차이로 방향 결정

**햅틱 피드백**
- **이동 성공**: Light Impact - 타일이 실제로 이동했을 때
- **병합 발생**: Medium Impact - 두 타일이 합쳐질 때
- **게임 오버**: Heavy Impact - 더 이상 이동할 수 없을 때

**예시 코드 구조**
```csharp
// 스와이프 방향 감지
if (swipeDirection == Vector2.up)
{
    MoveTiles(TileGrid.Direction.Up);
    // 햅틱: Handheld.Vibrate() 또는 iOS/Android 네이티브 햅틱
}
```

### 버튼 방식 (보조)

**레이아웃**
- 화면 네 모서리 또는 가장자리에 방향 버튼 배치
- 상하좌우 4개 버튼 또는 십자 패드 형태

**햅틱 피드백**
- **버튼 터치 시**: Soft Impact - 버튼 누름 피드백

---

## 2. 회전 기능 추가 시 컨트롤 옵션

### 옵션 A: 제스처 조합

**이동**
- 한 손가락 스와이프 (상하좌우)

**회전**
- 두 손가락 회전 제스처 또는 탭
- 시계방향 회전: 두 손가락을 시계방향으로 회전
- 반시계방향 회전: 두 손가락을 반시계방향으로 회전

**햅틱 피드백**
- **회전**: Soft/Light Impact (각 90도 회전마다)
- **이동**: Medium Impact

**장점**
- 직관적인 제스처
- 화면 공간을 UI가 차지하지 않음

**단점**
- 두 손가락 제스처는 학습 곡선이 있음
- 한 손 플레이가 어려울 수 있음

### 옵션 B: 존 분리

**화면 분할**
- **상단 70%**: 스와이프로 타일 이동
- **하단 30%**: 좌우 버튼으로 회전 제어

**햅틱 피드백**
- 각 동작마다 구별되는 진동 패턴
- 회전: Light Impact
- 이동: Medium Impact

**장점**
- 명확한 컨트롤 구분
- 한 손 플레이 가능

**단점**
- UI가 화면 공간 차지
- 게임 영역이 줄어듦

### 옵션 C: 길게 누르기 (모드 전환)

**기본 모드**
- 짧은 스와이프: 타일 이동

**회전 모드**
- 화면을 길게 누른 후 스와이프: 회전 방향 선택
- 또는 길게 눌러 회전 모드 진입 후 좌우 스와이프로 회전

**햅틱 피드백**
- **모드 전환**: 길게 누르기 활성화 시 진동으로 모드 변경 알림
- **동작 실행**: 각 동작에 맞는 Impact

**장점**
- 최소한의 UI
- 모드 구분이 명확

**단점**
- 학습 곡선
- 빠른 플레이 시 모드 전환이 번거로울 수 있음

---

## 3. 구현 우선순위

### 현재 프로젝트 적용 단계

1. **스와이프 제스처 구현**
   - 위치: `Assets/Scripts/TileBoard.cs` 수정
   - 현재 입력 시스템 (Assets/Scripts/TileBoard.cs:53-69)을 터치 기반으로 전환

2. **햅틱 피드백 추가**
   - iOS: `UnityEngine.iOS.Handheld.Vibrate()`
   - Android: `Vibration` 플러그인 또는 네이티브 플러그인 사용
   - 크로스 플랫폼 솔루션 고려

3. **UI 요소 (선택사항)**
   - 터치 컨트롤을 보조할 화면 UI
   - 튜토리얼용 제스처 가이드

---

## 4. 플랫폼별 햅틱 구현 참고

### iOS
```csharp
#if UNITY_IOS
using UnityEngine.iOS;

// 기본 진동
Handheld.Vibrate();

// 세밀한 제어는 네이티브 플러그인 필요
// UIImpactFeedbackGenerator (Light, Medium, Heavy)
// UINotificationFeedbackGenerator (Success, Warning, Error)
#endif
```

### Android
```csharp
#if UNITY_ANDROID
using UnityEngine;

// 기본 진동
Handheld.Vibrate();

// 패턴 진동은 AndroidJavaObject 사용
// vibrator.vibrate(pattern, repeat);
#endif
```

### 크로스 플랫폼 래퍼
```csharp
public static class HapticFeedback
{
    public static void LightImpact() { /* 플랫폼별 구현 */ }
    public static void MediumImpact() { /* 플랫폼별 구현 */ }
    public static void HeavyImpact() { /* 플랫폼별 구현 */ }
}
```

---

## 5. 권장 컨트롤 방식

**2048 게임의 경우**:
- **1순위**: 스와이프 제스처 + 햅틱 피드백
- **2순위**: 스와이프 + 보조 버튼

**회전 기능 추가 시**:
- **간단한 게임**: 옵션 B (존 분리)
- **빠른 플레이**: 옵션 A (제스처 조합)
- **복잡한 퍼즐**: 옵션 C (모드 전환)

---

## 참고 자료

- Unity Touch Input: https://docs.unity3d.com/ScriptReference/Touch.html
- iOS 햅틱: https://developer.apple.com/design/human-interface-guidelines/haptics
- Android 햅틱: https://developer.android.com/reference/android/os/Vibrator
