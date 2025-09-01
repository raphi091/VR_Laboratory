using UnityEngine;
using UnityEngine.UI;

public class PauseUIRoot_K : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;   // BasePanel
    public GameObject soundPanel;   // SettingPanel
    public GameObject exitPanel;    // ExitPanel

    [Header("First Select")]
    public GameObject firstOnPause;
    public GameObject firstOnSound;
    public GameObject firstOnExit;

    [Header("Buttons (Base)")]
    public Button btnOpenSound;   // 사운드
    public Button btnResume;      // 재개
    public Button btnExitAsk;     // 종료 확인

    [Header("Buttons (Sound / Setting)")]
    public Button btnCloseSound;  // ← SettingPanel 상단 X (필수)

    [Header("Buttons (Exit)")]
    public Button btnExitYes;
    public Button btnExitNo;
    public Button btnCloseExit;   // ← ExitPanel 상단 X가 있다면 여기에

    // PauseUIManager_Spawn_K에서 ui.Wire(this)로 호출됨
    public void Wire(PauseUIManager_Spawn_K mgr)
    {
        void Hook(Button b, UnityEngine.Events.UnityAction a)
        { if (b){ b.onClick.RemoveAllListeners(); b.onClick.AddListener(a); } }

        // Base
        Hook(btnOpenSound, mgr.OnClickOpenSound);
        Hook(btnResume,    mgr.OnClickResume);
        Hook(btnExitAsk,   mgr.OnClickExitAsk);

        // Setting (뒤로가기 = BasePanel)
        Hook(btnCloseSound, mgr.OnClickCloseSound);

        // Exit
        Hook(btnExitYes, mgr.OnClickExitYes);
        Hook(btnExitNo,  mgr.OnClickExitNo);

        // Exit에도 X가 있으면 동일 동작
        Hook(btnCloseExit, mgr.OnClickCloseSound);
    }
}
