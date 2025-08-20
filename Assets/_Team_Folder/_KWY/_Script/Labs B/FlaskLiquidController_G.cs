using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlaskLiquidController_G : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("안쪽 액체 메쉬의 렌더러")]
    public Renderer liquidRenderer;

    [Header("액체 채우기 설정")]
    [Tooltip("액체가 차오르는 속도")]
    public float fillSpeed = 0.5f;

    [Header("출렁임(Wobble) 효과 설정")]
    [Tooltip("최대 출렁임의 강도")]
    public float maxWobble = 0.03f;

    [Tooltip("출렁임의 속도")]
    public float wobbleSpeed = 10f;

    [Tooltip("출렁임이 진정되는 속도")]
    public float wobbleRecoverySpeed = 1.5f;

    private Material liquidMaterial;
    private float currentFillAmount = -1f;
    private float targetFillAmount = -1f;
    private float currentWobbleAmount = 0f;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private float time = 0.5f;

    void Start()
    {
        // 렌더러가 할당되지 않았으면 오류 메시지 출력 후 비활성화
        if (liquidRenderer == null)
        {
            Debug.LogError("Liquid Renderer가 할당되지 않았습니다!");
            this.enabled = false;
            return;
        }

        // 원본 머티리얼이 아닌, 이 오브젝트만 사용하는 복제본(인스턴스)을 만듭니다.
        liquidMaterial = liquidRenderer.material;

        // 시작 시 액체가 비어있도록 설정
        liquidMaterial.SetFloat("_FillAmount", currentFillAmount);

        // 초기 위치/회전 값 저장
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    void Update()
    {
        // 1. 액체 채우기 처리
        // 목표치(targetFillAmount)를 향해 현재 양(currentFillAmount)을 부드럽게 변경
        if (currentFillAmount != targetFillAmount)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
            liquidMaterial.SetFloat("_FillAmount", currentFillAmount);
        }

        // 2. 출렁임 강도 계산
        // 오브젝트의 이동 속도와 회전 속도를 기반으로 출렁임의 강도를 계산
        float deltaPos = (transform.position - lastPos).magnitude * 100f;
        float deltaRot = Quaternion.Angle(transform.rotation, lastRot);
        currentWobbleAmount += Mathf.Clamp01(deltaPos + deltaRot);

        // 3. 출렁임 효과 적용
        time += Time.deltaTime;

        // 사인, 코사인 함수를 이용해 시간에 따라 자연스럽게 출렁이는 값 생성
        float wobbleX = Mathf.Sin(time * wobbleSpeed) * maxWobble * currentWobbleAmount;
        float wobbleZ = Mathf.Cos(time * wobbleSpeed) * maxWobble * currentWobbleAmount;

        // 셰이더의 _WobbleX, _WobbleZ 파라미터에 계산된 값 전달
        liquidMaterial.SetFloat("_WobbleX", wobbleX);
        liquidMaterial.SetFloat("_WobbleZ", wobbleZ);

        // 4. 출렁임 진정 처리
        // 시간이 지나면서 출렁임이 자연스럽게 잦아들도록 함
        currentWobbleAmount = Mathf.Lerp(currentWobbleAmount, 0, Time.deltaTime * wobbleRecoverySpeed);

        // 다음 프레임에서의 계산을 위해 현재 위치/회전 값 저장
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    public void AddPowder()
    {
        targetFillAmount += 0.2f;
    }

    public void AddWater()
    {
        targetFillAmount += 1.0f;
    }
}
