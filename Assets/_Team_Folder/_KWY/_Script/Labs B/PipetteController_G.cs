using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public class PipetteController_G : MonoBehaviour, C_ExperimentTool
{
    [Header("입력 설정")]
    [Tooltip("컨트롤러 버튼")]
    public InputActionReference interactionAction;

    [Header("상태 표시 (플런저)")]
    [Tooltip("움직일 플런저 오브젝트의 Transform")]
    public Transform plunger;

    [Tooltip("플런저가 완전히 눌렸을 때 위치")]
    public float plungerDownLocalY = 0.05f;

    [Tooltip("플런저가 올라와 있을 때 위치")]
    public float plungerUpLocalY = 0.13f;

    [Tooltip("플런저가 움직이는 애니메이션 시간(초)")]
    public float plungerAnimationDuration = 0.2f;

    [Header("시각 UI")]
    public DynamicInfoUI_G infoPanel;

    [Header("아웃라인 설정")]
    [Tooltip("내용물이 없을 때의 아웃라인 색상")]
    public Color emptyOutlineColor = Color.white;
    [Tooltip("내용물이 있을 때의 아웃라인 색상")]
    public Color fullOutlineColor = Color.green;

    private Outline pipetteOutline;

    [SerializeField] private bool isWritable = true;
    [SerializeField] private ToolType toolType = ToolType.Pippet;
    private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();
    private Coroutine runningPlungerAnimation;
    private bool isHeld = false;

    public bool IsWritable { get => isWritable; set => isWritable = value; }
    public ToolType ToolType { get => toolType; set => toolType = value; }

    private C_ExperimentTool currentTarget;


    private void OnEnable()
    {
        interactionAction.action.started += OnInteractionPress;
        interactionAction.action.canceled += OnInteractionRelease;
    }

    private void OnDisable()
    {
        interactionAction.action.started -= OnInteractionPress;
        interactionAction.action.canceled -= OnInteractionRelease;
    }

    private void Start()
    {
        plunger.localPosition = new Vector3(plunger.localPosition.x, plungerUpLocalY, plunger.localPosition.z);

        if (!TryGetComponent(out pipetteOutline))
            Debug.Log("PipetteController ] Outline 없음");

        UpdateInfoPanel();
        UpdateOutline();
    }

    private void OnTriggerEnter(Collider other)
    {
        C_ExperimentTool targetTool = other.GetComponent<C_ExperimentTool>();
        if (targetTool != null)
        {
            currentTarget = targetTool;

            LiquidSource_G source = other.GetComponent<LiquidSource_G>();
            if (source != null)
            {
                if (source.closedModel != null) 
                    source.closedModel.SetActive(false);

                if (source.openModel != null) 
                    source.openModel.SetActive(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        C_ExperimentTool targetTool = other.GetComponent<C_ExperimentTool>();

        if (targetTool == currentTarget)
        {
            LiquidSource_G source = other.GetComponent<LiquidSource_G>();
            if (source != null)
            {
                if (source.closedModel != null) 
                    source.closedModel.SetActive(true);

                if (source.openModel != null) 
                    source.openModel.SetActive(false);
            }

            currentTarget = null;
        }
    }

    private void OnInteractionPress(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        AnimatePlunger(plungerDownLocalY);

        if (currentTarget == null) return;

        if (liquidDatas.Count == 0 && currentTarget.ToolType != ToolType.Pippet)
        {
            ImportLiquidData(currentTarget.ExportLiquidDatas());
            UpdateInfoPanel();
            UpdateOutline();
        }
        else if (liquidDatas.Count > 0 && currentTarget.IsWritable)
        {
            C_ExperimentDataParser.I.ParseEventArgs = new ParseEventArgs { fromTool = this, toTool = currentTarget };
            C_ExperimentDataParser.I.DataParsed.Invoke(C_ExperimentDataParser.I.ParseEventArgs);
            ClearData();
        }
    }

    private void OnInteractionRelease(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        AnimatePlunger(plungerUpLocalY);
    }

    public void ImportLiquidData(List<LiquidData_L> receivedDatas)
    {
        if (receivedDatas == null || receivedDatas.Count == 0) return;

        liquidDatas.AddRange(receivedDatas);
    }

    public List<LiquidData_L> ExportLiquidDatas()
    {
        return liquidDatas;
    }

    public void ClearData()
    {
        liquidDatas.Clear();
        UpdateInfoPanel();
        UpdateOutline();
    }

    private void AnimatePlunger(float targetY)
    {
        if (runningPlungerAnimation != null)
        {
            StopCoroutine(runningPlungerAnimation);
        }
        runningPlungerAnimation = StartCoroutine(AnimatePlungerRoutine(targetY));
    }

    private IEnumerator AnimatePlungerRoutine(float targetY)
    {
        if (plunger == null) yield break;

        float elapsedTime = 0f;
        Vector3 startPosition = plunger.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, targetY, startPosition.z);

        while (elapsedTime < plungerAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            plunger.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / plungerAnimationDuration);
            yield return null;
        }

        plunger.localPosition = targetPosition;
        runningPlungerAnimation = null;
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

        string contentList;
        if (liquidDatas != null && liquidDatas.Count > 0)
        {
            var contentNames = liquidDatas.Select(data => data.liquidName);
            contentList = "- " + string.Join("\n- ", contentNames);
        }
        else
        {
            contentList = "없음";
        }

        infoPanel.UpdateInfo("내용물", contentList);
    }

    private void UpdateOutline()
    {
        if (pipetteOutline == null) return;

        if (liquidDatas.Count > 0)
        {
            pipetteOutline.OutlineColor = fullOutlineColor;
        }
        else
        {
            pipetteOutline.OutlineColor = emptyOutlineColor;
        }
    }
}
