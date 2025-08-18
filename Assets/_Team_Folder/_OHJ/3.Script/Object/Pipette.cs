using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pipette : MonoBehaviour
{
    public Color liquidColor;
    public Color fresnelColor;

    private Material mat;

    [Header("입력 설정")]
    [SerializeField] private InputActionReference MixAction;    // 섞기 액션
    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();
    public bool isEnter = false;

    private void OnEnable()
    {
        // 이벤트 등록
        if(MixAction != null)
        {
            MixAction.action.Enable();
            MixAction.action.performed += OnChangeColor;
        }
    }

    private void OnDisable()
    {
        if (MixAction != null)
        {
            // 이벤트 해제
            MixAction.action.performed -= OnChangeColor;
            MixAction.action.Disable(); 
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mix"))
        {
            isEnter = true;
            mat = other.GetComponent<MeshRenderer>().material;

            parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = other.transform.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mix"))
            isEnter = false;
    }

    private void OnChangeColor(InputAction.CallbackContext context)
    {
        if(!isEnter)
        {
            return;
        }

        if (mat != null)
        {
            //프로퍼티 존재여부
            if (mat.HasProperty("_LiquidColor"))
            {
                mat.SetColor("_LiquidColor", liquidColor);
                Debug.Log("전체 색 변경 완료");
            }

            else
            {
                Debug.Log("liquidcolor 프로퍼티 없음");
            }

            if (mat.HasProperty("_FresnelColor"))
            {
                mat.SetColor("_FresnelColor", fresnelColor);
                Debug.Log("빛색 변경 완료");
            }

        }
            else
            {
                Debug.Log("변경할 액체가 선택되지 않음");
            }
    }
}
