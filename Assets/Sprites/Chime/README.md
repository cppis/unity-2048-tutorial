# Chime Grid Sprite

이 폴더에는 12×9 Chime 그리드 스프라이트 파일이 포함되어 있습니다.

## 파일 설명

### Grid_12x9.svg
- **형식**: SVG (Scalable Vector Graphics)
- **크기**: 1015×760 픽셀
- **그리드**: 12열 × 9행
- **셀 크기**: 80px
- **간격**: 5px
- **그리드 라인**: 흰색, 10% 투명도

#### Unity에서 SVG 사용하기

1. **Vector Graphics 패키지 설치** (권장)
   - Window > Package Manager
   - Unity Registry에서 "Vector Graphics" 검색
   - Install 클릭

2. **SVG 직접 사용**
   - SVG 파일을 Unity 프로젝트에 드래그
   - Inspector에서 SVG Importer 설정 확인
   - Sprite로 사용 가능

### generate_png.html
HTML 파일로, 브라우저에서 그리드를 렌더링하여 PNG로 저장할 수 있습니다.

#### HTML 파일로 PNG 생성하기

1. **브라우저에서 열기**
   ```bash
   # Windows (WSL에서)
   explorer.exe generate_png.html

   # 또는 직접 경로로 브라우저에서 열기
   # file:///mnt/c/Works/git/unity-2048-tutorial/Assets/Sprites/Chime/generate_png.html
   ```

2. **PNG로 저장**
   - 캔버스를 오른쪽 클릭
   - "이미지를 다른 이름으로 저장..." 선택
   - `Grid_12x9.png`로 저장

3. **Unity로 가져오기**
   - 저장한 PNG 파일을 이 폴더에 복사
   - Unity에서 자동으로 인식
   - Inspector에서 Texture Type을 "Sprite (2D and UI)"로 설정

## 온라인 변환 도구

SVG를 PNG로 변환하려면 다음 온라인 도구를 사용할 수 있습니다:

- [CloudConvert](https://cloudconvert.com/svg-to-png)
- [Convertio](https://convertio.co/svg-png/)
- [Online-Convert](https://image.online-convert.com/convert-to-png)

## 커맨드라인 변환 (선택사항)

Inkscape가 설치되어 있다면:
```bash
inkscape Grid_12x9.svg --export-type=png --export-filename=Grid_12x9.png
```

ImageMagick이 설치되어 있다면:
```bash
convert Grid_12x9.svg Grid_12x9.png
```

## 그리드 사양

- **총 셀 수**: 108 (12 × 9)
- **총 크기**: 1015 × 760 픽셀
- **셀 간격**: 5픽셀 (셀 사이 공백)
- **그리드 라인 위치**: 각 셀 사이의 간격 중앙
- **그리드 라인 스타일**: 1px 두께, 흰색, 10% 투명도 (rgba(255, 255, 255, 0.1))

## 사용 예시

Unity의 ChimeGrid 컴포넌트에서 이 그리드 스프라이트를 배경으로 사용할 수 있습니다:

1. Canvas 또는 Panel에 Image 컴포넌트 추가
2. Source Image를 `Grid_12x9` 스프라이트로 설정
3. RectTransform 크기를 그리드 크기에 맞게 조정
4. ChimeGrid의 셀들이 그리드 라인과 정확히 일치하도록 배치

## 참고

- 이 그리드는 `ChimeGrid.cs`의 설정과 일치합니다:
  - `WIDTH = 12`
  - `HEIGHT = 9`
  - `cellSize = 80f`
  - `spacing = 5f`
