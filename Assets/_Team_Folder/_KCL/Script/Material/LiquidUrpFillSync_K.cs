using UnityEngine;

[ExecuteAlways]
public class LiquidUrpFillSync_K : MonoBehaviour
{
    [Header("Refs")]
    public ContainerFillVisual_K visual;           // 병의 양(ml)
    public WaterPourController_K pourCtrl;         // (선택) 주둥이/FX/각도 참조
    public Renderer liquidRenderer;                // Liquid의 MeshRenderer

    [Header("Shader Fill Mapping")]
    public string fillProperty = "_Fill";          // Shader Graph 속성명
    public float shaderValueWhenFull  =  1.0f;     // 가득 찼을 때 _Fill 값
    public float shaderValueWhenEmpty = -1.0f;     // 비었을 때 _Fill 값
    [Range(0f,1f)] public float damp = 0.15f;      // 부드러운 보간(0=즉시)

    [Header("Raycast to gel tray (optional)")]
    public Transform rayOrigin;                    // 보통 SpoutPivot
    public float rayDistance = 0.25f;
    public LayerMask rayMask = ~0;
    public Transform gelTray;                      // 목표 트레이
    public Fill_K gelFill;                           // 트레이의 Fill 스크립트( isfilling )

    MaterialPropertyBlock _mpb;
    int _fillID;
    float _currentShaderFill;

    void Reset()
    {
        if (!visual)   visual   = GetComponentInParent<ContainerFillVisual_K>();
        if (!pourCtrl) pourCtrl = GetComponentInParent<WaterPourController_K>();
        if (!liquidRenderer)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.name.ToLower().Contains("liquid")) { liquidRenderer = r; break; }
        }
    }

    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _fillID = Shader.PropertyToID(fillProperty);
        if (!rayOrigin && pourCtrl && pourCtrl.spoutTip) rayOrigin = pourCtrl.spoutTip;
    }

    void OnValidate()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _fillID = Shader.PropertyToID(fillProperty);
    }

    void LateUpdate()
    {
        // 1) amount → _Fill 동기화
        if (visual && liquidRenderer)
        {
            float f = Mathf.Clamp01(visual.capacity > 0f ? visual.amount / visual.capacity : 0f);
            float target = Mathf.Lerp(shaderValueWhenEmpty, shaderValueWhenFull, f);

            if (damp > 0f && Application.isPlaying)
                _currentShaderFill = Mathf.Lerp(_currentShaderFill, target, 1f - Mathf.Exp(-damp * Time.deltaTime));
            else
                _currentShaderFill = target;

            liquidRenderer.GetPropertyBlock(_mpb, 0);
            _mpb.SetFloat(_fillID, _currentShaderFill);
            liquidRenderer.SetPropertyBlock(_mpb, 0);
        }

        // 2) 레이캐스트로 gelTray 채우기 (붓는 중일 때만 true)
        if (gelFill)
        {
            bool pouring = false;

            if (pourCtrl)
            {
                // 우선순위 1: FX가 실제로 분사 상태인지(EmitBurst/Emission 모두 커버)
                if (pourCtrl.fx) pouring = pourCtrl.fx.isPlaying;

                // 보수적 대안: 각도로 추정 (fx 미연결 대비)
                if (!pouring)
                {
                    float tilt = Vector3.Angle(pourCtrl.transform.up, Vector3.up);
                    float startAngle = pourCtrl.minAngleToPour;
                    if (pourCtrl.usePourableAngle && pourCtrl.pourable)
                        startAngle = Mathf.Max(startAngle, pourCtrl.pourable.pourAngle);
                    float endAngle = Mathf.Max(pourCtrl.fullRateAngle, startAngle + 1f);
                    float t = Mathf.Clamp01(Mathf.InverseLerp(startAngle, endAngle, tilt));
                    pouring = t > 0f;
                }
            }

            bool shouldFill = false;
            if (pouring && (Application.isPlaying || !Application.isPlaying)) // 에디터에서도 확인 원하면 유지
            {
                Vector3 origin = rayOrigin ? rayOrigin.position : transform.position;
                Vector3 dir    = Vector3.down;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
                {
                    if (gelTray && hit.transform == gelTray) shouldFill = true;
                }
            }

            gelFill.isfilling = shouldFill;
        }
    }
}
