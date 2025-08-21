using UnityEngine;
using UnityEngine.UI;

public class SoundUI : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    public Slider masterSlider;   // 전체
    public Slider bgmSlider;      // 배경음
    public Slider sfxSlider;      // 효과음

    [Header("Buttons")]
    public Button applyButton;    // 적용(= Resume 이름을 Apply로)
    public Button exitButton;     // 패널만 닫기

    [Header("Panel Root (닫을 대상)")]
    [Tooltip("비워두면 이 컴포넌트가 붙은 오브젝트를 닫습니다.")]
    public GameObject panelRoot;

    [Header("Defaults")]
    [Range(0f,1f)] public float defaultMaster = 1.0f;
    [Range(0f,1f)] public float defaultBGM    = 0.8f;
    [Range(0f,1f)] public float defaultSFX    = 0.8f;

    // PlayerPrefs Keys
    const string KEY_MASTER = "vol_master";
    const string KEY_BGM    = "vol_bgm";
    const string KEY_SFX    = "vol_sfx";

    void Awake()
    {
        if (!panelRoot) panelRoot = gameObject; // 패널 참조 자동 지정
    }

    void OnEnable()
    {
        // 저장된 값 로드 → 슬라이더 반영 → 실제 오디오에 1회 적용
        LoadFromPrefsIntoSliders();
        ApplyVolumes();
    }

    void Start()
    {
        // 슬라이더 변경 시 실시간 적용
        if (masterSlider) masterSlider.onValueChanged.AddListener(v =>
        {
            if (SoundManager.Instance) SoundManager.Instance.SetMasterVolume01(v);
        });
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(v =>
        {
            if (SoundManager.Instance) SoundManager.Instance.SetBGMVolume01(v);
        });
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v =>
        {
            if (SoundManager.Instance) SoundManager.Instance.SetSFXVolume01(v);
        });

        // 버튼 바인딩
        if (applyButton) applyButton.onClick.AddListener(OnClickApply);
        if (exitButton)  exitButton.onClick.AddListener(OnClickExit);
    }

    // ===== 버튼 동작 =====
    void OnClickApply()
    {
        // 현재 슬라이더 값을 저장 + 실제 볼륨에 적용 + 패널 닫기
        SavePrefsFromSliders();
        ApplyVolumes();
        if (panelRoot) panelRoot.SetActive(false);
    }

    void OnClickExit()
    {
        // 패널만 닫기 (저장/적용 X)
        if (panelRoot) panelRoot.SetActive(false);
    }

    // ===== 저장/로드/적용 유틸 =====
    void LoadFromPrefsIntoSliders()
    {
        float m = PlayerPrefs.GetFloat(KEY_MASTER, defaultMaster);
        float b = PlayerPrefs.GetFloat(KEY_BGM,    defaultBGM);
        float s = PlayerPrefs.GetFloat(KEY_SFX,    defaultSFX);

        if (masterSlider) masterSlider.SetValueWithoutNotify(m);
        if (bgmSlider)    bgmSlider.SetValueWithoutNotify(b);
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(s);
    }

    void SavePrefsFromSliders()
    {
        if (masterSlider) PlayerPrefs.SetFloat(KEY_MASTER, Mathf.Clamp01(masterSlider.value));
        if (bgmSlider)    PlayerPrefs.SetFloat(KEY_BGM,    Mathf.Clamp01(bgmSlider.value));
        if (sfxSlider)    PlayerPrefs.SetFloat(KEY_SFX,    Mathf.Clamp01(sfxSlider.value));
        PlayerPrefs.Save();
    }

    void ApplyVolumes()
    {
        if (!SoundManager.Instance) return;
        if (masterSlider) SoundManager.Instance.SetMasterVolume01(masterSlider.value);
        if (bgmSlider)    SoundManager.Instance.SetBGMVolume01(bgmSlider.value);
        if (sfxSlider)    SoundManager.Instance.SetSFXVolume01(sfxSlider.value);
    }
}
