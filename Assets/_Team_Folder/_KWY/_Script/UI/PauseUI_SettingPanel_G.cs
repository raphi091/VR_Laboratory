using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI_SettingPanel_G : MonoBehaviour
{
    private PauseUI_G pause;


    private void OnEnable()
    {
        pause = GetComponentInParent<PauseUI_G>();
    }

    public void OnClick_CloseSettings()
    {
        pause.OnClick_CloseSettings();
    }
}
