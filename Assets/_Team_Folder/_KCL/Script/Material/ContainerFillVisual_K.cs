using UnityEngine;

public class ContainerFillVisual_K : MonoBehaviour
{
    public enum ContentType { Liquid, Powder }
    public ContentType contentType = ContentType.Liquid;

    [Header("Refs")]
    public Transform fillRoot;     // 병 바닥 기준(로컬 피벗)
    public Transform volumeMesh;   // 내용물 몸통(실린더 등)

    [Header("Capacity/Amount")]
    public float capacity  = 300f; // 총 용량
    public float amount    = 270f; // 현재 양
    public float maxHeight = 0.14f; // '보이는' 최대 높이(로컬 Y)

    [Header("Behaviour")]
    public bool keepVolumeUpright = false; // 물기둥을 병과 같이 회전(false) / 항상 수직(true)

    [Header("Apply Options")]
    public bool autoScaleY = true;     // 플레이 중에만 Y 스케일 자동 반영
    public bool applyInEditor = false; // 에디터에서도 반영(기본 꺼짐)

    [Header("Simple Slosh (optional)")]
    public bool  enableSlosh     = true;
    [Range(0f, 3f)] public float sloshResponse = 1.2f;
    public float sloshHeightAmp  = 0.03f;
    public float sloshFollow     = 8f;
    public float sloshReturn     = 4f;

    Rigidbody rb;
    Quaternion _lastRot;
    bool _initRot = false;
    float sloshY;

    public float Fill01 => Mathf.Clamp01(capacity > 0f ? amount / capacity : 0f);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _lastRot = transform.rotation;
        _initRot = true;
    }

    void Reset()
    {
        if (!fillRoot) fillRoot = transform;
        if (!volumeMesh)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (!volumeMesh && t.name.ToLower().Contains("volume")) volumeMesh = t;
        }
    }

    void OnValidate()
    {
        amount = Mathf.Clamp(amount, 0f, Mathf.Max(0f, capacity));
        // 에디터에서 보정은 선택사항
        if (applyInEditor) ApplyFillVisual(0f);
    }

    void LateUpdate()
    {
        // 플레이 중에만 자동스케일(또는 applyInEditor ON이면 에디터에서도)
        if (Application.isPlaying || applyInEditor)
            ApplyFillVisual(Time.deltaTime);
    }

    void ApplyFillVisual(float dt)
    {
        if (!fillRoot || !volumeMesh) return;

        // 채움 비율 → 로컬 높이 계산
        float f      = Fill01;
        float hLocal = Mathf.Max(0.001f, f * maxHeight);

        // 부모 스케일 보정으로 월드 높이 환산
        float parentScaleY = volumeMesh.parent ? volumeMesh.parent.lossyScale.y : 1f;
        float hWorld = hLocal * parentScaleY;

        Vector3 worldBottom = fillRoot.position;
        Vector3 worldTop    = keepVolumeUpright
            ? worldBottom + Vector3.up * hWorld
            : fillRoot.TransformPoint(new Vector3(0f, hLocal, 0f));

        // 회전/위치
        if (keepVolumeUpright)
        {
            volumeMesh.up = Vector3.up;
            volumeMesh.position = (worldBottom + worldTop) * 0.5f;
        }
        else
        {
            volumeMesh.rotation = fillRoot.rotation;
            volumeMesh.position = (worldBottom + worldTop) * 0.5f;
        }

        // — 여기서부터 스케일 Y 반영(원치 않으면 autoScaleY 끄기) —
        if (autoScaleY)
        {
            var vs = volumeMesh.localScale;
            vs.y = hLocal;               // **여기만 Y 스케일 조정**
            volumeMesh.localScale = vs;
        }

        // 간단 출렁(선택)
        if (enableSlosh)
        {
            float angSpeed = 0f;
            if (_initRot)
            {
                float angleDeg = Quaternion.Angle(_lastRot, transform.rotation);
                angSpeed = (dt > 0f) ? angleDeg * Mathf.Deg2Rad / dt : 0f;
                _lastRot = transform.rotation;
            }
            else { _lastRot = transform.rotation; _initRot = true; }

            float tilt = Vector3.Angle(fillRoot.up, Vector3.up) * Mathf.Deg2Rad;
            float target = Mathf.Clamp01(angSpeed * sloshResponse + tilt * 0.2f) * sloshHeightAmp;

            float riseT  = (sloshFollow > 0f) ? (1f - Mathf.Exp(-sloshFollow * dt)) : 1f;
            float decayT = (sloshReturn > 0f) ? (1f - Mathf.Exp(-sloshReturn * dt)) : 1f;
            if (target > sloshY) sloshY = Mathf.Lerp(sloshY, target, riseT);
            else                 sloshY = Mathf.Lerp(sloshY, 0f,     decayT);

            // 슬로시 높이는 월드 위치로만 표현(메시 스케일은 건드리지 않음)
            if (keepVolumeUpright) volumeMesh.position += Vector3.up * sloshY * 0.0f; // 원하면 0.0f → 0.5f 정도로
        }
    }

    // 외부에서 양 증감
    public void Add(float delta)       => amount = Mathf.Clamp(amount + delta, 0f, capacity);
    public void SetAmount(float value) => amount = Mathf.Clamp(value, 0f, capacity);

    [ContextMenu("Force Apply Now")]
    void ForceApplyNow() => ApplyFillVisual(0f);
}
