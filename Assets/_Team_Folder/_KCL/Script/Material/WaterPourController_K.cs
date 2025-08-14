using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PourableItem_K))]
public class WaterPourController_K : MonoBehaviour
{
    // ───── Refs ─────
    public PourableItem_K pourable;              // (옵션) 임계각 참고용
    public ContainerFillVisual_K visual;         // 용량/잔량
    public Transform spoutTip;                   // "SpoutPivot"
    public ParticleSystem fx;                    // "FX" (Particle System)

    // ───── Angles & Rate ─────
    [Min(0f)] public float mlPerSecMax = 80f;    // 최대 유량(ml/s)
    [Range(0,180)] public float minAngleToPour = 60f;
    [Range(0,180)] public float fullRateAngle  = 140f;
    public bool usePourableAngle = false;        // pourable.pourAngle 포함 여부

    // ───── Mouth(detect) 옵션 ─────
    public bool requireMouthToPour = false;
    public float mouthDetectRadius = 0.035f;

    // ───── FX 옵션 ─────
    public bool playFxFromStart = false;         // 시작은 기본 정지
    public bool prewarmFx = true;
    public float fxForwardOffset = 0.03f;        // 주둥이 앞으로 살짝

    // ───── Debug / Test ─────
    public bool ignoreEmptyCheck = false;        // 잔량 0이면 기본 막음
    public bool forceFxAlways = false;           // 강제 분사(테스트용)
    public bool debugOverlay = true;

    // ───── FX 구동 방식 ─────
    public enum FxDrive { Emission, EmitBurst }
    public FxDrive fxDrive = FxDrive.EmitBurst;  // 신뢰성 높은 방식
    public int particlesPer100ml = 60;           // 유량→파티클 매핑

    // ───── 외부에서 읽을 상태 ─────
    public bool  IsPouring           { get; private set; }
    public float CurrentRateMlPerSec { get; private set; }

    // ───── 내부 디버그 ─────
    float _dbgAngle, _dbgT, _dbgRate, _dbgAmt;

    void Reset()
    {
        pourable = GetComponent<PourableItem_K>();
        if (!spoutTip) spoutTip = transform.Find("SpoutPivot");
    }

    void Awake()
    {
        if (!pourable) pourable = GetComponent<PourableItem_K>();
        if (!visual)   visual   = GetComponent<ContainerFillVisual_K>();
        if (!spoutTip) spoutTip = transform;

        // fx 자동 탐색(비워뒀을 때)
        if (!fx)
        {
            if (spoutTip) fx = spoutTip.GetComponentInChildren<ParticleSystem>(true);
            if (!fx)      fx = GetComponentInChildren<ParticleSystem>(true);
        }

        if (fx)
        {
            var main = fx.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = prewarmFx;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var em = fx.emission;
            em.enabled = true;
            em.rateOverTime = 0f;

            // 시작은 완전 정지(요청대로)
            fx.Clear(true);
            if (playFxFromStart) fx.Play(true);
            else fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Update()
    {
        // 강제 테스트: 항상 분사
        if (forceFxAlways)
        {
            IsPouring = true;
            CurrentRateMlPerSec = mlPerSecMax;
            ToggleFx(true, mlPerSecMax);
            _dbg(0f, 1f, mlPerSecMax);
            return;
        }

        // 잔량 체크
        if (!ignoreEmptyCheck && visual && visual.amount <= 0f)
        {
            IsPouring = false;
            CurrentRateMlPerSec = 0f;
            ToggleFx(false);
            _dbg(0f, 0f, 0f);
            return;
        }

        // 세움=0° ~ 뒤집힘=180°
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        float startAngle = minAngleToPour;
        if (usePourableAngle && pourable) startAngle = Mathf.Max(startAngle, pourable.pourAngle);
        float endAngle = Mathf.Max(fullRateAngle, startAngle + 1f);
        float t = Mathf.Clamp01(Mathf.InverseLerp(startAngle, endAngle, tiltAngle));

        // 입구 감지(옵션)
        bool hasMouth = false;
        BaffledFlask_K target = null;
        if (requireMouthToPour && spoutTip)
        {
            var hits = Physics.OverlapSphere(spoutTip.position, mouthDetectRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var m = hits[i].GetComponent<BaffledFlaskMouth_K>();
                if (m) { hasMouth = true; target = m.flask; break; }
            }
        }

        bool canPour = t > 0f && (!requireMouthToPour || hasMouth);
        IsPouring = canPour;
        CurrentRateMlPerSec = 0f;

        if (!canPour)
        {
            ToggleFx(false);
            _dbg(tiltAngle, t, 0f);
            return; // 임계각 전에는 절대 분사 X
        }

        // 유량 계산 & 반영
        float rateMlPerSec = mlPerSecMax * t;
        CurrentRateMlPerSec = rateMlPerSec;

        float deltaMl = rateMlPerSec * Time.deltaTime;
        if (visual) visual.Add(-deltaMl);
        if (target) target.AddWater(deltaMl);

        // FX
        ToggleFx(true, rateMlPerSec);
        _dbg(tiltAngle, t, rateMlPerSec);
    }

    void ToggleFx(bool on, float rateMlPerSec = 0f)
    {
        if (!fx) return;

        // 주둥이 위치/방향 + 앞으로 살짝
        if (spoutTip)
        {
            Vector3 pos = spoutTip.position + spoutTip.forward * fxForwardOffset;
            fx.transform.SetPositionAndRotation(pos, spoutTip.rotation);
        }

        var main = fx.main;
        main.loop = true;
        main.playOnAwake = false;
        main.prewarm = prewarmFx;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var em = fx.emission;
        em.enabled = true;

        if (!on)
        {
            if (fxDrive == FxDrive.Emission) em.rateOverTime = 0f;
            else fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }

        if (!fx.isPlaying) fx.Play(true);
        fx.Simulate(0f, true, false, true);

        if (fxDrive == FxDrive.Emission)
        {
            em.rateOverTime = Mathf.Max(30f, rateMlPerSec * 10f);
        }
        else // EmitBurst
        {
            float particlesPerMl = particlesPer100ml / 100f;
            float want = rateMlPerSec * particlesPerMl * Time.deltaTime;
            int count = Mathf.CeilToInt(want);
            if (count > 0) fx.Emit(count);
        }
    }

    // ───── Debug overlay ─────
    void _dbg(float angle, float t, float rate)
    {
        if (!debugOverlay) return;
        _dbgAngle = angle; _dbgT = t; _dbgRate = rate; _dbgAmt = visual ? visual.amount : -1f;
    }

    void OnGUI()
    {
        if (!debugOverlay) return;
        GUI.Label(new Rect(10, 10, 900, 24),
            $"angle={_dbgAngle:F1}  t={_dbgT:F2}  rate={_dbgRate:F1} ml/s  pouring={IsPouring}  amount={_dbgAmt:F1}");
    }

    void OnDrawGizmosSelected()
    {
        if (!spoutTip) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(spoutTip.position, mouthDetectRadius);
    }
}
