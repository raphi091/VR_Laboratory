
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class Pipette_O : MonoBehaviour
{
    public Color liquidColor;
    public Color fresnelColor;

    private Material mat;

    [Header("입력 정보")]
    [SerializeField] private InputActionReference AbsorbAction;    // Absorb Event
    [SerializeField] private InputActionReference ReleaseAction;    // Release Event

    [SerializeField] private InputActionReference MixAction;    // Mix Event
    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();
    public bool isEnter = false;

    [Header("Liquid List")]
    public Sample_O sample;
    public Flask_O flask;
    [SerializeField] private LiquidData_L liquidData;    // 피펫에 들어있는 액체

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
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mix"))
        {
            //Debug.Log("TriggerStay");
            isEnter = true;

            // 색깔 변하게 하기
            if (other.TryGetComponent<MeshRenderer>(out var renderer))
            {
                mat = renderer.material;
            }

            // 용액을 플라스크로 방출
            if(other.TryGetComponent<Flask_O>(out var flaskCom))
            {
                flask = flaskCom;
            }

            // 실험 Tool 정보
            parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = other.transform.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
        }

        else if(other.CompareTag("Absorb"))
        {
            isEnter = true;
            sample = other.GetComponent<Sample_O>();

            parseEventArgs.fromTool = other.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = this.transform.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
            Debug.Log("Absorb");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mix") || other.CompareTag("Absorb"))
        {
            isEnter = false;
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

        if(!flask.ispossibleMix)
        {
            Debug.Log("아직 모든 액체가 들어있지 않습니다.");
            return;
        }    

        if (mat != null)
        {
            //프로퍼티 들어있는지 여부
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

        }
            else
            {
                Debug.Log("fresnelcolor 없음");
            }
    }

    // 피펫 빨아들이기
    private void OnAbsorbLiquid(InputAction.CallbackContext context)
    {
        Debug.Log("AbsorbLiquid");
        if(!isEnter || sample == null || sample.liquidData == null)
        {
            return;
        }

        liquidData = sample.liquidData;
        Debug.Log($"{liquidData.name} 흡수");
    }

    // 피펫에서 액체 내뱉기
    private void OnReleaseLiquid(InputAction.CallbackContext context)
    {
        Debug.Log("ReleaseLiquid");
        if (!isEnter || flask == null || liquidData == null)
        {
            return;
        }

        // 하나만 담긴 리스트를 receiveliquid 메소드에 넘긴다.
        flask.ReceiveLiquid(new List<LiquidData_L> {liquidData});
        Debug.Log($"{liquidData.name} 방출");

        liquidData = null;
    }
}
