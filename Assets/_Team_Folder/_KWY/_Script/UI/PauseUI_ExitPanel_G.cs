using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI_ExitPanel_G : MonoBehaviour
{
    private PauseUI_G pause;


    private void OnEnable()
    {
        pause = GetComponentInParent<PauseUI_G>();
    }

    public void OnClick_CloseExitConfirm()
    {
        pause.OnClick_CloseExitConfirm();
    }

    public void OnClick_ExitGame()
    {
        pause.OnClick_ExitGame();
    }
}
