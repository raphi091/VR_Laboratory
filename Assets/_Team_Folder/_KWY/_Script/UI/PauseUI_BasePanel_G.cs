using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI_BasePanel_G : MonoBehaviour
{
    private PauseUI_G pause;


    private void OnEnable()
    {
        pause = GetComponentInParent<PauseUI_G>();
    }

    public void OnClick_Resume()
    {
        pause.OnClick_Resume();
    }

    public void OnClick_OpenSettings()
    {
        pause.OnClick_OpenSettings();
    }

    public void OnClick_OpenExitConfirm()
    {
        pause.OnClick_OpenExitConfirm();
    }
}
