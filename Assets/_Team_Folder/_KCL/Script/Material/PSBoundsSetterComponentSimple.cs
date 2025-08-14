using UnityEngine;

[ExecuteAlways]
public class PSBoundsSetterComponentSimple : MonoBehaviour
{
    public ParticleSystem ps;                         // 비워두면 자동
    public ParticleSystemRenderer psr;                // 비워두면 자동
    public Vector3 center = new Vector3(0f, -0.05f, 0.1f); // 커스텀 바운즈 중심
    public Vector3 size   = new Vector3(0.4f, 0.6f, 0.6f); // 커스텀 바운즈 크기(가로,세로,앞뒤)

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (!ps)  ps  = GetComponent<ParticleSystem>();
        if (!psr) psr = GetComponent<ParticleSystemRenderer>();
        if (!ps || !psr) { Debug.LogError("[PSBounds] FX 오브젝트에 ParticleSystem/Renderer가 없습니다."); return; }

        // 1) 카메라에 안 보이면 시뮬을 멈추지 않도록
        var main = ps.main;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        // 2) 공개 API로 바운즈 직접 세팅 (Use Custom Bounds 토글 없이도 동작)
        psr.localBounds = new Bounds(center, size);

        Debug.Log($"[PSBounds] 적용 완료: center={center}, size={size}");
    }
}
