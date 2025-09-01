using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_Gloves : MonoBehaviour
{
    [SerializeField] private Material glovesMaterial;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Hand"))
        {
            other.GetComponent<Renderer>().material = glovesMaterial;
        }
    }
}
