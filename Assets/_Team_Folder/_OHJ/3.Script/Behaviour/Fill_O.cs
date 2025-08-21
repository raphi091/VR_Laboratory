using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fill_O : MonoBehaviour
{
    public float filling = 0.1f;
    public float stopPos = 0.5f;
    public Vector3 pos;
    public Vector3 Startpos;
    public bool isfilling = false;

    private void Awake()
    {
        Startpos = transform.position;
    }

    private void Start()
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

    //private void Update()
    //{
    //    if(isfilling)
    //    {
    //        pos.y += filling * Time.deltaTime;

    //        if(pos.y > stopPos)
    //        {
    //            pos.y = stopPos;
    //        }
    //        transform.position = pos;
    //    }
    //}

    public void OriginPos()
    {
        transform.position = Startpos;
    }
}
