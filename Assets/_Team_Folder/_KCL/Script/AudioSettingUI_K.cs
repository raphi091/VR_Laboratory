using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;


public class AudioSettingUI_K : MonoBehaviour
{
    
    [Header("Sound Setting")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider bgmVolume;
    [SerializeField] private Slider sfxVolume;

    private GameSetting_K settings;

       private void Start()
    {
        settings = new GameSetting_K();

        LoadSettingsToUI();


        masterVolume.onValueChanged.AddListener(SetMasterVolume);
        bgmVolume.onValueChanged.AddListener(SetBGMVolume);
        sfxVolume.onValueChanged.AddListener(SetSFXVolume);


    }

    private void LoadSettingsToUI()
    {
        masterVolume.value = settings.masterVolume;
        bgmVolume.value = settings.bgmVolume;
        sfxVolume.value = settings.sfxVolume;

        SetMasterVolume(settings.masterVolume);
        SetBGMVolume(settings.bgmVolume);
        SetSFXVolume(settings.sfxVolume);

    }

    

    public void SetMasterVolume(float value)
    {
        float volume = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
        mixer.SetFloat("MasterVolume", volume);
       // DataManager.Instance.setting.masterVolume = value;
    }

    public void SetBGMVolume(float value)
    {
        float volume = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
        mixer.SetFloat("BGMVolume", volume);
      //  DataManager.Instance.setting.bgmVolume = value;
    }

    public void SetSFXVolume(float value)
    {
        float volume = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
        mixer.SetFloat("SFXVolume", volume);
      //  DataManager.Instance.setting.sfxVolume = value;
    }

   
}