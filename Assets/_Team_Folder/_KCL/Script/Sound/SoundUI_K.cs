using UnityEngine;
using UnityEngine.UI;

public class SoundUI_K : MonoBehaviour
{
    [Header("Sliders (0~1)")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Defaults")]
    [Range(0f,1f)] public float defaultMaster = 1.0f;
    [Range(0f,1f)] public float defaultBGM    = 0.8f;
    [Range(0f,1f)] public float defaultSFX    = 0.8f;

    // PlayerPrefs Keys
    const string KEY_MASTER = "vol_master";
    const string KEY_BGM    = "vol_bgm";
    const string KEY_SFX    = "vol_sfx";

    private void OnEnable()
    {
        LoadIntoSliders();
        ApplyAll();  // 패널 열릴 때 1회 적용

        // 변경 즉시 반영 + 저장
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => { ApplyMaster(v); SaveMaster(v); });
        if (bgmSlider)    bgmSlider.onValueChanged.AddListener(v => { ApplyBGM(v);    SaveBGM(v);    });
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener(v => { ApplySFX(v);    SaveSFX(v);    });
    }

    private void OnDisable()
    {
        // 리스너 정리
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (bgmSlider)    bgmSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider)    sfxSlider.onValueChanged.RemoveAllListeners();

        // 마지막 상태 저장 보증
        SaveAll();
    }

    #region Apply
    private void ApplyMaster(float v)
    {
        if (SoundManager_K.Instance) SoundManager_K.Instance.SetMasterVolume01(v);
    }
    private void ApplyBGM(float v)
    {
        if (SoundManager_K.Instance) SoundManager_K.Instance.SetBGMVolume01(v);
    }
    private void ApplySFX(float v)
    {
        if (SoundManager_K.Instance) SoundManager_K.Instance.SetSFXVolume01(v);
    }
    private void ApplyAll()
    {
        if (masterSlider) ApplyMaster(masterSlider.value);
        if (bgmSlider)    ApplyBGM(bgmSlider.value);
        if (sfxSlider)    ApplySFX(sfxSlider.value);
    }
    #endregion

    #region Save/Load
    private void LoadIntoSliders()
    {
        float m = PlayerPrefs.GetFloat(KEY_MASTER, defaultMaster);
        float b = PlayerPrefs.GetFloat(KEY_BGM,    defaultBGM);
        float s = PlayerPrefs.GetFloat(KEY_SFX,    defaultSFX);

        if (masterSlider) masterSlider.SetValueWithoutNotify(m);
        if (bgmSlider)    bgmSlider.SetValueWithoutNotify(b);
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(s);
    }

    private void SaveMaster(float v) { PlayerPrefs.SetFloat(KEY_MASTER, Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    private void SaveBGM(float v)    { PlayerPrefs.SetFloat(KEY_BGM,    Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    private void SaveSFX(float v)    { PlayerPrefs.SetFloat(KEY_SFX,    Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    private void SaveAll()
    {
        if (masterSlider) SaveMaster(masterSlider.value);
        if (bgmSlider)    SaveBGM(bgmSlider.value);
        if (sfxSlider)    SaveSFX(sfxSlider.value);
    }
    #endregion
}
