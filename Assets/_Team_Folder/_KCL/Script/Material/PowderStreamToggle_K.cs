using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PowderStreamToggle_K : MonoBehaviour
{
    public float onRate = 600f; // 붓는 중일 때 분사량
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;
        emission.rateOverTime = 0f; // 기본 꺼둠
    }

    public void SetPouring(bool isPouring)
    {
        emission.rateOverTime = isPouring ? onRate : 0f;
    }
}
