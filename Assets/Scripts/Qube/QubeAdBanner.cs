using UnityEngine;

/// <summary>
/// AppLovin MAX 배너 광고를 그리드 상단에 표시합니다.
/// SDK가 설치되지 않은 환경에서도 컴파일 오류 없이 동작합니다.
/// </summary>
public class QubeAdBanner : MonoBehaviour
{
    [Header("AppLovin MAX")]
    [Tooltip("AppLovin 대시보드에서 발급받은 SDK Key")]
    public string sdkKey = "YOUR_SDK_KEY";

    [Tooltip("AppLovin 대시보드에서 발급받은 Banner Ad Unit ID (Android)")]
    public string bannerAdUnitAndroid = "YOUR_BANNER_AD_UNIT_ID_ANDROID";

    [Tooltip("AppLovin 대시보드에서 발급받은 Banner Ad Unit ID (iOS)")]
    public string bannerAdUnitIOS = "YOUR_BANNER_AD_UNIT_ID_IOS";

    [Tooltip("배너 배경색 (투명 배경 시 사용)")]
    public Color bannerBackgroundColor = new Color(0.02f, 0.06f, 0.12f, 1f);

    private string adUnitId;
    private bool isSdkInitialized = false;

    public void Initialize()
    {
#if UNITY_EDITOR
        Debug.Log("[QubeAdBanner] 에디터에서는 광고가 표시되지 않습니다.");
        return;
#else
        adUnitId = GetAdUnitId();
        if (string.IsNullOrEmpty(sdkKey) || sdkKey == "YOUR_SDK_KEY")
        {
            Debug.LogWarning("[QubeAdBanner] SDK Key가 설정되지 않았습니다.");
            return;
        }
        InitializeMaxSdk();
#endif
    }

    private string GetAdUnitId()
    {
#if UNITY_ANDROID
        return bannerAdUnitAndroid;
#elif UNITY_IOS
        return bannerAdUnitIOS;
#else
        return bannerAdUnitAndroid;
#endif
    }

#if !UNITY_EDITOR
    private void InitializeMaxSdk()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
        MaxSdk.SetSdkKey(sdkKey);
        MaxSdk.InitializeSdk();
    }

    private void OnSdkInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
    {
        isSdkInitialized = true;
        Debug.Log("[QubeAdBanner] MAX SDK 초기화 완료");
        CreateBanner();
    }

    private void CreateBanner()
    {
        if (string.IsNullOrEmpty(adUnitId) || adUnitId.StartsWith("YOUR_"))
        {
            Debug.LogWarning("[QubeAdBanner] Ad Unit ID가 설정되지 않았습니다.");
            return;
        }

        MaxSdk.CreateBanner(adUnitId, MaxSdkBase.BannerPosition.TopCenter);
        MaxSdk.SetBannerBackgroundColor(adUnitId, bannerBackgroundColor);

        // 이벤트 콜백
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerLoaded;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerLoadFailed;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerClicked;

        MaxSdk.ShowBanner(adUnitId);
        Debug.Log("[QubeAdBanner] 배너 광고 표시 요청");
    }

    private void OnBannerLoaded(string unitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("[QubeAdBanner] 배너 로드 완료");
    }

    private void OnBannerLoadFailed(string unitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning($"[QubeAdBanner] 배너 로드 실패: {errorInfo.Message}");
    }

    private void OnBannerClicked(string unitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("[QubeAdBanner] 배너 클릭");
    }

    public void ShowBanner()
    {
        if (isSdkInitialized && !string.IsNullOrEmpty(adUnitId))
            MaxSdk.ShowBanner(adUnitId);
    }

    public void HideBanner()
    {
        if (isSdkInitialized && !string.IsNullOrEmpty(adUnitId))
            MaxSdk.HideBanner(adUnitId);
    }

    private void OnDestroy()
    {
        if (isSdkInitialized && !string.IsNullOrEmpty(adUnitId))
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerClicked;
            MaxSdk.DestroyBanner(adUnitId);
        }
    }
#else
    // ==================== 에디터 더미 배너 ====================

    private const float DUMMY_BANNER_HEIGHT = 350f;
    private GameObject dummyBanner;

    public void ShowBanner()
    {
        if (dummyBanner != null) dummyBanner.SetActive(true);
    }

    public void HideBanner()
    {
        if (dummyBanner != null) dummyBanner.SetActive(false);
    }

    /// <summary>
    /// 에디터용 더미 배너를 그리드 위에 생성합니다.
    /// gridTransform 기준으로 위쪽에 배치됩니다.
    /// </summary>
    public void CreateDummyBanner(Transform canvasTransform, RectTransform gridRect)
    {
        if (dummyBanner != null) return;

        dummyBanner = new GameObject("DummyAdBanner");
        dummyBanner.transform.SetParent(canvasTransform, false);

        RectTransform rect = dummyBanner.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // 그리드 위쪽에 배치
        float gridTop = gridRect.anchoredPosition.y + gridRect.sizeDelta.y / 2f;
        float bannerY = gridTop + DUMMY_BANNER_HEIGHT / 2f + 140f;
        rect.anchoredPosition = new Vector2(0f, bannerY);
        rect.sizeDelta = new Vector2(gridRect.sizeDelta.x, DUMMY_BANNER_HEIGHT);

        // 배경
        UnityEngine.UI.Image bg = dummyBanner.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        bg.raycastTarget = false;

        // "AD" 텍스트
        GameObject textObj = new GameObject("AdText");
        textObj.transform.SetParent(dummyBanner.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "AD BANNER (Editor Preview)";
        tmp.fontSize = 24;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.4f);
        tmp.raycastTarget = false;
    }
#endif
}
