using UnityEngine;
using UnityEngine.UI;

public class PauseUIRoot_K : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject soundPanel;
    public GameObject exitPanel;

    [Header("First Select")]
    public GameObject firstOnPause;
    public GameObject firstOnSound;
    public GameObject firstOnExit;

    [Header("Buttons")]
    public Button btnOpenSound;  // BasePanel: 사운드
    public Button btnResume;     // BasePanel: 게임 재개
    public Button btnExitAsk;    // BasePanel: 게임 종료
    public Button btnCloseSound; // SettingPanel: X
    public Button btnExitYes;    // ExitPanel: 체크
    public Button btnExitNo;     // ExitPanel: X

    // 매니저가 런타임에 이벤트를 연결할 때 호출
    public void Wire(PauseUIManager_Spawn_K mgr)
    {
        btnOpenSound.onClick.AddListener(mgr.OnClickOpenSound);
        btnResume   .onClick.AddListener(mgr.OnClickResume);
        btnExitAsk  .onClick.AddListener(mgr.OnClickExitAsk);
        btnCloseSound.onClick.AddListener(mgr.OnClickCloseSound);
        btnExitYes  .onClick.AddListener(mgr.OnClickExitYes);
        btnExitNo   .onClick.AddListener(mgr.OnClickExitNo);
    }
}
