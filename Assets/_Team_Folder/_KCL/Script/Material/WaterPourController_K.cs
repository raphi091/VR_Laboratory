using UnityEngine;

[RequireComponent(typeof(PourableItem_K))]
public class WaterPourController_K : MonoBehaviour
{
    [Header("Refs")]
    public PourableItem_K pourable;               // 기울기 판정(이미 쓰던 스크립트)
    public ContainerFillVisual_K visual;          // 병 안 'amount' 줄이기
    public Transform spoutTip;                    // SpoutPivot/FX 위치
    public ParticleSystem fx;                     // 물 파티클

    [Header("Rates")]
    public float mlPerSecMax = 80f;               // 최대 붓기 속도(각도 최대로)
    public float minAngleToPour = 60f;            // 시작되는 각도(기본 pourAngle과 동일)
    public float fullRateAngle  = 140f;           // 이 각도면 최대속도

    [Header("Mouth detection")]
    public float mouthDetectRadius = 0.035f;      // 주둥이 주변 탐지 반경
    public bool requireMouthToPour = false;       // 입구에 대야만 쏟게 할지

    ParticleSystem.EmissionModule em;

    void Awake()
    {
        if (!pourable) pourable = GetComponent<PourableItem_K>();
        if (!visual)   visual   = GetComponent<ContainerFillVisual_K>();
        if (!spoutTip) spoutTip = transform.Find("SpoutPivot/FX") ?? transform;

        if (fx)
        {
            em = fx.emission;
            em.rateOverTime = 0f;
        }
    }

    void Update()
    {
        // 빈 병이면 종료
        if (visual && visual.amount <= 0f) { ToggleFx(false); return; }

        // 각도→세기
        float angle = Vector3.Angle(Vector3.down, transform.up);
        float t = Mathf.InverseLerp(Mathf.Max(minAngleToPour, pourable ? pourable.pourAngle : minAngleToPour),
                                    fullRateAngle, angle);
        t = Mathf.Clamp01(t);

        // 플라스크 입 주변 탐지
        BaffledFlask_K target = null;
        bool hasMouth = false;
        if (spoutTip)
        {
            var hits = Physics.OverlapSphere(spoutTip.position, mouthDetectRadius, 
                                             ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var mouth = hits[i].GetComponent<BaffledFlaskMouth_K>();
                if (mouth) { target = mouth.flask; hasMouth = true; break; }
            }
        }

        bool canPour = t > 0f && (!requireMouthToPour || hasMouth);
        if (!canPour) { ToggleFx(false); return; }

        // 이번 프레임 붓는 양
        float rate  = mlPerSecMax * t;
        float delta = rate * Time.deltaTime;

        // 병 수위 감소
        if (visual) visual.Add(-delta);

        // 플라스크에 더해주기(입구에 대면)
        if (target) target.AddWater(delta);

        // 파티클 표시
        ToggleFx(true, rate);
    }

    void ToggleFx(bool on, float rate = 0f)
    {
        if (!fx) return;
        var e = fx.emission;
        e.rateOverTime = on ? rate * 10f : 0f; // 보기 좋게 강도 매핑(취향껏)
        if (on && !fx.isPlaying) fx.Play();
        if (!on && fx.isPlaying) fx.Stop();
    }
}
