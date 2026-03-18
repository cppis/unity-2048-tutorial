using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI Image 기반 경량 파티클 시스템.
/// 블록 배치/쿼드 완성 시 시각적 피드백을 제공합니다.
/// </summary>
public class QubeUIParticles : MonoBehaviour
{
    private const int PLACE_PARTICLE_COUNT = 10;
    private const int QUAD_PARTICLE_COUNT = 16;
    private const float PLACE_PARTICLE_LIFETIME = 0.5f;
    private const float QUAD_PARTICLE_LIFETIME = 0.7f;
    private const float PLACE_PARTICLE_SPEED = 200f;
    private const float QUAD_PARTICLE_SPEED = 350f;
    private const float PLACE_PARTICLE_SIZE = 6f;
    private const float QUAD_PARTICLE_SIZE = 8f;
    private const float FLASH_DURATION = 0.25f;

    private Transform particleContainer;
    private GameObject flashOverlay;

    public void Initialize(Transform canvasTransform)
    {
        // 파티클 컨테이너 생성
        GameObject containerObj = new GameObject("UIParticles");
        containerObj.transform.SetParent(canvasTransform, false);

        RectTransform rect = containerObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Canvas canvas = containerObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1500;

        particleContainer = containerObj.transform;

        // 화면 플래시 오버레이
        flashOverlay = new GameObject("FlashOverlay");
        flashOverlay.transform.SetParent(canvasTransform, false);

        RectTransform flashRect = flashOverlay.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        Image flashImg = flashOverlay.AddComponent<Image>();
        flashImg.color = new Color(1f, 1f, 1f, 0f);
        flashImg.raycastTarget = false;

        Canvas flashCanvas = flashOverlay.AddComponent<Canvas>();
        flashCanvas.overrideSorting = true;
        flashCanvas.sortingOrder = 1800;

        flashOverlay.SetActive(false);
    }

    /// <summary>
    /// 블록 배치 시 파티클 버스트
    /// </summary>
    public void EmitPlaceParticles(Vector2 position, Color color)
    {
        if (particleContainer == null) return;
        StartCoroutine(SpawnParticleBurst(position, color, PLACE_PARTICLE_COUNT,
            PLACE_PARTICLE_SIZE, PLACE_PARTICLE_SPEED, PLACE_PARTICLE_LIFETIME));
    }

    /// <summary>
    /// 쿼드 완성 시 파티클 폭발 + 화면 플래시
    /// </summary>
    public void EmitQuadParticles(Vector2 position, Color color)
    {
        if (particleContainer == null) return;
        StartCoroutine(SpawnParticleBurst(position, color, QUAD_PARTICLE_COUNT,
            QUAD_PARTICLE_SIZE, QUAD_PARTICLE_SPEED, QUAD_PARTICLE_LIFETIME));
        StartCoroutine(ScreenFlash(color));
    }

    private IEnumerator SpawnParticleBurst(Vector2 center, Color color, int count,
        float size, float speed, float lifetime)
    {
        List<ParticleData> particles = new List<ParticleData>();

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float particleSpeed = speed * Random.Range(0.6f, 1.2f);
            float particleSize = size * Random.Range(0.7f, 1.3f);

            GameObject obj = new GameObject("Particle");
            obj.transform.SetParent(particleContainer, false);

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(particleSize, particleSize);
            rect.anchoredPosition = center;

            particles.Add(new ParticleData
            {
                obj = obj,
                rect = rect,
                img = img,
                startPos = center,
                direction = direction,
                speed = particleSpeed,
                startSize = particleSize
            });
        }

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                if (p.obj == null) continue;

                // 이동 (감속)
                float moveT = 1f - (1f - t) * (1f - t); // ease-out
                p.rect.anchoredPosition = p.startPos + p.direction * (p.speed * moveT);

                // 크기 축소
                float scale = Mathf.Lerp(1f, 0f, t);
                p.rect.sizeDelta = new Vector2(p.startSize * scale, p.startSize * scale);

                // 페이드아웃
                Color c = p.img.color;
                c.a = 1f - t;
                p.img.color = c;
            }

            yield return null;
        }

        // 정리
        foreach (var p in particles)
        {
            if (p.obj != null) Destroy(p.obj);
        }
    }

    private IEnumerator ScreenFlash(Color color)
    {
        if (flashOverlay == null) yield break;

        flashOverlay.SetActive(true);
        Image img = flashOverlay.GetComponent<Image>();
        Color flashColor = new Color(color.r, color.g, color.b, 0.15f);

        float elapsed = 0f;
        while (elapsed < FLASH_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FLASH_DURATION;
            img.color = Color.Lerp(flashColor, new Color(color.r, color.g, color.b, 0f), t);
            yield return null;
        }

        img.color = new Color(1f, 1f, 1f, 0f);
        flashOverlay.SetActive(false);
    }

    private struct ParticleData
    {
        public GameObject obj;
        public RectTransform rect;
        public Image img;
        public Vector2 startPos;
        public Vector2 direction;
        public float speed;
        public float startSize;
    }
}
