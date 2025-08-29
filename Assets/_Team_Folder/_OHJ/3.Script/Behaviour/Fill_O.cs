using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fill_O : MonoBehaviour
{
    public float filling = 0.01f;
    public float stopPos = 0.1f;
    public Vector3 pos;
    public bool isfilling = false;  // Ã¤¿öÁö´Â Áß
    public bool isfull = false; //°¡µæÃ¤¿öÁü

    private void Start()
    {
        pos = transform.localPosition;
        StartCoroutine(Fill_co());
    }

    private IEnumerator Fill_co()
    {
        while (true)
        {
            if (isfilling)
            {
                pos.y += filling * Time.deltaTime;

                if (pos.y > stopPos)
                {
                    pos.y = stopPos;
                    isfilling = false;
                    isfull = true;
                    transform.localPosition = pos;
                    yield break;
                }
                transform.localPosition = pos;
            }

            yield return null;
        }
    }

}
