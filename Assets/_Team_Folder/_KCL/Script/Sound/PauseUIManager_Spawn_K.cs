using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PauseUIManager_Spawn_K : MonoBehaviour
{
    [Header("Prefab / Target")]
    [SerializeField] private PauseUIRoot_K uiPrefab;
    [SerializeField] private Transform head;          // 보통 Main Camera
    [SerializeField] private float distance = 1.2f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.05f, 0f);

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction; // GameInput ▸ Pause / Esc 등

    [Header("(선택) 액션맵 토글")]
    [Tooltip("일시정지 중 켤 UI 액션맵 이름(예: XRI UI, UI)")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string uiActionMapName = "XRI UI";
    [Tooltip("일시정지 중 비활성화할 맵들")]
    [SerializeField] private string[] mapsToDisableWhilePaused =
    {
        "XRI LeftHand","XRI RightHand",
        "XRI LeftHand Interaction","XRI RightHand Interaction",
        "XRI LeftHand Locomotion","XRI RightHand Locomotion"
    };

    [Header("SFX")]
    [SerializeField] private AudioClip uiClickSfx;

    [Header("Dev/Test")]
    [SerializeField] private bool openOnPlay = false;           // 실행하자마자 열기(테스트용)
    [SerializeField] private bool followHeadWhilePaused = true; // 일시정지 중 머리 움직임 추적
#if UNITY_EDITOR
    [SerializeField] private bool escHotkeyInEditor = true;     // 에디터에서 ESC로 토글
#endif

    private PauseUIRoot_K ui;                     // 런타임 인스턴스
    private bool paused;
    private float _savedTimeScale = 1f;
    private readonly List<InputActionMap> _disabledAtPause = new();

    // ─────────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (!head && Camera.main) head = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPause;
            pauseAction.action.Enable();
        }

        if (openOnPlay) PauseGame();
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPause;
            pauseAction.action.Disable();
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!escHotkeyInEditor) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (paused) ResumeGame(); else PauseGame();
        }
    }
#endif

    private void LateUpdate()
    {
        if (paused && followHeadWhilePaused) PositionUI();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 입력 이벤트
    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;
        if (paused) ResumeGame(); else PauseGame();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // UI 위치/방향
    private void PositionUI()
    {
        if (!head || !ui) return;

        var pos = head.position + head.forward.normalized * distance + head.TransformVector(localOffset);

        // 수평만 바라보게(어지럼 방지)
        var fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = head.forward;

        var rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        ui.transform.SetPositionAndRotation(pos, rot);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 버튼 핸들러
    public void OnClickOpenSound()  { PlayClick(); ShowSound(); }
    public void OnClickResume()     { PlayClick(); ResumeGame(); }
    public void OnClickExitAsk()    { PlayClick(); ShowExit(); }
    public void OnClickCloseSound() { PlayClick(); ShowPauseOnly(); }
    public void OnClickExitYes()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickExitNo()     { PlayClick(); ShowPauseOnly(); }

    // ─────────────────────────────────────────────────────────────────────────────
    // 코어 플로우
    public void PauseGame()
    {
        if (paused) return;

        // UI 인스턴스 준비
        if (ui == null)
        {
            if (!uiPrefab)
            {
                Debug.LogWarning("[PauseUI] uiPrefab 미지정 — 일시정지를 취소합니다.");
                return;
            }
            ui = Instantiate(uiPrefab);
            ui.Wire(this); // 버튼 이벤트 연결
        }

        // 시간 정지
        _savedTimeScale = Mathf.Max(0f, Time.timeScale);
        Time.timeScale  = 0f;

        paused = true;

        ui.gameObject.SetActive(true);
        ShowPauseOnly();
        PositionUI();

        ToggleMapsForPause(true); // 맵 토글
    }

    public void ResumeGame()
    {
        if (!paused) return;

        paused = false;

        // 시간 복구
        Time.timeScale = _savedTimeScale;

        if (ui) ui.gameObject.SetActive(false);

        ToggleMapsForPause(false); // 맵 복구
    }

    // 씬 전환 등 강제 닫기용
    public void ForceClose()
    {
        if (!paused && (ui == null || !ui.gameObject.activeSelf)) return;

        paused = false;
        Time.timeScale = _savedTimeScale;
        if (ui) ui.gameObject.SetActive(false);
        ToggleMapsForPause(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 패널 전환
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 액션맵 토글
    private void ToggleMapsForPause(bool pause)
    {
        if (!inputActions) return;

        // UI 맵 on/off
        var uiMap = string.IsNullOrEmpty(uiActionMapName)
            ? null
            : inputActions.FindActionMap(uiActionMapName, throwIfNotFound: false);

        if (pause) uiMap?.Enable();
        else       uiMap?.Disable();

        // 나머지 맵들 토글
        if (pause)
        {
            _disabledAtPause.Clear();
            foreach (var name in mapsToDisableWhilePaused)
            {
                var map = inputActions.FindActionMap(name, throwIfNotFound: false);
                if (map != null && map.enabled)
                {
                    map.Disable();
                    _disabledAtPause.Add(map);
                }
            }
        }
        else
        {
            foreach (var map in _disabledAtPause) map?.Enable();
            _disabledAtPause.Clear();
        }
    }
}
