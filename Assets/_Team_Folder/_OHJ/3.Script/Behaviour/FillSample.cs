using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FillSample : MonoBehaviour
{
    public float filling = 0.1f;
    public float stopPos = 0.5f;
    public Vector3 pos;
    //public Vector3 Startpos;
    public bool isfilling = false;

    [Header("이벤트")]
    [SerializeField] private InputActionReference FillAction;

    private void OnEnable()
    {
        if(FillAction != null)
        {
            FillAction.action.Enable();
            FillAction.action.performed += OnFillPerformed;
        }
    }

    private void OnDisable()
    {
        if(FillAction != null)
        {
            FillAction.action.performed -= OnFillPerformed;
            FillAction.action.Disable();
        }
    }

    private void OnFillPerformed(InputAction.CallbackContext context)
    {
        pos = transform.position;
        StartCoroutine(Fill_co());
    }

    private IEnumerator Fill_co()
    {
        while (true)
        {
            if (isfilling)
            {
                Debug.Log("1");
                pos.y += filling * Time.deltaTime;

                if (pos.y > stopPos)
                {
                    pos.y = stopPos;
                    yield break;
                }
                transform.position = pos;
                Debug.Log("올라왔습니다");
            }

            yield return null;
        }
    }

    public void OriginPos()
    {
        //transform.position = Startpos;
    }
}
