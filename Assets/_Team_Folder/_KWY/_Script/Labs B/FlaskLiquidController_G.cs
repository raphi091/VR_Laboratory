using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("액체 색상 설정")]
    [Tooltip("기본 색상")]
    public Color baseColor = new Color(0.85f, 1f, 1f);

    [Tooltip("액체 배지 색상")]
    public Color clearLiquidColor = Color.yellow;

    [Tooltip("고체 배지 색상")]
    public Color agarMixColor = new Color(1f, 1f, 0.8f);

    [Tooltip("멸균 후 색상")]
    public Color cloudyLiquidColor = new Color(0.8f, 0.8f, 0.2f);

    [Tooltip("색이 변화하는데 걸리는 시간")]
    public float colorChangeDuration = 1.5f;

    [Header("섞기(Mixing) 설정")]
    [Tooltip("흔들기 강도")]
    public float shakeThreshold = 0.5f;

    [Tooltip("흔들어야 하는 최소 시간(초)")]
    public float requiredShakeDuration = 2f;

    [Header("지속적인 붓기 설정")]
    [Tooltip("가루가 채워지는 속도 (초당)")]
    public float powderFillRate = 0.4f;

    [Tooltip("물이 채워지는 속도 (초당)")]
    public float waterFillRate = 1.0f;

    [Tooltip("붓기를 감지할 Raycast의 최대 거리")]
    public float pourCheckDistance = 0.5f;

    [Tooltip("내용물을 밖으로 부을 때 비워지는 속도 (초당)")]
    public float pourOutRate = 1.0f;

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

    [Header("내부 파티클 효과")]
    [Tooltip("섞이지 않은 LB 가루 파티클")]
    public ParticleSystem unmixedLBParticles;

    [Tooltip("섞이지 않은 Agar 가루 파티클")]
    public ParticleSystem unmixedAgarParticles;

    [Header("은박지 설정")]
    [Tooltip("은박지 모델링")]
    public GameObject foilVisual;
    public bool IsFoiled = false;

    [Header("시각 UI")]
    public DynamicInfoUI_G infoPanel;

    private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();
    private Material liquidMaterial;
    private PetriDishController_G currentTargetDish;
    private bool isMixed = false;
    private bool isPouring = false;
    private bool isInCleanBench = false;
    private bool isHeld = false;
    private float currentFillAmount = -1f;
    private float targetFillAmount = -1f;
    private float currentWobbleAmount = 0f;
    private float time = 0.5f;
    private float timeShaking = 0f;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private Coroutine runningColorChange;

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
        liquidMaterial.SetFloat("_Fill", currentFillAmount);
        lastPos = transform.position;
        lastRot = transform.rotation;

        if (pourParticles != null)
        {
            var emission = pourParticles.emission;
            emission.rateOverTime = 0;
        }

        if (foilVisual != null) 
            foilVisual.SetActive(false);

        ClearData();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CleanBenchTrigger_G>() != null)
        {
            isInCleanBench = true;
            Debug.Log(gameObject.name + "이(가) 클린벤치에 들어왔습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CleanBenchTrigger_G>() != null)
        {
            isInCleanBench = false;
            Debug.Log(gameObject.name + "이(가) 클린벤치에서 나갔습니다.");
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
            liquidMaterial.SetFloat("_Fill", currentFillAmount);
        }

        float deltaPos = (transform.position - lastPos).magnitude * 100f;
        float deltaRot = Quaternion.Angle(transform.rotation, lastRot);
        currentWobbleAmount += Mathf.Clamp01(deltaPos + deltaRot);

        time += Time.deltaTime;
        float wobbleX = Mathf.Sin(time * wobbleSpeed) * maxWobble * currentWobbleAmount;
        float wobbleZ = Mathf.Cos(time * wobbleSpeed) * maxWobble * currentWobbleAmount;

        liquidMaterial.SetFloat("_WobbleX", wobbleX);
        liquidMaterial.SetFloat("_WobbleZ", wobbleZ);

        if (!isMixed && isHeld)
        {
            if (currentWobbleAmount > shakeThreshold)
            {
                timeShaking += Time.deltaTime;
            }
            else
            {
                timeShaking = 0f;
            }

            if (timeShaking >= requiredShakeDuration)
            {
                isMixed = true;
                Debug.Log("플라스크를 흔들어서 내용물이 섞였습니다!");

                if (unmixedLBParticles != null) 
                    unmixedLBParticles.Stop();

                if (unmixedAgarParticles != null) 
                    unmixedAgarParticles.Stop();

                UpdateLiquidColor();
            }
        }
        else
        {
            timeShaking = 0f;
        }

        currentWobbleAmount = Mathf.Lerp(currentWobbleAmount, 0, Time.deltaTime * wobbleRecoverySpeed);

        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    private void HandlePouring()
    {
        if (pourParticles == null || liquidDatas.Count == 0)
        {
            StopPouring();
            return;
        }

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);

        if (tiltAngle > pourAngleThreshold)
        {
            if (!isPouring)
            {
                isPouring = true;
                pourParticles.Play();
            }

            var emission = pourParticles.emission;
            float tiltProgress = Mathf.InverseLerp(pourAngleThreshold, 180f, tiltAngle);
            emission.rateOverTime = Mathf.Lerp(0, maxEmissionRate, tiltProgress);

            HandleDataTransfer();
        }
        else
        {
            StopPouring();
        }
    }

    private void StopPouring()
    {
        if (isPouring)
        {
            isPouring = false;
            pourParticles.Stop();
        }
    }

    private void HandleDataTransfer()
    {
        RaycastHit hit;

        if (Physics.Raycast(pourOrigin.position, Vector3.down, out hit, pourCheckDistance))
        {
            C_ExperimentTool targetTool = hit.collider.GetComponent<C_ExperimentTool>();
            PetriDishController_G dish = targetTool as PetriDishController_G;

            if (dish != null && dish.currentState == PetriDishController_G.DishState.Empty)
            {
                C_ExperimentDataParser.I.ParseEventArgs = new ParseEventArgs { fromTool = this, toTool = targetTool };
                C_ExperimentDataParser.I.DataParsed.Invoke(C_ExperimentDataParser.I.ParseEventArgs);
            }

            targetFillAmount -= pourOutRate * Time.deltaTime;
            targetFillAmount = Mathf.Clamp(targetFillAmount, -1f, 0f);

            if (targetFillAmount <= -0.99f)
            {
                ClearData();
            }
        }
    }

    public void ReceiveContinuousPour(LiquidData_L receivedData)
    {
        if (!liquidDatas.Contains(receivedData))
        {
            liquidDatas.Add(receivedData);
            isMixed = false;
        }

        switch (receivedData.type)
        {
            case PourableType.LB:
                if (unmixedLBParticles != null && !unmixedLBParticles.isPlaying)
                    unmixedLBParticles.Play();
                break;
            case PourableType.Agar:
                if (unmixedAgarParticles != null && !unmixedAgarParticles.isPlaying)
                    unmixedAgarParticles.Play();
                break;
            case PourableType.Water:
                targetFillAmount += waterFillRate * Time.deltaTime;
                break;
        }

        targetFillAmount = Mathf.Clamp(targetFillAmount, -1f, 0f);

        UpdateInfoPanel();
    }

    public void AddMaterial(PourableType type)
    {
        switch (type)
        {
            case PourableType.LB:
                targetFillAmount += 0.05f;
                break;
            case PourableType.Agar:
                targetFillAmount += 0.05f;
                break;
            case PourableType.Water:
                targetFillAmount += 0.2f;
                break;
        }

        targetFillAmount = Mathf.Clamp(targetFillAmount, -1f, 0f);
    }

    public void ImportLiquidData(List<LiquidData_L> receivedDatas)
    {
        if (!isInCleanBench)
        {
            UIManager_G.Instance.ShowWarningMessage("멸균 작업은 클린벤치 안에서 진행해주세요.");
            return;
        }

        if (receivedDatas != null && receivedDatas.Count == 1)
        {
            LiquidData_L singleData = receivedDatas[0];

            if (singleData.type == PourableType.Microbe)
            {
                // 데이터 리스트에 미생물 추가
                this.liquidDatas.Add(singleData);
                Debug.Log(singleData.liquidName + " 미생물이 플라스크에 접종되었습니다.");
            }
            else
            {
                Debug.LogWarning("파이펫으로는 미생물만 넣을 수 있습니다. (" + singleData.liquidName + " 넣기 시도)");
            }
        }

        UpdateInfoPanel();
    }

    private void UpdateLiquidColor()
    {
        if (liquidMaterial == null) return;

            Debug.Log(1);
        var containedTypes = liquidDatas.Select(data => data.type).ToList();

        if (containedTypes.Contains(PourableType.Agar) &&
            containedTypes.Contains(PourableType.LB) &&
            containedTypes.Contains(PourableType.Water))
        {
            Debug.Log(2);
            StartColorChange(agarMixColor);

            if (foilVisual != null)
            {
                IsFoiled = true;
                foilVisual.SetActive(true);
                Debug.Log("은박지가 씌워졌습니다.");
            }
        }
        else if (containedTypes.Contains(PourableType.LB) &&
                 containedTypes.Contains(PourableType.Water))
        {
            Debug.Log(3);
            StartColorChange(clearLiquidColor);

            if (foilVisual != null)
            {
                IsFoiled = true;
                foilVisual.SetActive(true);
                Debug.Log("은박지가 씌워졌습니다.");
            }

        }
    }

    public void SetStateToCloudy()
    {
        if (liquidMaterial != null)
        {
            StartColorChange(cloudyLiquidColor);
        }
    }

    private void StartColorChange(Color targetColor)
    {
        if (runningColorChange != null)
        {
            StopCoroutine(runningColorChange);
        }

        runningColorChange = StartCoroutine(ChangeColorRoutine(targetColor));
    }

    public void EndAutoclave()
    {
        if (foilVisual != null)
        {
            IsFoiled = false;
            foilVisual.SetActive(false);
            Debug.Log("Autoclave 기계 사용 끝");
        }
    }

    private IEnumerator ChangeColorRoutine(Color targetColor)
    {
        float elapsedTime = 0f;
        Color startLiquidColor = liquidMaterial.GetColor("_LiquidColor");
        Color startFresnelColor = liquidMaterial.GetColor("_FresnelColor");

        Color targetFresnelColor = new Color(targetColor.r - 0.05f, targetColor.g - 0.05f, targetColor.b);

        while (elapsedTime < colorChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / colorChangeDuration;

            liquidMaterial.SetColor("_LiquidColor", Color.Lerp(startLiquidColor, targetColor, t));
            liquidMaterial.SetColor("_FresnelColor", Color.Lerp(startFresnelColor, targetFresnelColor, t));

            yield return null;
        }

        liquidMaterial.SetColor("_LiquidColor", targetColor);
        liquidMaterial.SetColor("_FresnelColor", targetFresnelColor);
        runningColorChange = null;
    }

    public void RemoveFoil()
    {
        if (foilVisual != null)
        {
            IsFoiled = false;
            foilVisual.SetActive(false);
            Debug.Log("은박지가 제거되었습니다.");
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
        isMixed = false;
        timeShaking = 0f;
        IsFoiled = false;

        if (foilVisual != null) 
            foilVisual.SetActive(false);

        if (unmixedLBParticles != null) 
            unmixedLBParticles.Stop();

        if (unmixedAgarParticles != null) 
            unmixedAgarParticles.Stop();

        Color _baseColor = new Color(baseColor.r - 0.05f, baseColor.g - 0.05f, baseColor.b);
        liquidMaterial.SetColor("_LiquidColor", baseColor);
        liquidMaterial.SetColor("_FresnelColor", _baseColor);

        UpdateInfoPanel();
    }

    public void OnGrab()
    {
        isHeld = true;

        if (infoPanel != null)
            infoPanel.gameObject.SetActive(true);
    }

    public void OnRelease()
    {
        isHeld = false;

        if (infoPanel != null)
            infoPanel.gameObject.SetActive(false);
    }

    private void UpdateInfoPanel()
    {
        if (infoPanel == null) return;
        string description;
        if (liquidDatas != null && liquidDatas.Count > 0)
        {
            string contentsName = string.Join(", ", liquidDatas.Select(data => data.liquidName));
            description = "내용물: " + contentsName;
        }
        else
        {
            description = "내용물: 없음";
        }
        infoPanel.UpdateContent(description);
    }
}
