using UnityEngine;

public class PourReceiverFillSimple : MonoBehaviour
{
    public Renderer liquidRenderer;       // LiquidMesh의 MeshRenderer
    public string fillPropName = "_Fill"; // Shader의 Fill 프로퍼티 이름
    public float capacityMl = 300f;       // 만수 용량

    [Header("보이는 속도(초당 Fill 변화량)")]
    public float fillLerpSpeed = 0.35f;   // 0→1 가는 데 대략 3초 (=0.33)

    float currentMl;      // 실제 양(mL)
    float visualFill01;   // 머티에 넣을 0~1

    Material mat;

    void Awake()
    {
        if (!liquidRenderer) { enabled = false; return; }
        mat = liquidRenderer.material;
        // 프로퍼티 자동 보정
        if (mat && !mat.HasProperty(fillPropName))
        {
            if (mat.HasProperty("_Fill")) fillPropName = "_Fill";
            else if (mat.HasProperty("Fill")) fillPropName = "Fill";
        }
        ApplyFill(0f, true);
    }

    void Update()
    {
        float target = capacityMl > 0 ? Mathf.Clamp01(currentMl / capacityMl) : 0f;
        visualFill01 = Mathf.MoveTowards(visualFill01, target, fillLerpSpeed * Time.deltaTime);
        ApplyFill(visualFill01, false);
    }

    public void AddLiquid(float ml)
    {
        currentMl = Mathf.Clamp(currentMl + ml, 0f, capacityMl);
    }

    // 필요하면 외부에서 빼는 것도 가능
    public void RemoveLiquid(float ml)
    {
        currentMl = Mathf.Clamp(currentMl - ml, 0f, capacityMl);
    }

    void ApplyFill(float v01, bool instant)
    {
        if (!mat || !mat.HasProperty(fillPropName)) return;
        mat.SetFloat(fillPropName, v01);
    }
}
