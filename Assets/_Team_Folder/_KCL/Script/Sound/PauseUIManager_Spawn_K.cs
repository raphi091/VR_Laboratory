using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

public class PauseUIManager_Spawn_K : MonoBehaviour
{
    [Header("Prefab / Target")]
    [SerializeField] private PauseUIRoot_K uiPrefab;
    [Tooltip("비워두면 활성 카메라(플레이어 시야)를 자동 탐색합니다.")]
    [SerializeField] private Transform head; // 비워두기 권장
    [SerializeField] private float distance = 1.5f;
    [SerializeField] private Vector3 localOffset = new(0f, -0.05f, 0f);

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("(선택) 액션맵 토글")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string uiActionMapName = "XRI UI";
    [SerializeField] private string[] mapsToDisableWhilePaused =
    {
        "XRI LeftHand Locomotion","XRI RightHand Locomotion"
    };

    [Header("SFX")] [SerializeField] private AudioClip uiClickSfx;

    [Header("Dev/Test")]
    [SerializeField] private bool openOnPlay = false;
    [SerializeField] private bool followHeadWhilePaused = false;
#if UNITY_EDITOR
    [SerializeField] private bool escHotkeyInEditor = true;
#endif

    [Header("Auto Fit (전체 화면 안에 보이게)")]
    [SerializeField] private bool autoFitToView = true;
    [Range(0.5f, 0.95f)] [SerializeField] private float fitViewportPercent = 0.75f;
    [SerializeField] private Vector2 scaleClamp = new(0.05f, 2.0f);
    [SerializeField] private bool onlyShrink = true;
    [SerializeField] private float nearClipPadding = 0.25f;

    [Header("Overlay / Screen-Space 차단(선택)")]
    [Tooltip("일시정지 중 모든 Screen-Space Canvas를 임시 비활성화합니다.")]
    [SerializeField] private bool disableScreenSpaceCanvasesWhilePaused = true;
    [Tooltip("일시정지 중 꺼둘 추가 카메라(URP Overlay 등)")]
    [SerializeField] private Camera[] overlayCamerasToDisableWhilePaused;
    [Tooltip("차단 제외할 Screen-Space 캔버스(화이트리스트)")]
    [SerializeField] private Canvas[] keepScreenSpaceCanvases;

    // ───────── 내부 상태 ─────────
    private PauseUIRoot_K ui;
    private bool paused;
    private float _savedTimeScale = 1f;
    private readonly List<InputActionMap> _disabledAtPause = new();

    private Camera _headCam;
    private float _nextHeadSearchTime;
    private const float HEAD_SEARCH_INTERVAL = 1.0f;

    // 복구용 캐시
    private readonly List<Canvas> _disabledScreenSpaceCanvases = new();
    private readonly List<Behaviour> _disabledOverlayBehaviours = new();

    // XR Device Simulator UI 판별용(리플렉션)
    private static Type _xrSimUIType;
    private bool IsXRDeviceSimulatorCanvas(Canvas c)
    {
        if (_xrSimUIType == null)
            _xrSimUIType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Samples.DeviceSimulator.XRDeviceSimulatorUI, Unity.XR.Interaction.Toolkit");
        if (_xrSimUIType != null && c.GetComponentInParent(_xrSimUIType) != null) return true;
        return c.name.Contains("XR Device Simulator");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        ResolveHead(true);
        BindBButtonToUIClickIfNeeded(); // XR 컨트롤러 B/Y를 UI 클릭에 바인딩
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
        if (escHotkeyInEditor &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        { if (paused) ResumeGame(); else PauseGame(); }
    }
#endif

    private void LateUpdate()
    {
        if (Time.unscaledTime >= _nextHeadSearchTime &&
            (_headCam == null || !_headCam.isActiveAndEnabled))
        {
            EnsureHeadAndCanvas();
        }
        if (paused && followHeadWhilePaused) PositionUI();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;
        if (paused) ResumeGame(); else PauseGame();
    }

    public void PauseGame()
    {
        if (paused) return;

        // 1) 카메라 먼저 확보
        EnsureHeadAndCanvas();

        // 2) UI 인스턴스
        if (ui == null)
        {
            if (!uiPrefab) { Debug.LogWarning("[PauseUI] uiPrefab 미지정"); return; }
            ui = Instantiate(uiPrefab);
            PrepareUIForVR(ui.transform);   // WorldSpace/레이어/알파 교정
            BindCanvasWorldCamera();        // XR 카메라로 worldCamera 지정
            EnsureXRRaycaster(ui.transform);// XR 레이캐스터 보장
            // Wire()는 사용하지 않습니다 (모든 버튼은 인스펙터에서 직접 OnClick 연결)
        }

        // 3) 외부 Screen-Space/Overlay 차단
        if (disableScreenSpaceCanvasesWhilePaused) DisableForeignScreenSpaceCanvases();
        DisableOverlayCameras();

        // 4) 일시정지
        _savedTimeScale = Mathf.Max(0f, Time.timeScale);
        Time.timeScale = 0f;
        paused = true;

        ui.gameObject.SetActive(true);
        ShowPauseOnly();
        PositionUI();              // 열 때 1회 배치
        ToggleMapsForPause(true);
    }

    public void ResumeGame()
    {
        if (!paused) return;
        paused = false;

        Time.timeScale = _savedTimeScale;
        if (ui) ui.gameObject.SetActive(false);
        RestoreForeignScreenSpaceCanvases();
        RestoreOverlayCameras();
        ToggleMapsForPause(false);
    }

    public void ForceClose()
    {
        if (!paused && (ui == null || !ui.gameObject.activeSelf)) return;
        paused = false;

        Time.timeScale = _savedTimeScale;
        if (ui) ui.gameObject.SetActive(false);
        RestoreForeignScreenSpaceCanvases();
        RestoreOverlayCameras();
        ToggleMapsForPause(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    private void EnsureHeadAndCanvas() { ResolveHead(true); BindCanvasWorldCamera(); }

    private void BindCanvasWorldCamera()
    {
        if (ui == null || _headCam == null) return;
        var canvases = ui.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = _headCam;
        }
    }

    private Transform ResolveHead(bool force)
    {
        if (!force && head && head.gameObject.activeInHierarchy && _headCam && _headCam.isActiveAndEnabled) return head;

        _nextHeadSearchTime = Time.unscaledTime + HEAD_SEARCH_INTERVAL;

        Camera cam = null;
        if (Camera.main && Camera.main.isActiveAndEnabled) cam = Camera.main;

        if (!cam)
            foreach (var c in Camera.allCameras)
                if (c.isActiveAndEnabled && c.stereoTargetEye != StereoTargetEyeMask.None) { cam = c; break; }

        if (!cam)
            foreach (var c in Camera.allCameras)
                if (c.isActiveAndEnabled) { cam = c; break; }

        if (cam) { _headCam = cam; head = cam.transform; }
        return head;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    private void PositionUI()
    {
        if (_headCam == null || head == null || ui == null) return;

        var minDist = _headCam.nearClipPlane + nearClipPadding;
        var useDist = Mathf.Max(distance, minDist);

        var h = head;
        var pos = h.position + h.forward.normalized * useDist + h.TransformVector(localOffset);

        var fwd = h.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = h.forward;
        var rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);

        ui.transform.SetPositionAndRotation(pos, rot);

        if (autoFitToView) FitToView_ByBounds();
    }

    // 화면 투영 크기 기준 자동 축소
    private void FitToView_ByBounds()
    {
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(ui.transform);
        Vector3 c = b.center, e = b.extents;
        Vector3[] local =
        {
            new(c.x-e.x, c.y-e.y, c.z-e.z), new(c.x+e.x, c.y-e.y, c.z-e.z),
            new(c.x-e.x, c.y+e.y, c.z-e.z), new(c.x+e.x, c.y+e.y, c.z-e.z),
            new(c.x-e.x, c.y-e.y, c.z+e.z), new(c.x+e.x, c.y-e.y, c.z+e.z),
            new(c.x-e.x, c.y+e.y, c.z+e.z), new(c.x+e.x, c.y+e.y, c.z+e.z),
        };

        float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
        for (int i = 0; i < local.Length; i++)
        {
            var w = ui.transform.TransformPoint(local[i]);
            var v = _headCam.WorldToViewportPoint(w);
            minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x);
            minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
        }

        float maxDim = Mathf.Max(maxX - minX, maxY - minY);
        if (maxDim <= 1e-4f) return;

        float factor = fitViewportPercent / maxDim;
        bool shrink = factor < 1f - 0.01f;
        bool grow   = (!onlyShrink) && factor > 1f + 0.01f;
        if (!shrink && !grow) return;

        float cur = ui.transform.localScale.x;
        float next = cur * factor;
        if (onlyShrink) next = Mathf.Min(cur, next);
        ui.transform.localScale = Vector3.one * Mathf.Clamp(next, scaleClamp.x, scaleClamp.y);
    }

    // 프리팹을 VR에 맞게 강제 교정(WorldSpace/레이어/알파 등)
    private void PrepareUIForVR(Transform root)
    {
        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = _headCam;
            c.sortingOrder = 0;
            var rt = c.GetComponent<RectTransform>();
            if (rt) rt.localScale = Vector3.one;
        }

        var groups = root.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var g in groups) { g.alpha = 1f; g.interactable = true; g.blocksRaycasts = true; }

        int uiLayer = LayerMask.NameToLayer("UI");
        int targetLayer = (_headCam && (_headCam.cullingMask & (1 << uiLayer)) != 0) ? uiLayer : LayerMask.NameToLayer("Default");
        SetLayerRecursively(root.gameObject, targetLayer);

        if (root.localScale.x > 0.2f) root.localScale = Vector3.one * 0.001f;
    }
    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer; foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 외부 Screen-Space Canvas 임시 비활성/복구
    private void DisableForeignScreenSpaceCanvases()
    {
        _disabledScreenSpaceCanvases.Clear();
        var all = FindObjectsOfType<Canvas>(true);
        foreach (var c in all)
        {
            if (!c.isActiveAndEnabled) continue;
            if (ui != null && c.transform.IsChildOf(ui.transform)) continue; // 우리 UI 제외
            if (c.worldCamera == _headCam) continue;                          // XR 메인카메라에 묶인 것 제외
            if (IsXRDeviceSimulatorCanvas(c)) continue;                       // XR Device Simulator UI 제외
            if (keepScreenSpaceCanvases != null && Array.IndexOf(keepScreenSpaceCanvases, c) >= 0) continue;

            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                c.enabled = false;
                _disabledScreenSpaceCanvases.Add(c);
            }
        }
    }
    private void RestoreForeignScreenSpaceCanvases()
    {
        foreach (var c in _disabledScreenSpaceCanvases) if (c) c.enabled = true;
        _disabledScreenSpaceCanvases.Clear();
    }

    // Overlay 카메라 임시 비활성/복구
    private void DisableOverlayCameras()
    {
        _disabledOverlayBehaviours.Clear();
        if (overlayCamerasToDisableWhilePaused == null) return;

        foreach (var cam in overlayCamerasToDisableWhilePaused)
        {
            if (!cam) continue;
            if (cam == _headCam) continue; // 메인 XR 카메라 보호
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data && data.renderType == CameraRenderType.Base) continue; // Base 카메라 보호
#endif
            if (cam.enabled) { cam.enabled = false; _disabledOverlayBehaviours.Add(cam); }
        }
    }
    private void RestoreOverlayCameras()
    {
        foreach (var b in _disabledOverlayBehaviours) if (b) b.enabled = true;
        _disabledOverlayBehaviours.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 패널 전환 & 클릭 SFX (포커스/포인터 초기화 포함)
    private readonly List<CanvasGroup> _uiGroups = new();
    private void CacheCanvasGroupsIfNeeded()
    {
        if (ui == null || _uiGroups.Count > 0) return;
        ui.GetComponentsInChildren(true, _uiGroups);
    }
    private void SetAllCanvasGroupsBlocksRaycasts(bool on)
    {
        CacheCanvasGroupsIfNeeded();
        foreach (var g in _uiGroups) if (g) g.blocksRaycasts = on;
    }

    private IEnumerator SwitchPanelRoutine(Action doToggle, GameObject firstSelect)
    {
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);

        SetAllCanvasGroupsBlocksRaycasts(false); // 포인터 캡처 해제
        doToggle?.Invoke();
        Canvas.ForceUpdateCanvases();

        yield return null; // 다음 프레임

        SetAllCanvasGroupsBlocksRaycasts(true);
        if (es != null && firstSelect != null) es.SetSelectedGameObject(firstSelect);
    }

    private void ShowPauseOnly()
    {
        StartCoroutine(SwitchPanelRoutine(() =>
        {
            ui.pausePanel.SetActive(true);
            ui.soundPanel.SetActive(false);
            ui.exitPanel .SetActive(false);
        }, ui.firstOnPause));
    }

    public void OnClickOpenSound()
    {
        PlayClick();
        StartCoroutine(SwitchPanelRoutine(() =>
        {
            ui.pausePanel.SetActive(false);
            ui.soundPanel.SetActive(true);
            ui.exitPanel .SetActive(false);
        }, ui.firstOnSound));
    }

    public void OnClickExitAsk()
    {
        PlayClick();
        StartCoroutine(SwitchPanelRoutine(() =>
        {
            ui.pausePanel.SetActive(false);
            ui.soundPanel.SetActive(false);
            ui.exitPanel .SetActive(true);
        }, ui.firstOnExit));
    }

    public void OnClickCloseSound(){ PlayClick(); ShowPauseOnly(); }
    public void OnClickResume(){ PlayClick(); ResumeGame(); }
    public void OnClickExitYes(){
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickExitNo(){ PlayClick(); ShowPauseOnly(); }

    // 클릭 사운드 재생(없으면 조용히 무시)
    private void PlayClick()
    {
        if (uiClickSfx == null) return;
        var sm = SoundManager_K.Instance;
        if (sm != null) sm.PlaySFX(uiClickSfx);
    }

    private void ToggleMapsForPause(bool pause)
    {
        if (!inputActions) return;
        var uiMap = string.IsNullOrEmpty(uiActionMapName) ? null : inputActions.FindActionMap(uiActionMapName, false);
        if (pause) uiMap?.Enable(); else uiMap?.Disable();

        if (pause)
        {
            _disabledAtPause.Clear();
            foreach (var name in mapsToDisableWhilePaused)
            {
                var map = inputActions.FindActionMap(name, false);
                if (map != null && map.enabled) { map.Disable(); _disabledAtPause.Add(map); }
            }
        }
        else
        {
            foreach (var map in _disabledAtPause) map?.Enable();
            _disabledAtPause.Clear();
        }
    }

    // 런타임에 B 버튼을 UI Click/Submit에 추가
    private void BindBButtonToUIClickIfNeeded()
    {
        if (!inputActions) return;

        var map = inputActions.FindActionMap(uiActionMapName, throwIfNotFound: false);
        if (map == null) return;

        TryAddSecondaryBinding(map.FindAction("Click",  throwIfNotFound: false));
        TryAddSecondaryBinding(map.FindAction("Submit", throwIfNotFound: false));
    }

    private void TryAddSecondaryBinding(InputAction action)
    {
        if (action == null) return;

        bool wasEnabled = action.enabled;
        if (wasEnabled) action.Disable();

        void AddIfMissing(string path)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (string.Equals(b.effectivePath, path, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(b.path,         path, StringComparison.OrdinalIgnoreCase))
                    return; // 이미 있음
            }
            action.AddBinding(path);
        }

        AddIfMissing("<XRController>{RightHand}/secondaryButton"); // B
        AddIfMissing("<XRController>{LeftHand}/secondaryButton");  // Y

        if (wasEnabled) action.Enable();
    }

    // 월드 스페이스 캔버스에 XR 레이캐스터 자동 부착(필요 시)
    private void EnsureXRRaycaster(Transform root)
    {
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
        {
            if (!c.TryGetComponent<TrackedDeviceGraphicRaycaster>(out _))
                c.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }
}
