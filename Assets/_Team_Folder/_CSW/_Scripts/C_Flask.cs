using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_Flask : MonoBehaviour
{
    [SerializeField] private List<LiquidData_L> liquidData=new List<LiquidData_L>();
    [SerializeField] private Material liquidMaterial;
    
    private Ch_VelocityInteractable velocityInteractable;
    private ParticleSystem particleSystem;
    private bool combineFailed = false;

    private void Awake()
    {
        particleSystem=GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (velocityInteractable.velocity.magnitude > 0.06f)
        {
            
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pippet"))
        {
            
        }
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Pippet"))
        {
            if (liquidData.Contains(GetComponent<C_Pipette>().LiquidData))
            {
                
            }
            else
            {
                combineFailed = true;
            }
        }
    }
}
