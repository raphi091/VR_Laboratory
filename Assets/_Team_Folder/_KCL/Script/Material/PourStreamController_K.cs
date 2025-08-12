using UnityEngine;

public class PourStreamController_K : MonoBehaviour
{
    [Header("Refs")]
    public PourableItem_K pourable;          // 루트 병(또는 통)에 붙은 스크립트
    public ParticleSystem streamFx;          // 자식 FX 파티클 (SpoutPivot/FX)
    
    // (선택) 병 안 내용물 수위 표시용. 있으면 0일 때 자동 차단됨.
    private ContainerFillVisual_K fill;      // 같은 오브젝트에 붙어있다고 가정(없어도 동작)

    [Header("Rates")]
    public float gramsPerSecond = 1.0f;      // LB 쏟는 속도 (g/s)
    public float mlPerSecond    = 50f;       // 물 쏟는 속도 (mL/s)

    [Header("FX")]
    public float fxRateWhenPouring = 500f;   // 붓는 중일 때 Emission rate
    
    [Header("Options")]
    public bool requireMouthTrigger = true;  // 입구 트리거 안에서만 붓기
    public bool debugLogs = false;

    private BaffledFlask_K targetFlask;      // 현재 붓고 있는 대상 플라스크
    private ParticleSystem.EmissionModule em;
    private bool hasFx;

    void Reset()
    {
        if (!pourable)   pourable = GetComponent<PourableItem_K>();
        if (!streamFx)   streamFx = GetComponentInChildren<ParticleSystem>(true);
    }

    void Awake()
    {
        if (!pourable) pourable = GetComponent<PourableItem_K>();
        if (!streamFx) streamFx = GetComponentInChildren<ParticleSystem>(true);

        hasFx = streamFx != null;
        if (hasFx)
        {
            em = streamFx.emission;
            em.rateOverTime = 0f; // 기본 꺼둠
        }

        fill = GetComponent<ContainerFillVisual_K>(); // 있으면 사용
    }

    void Update()
    {
        // 1) 기본 붓기 조건
        bool insideMouth = !requireMouthTrigger || targetFlask != null;
        bool tilting     = pourable && pourable.IsPouring();
        bool canPour     = insideMouth && tilting;

        // 2) 병 수위가 0이면 강제 차단 (요청하신 부분)
        if (fill != null && fill.amount <= 0.0001f)
            canPour = false;

        // 3) FX On/Off
        if (hasFx)
            em.rateOverTime = canPour ? fxRateWhenPouring : 0f;

        // 4) 실제 양 이동
        if (!canPour || targetFlask == null) return;

        float dt = Time.deltaTime;

        switch (pourable.ingredient)
        {
            case PourableItem_K.IngredientType.LB:
            {
                float g = gramsPerSecond * dt;
                targetFlask.AddLB(g);

                // 병 수위도 감소 (있을 때만)
                if (fill != null) fill.Add(-g);
                if (debugLogs) Debug.Log($"=> LB +{g:0.00} g");
                break;
            }
            case PourableItem_K.IngredientType.Water:
            {
                float ml = mlPerSecond * dt;
                targetFlask.AddWater(ml);

                if (fill != null) fill.Add(-ml);
                if (debugLogs) Debug.Log($"=> Water +{ml:0.0} mL");
                break;
            }
            case PourableItem_K.IngredientType.Agar:
            {
                // 필요시 targetFlask에 AddAgar 구현해서 사용
                break;
            }
        }
    }

    // 입구 트리거에 들어오면 대상 플라스크 기억
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth))
        {
            targetFlask = mouth.flask;
            if (debugLogs) Debug.Log("[Pour] Mouth entered");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth) && mouth.flask == targetFlask)
        {
            targetFlask = null;
            if (hasFx) em.rateOverTime = 0f;
            if (debugLogs) Debug.Log("[Pour] Mouth exited");
        }
    }
}
