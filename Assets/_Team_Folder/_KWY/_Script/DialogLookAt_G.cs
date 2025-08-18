using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLookAt_G : MonoBehaviour
{
    void Update()
    {
        transform.LookAt(Camera.main.transform);
    }
}
