using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GameSetting_K
{
    
    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
    
    public GameSetting_K()
    {
        masterVolume = 1f;
        bgmVolume = 1f;
        sfxVolume = 1f;
    }
}