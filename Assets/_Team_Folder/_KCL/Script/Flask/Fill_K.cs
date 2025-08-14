using UnityEngine;

public class Fill_K : MonoBehaviour
{
    [Header("Amount (ml)")]
    public float capacity = 300f;   // 트레이 최대 용량
    public float amount   = 0f;     // 현재 양
    public float mlPerSec = 60f;    // 채워지는 속도 (ml/s)

    [Header("State (set by others)")]
    public bool isfilling = false;  // LiquidUrpFillSync_K에서 토글

    void Update()
    {
        if (isfilling)
            amount = Mathf.Min(capacity, amount + mlPerSec * Time.deltaTime);
    }
}
