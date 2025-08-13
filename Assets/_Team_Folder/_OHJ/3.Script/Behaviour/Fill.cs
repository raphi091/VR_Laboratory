using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fill : MonoBehaviour
{
    public float filling = 0.1f;
    public float stopPos = 0.5f;
    public Vector3 pos = Vector3.zero;
    public bool isfilling = false;

    private void Start()
    {
        pos = transform.position;
    }

    private void Update()
    {
        if(isfilling)
        {
            pos.y += filling * Time.deltaTime;

            if(pos.y > stopPos)
            {
                pos.y = stopPos;
            }
            transform.position = pos;
        }
    }
}
