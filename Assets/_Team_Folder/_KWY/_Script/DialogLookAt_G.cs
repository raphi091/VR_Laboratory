using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLookAt_G : MonoBehaviour
{
    private void Update()
    {
        transform.LookAt(Camera.main.transform);
    }
}
