using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PauseUIManager_Spawn_K : MonoBehaviour
{
    [Header("Prefab / Target")]
    [SerializeField] private PauseUIRoot_K uiPrefab;
    [SerializeField] private Transform head;   // 보통 Main Camera
    [SerializeField] private float distance = 1.2f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.05f, 0f);

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction; // ← 여기엔 GameInput ▸ Gameplay ▸ Pause 액션을 드래그

    [Header("(선택) 액션맵 토글")]
    [Tooltip("일시정지 중에 켤 UI 액션맵 이름. (예: XRI UI 또는 UI)")]
    [SerializeField] private InputActionAsset inputActions;          // 액션 에셋 전체
    [SerializeField] private string uiActionMapName = "XRI UI";      // UI용 액션맵 이름
    [Tooltip("일시정지 중에 끌 액션맵들(이동/상호작용 등)을 나열")]
    [SerializeField] private string[] mapsToDisableWhilePaused =
    {
        "XRI LeftHand", "XRI RightHand",
        "XRI LeftHand Interaction", "XRI RightHand Interaction",
        "XRI LeftHand Locomotion",  "XRI RightHand Locomotion"
    };

    [Header("SFX")]
    [SerializeField] private AudioClip uiClickSfx;

    private PauseUIRoot_K ui;    // 인스턴스
    private bool paused;
    private readonly List<InputActionMap> _disabledAtPause = new();

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPause;
            pauseAction.action.Enable();
        }
    }
    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPause;
            pauseAction.action.Disable();
        }
    }

    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;
        if (paused) ResumeGame(); else PauseGame();
    }

    private void PositionUI()
    {
        if (!head || !ui) return;
        var pos = head.position + head.forward.normalized * distance + head.TransformVector(localOffset);
        var fwd = head.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = head.forward;
        var rot = Quaternion.LookRotation(fwd, Vector3.up);
        ui.transform.SetPositionAndRotation(pos, rot);
    }

    // ====== 외부(버튼)에서 호출되는 핸들러 ======
    public void OnClickOpenSound(){ PlayClick(); ShowSound(); }
    public void OnClickResume()   { PlayClick(); ResumeGame(); }
    public void OnClickExitAsk()  { PlayClick(); ShowExit(); }
    public void OnClickCloseSound(){ PlayClick(); ShowPauseOnly(); }
    public void OnClickExitYes()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickExitNo()   { PlayClick(); ShowPauseOnly(); }

    // ====== 코어 플로우 ======
    public void PauseGame()
    {
        if (paused) return;
        paused = true;
        Time.timeScale = 0f;

        if (ui == null)
        {
            ui = Instantiate(uiPrefab);
            ui.Wire(this); // 버튼 이벤트 연결
        }
        ui.gameObject.SetActive(true);
        ShowPauseOnly();
        PositionUI();

        ToggleMapsForPause(true);   // << 여기서 맵 토글
    }

    public void ResumeGame()
    {
        paused = false;
        Time.timeScale = 1f;
        if (ui) ui.gameObject.SetActive(false);

        ToggleMapsForPause(false);  // << 복구
    }

    private void ShowPauseOnly()
    {
        ui.pausePanel.SetActive(true);
        ui.soundPanel.SetActive(false);
        ui.exitPanel .SetActive(false);
        EventSystem.current?.SetSelectedGameObject(ui.firstOnPause);
    }
    private void ShowSound()
    {
        ui.pausePanel.SetActive(false);
        ui.soundPanel.SetActive(true);
        ui.exitPanel .SetActive(false);
        EventSystem.current?.SetSelectedGameObject(ui.firstOnSound);
    }
    private void ShowExit()
    {
        ui.pausePanel.SetActive(false);
        ui.soundPanel.SetActive(false);
        ui.exitPanel .SetActive(true);
        EventSystem.current?.SetSelectedGameObject(ui.firstOnExit);
    }

    private void PlayClick()
    {
        if (uiClickSfx && SoundManager_K.Instance)
            SoundManager_K.Instance.PlaySFX(uiClickSfx);
    }

    // ====== 액션맵 토글 ======
    private void ToggleMapsForPause(bool pause)
    {
        if (!inputActions) return;

        // UI 맵
        var uiMap = string.IsNullOrEmpty(uiActionMapName) ? null : inputActions.FindActionMap(uiActionMapName, false);
        if (pause) uiMap?.Enable(); else uiMap?.Disable();

        // 끌/복구할 맵들
        if (pause)
        {
            _disabledAtPause.Clear();
            foreach (var name in mapsToDisableWhilePaused)
            {
                var map = inputActions.FindActionMap(name, false);
                if (map != null && map.enabled)
                {
                    map.Disable();
                    _disabledAtPause.Add(map);
                }
            }
        }
        else
        {
            foreach (var map in _disabledAtPause)
                map?.Enable();
            _disabledAtPause.Clear();
        }
    }
}
