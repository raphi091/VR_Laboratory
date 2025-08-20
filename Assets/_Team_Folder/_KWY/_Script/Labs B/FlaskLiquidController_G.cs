using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlaskLiquidController_G : MonoBehaviour, C_ExperimentTool
{
    [Header("Experiment Tool 설정")]
    [SerializeField] private bool isWritable = true;
    [SerializeField] private ToolType toolType = ToolType.Flask;

    [Header("필수 연결 요소")]
    [Tooltip("안쪽 액체 메쉬의 렌더러")]
    public Renderer liquidRenderer;

    [Header("액체 채우기 설정")]
    [Tooltip("액체가 차오르는 속도")]
    public float fillSpeed = 0.5f;

    [Tooltip("붓기를 감지할 Raycast의 최대 거리")]
    public float pourCheckDistance = 0.5f;

    [Header("출렁임(Wobble) 효과 설정")]
    [Tooltip("최대 출렁임의 강도")]
    public float maxWobble = 0.03f;

    [Tooltip("출렁임의 속도")]
    public float wobbleSpeed = 10f;

    [Tooltip("출렁임이 진정되는 속도")]
    public float wobbleRecoverySpeed = 1.5f;

    [Header("붓기 상호작용")]
    [Tooltip("내용물이 쏟아져 나오기 시작하는 각도")]
    public float pourAngleThreshold = 75f;

    [Tooltip("내용물이 나오는 시작점")]
    public Transform pourOrigin;

    [Tooltip("파티클 시스템")]
    public ParticleSystem pourParticles;

    [Tooltip("최대로 쏟아져 나올 때의 초당 파티클 개수")]
    public float maxEmissionRate = 200f;

    private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();
    private Material liquidMaterial;
    private PetriDishController_G currentTargetDish;
    private bool isPouring = false;
    private float currentFillAmount = -1f;
    private float targetFillAmount = -1f;
    private float currentWobbleAmount = 0f;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private float time = 0.5f;

    public bool IsWritable { get => isWritable; set => isWritable = value; }
    public ToolType ToolType { get => toolType; set => toolType = value; }


    private void Start()
    {
        if (liquidRenderer == null)
        {
            Debug.LogError("Liquid Renderer가 할당되지 않았습니다!");
            this.enabled = false;
            return;
        }

        currentFillAmount = -1f;
        targetFillAmount = -1f;
        liquidMaterial = liquidRenderer.material;
        liquidMaterial.SetFloat("_FillAmount", currentFillAmount);
        lastPos = transform.position;
        lastRot = transform.rotation;

        if (pourParticles != null)
        {
            var emission = pourParticles.emission;
            emission.rateOverTime = 0;
        }
    }

    private void Update()
    {
        HandleWobble();
        HandlePouring();
    }

    private void HandleWobble()
    {
        if (currentFillAmount != targetFillAmount)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSpeed);
            liquidMaterial.SetFloat("_FillAmount", currentFillAmount);
        }

        float deltaPos = (transform.position - lastPos).magnitude * 100f;
        float deltaRot = Quaternion.Angle(transform.rotation, lastRot);
        currentWobbleAmount += Mathf.Clamp01(deltaPos + deltaRot);

        time += Time.deltaTime;
        float wobbleX = Mathf.Sin(time * wobbleSpeed) * maxWobble * currentWobbleAmount;
        float wobbleZ = Mathf.Cos(time * wobbleSpeed) * maxWobble * currentWobbleAmount;

        liquidMaterial.SetFloat("_WobbleX", wobbleX);
        liquidMaterial.SetFloat("_WobbleZ", wobbleZ);

        currentWobbleAmount = Mathf.Lerp(currentWobbleAmount, 0, Time.deltaTime * wobbleRecoverySpeed);

        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    private void HandlePouring()
    {
        if (pourParticles == null || liquidDatas.Count == 0) return;

        var emission = pourParticles.emission;
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

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
            C_ExperimentTool targetTool = hit.collider.GetComponent<C_ExperimentTool>();
            if (targetTool != null && targetTool.IsWritable && targetTool.ToolType == ToolType.Tray)
            {
                C_ExperimentDataParser.I.ParseEventArgs = new ParseEventArgs { fromTool = this, toTool = targetTool };
                C_ExperimentDataParser.I.DataParsed.Invoke(C_ExperimentDataParser.I.ParseEventArgs);
                ClearData();
            }
        }
    }

    public void ReceiveContinuousPour(PourableType type)
    {
        float powderPourRate = 0.01f;
        float waterPourRate = 0.1f;

        switch (type)
        {
            case PourableType.LB:
                targetFillAmount += powderPourRate * Time.deltaTime;
                break;
            case PourableType.Agar:
                targetFillAmount += powderPourRate * Time.deltaTime;
                break;
            case PourableType.Water:
                targetFillAmount += waterPourRate * Time.deltaTime;
                break;
        }

        targetFillAmount = Mathf.Clamp(targetFillAmount, -1f, 0f);
    }

    public void AddMaterial(PourableType type)
    {
        switch (type)
        {
            case PourableType.LB:
                targetFillAmount += 0.2f;
                break;
            case PourableType.Agar:
                targetFillAmount += 0.2f;
                break;
            case PourableType.Water:
                targetFillAmount += 0.5f;
                break;
        }

        targetFillAmount = Mathf.Clamp(targetFillAmount, -1f, 0f);
    }

    public void ImportLiquidData(List<LiquidData_L> receivedDatas)
    {
        this.liquidDatas.AddRange(receivedDatas);

        foreach (var data in receivedDatas)
        {
            AddMaterial(data.type);
        }
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        return this.liquidDatas;
    }

    public void ClearData()
    {
        this.liquidDatas.Clear();
        targetFillAmount = -1f;
    }
}
