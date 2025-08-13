using UnityEngine;

public class LBPourController_K : MonoBehaviour
{
    public PourableItem_K pourable;               // 통 본체에 붙은 스크립트
    public PowderStreamToggle_K streamToggle;       // FX에 붙은 토글

    void Reset() {
        if (!pourable) pourable = GetComponent<PourableItem_K>();
        if (!streamToggle) streamToggle = GetComponentInChildren<PowderStreamToggle_K>(true);
    }

    void Update() {
        if (!pourable || !streamToggle) return;
        // (최소 버전) 플라스크 조건 없이 '기울이면 분사'
        bool pouring = pourable.IsPouring();
        streamToggle.SetPouring(pouring);
    }
}
