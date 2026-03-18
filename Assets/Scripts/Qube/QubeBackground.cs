using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 프로시저럴 그라데이션 배경 + 비네트 오버레이를 생성합니다.
/// </summary>
public class QubeBackground : MonoBehaviour
{
    private const int GRADIENT_TEX_HEIGHT = 256;
    private const int VIGNETTE_TEX_SIZE = 256;
    private const int AMBIENT_PARTICLE_COUNT = 10;
    private const float AMBIENT_PARTICLE_MIN_SIZE = 3f;
    private const float AMBIENT_PARTICLE_MAX_SIZE = 8f;
    private const float AMBIENT_PARTICLE_MIN_SPEED = 8f;
    private const float AMBIENT_PARTICLE_MAX_SPEED = 25f;
    private const float AMBIENT_PARTICLE_ALPHA = 0.15f;

    [Header("Gradient")]
    public Color topColor = new Color(0.08f, 0.16f, 0.26f, 1f);
    public Color bottomColor = new Color(0.02f, 0.06f, 0.12f, 1f);

    [Header("Vignette")]
    public float vignetteIntensity = 0.45f;
    public float vignetteRadius = 0.8f;

    private Image backgroundImage;
    private GameObject vignetteObject;
    private List<AmbientParticle> ambientParticles = new List<AmbientParticle>();
    private RectTransform ambientContainer;

    public void Apply(Transform canvasTransform)
    {
        ApplyGradient(canvasTransform);
        CreateVignette(canvasTransform);
        CreateAmbientParticles(canvasTransform);
    }

    private void Update()
    {
        UpdateAmbientParticles();
    }

    private void ApplyGradient(Transform canvasTransform)
    {
        // Background Image 찾기 (이미 존재하는 것 사용 또는 생성)
        Transform bgTransform = canvasTransform.Find("Background");
        if (bgTransform != null)
        {
            backgroundImage = bgTransform.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasTransform, false);
            bgObj.transform.SetAsFirstSibling();

            RectTransform rect = bgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.raycastTarget = false;
        }

        Texture2D gradientTex = CreateGradientTexture();
        backgroundImage.sprite = Sprite.Create(
            gradientTex,
            new Rect(0, 0, 1, GRADIENT_TEX_HEIGHT),
            new Vector2(0.5f, 0.5f));
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.color = Color.white;
    }

    private Texture2D CreateGradientTexture()
    {
        Texture2D tex = new Texture2D(1, GRADIENT_TEX_HEIGHT, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < GRADIENT_TEX_HEIGHT; y++)
        {
            float t = (float)y / (GRADIENT_TEX_HEIGHT - 1);
            Color color = Color.Lerp(bottomColor, topColor, t);
            tex.SetPixel(0, y, color);
        }

        tex.Apply();
        return tex;
    }

    private void CreateVignette(Transform canvasTransform)
    {
        if (vignetteObject != null)
            Object.Destroy(vignetteObject);

        vignetteObject = new GameObject("Vignette");
        vignetteObject.transform.SetParent(canvasTransform, false);
        // 가장 위 레이어 (UI 요소 아래)에 배치하되, Background 바로 위
        vignetteObject.transform.SetSiblingIndex(1);

        RectTransform rect = vignetteObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image vignetteImage = vignetteObject.AddComponent<Image>();
        vignetteImage.raycastTarget = false;

        Texture2D vignetteTex = CreateVignetteTexture();
        vignetteImage.sprite = Sprite.Create(
            vignetteTex,
            new Rect(0, 0, VIGNETTE_TEX_SIZE, VIGNETTE_TEX_SIZE),
            new Vector2(0.5f, 0.5f));
        vignetteImage.color = Color.white;
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.preserveAspect = false;
    }

    private Texture2D CreateVignetteTexture()
    {
        Texture2D tex = new Texture2D(VIGNETTE_TEX_SIZE, VIGNETTE_TEX_SIZE, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = VIGNETTE_TEX_SIZE / 2f;

        for (int y = 0; y < VIGNETTE_TEX_SIZE; y++)
        {
            for (int x = 0; x < VIGNETTE_TEX_SIZE; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 0f;
                if (dist > vignetteRadius)
                {
                    float t = (dist - vignetteRadius) / (1.414f - vignetteRadius);
                    alpha = Mathf.Clamp01(t) * vignetteIntensity;
                }

                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    // ==================== 앰비언트 파티클 ====================

    private void CreateAmbientParticles(Transform canvasTransform)
    {
        GameObject containerObj = new GameObject("AmbientParticles");
        containerObj.transform.SetParent(canvasTransform, false);
        // Background와 Vignette 바로 위, 게임 요소 아래
        containerObj.transform.SetSiblingIndex(2);

        ambientContainer = containerObj.AddComponent<RectTransform>();
        ambientContainer.anchorMin = Vector2.zero;
        ambientContainer.anchorMax = Vector2.one;
        ambientContainer.offsetMin = Vector2.zero;
        ambientContainer.offsetMax = Vector2.zero;

        for (int i = 0; i < AMBIENT_PARTICLE_COUNT; i++)
        {
            AmbientParticle p = CreateOneAmbientParticle(randomizeY: true);
            ambientParticles.Add(p);
        }
    }

    private AmbientParticle CreateOneAmbientParticle(bool randomizeY)
    {
        GameObject obj = new GameObject("AmbientDot");
        obj.transform.SetParent(ambientContainer, false);

        Image img = obj.AddComponent<Image>();
        float alpha = AMBIENT_PARTICLE_ALPHA * Random.Range(0.5f, 1.0f);
        img.color = new Color(0.6f, 0.8f, 1f, alpha);
        img.raycastTarget = false;

        float size = Random.Range(AMBIENT_PARTICLE_MIN_SIZE, AMBIENT_PARTICLE_MAX_SIZE);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        // 화면 범위 내 랜덤 위치
        float xRange = 540f; // 캔버스 반폭 기준
        float yRange = 960f;
        float startX = Random.Range(-xRange, xRange);
        float startY = randomizeY ? Random.Range(-yRange, yRange) : -yRange;
        rect.anchoredPosition = new Vector2(startX, startY);

        return new AmbientParticle
        {
            rect = rect,
            img = img,
            speed = Random.Range(AMBIENT_PARTICLE_MIN_SPEED, AMBIENT_PARTICLE_MAX_SPEED),
            drift = Random.Range(-5f, 5f),
            yRange = yRange,
            xRange = xRange
        };
    }

    private void UpdateAmbientParticles()
    {
        for (int i = 0; i < ambientParticles.Count; i++)
        {
            var p = ambientParticles[i];
            if (p.rect == null) continue;

            Vector2 pos = p.rect.anchoredPosition;
            pos.y += p.speed * Time.deltaTime;
            pos.x += p.drift * Time.deltaTime;

            // 화면 위로 벗어나면 아래에서 재생성
            if (pos.y > p.yRange)
            {
                pos.y = -p.yRange;
                pos.x = Random.Range(-p.xRange, p.xRange);
                p.speed = Random.Range(AMBIENT_PARTICLE_MIN_SPEED, AMBIENT_PARTICLE_MAX_SPEED);
                p.drift = Random.Range(-5f, 5f);
            }

            p.rect.anchoredPosition = pos;
        }
    }

    private class AmbientParticle
    {
        public RectTransform rect;
        public Image img;
        public float speed;
        public float drift;
        public float yRange;
        public float xRange;
    }
}
