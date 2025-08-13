using UnityEngine;

public class BaffledFlask_K : MonoBehaviour
{
    [Header("Visual (optional)")]
    public PowderPileFX_K lbPowderPile;              // 가루 더미(선택)
    public LiquidAppearanceController_K liquid;    // 액체 색 변경(선택)

    [Header("Amounts (누적)")]
    public float lbGrams;   // g
    public float waterMl;   // mL

    [Header("State")]
    public bool ingredientsIn; // 두 재료가 모두 들어왔는지
    public bool lightlyShaken; // 가볍게 흔들기 완료
    public bool isMixed;       // 최종 섞임 완료(색 변경됨)

    public void AddLB(float grams)
    {
        lbGrams += grams;
        if (lbPowderPile) lbPowderPile.AddAmount(grams);
        UpdateIngredientsFlag();
        TryFinishMix();
    }

    public void AddWater(float ml)
    {
        waterMl += ml;
        UpdateIngredientsFlag();
        TryFinishMix();
    }

    void UpdateIngredientsFlag()
    {
        // 임계치(100mL 등) 없이, 둘 다 '0보다 크면' 재료 투입 완료로 간주
        ingredientsIn = (lbGrams > 0f && waterMl > 0f);
    }

    // Shaker에서 호출: 가볍게 흔들기 끝났을 때
    public void OnLightShakeDone()
    {
        lightlyShaken = true;
        TryFinishMix();
    }

    void TryFinishMix()
    {
        if (isMixed) return;
        if (ingredientsIn && lightlyShaken)
        {
            isMixed = true;
            if (liquid) liquid.SetClearYellow();   // 맑은 노랑
            Debug.Log($"[Flask] 섞임 완료 (LB {lbGrams:0.00} g, Water {waterMl:0.0} mL) → 맑은 노랑");
        }
    }
}
