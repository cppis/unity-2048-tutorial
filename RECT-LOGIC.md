# 직사각형 블록 병합 로직

블록 이동 후 같은 색의 1x1 블록들이 직사각형을 형성하면 자동으로 하나의 큰 블록으로 병합됩니다.

## 구현 개요

### 실행 시점
- 블록 이동이 완료된 후 `CheckAdjacentBlocks()` 메서드에서 자동 실행
- 드래그 종료 시 `Block.cs:OnEndDrag`에서 호출됨

### 우선순위
1. **직사각형 병합 시도** (우선)
   - 같은 색 1x1 블록들이 직사각형을 형성하면 즉시 병합
2. **기존 이벤트 로직** (대체)
   - 직사각형이 형성되지 않으면 `OnAdjacentMatchFound` 이벤트 발생

## 핵심 알고리즘

### 1. FindRectangle (BlockGrid.cs:723-784)

같은 색의 연결된 1x1 블록들을 찾아 직사각형을 형성하는지 확인합니다.

**알고리즘 단계:**
1. BFS(너비 우선 탐색)로 같은 색의 인접한 블록들을 모두 찾음
2. 찾은 블록들이 직사각형을 이루는지 `IsRectangle()` 호출하여 검증
3. 유효한 직사각형이면 블록 리스트 반환, 아니면 null 반환

**조건:**
- 시작 블록이 1x1이어야 함 (이미 병합된 블록은 제외)
- 모든 연결된 블록이 같은 색이어야 함
- 모든 블록이 1x1이어야 함
- 최소 2개 이상의 블록이 필요

**BFS 탐색:**
```csharp
Queue<Block> queue = new Queue<Block>();
queue.Enqueue(startBlock);

while (queue.Count > 0)
{
    Block current = queue.Dequeue();
    // 4방향 (상/하/좌/우) 탐색
    // 같은 색의 1x1 블록 발견 시 큐에 추가
}
```

### 2. IsRectangle (BlockGrid.cs:789-833)

블록들이 유효한 직사각형을 형성하는지 검증합니다.

**검증 로직:**
1. **경계 박스 계산**: 모든 블록의 최소/최대 x, y 좌표 찾기
   ```
   minX, minY = 가장 왼쪽 아래 좌표
   maxX, maxY = 가장 오른쪽 위 좌표
   ```

2. **면적 일치 확인**:
   ```
   rectWidth × rectHeight == 블록 개수
   ```

3. **빈 공간 확인**: 직사각형 내부의 모든 셀이 블록으로 채워져 있는지 확인
   ```csharp
   for (int x = minX; x <= maxX; x++)
       for (int y = minY; y <= maxY; y++)
           // 모든 셀에 블록이 있어야 함
   ```

**유효/무효 예시:**
```
유효한 직사각형 (2x3):
O O O
O O O

무효한 형태 (L자):
O O .
O . .

무효한 형태 (빈 공간):
O O O
O . O
```

### 3. MergeBlocksToRectangle (BlockGrid.cs:838-881)

여러 1x1 블록을 하나의 큰 블록으로 병합합니다.

**병합 과정:**
1. **경계 박스 계산**: 병합될 블록의 위치와 크기 계산
   ```csharp
   int rectWidth = maxX - minX + 1;
   int rectHeight = maxY - minY + 1;
   ```

2. **기존 블록 제거**:
   ```csharp
   foreach (Block block in blocks)
   {
       activeBlocks.Remove(block);
       Destroy(block.gameObject);
   }
   ```

3. **그리드 셀 정리**:
   ```csharp
   for (int x = minX; x <= maxX; x++)
       for (int y = minY; y <= maxY; y++)
           grid[x, y] = null;
   ```

4. **새로운 큰 블록 생성**:
   ```csharp
   SpawnBlock(minX, minY, mergeColor, rectWidth, rectHeight);
   ```

5. **디버그 로그**: 병합 정보 출력
   ```
   Merging 4 blocks into 2x2 rectangle at (1,2)
   ```

### 4. CheckAdjacentBlocks 수정 (BlockGrid.cs:668-698)

블록 이동 후 자동으로 실행되는 메인 로직입니다.

**실행 순서:**
```csharp
public void CheckAdjacentBlocks(Block block)
{
    // 1. 직사각형 병합 시도 (우선)
    List<Block> rectangleBlocks = FindRectangle(block);
    if (rectangleBlocks != null && rectangleBlocks.Count > 1)
    {
        MergeBlocksToRectangle(rectangleBlocks);
        return; // 병합 성공 - 종료
    }

    // 2. 직사각형이 안되면 기존 이벤트 로직
    // 직선 매칭 확인 및 OnAdjacentMatchFound 이벤트 발생
}
```

## 작동 예시

### 예시 1: 2x2 직사각형 병합
```
이동 전:        이동 후:              결과:
R . .          R R .                [2x2]
R . .    →     R R .    →           [  R  ]
. . B          . . B
```
- 4개의 빨간색 1x1 블록이 2x2 직사각형 형성
- 자동으로 하나의 2x2 빨간 블록으로 병합

### 예시 2: 1x3 세로 직사각형 병합
```
이동 전:        이동 후:              결과:
B . .          B . .                [1x3]
. . .    →     B . .    →           [ B ]
B . .          B . .
```
- 3개의 파란색 1x1 블록이 1x3 직사각형 형성
- 자동으로 하나의 1x3 파란 블록으로 병합

### 예시 3: 3x2 가로 직사각형 병합
```
이동 전:        이동 후:              결과:
G G .          G G G                [3x2]
. . G    →     G G G    →           [  G  ]
. . .          . . .
```
- 6개의 녹색 1x1 블록이 3x2 직사각형 형성
- 자동으로 하나의 3x2 녹색 블록으로 병합

### 예시 4: L자 형태 (병합 안됨)
```
이동 전:        이동 후:              결과:
G . .          G G .                병합 안됨
. . .    →     G . .    →           (직사각형 아님)
G . .          . . .                이벤트 발생
```
- 연결된 블록이지만 직사각형이 아님
- 면적 체크 실패: 2×2=4 ≠ 3개 블록
- 기존 `OnAdjacentMatchFound` 이벤트 발생

### 예시 5: 빈 공간 있음 (병합 안됨)
```
이동 전:        이동 후:              결과:
R R .          R R R                병합 안됨
. . .    →     R . R    →           (빈 공간 있음)
R . R          . . .                이벤트 발생
```
- 면적은 일치하지만 중간에 빈 공간 존재
- 직사각형 검증 실패
- 기존 이벤트 로직으로 처리

## 구현 위치

### BlockGrid.cs
- **Line 668-698**: `CheckAdjacentBlocks()` - 메인 로직
- **Line 723-784**: `FindRectangle()` - 직사각형 탐색
- **Line 789-833**: `IsRectangle()` - 직사각형 검증
- **Line 838-881**: `MergeBlocksToRectangle()` - 병합 실행

### Block.cs
- **Line 337**: OnEndDrag에서 `grid.CheckAdjacentBlocks(this)` 호출
- **Line 361**: OnEndDrag에서 `grid.CheckAdjacentBlocks(this)` 호출

## 디버그 및 테스트

### 콘솔 로그
병합이 발생하면 다음과 같은 디버그 로그가 출력됩니다:
```
Merging 4 blocks into 2x2 rectangle at (1,2)
Merging 6 blocks into 3x2 rectangle at (0,0)
Merging 2 blocks into 1x2 rectangle at (3,4)
```

### 테스트 방법
1. Unity에서 게임 실행
2. 같은 색 1x1 블록 여러 개를 그리드에 배치
3. 블록을 드래그하여 직사각형 형태로 배치
4. 드래그 종료 시 자동으로 병합되는지 확인
5. Console 창에서 병합 로그 확인

### 테스트 케이스
- [x] 2x2 정사각형
- [x] 1xN 세로 직사각형 (N=2,3,4...)
- [x] Nx1 가로 직사각형 (N=2,3,4...)
- [x] NxM 직사각형 (다양한 크기)
- [x] L자/T자 형태 (병합되지 않아야 함)
- [x] 빈 공간이 있는 형태 (병합되지 않아야 함)
- [x] 다른 색 블록 혼합 (병합되지 않아야 함)
- [x] 이미 병합된 블록은 제외 (1x1만 병합 대상)

## 제한사항

### 병합 조건
- **1x1 블록만 병합**: 이미 병합된 블록(2x1, 1x2 등)은 추가 병합 안됨
- **완전한 직사각형만**: 빈 공간이 하나라도 있으면 병합 안됨
- **같은 색만**: 다른 색 블록이 섞이면 병합 안됨
- **연결된 블록만**: BFS로 연결되지 않은 블록은 제외

### 성능 고려사항
- BFS 탐색은 최대 그리드 전체 크기만큼 실행 (O(width × height))
- 직사각형 검증은 O(블록 개수)
- 매 블록 이동마다 실행되므로 최적화 필요시 캐싱 고려

## 향후 개선 사항

### 가능한 확장
1. **다단계 병합**: 큰 블록끼리도 병합 가능하도록
2. **애니메이션**: 병합 시 부드러운 시각 효과 추가
3. **점수 시스템**: 병합 크기에 따른 점수 부여
4. **사운드 효과**: 병합 시 효과음 재생
5. **파티클 효과**: 병합 시 파티클 이펙트
6. **최대 크기 제한**: 너무 큰 블록 생성 방지
7. **특수 블록**: 특정 크기 달성 시 특수 능력 부여

## 참고

이 로직은 2048 게임의 타일 병합 메커니즘에서 영감을 받았으나, 2차원 직사각형 병합으로 확장되었습니다.
