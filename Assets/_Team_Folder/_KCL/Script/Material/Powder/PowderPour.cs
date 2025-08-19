using UnityEngine;

public class PowderPour : MonoBehaviour
{
    [Header("Refs")]
    public Transform pourOrigin;      // 통 입구
    public ParticleSystem ps;         // 시각용 파티클

    [Header("Pour Settings")]
    [Range(0, 90)] public float startAngle = 70f;  // 이 각도↑에서만 쏟음
    public float gPerSecAtMax = 20f;              // 최대 기울기일 때 초당 g
    public float downRayLen = 0.7f;                // 아래로 레이 길이
    public float sphereRadius = 0.045f;            // 입구 반지름 정도
    public LayerMask receiverMask = ~0;            // Receiver 레이어만 체크 추천

    // ParticleSystem.EmissionModule em; // 이 변수는 더 이상 필요 없어요.

    void Awake()
    {
        if (ps)
        {
            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Awake에서 모듈 변수를 저장하는 대신,
            // ps.emission.enabled = false;
            // ps.emission.rateOverTime = 0;
            // 와 같이 직접 사용하거나, Update에서 가져와서 사용하도록 합니다.
            var em = ps.emission;
            em.enabled = false;
            em.rateOverTime = 0;
        }
    }

    void Update()
    {
        if (!pourOrigin || !ps) return;

        // 파티클은 입구 위치에서 '아래'로
        ps.transform.SetPositionAndRotation(
            pourOrigin.position,
            Quaternion.LookRotation(Vector3.down, transform.forward)
        );

        // 기울기 → 0~1
        float angle = Vector3.Angle(transform.up, Vector3.up);
        float t = Mathf.InverseLerp(startAngle, 110f, angle);

        // WaterPour 스크립트처럼 매 프레임 모듈을 새로 가져옵니다.
        var emission = ps.emission;
        
        bool pouring = t > 0.01f;
        emission.enabled = pouring;
        if (!pouring) return;

        emission.rateOverTime = Mathf.Lerp(120f, 600f, t);

        // 이번 프레임 들어갈 g
        float gThis = Mathf.Clamp(gPerSecAtMax * t * Time.deltaTime, 0f, gPerSecAtMax * 0.05f);

        // ★ 아래로 SphereCast (Trigger 포함)
        Ray ray = new Ray(pourOrigin.position, Vector3.down);
        if (Physics.SphereCast(ray, sphereRadius, out var hit, downRayLen, receiverMask, QueryTriggerInteraction.Collide))
        {
            var mixed = hit.collider.GetComponentInParent<MixedReceiverFill>();
            if (mixed != null)
            {
                mixed.AddPowder(gThis);
                Debug.DrawRay(pourOrigin.position, Vector3.down * hit.distance, Color.yellow);
                return;
            }
        }

        // 디버그 라인
        Debug.DrawRay(pourOrigin.position, Vector3.down * downRayLen, Color.red);
    }
}