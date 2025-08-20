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

    [Header("연결 요소")]
    [Tooltip("활성화시킬 파티클 시스템")]
    public ParticleSystem pourParticles;

    private bool isPouring = false;
    private FlaskLiquidController_G targetFlask;

    private void Update()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        if (tiltAngle > pourAngleThreshold)
        {
            RaycastHit hit;
            if (Physics.Raycast(pourOrigin.position, Vector3.down, out hit, 0.2f))
            {
                if (hit.collider.CompareTag("FlaskOpening"))
                {
                    targetFlask = hit.collider.GetComponentInParent<FlaskLiquidController_G>();
                    if (targetFlask != null)
                    {
                        StartPouring();
                        return;
                    }
                }
            }
        }

        StopPouring();
    }

    private void StartPouring()
    {
        if (!isPouring)
        {
            isPouring = true;
            pourParticles.Play();

            switch (contentType)
            {
                case PourableType.LB:
                    targetFlask.AddPowder();
                    break;
                case PourableType.Water:
                    targetFlask.AddWater();
                    break;
                case PourableType.Agar:
                    targetFlask.AddWater();
                    break;
                case PourableType.None:
                    // None이면 작동 중지
                    break;
            }
        }
    }

    private void StopPouring()
    {
        if (isPouring)
        {
            isPouring = false;
            pourParticles.Stop();
            targetFlask = null;
        }
    }
}
