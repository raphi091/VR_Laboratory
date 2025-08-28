using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pipette_O : MonoBehaviour
{
    [Header("샘플 염색약")]
    public LiquidData_L DNA_DYE;
    public Color SampleStartColor;    // 시작색
    public Color liquidColor;
    public Color fresnelColor;

    [Header("겔 염색약")]
    public LiquidData_L SYBR_DYE;
    public Color GelStartColor; // 시작색
    public Color gelColor;
    public Color gelfresnelColor;

    public Material mat;

    [Header("입력 정보")]
    [SerializeField] private InputActionReference AbsorbAction;    // Absorb Event
    [SerializeField] private InputActionReference ReleaseAction;    // Release Event


    private InputActionReference MixAction;    // Mix Event
    private InputActionReference FillHoleAction;    // 채우기 -> 파란색으로 물들이기
    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();

    [Header("Liquid List")]
    public Sample_O sample;
    public SampleFlask_O flask; //샘플 플라스크
    public Liquid_O liquid; // 플라스크 속 액체
    // [SerializeField] private LiquidData_L liquidData;    // 피펫에 들어있는 액체
    [SerializeField] private List<LiquidData_L> liquidDatas = new List<LiquidData_L>();    //피펫이 들어있는 액체
    public bool isEnter = false;

    [Header("구멍에 샘플채우기")]
    public GameObject currentHole;
    public bool isAbsorb = false;

    [Header("아웃라인")]
    public Outline targetOutline;  // 타겟아웃라인
    public Outline selfOutline; // 자신 아웃라인
    public Color enterColor;    //용액 추출 가능할 때 아웃라인 색깔
    public Color containColor;  // 피펫에 액체가 들어있을 때 아웃라인 색깔
    private Color originColor = Color.white;    // 원래 색깔 (흰색)

    [Header("시각 UI")]
    public DynamicInfoUI_G infoPanel;

    [Header("입력 설정")]
    public InputActionReference interactionAction;

    [Header("Pippet 움직임")]
    public Transform plunger;
    public float plungerDownLocalY = 0.05f;  // 얼마나 내려가나
    public float plungerUpLocalY = 0.13f;    // 올라갈 때
    private float plungerAnimationDuration = 0.2f;
    private Coroutine runningPlungerAnimation;

    private void Awake()
    {
        selfOutline = GetComponent<Outline>();

        if(selfOutline != null)
        {
            selfOutline.enabled = false;
            selfOutline.OutlineColor = originColor;
        }

        Outline[] outlines = FindObjectsOfType<Outline>();
        foreach(Outline o in outlines)
        {
            o.enabled = false;
        }

  
    }

    private void OnEnable()
    {
        // 눌렀을 때
        interactionAction.action.started += OnInteractionPress;
        interactionAction.action.canceled += OnInteractionRelease;

        // 액체가 피펫으로 이동 실행
        if (AbsorbAction != null)
        {
            AbsorbAction.action.Enable();
            AbsorbAction.action.performed += OnAbsorbLiquid;
        }

        // 액체가 플라스크로 이동 실행
        if (ReleaseAction != null)
        {
            ReleaseAction.action.Enable();
            ReleaseAction.action.performed += OnReleaseLiquid;
        }

        // 섞기 이벤트 실행
        if (MixAction != null)
        {
            MixAction.action.Enable();
            MixAction.action.performed += OnChangeColor;
        }

        // 구멍 채우기
        if (FillHoleAction != null)
        {
            FillHoleAction.action.Enable();
            FillHoleAction.action.performed += FillHoleByDye;
        }
    }

    private void OnDisable()
    {
        // 뗐을 때
        interactionAction.action.started -= OnInteractionPress;
        interactionAction.action.canceled -= OnInteractionRelease;

        // 액체가 피펫으로 이동 이벤트 해제
        if (AbsorbAction != null)
        {
            AbsorbAction.action.performed -= OnAbsorbLiquid;
            AbsorbAction.action.Disable();
        }

        // 액체가 플라스크로 이동 이벤트 해제
        if (ReleaseAction != null)
        {
            ReleaseAction.action.performed -= OnReleaseLiquid;
            ReleaseAction.action.Disable();
        }

        // 섞기 이벤트 해제
        if (MixAction != null)
        {
            MixAction.action.performed -= OnChangeColor;
            MixAction.action.Disable(); 
        }

        // 구멍 채우기 이벤트 해제
        if (FillHoleAction != null)
        {
            FillHoleAction.action.performed -= FillHoleByDye;
            FillHoleAction.action.Disable();
        }
    }

    private void Update()
    {
        if(Keyboard.current.bKey.wasPressedThisFrame)
        {
            OnInteractionPress(new InputAction.CallbackContext());
            OnAbsorbLiquid(new InputAction.CallbackContext());
        }

        if (Keyboard.current.bKey.wasReleasedThisFrame)
        {
            OnInteractionRelease(new InputAction.CallbackContext());
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            OnReleaseLiquid(new InputAction.CallbackContext());
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mix"))
        {
            isEnter = true;
            HandleOutline(other);

            // 색깔 변하게 하기
            //MeshRenderer[] renderers = other.GetComponentsInChildren<MeshRenderer>();
            //foreach (var r in renderers)
            //{
            //    if(r.CompareTag("Liquid"))
            //    {
            //        mat = r.material;
            //        break;
            //    }
            //}

            Liquid_O findliquid = other.GetComponentInChildren<Liquid_O>();
            if(findliquid != null)
            {
                liquid = findliquid;
                mat = liquid.mat;
            }

            else
            {
                Debug.LogError("자식에 liquid_O 컴포넌트가 없습니다");
            }

            // 용액을 플라스크로 방출
            if(other.TryGetComponent<SampleFlask_O>(out var flaskCom))
            {
                flask = flaskCom;

                if(flask.isFillSample)
                {
                    Debug.Log("염색약 채취 가능");
                }
            }
        }

        else if(other.CompareTag("Absorb"))
        {
            isEnter = true;
            HandleOutline(other);

            sample = other.GetComponent<Sample_O>();

            parseEventArgs.fromTool = other.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = this.transform.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
        }

        else if (other.CompareTag("Hole"))
        {
            isEnter = true;
            HandleOutline(other);

            currentHole = other.gameObject;

            if(currentHole == null)
            {
                Debug.LogWarning("currentHole 할당 실패");
            }

            parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = other.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

            Debug.Log($"Hole 닿음: {other.name}");
        }


    }

    private void OnTriggerExit(Collider other)
    {
        isEnter = false;

        if(other.GetComponent<SampleFlask_O>() != null)
        {
            flask = null;
        }

        if(other.CompareTag("Hole"))
        {
            currentHole = null;
        }

        Outline exitOutline = other.GetComponent<Outline>();
        if(exitOutline != null && targetOutline == exitOutline)
        {
            targetOutline.OutlineColor = originColor;
            targetOutline.enabled = false;
            targetOutline = null;
        }
    }

    private void HandleOutline(Collider other)
    {
        Outline o = other.GetComponent<Outline>();

        if(o == null)
        {
            o = other.GetComponentInChildren<Outline>();
        }

        if(o == null)
        {
            targetOutline = null;
            return;
        }    

        // 같은 오브젝트라면 enabled만 다시 켜줄지 확인
        if(targetOutline == o)
        {
            if(!targetOutline.enabled)
            {
                targetOutline.enabled = true;
                targetOutline.OutlineColor = enterColor;
            }
            return;
        }

        // 이전 오브젝트 끄기
        if(targetOutline != null)
        {
            targetOutline.enabled = false;
        }

        // 새로운 외곽선 적용
        if(o != null)
        {
            targetOutline = o;
            targetOutline.OutlineColor = enterColor;
            targetOutline.enabled = true;
        }
    }

    // 색깔 변화
    private void OnChangeColor(InputAction.CallbackContext context)
    {
        Debug.Log("OnChangeColor");
        if(!isEnter)
        {
            return;
        }

        if (!flask.ispossibleMix || !flask.ispossiblePour || flask.Dye == null)
        {
            Debug.Log("아직 모든 액체가 들어있지 않거나 염색약 없음.");
            return;
        }    

        if(liquidDatas.Count== 0)
        {
            Debug.Log("피펫에 liquidDatas가 없습니다. 염료 넣기 실패");
            return;
        }

        if(mat == null)
        {
            Debug.Log("mat가 null입니다. materal를 설정이 안됨");
            return;
        }

        //피펫에 염색 용액이 있다면 용액을 Dye에 넣기
        if(liquidDatas.Contains(DNA_DYE))
        {
            flask.Dye = DNA_DYE;
        }

        else if (liquidDatas.Contains(SYBR_DYE))
        {
            flask.Dye = SYBR_DYE;
        }

        else
        {
            Debug.LogWarning("염색약이 없습니다");
            return;
        }

        // 플라스크에 다른 염색약 나오면 작동이 안되도록 예외처리
        // 겔이 든 플라스크 여부에 따라 색깔 다르게 만들기
        if (flask.isGel) // 겔이 든 플라스크
        {
            //샘플 프로퍼티 들어있는지 여부
            if (mat.HasProperty("_LiquidColor"))
            {
                mat.SetColor("_LiquidColor", gelColor);
                
                Debug.Log("액채 색 변경");
            }

            else
            {
                Debug.Log("liquidcolor가 없음");
            }

            if (mat.HasProperty("_FresnelColor"))
            {
                //mat.SetColor("_FresnelColor", gelfresnelColor);
                liquid.ChangeLiquidColor(gelfresnelColor, 1f);
                Debug.Log("fresnelcolor 변경됨");
            }

            else
            {
                Debug.Log("fresnelcolor 없음");
            }
        }

        //샘플 플라스크
        else
        {
            //샘플 프로퍼티 들어있는지 여부
            if (mat.HasProperty("_LiquidColor"))
            {
                mat.SetColor("_LiquidColor", liquidColor);
                Debug.Log("액채 색 변경");
            }

            else
            {
                Debug.Log("liquidcolor 가 없습니다");
            }

            if (mat.HasProperty("_FresnelColor"))
            {
                mat.SetColor("_FresnelColor", fresnelColor);
                liquid.ChangeLiquidColor(fresnelColor, 1f);
                Debug.Log("fresnelcolor 변경됨");
            }

            else
            {
                Debug.Log("fresnelcolor 없음");
            }
        }

        flask.ispossibleMix = false;
        flask.isFillSample = true;
    }

    // 피펫 빨아들이기
    private void OnAbsorbLiquid(InputAction.CallbackContext context)
    {
        Debug.Log("AbsorbLiquid 호출");
        if(!isEnter || sample == null || sample.liquidData == null)
        {
            Debug.Log("흡수할 수 없습니다");
            return;
        }

        // 겔 플라스크에서 추출 금지
        if(flask != null && flask.isGel)
        {
            Debug.LogError("겔 플라스크에선 흡수할 수 없습니다.");
            liquidDatas.Clear();
            isAbsorb = false;
            return;
        }

        // 염색되지 않은 샘플 플라스크에서 추출 방지
        if(flask != null && !flask.isGel && flask.Dye == null)
        {
            Debug.LogError("염색되지 않은 샘플 플라스크에서 흡수할 수 없습니다");
            return;
        }

        // 자기 자신의 아웃라인
        if (selfOutline != null && !selfOutline.enabled)
        {
            selfOutline.enabled = true;
        }

        if(selfOutline != null)
        {
            selfOutline.OutlineColor = containColor;
        }

        if(!liquidDatas.Contains(sample.liquidData))
        {
            liquidDatas.Add(sample.liquidData);
            UpdateInfoPanel();
            Debug.Log("피펫에 추가");
        }

        else
        {
            Debug.LogWarning($"{sample.liquidData.liquidName}은 이미 피펫에 있습니다");
        }


        // 실험 Tool 정보
        parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
        parseEventArgs.toTool = sample.transform.GetComponent<C_ExperimentTool>();
        C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

        if(flask != null && flask.isFillSample && !flask.isGel)
        {
            isAbsorb = true;
            Debug.Log("파란 염색약 흡수");
             
            //만약 염색했다면 모두 가져오되, 중복으로 가져오지 않도록 하기
            if (flask.Dye != null)
            {
                foreach(var liquid in flask.receiveddLiquids)
                {
                    if(!liquidDatas.Contains(liquid))
                    {
                        liquidDatas.Add(liquid);
                    }
                }
                UpdateInfoPanel();
            }
        }
    }

    // 피펫에서 액체 내뱉기
    private void OnReleaseLiquid(InputAction.CallbackContext context)
    {
        Debug.Log("ReleaseLiquid");
        if (!isEnter || liquidDatas.Count == 0)
        {
            return;
        }

        if(flask == null && sample.CompareTag("Absorb") && !isAbsorb)
        {
            Debug.LogWarning("플라스크가 아닙니다");
            liquidDatas.Clear();
            UpdateInfoPanel();
            return;
        }

        if(sample != null && flask != null && sample.CompareTag("Absorb"))
        {
            if(liquidDatas.Contains(DNA_DYE))
            {
                if(flask.isGel)
                {
                    liquidDatas.Clear();
                    UpdateInfoPanel();
                    Debug.LogError("겔 플라스크입니다.");
                    return;
                }

                if (flask.Dye != null)
                {
                    liquidDatas.Clear();
                    Debug.LogError("이미 염색된 플라스크입니다 제거합니다");
                }

                if (flask.ispossibleMix)
                {
                    flask.ReceiveLiquid(liquidDatas);
                    flask.Dye = DNA_DYE;
                    OnChangeColor(context);
                }
 
                else
                {
                    Debug.Log("아직 모든 샘플이 들어가지 않아 염색이 불가능");
                }
            }

            else if(liquidDatas.Contains(SYBR_DYE))
            {
                if(!flask.isGel)
                {
                    liquidDatas.Clear();
                    UpdateInfoPanel();
                    Debug.LogError("겔 플라스크가 아닙니다!!");
                    return;
                }

                if(flask.Dye != null)
                {
                    liquidDatas.Clear();
                    UpdateInfoPanel();
                    Debug.LogError("이미 염색된 플라스크입니다");
                    return;
                }

                if(flask.ispossiblePour)
                {
                    flask.ReceiveLiquid(liquidDatas);
                    flask.Dye = SYBR_DYE;
                    OnChangeColor(context);
                }

                else
                {
                    Debug.Log("아직 모든 샘플이 들어가지 않아 염색이 불가능");
                }
            }

            else
            {
                // 하나만 담긴 리스트를 receiveliquid 메소드에 넘긴다.
                flask.ReceiveLiquid(liquidDatas);

                if(flask.isAddsuccess)
                {
                    liquid.FillLiquid();
                    Debug.Log($"{liquidDatas}추가");
                }

                else
                {
                    Debug.Log("중복 액체로 채울 수 없습니다.");
                }
            }

            // 실험 Tool 정보
            parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = flask.transform.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

        }

        // 구멍에 샘플 채우기
        // 구멍 오브젝트가 있어야하고, 흡수된 상태일 때 구멍채우기 메소드 호출
        if (currentHole != null && currentHole.CompareTag("Hole") && isAbsorb)
        {
            FillHoleByDye(context);
            Debug.Log("구멍을 채웠습니다");
        }

        if(selfOutline != null)
        {
            selfOutline.enabled = false;
            selfOutline.OutlineColor = originColor;
        }

        liquidDatas.Clear();
        UpdateInfoPanel();
    }

    // 구멍에 샘플 채우기
    private void FillHoleByDye(InputAction.CallbackContext context)
    {
        if(!isEnter || !isAbsorb)
        {
            return;
        }

        if(currentHole == null)
        {
            Debug.Log("currentHole이 null입니다");
            return;
        }

        MeshRenderer holeRender = currentHole.GetComponent<MeshRenderer>();
        if(holeRender != null)
        {
            holeRender.material.color = Color.blue;
            Debug.Log("구멍에 파란색 염색약을 넣었습니다.");
        }

        else
        {
            Debug.Log("Render를 찾을 수 없습니다.");
        }

        isAbsorb = false;
    }

    private void OnInteractionPress(InputAction.CallbackContext context)
    {
        AnimatePlunger(plungerDownLocalY);
    }

    private void OnInteractionRelease(InputAction.CallbackContext context)
    {

        AnimatePlunger(plungerUpLocalY);
    }


    // 파이펫 움직임
    private void AnimatePlunger(float targetY)
    {
        if (runningPlungerAnimation != null)
        {
            StopCoroutine(runningPlungerAnimation);
        }
        runningPlungerAnimation = StartCoroutine(AnimatePlungerRoutine(targetY));
    }

    // 파이펫 애니메이션
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

        infoPanel.UpdateInfo(contentList);
    }
}
