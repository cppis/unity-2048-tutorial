using UnityEngine;

/// <summary>
/// 프로시저럴 사운드 생성 및 재생 유틸리티.
/// 외부 오디오 에셋 없이 코드로 틱/클릭 사운드를 생성합니다.
/// </summary>
public class QubeAudio : MonoBehaviour
{
    private const int SAMPLE_RATE = 44100;
    private const float TICK_DURATION = 0.015f; // 15ms 짧은 클릭
    private const float TICK_FREQUENCY = 3200f; // 고주파 틱
    private const float TICK_VOLUME = 0.3f;

    private AudioSource audioSource;
    private AudioClip tickClip;

    public void Initialize(Transform parent)
    {
        audioSource = parent.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = parent.gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        tickClip = CreateTickClip();
    }

    public void PlayTick()
    {
        if (audioSource != null && tickClip != null)
        {
            audioSource.PlayOneShot(tickClip, TICK_VOLUME);
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

            // 감쇠 엔벨로프: 빠르게 시작 → 급격히 감쇠
            float envelope = 1f - normalizedT;
            envelope *= envelope; // 제곱 감쇠로 더 날카롭게

            // 사인파 + 약간의 노이즈로 기계적 틱 느낌
            float sine = Mathf.Sin(2f * Mathf.PI * TICK_FREQUENCY * t);
            float noise = (Random.value * 2f - 1f) * 0.15f;

            samples[i] = (sine + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Tick", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
