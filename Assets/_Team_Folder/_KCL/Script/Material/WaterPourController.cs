using UnityEngine;

public class WaterPourController : MonoBehaviour
{
    public Transform pourOrigin;          // 병 입구
    public ParticleSystem ps;             // PS_WaterStream

    [Header("간단 설정")]
    [Range(0,90)] public float startAngle = 60f;  // 이 각도 이상 기울이면 시작
    public float mlPerSecAtMax = 50f;             // 최대 기울기일 때 초당 mL
    public float rayDistance = 0.5f;              // 입구 아래로 쏠 거리
    public LayerMask receiverMask = ~0;           // 플라스크 레이어(기본: 전부)

    ParticleSystem.EmissionModule em;

    void Awake()
    {
        if (!ps) return;
        var m = ps.main;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        em = ps.emission;
        em.enabled = false;
        em.rateOverTime = 0;
    }

    void Update()
    {
        if (!pourOrigin || !ps) return;

        // 파티클을 입구 위치에 두고 항상 “아래”로 쏘기
        ps.transform.SetPositionAndRotation(
            pourOrigin.position,
            Quaternion.LookRotation(Vector3.down, transform.forward)
        );

        // 기울기(0~1)
        float angle = Vector3.Angle(transform.up, Vector3.up);
        float t = Mathf.InverseLerp(startAngle, 110f, angle);

        // 파티클 켜고 끄기(시각용)
        bool pouring = t > 0.01f;
        em.enabled = pouring;
        if (pouring && !ps.isEmitting) ps.Play();
        if (!pouring) return;

        em.rateOverTime = 600f * t;

        // 이번 프레임 주입량(mL)
        float mlThisFrame = mlPerSecAtMax * t * Time.deltaTime;

        // 아래로 레이 → 맞은 그릇에 전달
        if (Physics.Raycast(pourOrigin.position, Vector3.down,
                            out var hit, rayDistance, receiverMask,
                            QueryTriggerInteraction.Ignore))
        {
            var r = hit.collider.GetComponentInParent<PourReceiverFillSimple>();
            if (r != null) r.AddLiquid(mlThisFrame);
        }

        Debug.DrawRay(pourOrigin.position, Vector3.down * rayDistance, Color.cyan);
    }
}
