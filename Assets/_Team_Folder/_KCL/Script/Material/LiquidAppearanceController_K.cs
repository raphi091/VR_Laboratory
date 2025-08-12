using UnityEngine;

public class LiquidAppearanceController_K : MonoBehaviour
{
    public Renderer liquidRenderer;

    public void SetClearYellow()
    {
        if (liquidRenderer != null)
        {
            liquidRenderer.material.color = new Color(0.95f, 0.88f, 0.35f, 1f); // 맑은 노랑
        }
    }

    public void SetTurbidYellow()
    {
        if (liquidRenderer != null)
        {
            liquidRenderer.material.color = new Color(0.85f, 0.78f, 0.2f, 1f); // 탁한 노랑
        }
    }
}
