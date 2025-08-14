using UnityEngine;

[RequireComponent(typeof(PourableItem_K))]
public class WaterPourController_K : MonoBehaviour
{
    [Header("Refs")]
    public PourableItem_K pourable;
    public ContainerFillVisual_K visual;
    public Transform spoutTip;
    public ParticleSystem fx;

    [Header("Rates (mL/s)")]
    [Min(0f)] public float mlPerSecMax = 80f;
    [Range(0f,180f)] public float minAngleToPour = 60f;
    [Range(0f,180f)] public float fullRateAngle  = 140f;

    [Header("Mouth detection")]
    public float mouthDetectRadius = 0.035f;
    public bool requireMouthToPour = false;

    [Header("Start/FX options")]
    public bool playFxFromStart = true;           // 시작하자마자 FX를 Play 상태로
    public bool prewarmFx = true;                 // 첫 프레임부터 안정적으로 보이게
    public bool forcePourOnStart = false;         // (선택) 시작 직후 강제 붓기 디버그
    public float startPourDuration = 0.5f;        // 강제 붓기 시간
    public float startPourRate = 60f;             // 강제 붓기 속도(mL/s)

    float startTimer;

    void Reset()
    {
        pourable = GetComponent<PourableItem_K>();
        if (!spoutTip) spoutTip = transform.Find("SpoutPivot/FX");
    }

    void Awake()
    {
        if (!pourable) pourable = GetComponent<PourableItem_K>();
        if (!visual)   visual   = GetComponent<ContainerFillVisual_K>();
        if (!spoutTip) spoutTip = transform;

        // ★ FX를 항상 켜둔 채 Emission만 조절하도록 세팅
        if (fx)
        {
            var main = fx.main;
            main.loop = true;
            main.playOnAwake = false; // 코드에서 Play 제어
            main.prewarm = prewarmFx;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            var e = fx.emission;
            e.enabled = true;
            e.rateOverTime = 0f; // 시작은 0 (기울이면 바로 증가)

            if (playFxFromStart) fx.Play(true);   // ← 시작부터 Play 상태 유지
        }
    }

    void Start()
    {
        if (forcePourOnStart) startTimer = startPourDuration;
    }

        void Update()
    {
        // (옵션) 시작 직후 강제 붓기 디버그
        if (startTimer > 0f)
        {
            float startDeltaMl = startPourRate * Time.deltaTime;   // << 이름 변경
            if (visual) visual.Add(-startDeltaMl);
            ToggleFx(true, startPourRate);
            startTimer -= Time.deltaTime;
            return;
        }

        // 빈 병이면 FX만 0으로 유지
        if (visual && visual.amount <= 0f) { ToggleFx(false); return; }

        // 세움=0°, 뒤집힘=180°
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        float startAngle = Mathf.Max(minAngleToPour, (pourable ? pourable.pourAngle : 0f));
        float endAngle   = Mathf.Max(fullRateAngle, startAngle + 1f);
        float t = Mathf.Clamp01(Mathf.InverseLerp(startAngle, endAngle, tiltAngle));

        // 입구 감지(필요 시)
        BaffledFlask_K target = null;
        bool hasMouth = false;
        if (requireMouthToPour && spoutTip)
        {
            var hits = Physics.OverlapSphere(spoutTip.position, mouthDetectRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var mouth = hits[i].GetComponent<BaffledFlaskMouth_K>();
                if (mouth) { target = mouth.flask; hasMouth = true; break; }
            }
        }

        bool canPour = t > 0f && (!requireMouthToPour || hasMouth);
        if (!canPour) { ToggleFx(false); return; }

        // 이번 프레임 붓는 양(mL)
        float rate      = mlPerSecMax * t;
        float deltaMl   = rate * Time.deltaTime;      // << 이름 변경

        if (visual) visual.Add(-deltaMl);             // 병 수량 감소
        if (target) target.AddWater(deltaMl);         // 플라스크 증가(있다면)

        ToggleFx(true, rate);                         // FX는 emission만 조절 (Play 유지)
    }

    void ToggleFx(bool on, float rate = 0f)
    {
        if (!fx) return;

        // 주둥이 위치/방향 맞추기
        if (spoutTip) fx.transform.SetPositionAndRotation(spoutTip.position, spoutTip.rotation);

        var e = fx.emission;
        if (on)
        {
            e.enabled = true;
            e.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Max(20f, rate * 10f));
            if (!fx.isPlaying) fx.Play(true);     // 혹시 꺼져 있으면 켜줌
            // 첫 프레임 끊김 방지
            fx.Simulate(0f, true, false, true);
        }
        else
        {
            e.rateOverTime = 0f;                  // 멈출 때도 Stop하지 않음(계속 Play)
            e.enabled = true;                     // emission만 0으로 유지
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!spoutTip) return;
        Gizmos.color = new Color(0,1,1,0.25f);
        Gizmos.DrawSphere(spoutTip.position, mouthDetectRadius);
    }
}
