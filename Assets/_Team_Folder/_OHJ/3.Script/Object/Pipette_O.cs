using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pipette_O : MonoBehaviour
{
    [Header("샘플 염색약")]
    public LiquidData_L DNA_DYE;
    public Color liquidColor;
    public Color fresnelColor;

    [Header("겔 염색약")]
    public LiquidData_L SYBR_DYE;
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
    [SerializeField] private LiquidData_L liquidData;    // 피펫에 들어있는 액체
    public bool isEnter = false;

    [Header("구멍에 샘플채우기")]
    public GameObject currentHole;
    public bool isAbsorb = false;

    [Header("아웃라인")]
    [SerializeField] private Outline outline;

    [Header("파티클")]
    [SerializeField] private GameObject ParticleObj;
    [SerializeField] private ParticleSystem particle;

    private void Awake()
    {
        Outline[] outlines = FindObjectsOfType<Outline>();
        foreach(Outline o in outlines)
        {
            o.enabled = false;
        }
    }

    private void OnEnable()
    {
        // 액체가 피펫으로 이동 실행
        if(AbsorbAction != null)
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
            OnAbsorbLiquid(new InputAction.CallbackContext());
        }

        if(Keyboard.current.nKey.wasPressedThisFrame)
        {
            OnReleaseLiquid(new InputAction.CallbackContext());
        }
    }

    public Outline currentOutline;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mix"))
        {
            isEnter = true;
            HandleOutline(other);

            // 색깔 변하게 하기
            MeshRenderer[] renderers = other.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if(r.CompareTag("Liquid"))
                {
                    mat = r.material;
                    break;
                }
            }

            Liquid_O findliquid = other.GetComponentInChildren<Liquid_O>();
            if(findliquid != null)
            {
                liquid = findliquid;
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

        if(currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }

    private void HandleOutline(Collider other)
    {
        // 기존 외곽선 비활성화
        if(currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }

        // 새로운 외곽선
        Outline o = other.GetComponent<Outline>();
        if(o != null)
        {
            currentOutline = o;
            currentOutline.enabled = true;
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

        if(liquidData == null)
        {
            Debug.Log("피펫에 liquidData가 없습니다. 염료 넣기 실패");
            return;
        }

        if(mat == null)
        {
            Debug.Log("mat가 null입니다. materal를 설정이 안됨");
            return;
        }

        //피펫에 있는 용액을 Dye에 넣기
        flask.Dye = liquidData;

        // 플라스크에 다른 염색약 나오면 작동이 안되도록 예외처리
        // 겔이 든 플라스크 여부에 따라 색깔 다르게 만들기
        if(flask.isGel) // 겔이 든 플라스크
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
                mat.SetColor("_FresnelColor", gelfresnelColor);
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

        if(flask != null && flask.isGel)
        {
            Debug.LogError("겔 플라스크에선 흡수할 수 없습니다.");
            liquidData = null;
            isAbsorb = false;
            return;
        }

        if(!ParticleObj.activeInHierarchy)
        {
            ParticleObj.SetActive(true);
        }

        if(!particle.isPlaying)
        {
            particle.Play();
        }

        liquidData = sample.liquidData;

        // 실험 Tool 정보
        parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
        parseEventArgs.toTool = sample.transform.GetComponent<C_ExperimentTool>();
        C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

        if(flask != null && flask.isFillSample && !flask.isGel)
        {
            isAbsorb = true;
            Debug.Log("파란 염색약 흡수");
             
            //만약 염색했다면 무조건 염색 용액이 나오도록
            if (flask.Dye != null)
            {
                liquidData = DNA_DYE;
            }
        }


    }

    // 피펫에서 액체 내뱉기
    private void OnReleaseLiquid(InputAction.CallbackContext context)
    {
        Debug.Log("ReleaseLiquid");
        if (!isEnter || liquidData == null)
        {
            return;
        }

        if(sample != null && flask != null && sample.CompareTag("Absorb"))
        {
            if(liquidData == DNA_DYE)
            {
                if(flask.isGel)
                {
                    Debug.LogError("겔 플라스크입니다.");
                    return;
                }

                if (flask.Dye != null)
                {
                    Debug.LogError("이미 염색된 플라스크입니다");
                    return;
                }

                if (flask.ispossibleMix)
                {
                    flask.Dye = liquidData;
                    OnChangeColor(context);
                }
 
                else
                {
                    Debug.Log("아직 모든 샘플이 들어가지 않아 염색이 불가능");
                }
            }

            else if(liquidData == SYBR_DYE)
            {
                if(!flask.isGel)
                {
                    Debug.LogError("겔 플라스크가 아닙니다!!");
                    return;
                }

                if(flask.Dye != null)
                {
                    Debug.LogError("이미 염색된 플라스크입니다");
                    return;
                }

                if(flask.ispossiblePour)
                {
                    flask.Dye = liquidData;
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
                flask.ReceiveLiquid(new List<LiquidData_L> {liquidData});

                if(flask.isAddsuccess)
                {
                    

                    liquid.FillLiquid();
                    Debug.Log($"{liquidData.name}추가");
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

        if (ParticleObj.activeInHierarchy)
        {
            ParticleObj.SetActive(false);
        }
        particle.Stop();
        liquidData = null;
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
}
