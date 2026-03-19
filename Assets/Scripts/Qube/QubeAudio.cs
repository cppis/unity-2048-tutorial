using UnityEngine;

/// <summary>
/// 프로시저럴 사운드 생성 및 재생 유틸리티.
/// 외부 오디오 에셋 없이 코드로 틱/클릭 사운드를 생성합니다.
/// </summary>
public class QubeAudio : MonoBehaviour
{
    private const int SAMPLE_RATE = 44100;

    // 틱 사운드 (회전용)
    private const float TICK_DURATION = 0.015f;
    private const float TICK_FREQUENCY = 3200f;
    private const float TICK_VOLUME = 0.3f;

    // 배치 사운드 (착착 흡착)
    private const float SNAP_DURATION = 0.08f;
    private const float SNAP_VOLUME = 0.6f;

    // 라인 클리어 사운드
    private const float LINE_CLEAR_DURATION = 0.25f;
    private const float LINE_CLEAR_VOLUME = 0.5f;

    // 별 수집 사운드
    private const float STAR_DURATION = 0.12f;
    private const float STAR_VOLUME = 0.45f;

    // 연쇄 소거 사운드
    private const float CHAIN_DURATION = 0.18f;
    private const float CHAIN_VOLUME = 0.5f;

    private AudioSource audioSource;
    private AudioClip tickClip;
    private AudioClip snapClip;
    private AudioClip lineClearClip;
    private AudioClip starClip;
    private AudioClip[] chainClips; // 깊이별 피치 다른 클립

    public void Initialize(Transform parent)
    {
        audioSource = parent.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = parent.gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        tickClip = CreateTickClip();
        snapClip = CreateSnapClip();
        lineClearClip = CreateLineClearClip();
        starClip = CreateStarClip();
        chainClips = new AudioClip[4];
        for (int i = 0; i < 4; i++)
            chainClips[i] = CreateChainClip(i);
    }

    public void PlayTick()
    {
        if (audioSource != null && tickClip != null)
            audioSource.PlayOneShot(tickClip, TICK_VOLUME);
    }

    public void PlaySnap()
    {
        if (audioSource != null && snapClip != null)
            audioSource.PlayOneShot(snapClip, SNAP_VOLUME);
    }

    public void PlayLineClear()
    {
        if (audioSource != null && lineClearClip != null)
            audioSource.PlayOneShot(lineClearClip, LINE_CLEAR_VOLUME);
    }

    public void PlayStarCollect()
    {
        if (audioSource != null && starClip != null)
            audioSource.PlayOneShot(starClip, STAR_VOLUME);
    }

    public void PlayChain(int depth)
    {
        if (audioSource != null && chainClips != null)
        {
            int index = Mathf.Clamp(depth - 1, 0, chainClips.Length - 1);
            audioSource.PlayOneShot(chainClips[index], CHAIN_VOLUME);
        }
    }

    private AudioClip CreateTickClip()
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * TICK_DURATION);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float normalizedT = (float)i / sampleCount;

            float envelope = 1f - normalizedT;
            envelope *= envelope;

            float sine = Mathf.Sin(2f * Mathf.PI * TICK_FREQUENCY * t);
            float noise = (Random.value * 2f - 1f) * 0.15f;

            samples[i] = (sine + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Tick", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSnapClip()
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * SNAP_DURATION);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float normalizedT = (float)i / sampleCount;

            // === 착착 흡착 사운드 ===
            // 짧고 둔탁한 충돌 + 최소 울림

            float envelope = Mathf.Pow(1f - normalizedT, 6f); // 극도로 빠른 감쇠

            // 중저음 충돌 (착!)
            float body = Mathf.Sin(2f * Mathf.PI * 180f * t);
            float sub = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.6f;

            // 초반 짧은 노이즈 (질감)
            float noise = normalizedT < 0.15f
                ? (Random.value * 2f - 1f) * 0.35f * (1f - normalizedT / 0.15f)
                : 0f;

            samples[i] = (body + sub + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Snap", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateLineClearClip()
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * LINE_CLEAR_DURATION);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float normalizedT = (float)i / sampleCount;

            // 상승 아르페지오 (밝고 경쾌)
            float envelope = Mathf.Pow(1f - normalizedT, 1.5f);
            float freq = Mathf.Lerp(400f, 1200f, normalizedT * normalizedT);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.3f;

            samples[i] = (sine + harmonic) * envelope;
        }

        AudioClip clip = AudioClip.Create("LineClear", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateStarClip()
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * STAR_DURATION);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float normalizedT = (float)i / sampleCount;

            // 맑은 벨 (고음 사인파 + 감쇠)
            float envelope = Mathf.Pow(1f - normalizedT, 2f);
            float bell = Mathf.Sin(2f * Mathf.PI * 1600f * t);
            float overtone = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.3f;

            samples[i] = (bell + overtone) * envelope;
        }

        AudioClip clip = AudioClip.Create("Star", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateChainClip(int depthIndex)
    {
        int sampleCount = Mathf.CeilToInt(SAMPLE_RATE * CHAIN_DURATION);
        float[] samples = new float[sampleCount];

        // 깊이별 피치 상승
        float basePitch = 300f + depthIndex * 150f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float normalizedT = (float)i / sampleCount;

            float envelope = Mathf.Pow(1f - normalizedT, 2.5f);
            float freq = basePitch + normalizedT * 200f;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float sub = Mathf.Sin(2f * Mathf.PI * basePitch * 0.5f * t) * 0.5f;
            float noise = normalizedT < 0.1f
                ? (Random.value * 2f - 1f) * 0.3f * (1f - normalizedT / 0.1f)
                : 0f;

            samples[i] = (sine + sub + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create($"Chain_{depthIndex}", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
