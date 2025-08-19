using UnityEngine;

public class MixedReceiverFill : MonoBehaviour
{
    [Header("Renderer (각각 선택 사항)")]
    public Renderer liquidRenderer;            // 물 볼륨 메쉬의 Renderer
    public Renderer powderRenderer;            // 가루 볼륨 메쉬의 Renderer

    [Header("Shader Fill Property 이름")]
    public string liquidFillProp = "_Fill";
    public string powderFillProp = "_Fill";

    [Header("셰이더 값 범위(머티에서 '_Fill' 슬라이더로 실측)")]
    public float liquidShaderMin = 0f;         // 빈 상태일 때 셰이더 값
    public float liquidShaderMax = 1f;         // 가득일 때 셰이더 값
    public float powderShaderMin = 0f;
    public float powderShaderMax = 1f;

    [Header("용량")]
    public float capacityMl = 300f;            // 물 최대량
    public float capacityG  = 300f;            // 가루 최대량

    [Header("표시 속도(초당 0~1 변화량)")]
    public float fillLerpSpeed = 0.2f;

    [Header("Start Fill (0~1)")]
    [Range(0,1)] public float startLiquid01 = 0f;
    [Range(0,1)] public float startPowder01 = 0f;

    [Tooltip("체크 시 플레이 시작 시 항상 완전 빈 상태로 초기화")]
    public bool forceEmptyOnPlay = true;

    // 내부 상태
    float ml, g;                                // 실제 양
    float visualLiquid01, visualPowder01;       // 보이는 값(0~1)
    Material liquidMat, powderMat;

    void Awake()
    {
        if (liquidRenderer) liquidMat = liquidRenderer.material;
        if (powderRenderer) powderMat = powderRenderer.material;

        // 프로퍼티 자동 보정
        FixProp(liquidMat, ref liquidFillProp);
        FixProp(powderMat, ref powderFillProp);
    }

    void OnEnable()
    {
        if (forceEmptyOnPlay)
        {
            // 시작 시 항상 0으로
            ml = 0f; g = 0f;
            visualLiquid01 = 0f;
            visualPowder01 = 0f;
        }
        else
        {
            // 시작 비율을 사용하려면 forceEmptyOnPlay 끄고 아래 값으로 시작
            ml = Mathf.Clamp01(startLiquid01) * Mathf.Max(0.0001f, capacityMl);
            g  = Mathf.Clamp01(startPowder01)  * Mathf.Max(0.0001f, capacityG );
            visualLiquid01 = capacityMl > 0 ? ml / capacityMl : 0f;
            visualPowder01 = capacityG  > 0 ? g  / capacityG  : 0f;
        }

        ApplyImmediate(); // 셰이더에 즉시 반영
    }

    void Update()
    {
        float targetL = capacityMl > 0 ? Mathf.Clamp01(ml / capacityMl) : 0f;
        float targetP = capacityG  > 0 ? Mathf.Clamp01(g  / capacityG ) : 0f;

        visualLiquid01 = Mathf.MoveTowards(visualLiquid01, targetL, fillLerpSpeed * Time.deltaTime);
        visualPowder01 = Mathf.MoveTowards(visualPowder01, targetP, fillLerpSpeed * Time.deltaTime);

        if (liquidMat && liquidMat.HasProperty(liquidFillProp))
        {
            float v = Mathf.Lerp(liquidShaderMin, liquidShaderMax, visualLiquid01);
            liquidMat.SetFloat(liquidFillProp, v);
        }
        if (powderMat && powderMat.HasProperty(powderFillProp))
        {
            float v = Mathf.Lerp(powderShaderMin, powderShaderMax, visualPowder01);
            powderMat.SetFloat(powderFillProp, v);
        }
    }

    public void AddLiquid(float addMl) { ml = Mathf.Clamp(ml + addMl, 0f, Mathf.Max(0.0001f, capacityMl)); }
    public void AddPowder(float addG)  { g  = Mathf.Clamp(g  + addG,  0f, Mathf.Max(0.0001f, capacityG )); }

    public void RemoveLiquid(float subMl){ ml = Mathf.Clamp(ml - subMl, 0f, Mathf.Max(0.0001f, capacityMl)); }
    public void RemovePowder(float subG) { g  = Mathf.Clamp(g  - subG,  0f, Mathf.Max(0.0001f, capacityG )); }

    void ApplyImmediate()
    {
        if (liquidMat && liquidMat.HasProperty(liquidFillProp))
        {
            float v = Mathf.Lerp(liquidShaderMin, liquidShaderMax, visualLiquid01);
            liquidMat.SetFloat(liquidFillProp, v);
        }
        if (powderMat && powderMat.HasProperty(powderFillProp))
        {
            float v = Mathf.Lerp(powderShaderMin, powderShaderMax, visualPowder01);
            powderMat.SetFloat(powderFillProp, v);
        }
    }

    static void FixProp(Material m, ref string propName)
    {
        if (!m) return;
        if (m.HasProperty(propName)) return;
        if (m.HasProperty("_Fill"))        { propName = "_Fill";        return; }
        if (m.HasProperty("Fill"))         { propName = "Fill";         return; }
        if (m.HasProperty("_ClipHeight"))  { propName = "_ClipHeight";  return; }
    }
}
