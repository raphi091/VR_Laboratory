using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PourableType
{
    None,
    LB,
    Water,
    Agar
}

public class PouringController_G : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("붓는 내용물의 종류")]
    public PourableType contentType = PourableType.None;

    [Tooltip("내용물이 쏟아져 나오기 시작하는 각도")]
    public float pourAngleThreshold = 80f;

    [Tooltip("내용물이 나오는 시작점")]
    public Transform pourOrigin;

    [Tooltip("붓기를 감지할 Raycast의 최대 거리")]
    public float pourCheckDistance = 0.5f;

    [Header("연결 요소")]
    [Tooltip("활성화시킬 파티클 시스템")]
    public ParticleSystem pourParticles;

    [Tooltip("최대 초당 파티클 개수")]
    public float maxEmissionRate = 200f;


    private void Start()
    {
        var emission = pourParticles.emission;
        emission.rateOverTime = 0;
    }

    private void Update()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        var emission = pourParticles.emission;

        if (tiltAngle > pourAngleThreshold)
        {
            float tiltProgress = Mathf.InverseLerp(pourAngleThreshold, 180f, tiltAngle);
            emission.rateOverTime = Mathf.Lerp(0, maxEmissionRate, tiltProgress);
            HandleDataTransfer();
        }
        else
        {
            emission.rateOverTime = 0;
        }
    }

    private void HandleDataTransfer()
    {
        RaycastHit hit;
        if (Physics.Raycast(pourOrigin.position, Vector3.down, out hit, pourCheckDistance))
        {
            Debug.Log("Raycast Hit: " + hit.collider.name);

            FlaskLiquidController_G targetFlask = hit.collider.GetComponentInParent<FlaskLiquidController_G>();
            if (targetFlask != null && targetFlask.IsWritable)
            {
                targetFlask.ReceiveContinuousPour(contentType);
            }
        }
    }
}
