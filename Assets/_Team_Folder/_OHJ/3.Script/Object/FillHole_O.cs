using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


// 또다른 피펫
// 용액 5개 + 파란 염색약 넣은 용액을 가지고 => tag가 Mix
// 구멍에 넣는다 -> 샘플을 파란색으로 만든다. => tag가 Hole
public class FillHole_O : MonoBehaviour
{
    public SampleFlask_O flask;
    //public Renderer render;
    public GameObject currentHole;

    [Header("입력 정보")]
    [SerializeField] private InputActionReference AbsorbDyeAction; // 염색약 빨아들이기
    [SerializeField] private InputActionReference FillHoleAction;   // 채우기 -> 파란색으로 물들이기

    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();

    public bool isEnter = false;
    public bool isAbsorb = false;   //염색약 넣었는가?

    private void OnEnable()
    {
        if(AbsorbDyeAction != null)
        {
            AbsorbDyeAction.action.Enable();
            AbsorbDyeAction.action.performed += OnAbsorbDye;
        }

        if(FillHoleAction != null)
        {
            FillHoleAction.action.Enable();
            FillHoleAction.action.performed += FillHoleByDye;
        }
    }

    private void OnDisable()
    {
        if (AbsorbDyeAction != null)
        {
            AbsorbDyeAction.action.performed -= OnAbsorbDye;
            AbsorbDyeAction.action.Disable();
        }

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
            OnAbsorbDye(new InputAction.CallbackContext());
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            FillHoleByDye(new InputAction.CallbackContext());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Mix"))
        {
            isEnter = true;
            if (other.TryGetComponent<SampleFlask_O>(out var flaskCom))
            {
                flask = flaskCom;
                if(flask.isFillSample)
                {
                    Debug.Log("염색약 채취 가능");
                }

                else
                {
                    Debug.Log("염색약 채취 불가능");
                }
            }

            parseEventArgs.fromTool = other.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = this.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
            Debug.Log($"샘플 닿음: {other.name}");
        }

        if(other.CompareTag("Hole"))
        {
            isEnter = true;
            currentHole = other.gameObject;

            parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
            parseEventArgs.toTool = other.GetComponent<C_ExperimentTool>();
            C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
            Debug.Log($"샘플 닿음: {other.name}");

        }
    }

    private void OnTriggerExit(Collider other)
    {
        isEnter = false;
    }

    private void OnAbsorbDye(InputAction.CallbackContext context)
    { 
        if(!isEnter || !flask.isFillSample)
        {
            return;
        }
        isAbsorb = true;
        Debug.Log("파란 용액 채취완료");
    }

    private void FillHoleByDye(InputAction.CallbackContext context)
    {
        if (!isEnter || !flask.isFillSample || !isAbsorb)
        {
            return;
        }

        MeshRenderer holeRender = currentHole.GetComponent<MeshRenderer>();
        if(holeRender != null)
        {
            holeRender.material.color = Color.blue;
            Debug.Log("구멍에 파란색 염색약 넣었습니다");
        }

        else
        {
            Debug.Log("Render를 찾을 수 없습니다.");
        }

        isAbsorb = false;
        currentHole = null;
    }
}
