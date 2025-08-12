using UnityEngine;

public class ContainerFillVisual_K : MonoBehaviour
{
    public enum ContentType { Liquid, Powder }
    public ContentType contentType = ContentType.Liquid;

    [Header("Refs")]
    public Transform fillRoot;     // 병 안 바닥에 피벗 둔 빈 오브젝트
    public Transform volumeMesh;   // Cylinder 등: 내용물 몸통
    public Transform surface;      // 윗면(원형): 얇게

    [Header("Capacity/Amount")]
    public float capacity = 200f;  // mL 또는 g (단위는 자유)
    public float amount   = 140f;  // 초기 담긴 양
    public float maxHeight = 0.12f; // volumeMesh가 Y로 커질 최대 높이(씬 스케일에 맞춤)

    [Header("Liquid surface follow gravity")]
    public bool keepSurfaceLevel = true; // 액체: 수평 유지

    float lastShownFill = -1f;

    public float Fill01 => Mathf.Clamp01(capacity > 0 ? amount / capacity : 0f);

    void Reset()
    {
        if (!fillRoot)  fillRoot  = transform;
        if (!volumeMesh || !surface)
        {
            // 자식 이름 관례로 자동 찾기 (선택)
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!volumeMesh && t.name.ToLower().Contains("volume")) volumeMesh = t;
                if (!surface   && t.name.ToLower().Contains("surface")) surface   = t;
            }
        }
    }

    void LateUpdate()
    {
        // 수위/크기 업데이트
        ApplyFillVisual();

        // 액체면 수평 유지
        if (keepSurfaceLevel && surface)
            surface.up = Vector3.up; // 항상 세계의 위쪽을 바라보게
    }

    void ApplyFillVisual()
    {
        if (!volumeMesh) return;

    // 1) 채움 비율
    float fill = Fill01; // 0~1

    // 2) 볼륨 높이(Y) 반영
    var s = volumeMesh.localScale;
    s.y = Mathf.Max(0.001f, fill * maxHeight);
    volumeMesh.localScale = s;

    // 3) 바닥에서 위로만 자라 보이게 위치 보정
    var vp = volumeMesh.localPosition;
    vp.y = s.y * 0.5f;
    volumeMesh.localPosition = vp;

    // 4) 액면(서피스) 위치/표시
    if (surface)
    {
        var p = surface.localPosition;
        p.y = s.y;                 // 볼륨 꼭대기와 동일 높이
        surface.localPosition = p;
        surface.gameObject.SetActive(fill > 0.001f);
    }

    // 5) 마지막 표시값 저장
    lastShownFill = fill;
    }

    // 외부에서 양 증감
    public void Add(float delta)  { amount = Mathf.Clamp(amount + delta, 0f, capacity); }
    public void SetAmount(float v){ amount = Mathf.Clamp(v, 0f, capacity); }
}
