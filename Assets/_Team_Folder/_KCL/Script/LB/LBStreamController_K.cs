using UnityEngine;

public class LBStreamController_K : MonoBehaviour
{
    [Header("Refs")]
    public PourableItem_K pourable;          // 루트(LBContainer)에 붙은 기울기 판정
    public ParticleSystem streamFx;          // 자식 FX 파티클 (노란 가루)
    public float gramsPerSecond = 1.0f;      // 초당 쌓이는 g (연출용 숫자)

    private ParticleSystem.EmissionModule em;
    private BaffledFlask_K targetFlask;      // 담기는 대상 (입구 트리거로 세팅)

    void Reset()
    {
        if (!pourable)  pourable  = GetComponent<PourableItem_K>();
        if (!streamFx)  streamFx  = GetComponentInChildren<ParticleSystem>(true);
    }

    void Awake()
    {
        if (streamFx != null) em = streamFx.emission;
        if (em.rateOverTime.constant > 0f) em.rateOverTime = 0f; // 기본 꺼둠
    }

    void Update()
    {
        bool isPouring = pourable && pourable.ingredient == PourableItem_K.IngredientType.LB
                         && pourable.IsPouring()
                         && targetFlask != null;

        // 파티클 ON/OFF (연출)
        if (streamFx) em.rateOverTime = isPouring ? 500f : 0f;

        // 실제 g 누적
        if (isPouring)
            targetFlask.AddLB(Time.deltaTime * gramsPerSecond);
    }

    // LB 통의 콜라이더가 플라스크 입구 트리거 안으로 들어갔을 때
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth))
            targetFlask = mouth.flask;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth) && mouth.flask == targetFlask)
        {
            targetFlask = null;
            if (streamFx != null) em.rateOverTime = 0f;
        }
    }
}
