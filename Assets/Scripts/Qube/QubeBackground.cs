using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로시저럴 그라데이션 배경 + 비네트 오버레이를 생성합니다.
/// </summary>
public class QubeBackground : MonoBehaviour
{
    private const int GRADIENT_TEX_HEIGHT = 256;
    private const int VIGNETTE_TEX_SIZE = 256;

    [Header("Gradient")]
    public Color topColor = new Color(0.08f, 0.16f, 0.26f, 1f);
    public Color bottomColor = new Color(0.02f, 0.06f, 0.12f, 1f);

    [Header("Vignette")]
    public float vignetteIntensity = 0.45f;
    public float vignetteRadius = 0.8f;

    private Image backgroundImage;
    private GameObject vignetteObject;

    public void Apply(Transform canvasTransform)
    {
        ApplyGradient(canvasTransform);
        CreateVignette(canvasTransform);
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
}
