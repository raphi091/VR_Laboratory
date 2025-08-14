using UnityEngine;

public class ContainerFillVisual_K : MonoBehaviour
{
    public enum ContentType { Liquid, Powder }
    public ContentType contentType = ContentType.Liquid;

    [Header("Ref")]
    public Transform liquid;                 // Glass/Liquid

    [Header("Capacity / Amount (ml)")]
    public float capacity  = 300f;           // 총 용량
    public float amount    = 300f;           // 현재 양(시작 가득이면 capacity와 같게)
    public float maxHeight = 0.14f;          // 가득 찼을 때 '보이는' 높이(로컬 Y)

    [Header("Behaviour")]
    public bool keepVolumeUpright = false;   // true: 항상 수직 / false: 병과 함께 기울기
    public bool pivotIsCenter     = true;    // Glass 피벗이 중앙이면 ON, 바닥 피벗이면 OFF
    public float extraBottomOffset = 0f;     // 바닥 미세 보정(로컬 Y)

    [Header("Apply Options")]
    public bool autoScaleY   = true;         // ← 반드시 ON: Y 스케일로 수위 반영
    public bool applyInEditor = false;       // 에디터에서도 미리보기 적용

    [Header("Simple Slosh (optional)")]
    public bool  enableSlosh     = false;
    [Range(0f, 3f)] public float sloshResponse = 1.2f;
    public float sloshHeightAmp  = 0.03f;
    public float sloshFollow     = 8f;
    public float sloshReturn     = 4f;

    Quaternion _lastRot;
    bool _initRot = false;
    float sloshY;

    public float Fill01 => Mathf.Clamp01(capacity > 0f ? amount / capacity : 0f);

    void Awake()
    {
        if (!liquid)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (!liquid && t.name.ToLower().Contains("liquid")) liquid = t;
        }
        _lastRot = transform.rotation;
        _initRot = true;
    }

    void OnValidate()
    {
        amount = Mathf.Clamp(amount, 0f, Mathf.Max(0f, capacity));
        if (applyInEditor) ApplyFillVisual(0f);
    }

    void LateUpdate()
    {
        if (Application.isPlaying || applyInEditor)
            ApplyFillVisual(Time.deltaTime);
    }

    void ApplyFillVisual(float dt)
    {
        if (!liquid) return;

        // 채움 비율 → 로컬 높이
        float f      = Fill01;
        float hLocal = Mathf.Max(0.001f, f * maxHeight);

        // Glass 피벗이 중앙이면 바닥은 -maxHeight/2, 바닥 피벗이면 0
        float baseBottom    = pivotIsCenter ? (-0.5f * maxHeight) : 0f;
        float bottomLocalY  = baseBottom + extraBottomOffset;

        // 부모 스케일 보정(월드 높이)
        float parentScaleY = liquid.parent ? liquid.parent.lossyScale.y : 1f;
        float hWorld = hLocal * parentScaleY;

        // 바닥/꼭대기(월드 좌표)
        Vector3 worldBottom = transform.TransformPoint(new Vector3(0f, bottomLocalY, 0f));
        Vector3 worldTop;

        if (keepVolumeUpright)
        {
            worldTop  = worldBottom + Vector3.up * hWorld;
            liquid.up = Vector3.up;
        }
        else
        {
            worldTop = transform.TransformPoint(new Vector3(0f, bottomLocalY + hLocal, 0f));
            liquid.rotation = transform.rotation;
        }

        // 위치: 바닥~꼭대기 중간
        liquid.position = (worldBottom + worldTop) * 0.5f;

        // **핵심: Y 스케일로 수위 반영**
        if (autoScaleY)
        {
            var vs = liquid.localScale;
            vs.y = hLocal;              // 가득차면 maxHeight, 비면 거의 0
            liquid.localScale = vs;
        }

        // (선택) 간단 출렁
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

            float tilt   = Vector3.Angle(transform.up, Vector3.up) * Mathf.Deg2Rad;
            float target = Mathf.Clamp01(angSpeed * sloshResponse + tilt * 0.2f) * sloshHeightAmp;

            float riseT  = (sloshFollow > 0f) ? (1f - Mathf.Exp(-sloshFollow * dt)) : 1f;
            float decayT = (sloshReturn > 0f) ? (1f - Mathf.Exp(-sloshReturn * dt)) : 1f;
            if (target > sloshY) sloshY = Mathf.Lerp(sloshY, target, riseT);
            else                 sloshY = Mathf.Lerp(sloshY, 0f,     decayT);
        }
    }

    // 외부 제어(API)
    public void Add(float delta)
    {
        amount = Mathf.Clamp(amount + delta, 0f, capacity);
        if (applyInEditor || Application.isPlaying) ApplyFillVisual(0f);
    }
    public void SetAmount(float value)
    {
        amount = Mathf.Clamp(value, 0f, capacity);
        if (applyInEditor || Application.isPlaying) ApplyFillVisual(0f);
    }

    [ContextMenu("Force Apply Now")]
    void ForceApplyNow() => ApplyFillVisual(0f);
}
