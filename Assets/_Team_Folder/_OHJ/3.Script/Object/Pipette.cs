
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pipette : MonoBehaviour
{
    public Color liquidColor;
    public Color fresnelColor;

    private Material mat;

    [Header("�Է� ����")]
    [SerializeField] private InputActionReference MixAction;    // ���� �׼�
    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();
    public bool isEnter = false;

    private void OnEnable()
    {
        // �̺�Ʈ ���
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
            // �̺�Ʈ ����
            MixAction.action.performed -= OnChangeColor;
            MixAction.action.Disable(); 
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mix"))
        {
            Debug.Log("TriggerStay");
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
        Debug.Log("OnChangeColor");
        if(!isEnter)
        {
            return;
        }

        if (mat != null)
        {
            //������Ƽ ���翩��
            if (mat.HasProperty("_LiquidColor"))
            {
                mat.SetColor("_LiquidColor", liquidColor);
                Debug.Log("��ü �� ���� �Ϸ�");
            }

            else
            {
                Debug.Log("liquidcolor ������Ƽ ����");
            }

            if (mat.HasProperty("_FresnelColor"))
            {
                mat.SetColor("_FresnelColor", fresnelColor);
                Debug.Log("���� ���� �Ϸ�");
            }

        }
            else
            {
                Debug.Log("������ ��ü�� ���õ��� ����");
            }
    }
}
