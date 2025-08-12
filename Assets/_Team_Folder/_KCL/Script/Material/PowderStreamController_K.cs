using UnityEngine;

public class PowderStreamController_K : MonoBehaviour
{
    public PourableItem_K source;           // LBContainer에 붙은 PourableItem
    public ParticleSystem streamFx;       // Spout에 있는 ParticleSystem
    public float gramsPerSecond = 2f;     // 초당 LB 그램

    private BaffledFlask_K targetFlask;     // 현재 입구 트리거 안에 있는 플라스크

    void Reset()
    {
        source = GetComponent<PourableItem_K>();
        if (!streamFx) streamFx = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        bool pouring = source && source.ingredient == PourableItem_K.IngredientType.LB && source.IsPouring() && targetFlask;
        var emission = streamFx.emission;
        emission.rateOverTime = pouring ? 600f : 0f;  // 붓는 중일 때만 파티클 발사

        if (pouring)
        {
            targetFlask.AddLB(Time.deltaTime * gramsPerSecond);
        }
    }

    // LBContainer의 콜라이더가 플라스크 입구 트리거에 들어올 때 연결
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth))
            targetFlask = mouth.flask;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out BaffledFlaskMouth_K mouth) && mouth.flask == targetFlask)
            targetFlask = null;
    }
}
