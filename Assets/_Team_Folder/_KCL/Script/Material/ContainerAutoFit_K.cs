using UnityEngine;

public class ContainerAutoFit_K : MonoBehaviour
{
    public CapsuleCollider bottle;            // DIWaterBottle의 콜라이더
    public Transform fillRoot;
    public Transform volumeMesh;
    public Transform surface;
    public ContainerFillVisual_K visual;

    [Range(0.85f, 0.99f)] public float diameterFit = 0.96f; // 벽과 여유
    public float bottomLift = 0.02f;                         // 바닥에서 살짝 띄우기(월드 단위)
    [Range(0.6f, 0.95f)] public float maxHeightFactor = 0.85f;

    void Reset()
    {
        if (!bottle) bottle = GetComponent<CapsuleCollider>();
        if (!visual) visual = GetComponent<ContainerFillVisual_K>();
    }

    void Start()      { Fit(); }
    void OnValidate() { if (Application.isEditor) Fit(); }

    public void Fit()
    {
        if (!bottle || !fillRoot || !volumeMesh || !surface || !visual) return;

        // 0) 로컬 회전 리셋(눕는 현상 방지)
        fillRoot.localRotation  = Quaternion.identity;
        volumeMesh.localRotation = Quaternion.identity;
        surface.localRotation    = Quaternion.identity;

        // 1) FillRoot를 병 바닥(로컬)으로 이동
        //    Capsule 중심이 중간이므로 -height/2가 바닥
        float worldBottomLocalY = -bottle.height * 0.5f + bottomLift;
        fillRoot.localPosition = new Vector3(0f, worldBottomLocalY, 0f);

        // 2) 월드 지름 → 로컬 스케일 변환(부모 스케일 보정이 핵심!)
        Transform parent = volumeMesh.parent ? volumeMesh.parent : transform;
        Vector3 parentLossy = parent.lossyScale;

        float worldDiameter = 2f * bottle.radius * diameterFit;   // 월드 단위 지름
        float sx = worldDiameter / Mathf.Max(0.0001f, parentLossy.x);
        float sz = worldDiameter / Mathf.Max(0.0001f, parentLossy.z);

        var vs = volumeMesh.localScale;
        volumeMesh.localScale = new Vector3(sx, vs.y, sz);

        var ss = surface.localScale;
        surface.localScale = new Vector3(sx, ss.y, sz);

        // 3) 수위 최대 높이(월드 → 로컬 보정)
        float worldMaxH = bottle.height * maxHeightFactor;
        visual.maxHeight = worldMaxH / Mathf.Max(0.0001f, parentLossy.y);

        // 4) 액면을 꼭대기에서 살짝 띄우기(겹침 방지)
        var p = surface.localPosition;
        p.y = volumeMesh.localScale.y + 0.0002f; // 로컬
        surface.localPosition = p;
    }
}
