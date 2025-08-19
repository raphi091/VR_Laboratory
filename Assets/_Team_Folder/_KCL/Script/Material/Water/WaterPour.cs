using UnityEngine;

public class WaterPour : MonoBehaviour
{
    public Transform pourOrigin;                 // 병 입구
    [SerializeField] private ParticleSystem ps;  // PS_WaterStream

    [Header("Pour Settings")]
    [Range(0, 90)] public float startAngle = 60f;
    public float mlPerSecAtMax = 50f;

    [Header("Hit Test")]
    public float downRayLen = 0.7f;
    public float sphereRadius = 0.045f;
    public LayerMask receiverMask = ~0;

    void Reset() => TryAutoWire();
    void OnValidate() => TryAutoWire();

    void Awake()
    {
        TryAutoWire();
        if (ps)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void TryAutoWire()
    {
        if (!pourOrigin)
        {
            var t = transform.Find("PourOrigin");
            if (t) pourOrigin = t;
        }
        if (!ps && pourOrigin)
            ps = pourOrigin.GetComponentInChildren<ParticleSystem>(true);
    }

    void Update()
    {
        if (!pourOrigin) return;

        // 파티클 위치/방향(아래로)
        if (ps)
        {
            ps.transform.SetPositionAndRotation(
                pourOrigin.position,
                Quaternion.LookRotation(Vector3.down, transform.forward)
            );
        }

        // 기울기 → 0~1
        float angle = Vector3.Angle(transform.up, Vector3.up);
        float t = Mathf.InverseLerp(startAngle, 110f, angle);

        // EmissionModule.enabled를 건드리지 않고 Play/Stop만 사용
        if (ps)
        {
            var emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0f, 600f, t));

            if (t > 0.01f)
            {
                if (!ps.isEmitting) ps.Play();
            }
            else
            {
                if (ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return; // 붓지 않음
            }
        }
        else
        {
            if (t <= 0.01f) return;
        }

        // 실제 주입
        float mlThis = Mathf.Clamp(mlPerSecAtMax * t * Time.deltaTime, 0f, mlPerSecAtMax * 0.05f);

        // 아래로 SphereCast (Trigger 포함)
        var ray = new Ray(pourOrigin.position, Vector3.down);
        if (Physics.SphereCast(ray, sphereRadius, out var hit, downRayLen, receiverMask, QueryTriggerInteraction.Collide))
        {
            var mixed = hit.collider.GetComponentInParent<MixedReceiverFill>();
            if (mixed != null)
            {
                mixed.AddLiquid(mlThis);
                Debug.DrawRay(pourOrigin.position, Vector3.down * hit.distance, Color.cyan);
            }
            else
            {
                Debug.DrawRay(pourOrigin.position, Vector3.down * hit.distance, Color.magenta);
            }
        }
        else
        {
            Debug.DrawRay(pourOrigin.position, Vector3.down * downRayLen, Color.red);
        }
    }
}
