using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이벤트 구독자
public class FillEvent : MonoBehaviour
{
    public float filling = 0.1f;
    public float stopPos = 0.5f;
    public Vector3 pos;

    private void Awake()
    {
        pos = transform.position;
    }

    private void StartFilling()
    {
        StopAllCoroutines();    // 중복제거
        StartCoroutine(Fill_co());
    }

    private IEnumerator Fill_co()
    {
        pos.y += filling * Time.deltaTime;
         
        if(pos.y > stopPos)
        {
            pos.y = stopPos;
            yield break;
        }

        transform.position = pos;
        yield return null;
    }
}
