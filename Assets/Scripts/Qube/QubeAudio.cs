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

    // 배치 사운드 (자성체 흡착)
    private const float SNAP_DURATION = 0.15f;
    private const float SNAP_VOLUME = 0.6f;

    private AudioSource audioSource;
    private AudioClip tickClip;
    private AudioClip snapClip;

    public void Initialize(Transform parent)
    {
        audioSource = parent.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = parent.gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        tickClip = CreateTickClip();
        snapClip = CreateSnapClip();
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

            // === 저음 자성체 흡착 사운드 ===
            // 2단계: 착 충돌(0~20%) → 저음 울림(20~100%)

            float sample = 0f;

            if (normalizedT < 0.2f)
            {
                // 1단계: 착! 저음 금속 충돌
                float hitT = normalizedT / 0.2f;
                float hitEnv = Mathf.Pow(1f - hitT, 3f);

                // 저음 금속 충돌음
                float metal1 = Mathf.Sin(2f * Mathf.PI * 140f * t);
                float metal2 = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.8f;
                float sub = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.9f;

                // 짧은 충돌 노이즈 (착!)
                float hitNoise = hitT < 0.3f
                    ? (Random.value * 2f - 1f) * 0.3f * (1f - hitT / 0.3f)
                    : 0f;

                sample = (metal1 + metal2 + sub + hitNoise) * hitEnv;
            }
            else
            {
                // 2단계: 깊은 저음 울림
                float ringT = (normalizedT - 0.2f) / 0.8f;
                float ringEnv = Mathf.Pow(1f - ringT, 2f) * 0.35f;

                float ring = Mathf.Sin(2f * Mathf.PI * 55f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.7f;

                sample = (ring + sub) * ringEnv;
            }

            samples[i] = sample;
        }

        AudioClip clip = AudioClip.Create("Snap", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
