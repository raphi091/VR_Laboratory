using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class C_Pipette : MonoBehaviour
{
    [SerializeField] private LiquidData_L liquidData;
    [SerializeField] private InputActionReference rightPippetPush;
    [SerializeField] private InputActionReference leftPippetPush;
    
    public LiquidData_L LiquidData { get => liquidData; set => liquidData = value; }
    
    private ParticleSystem particleSystem;
    private bool isSelected = false;

    private void Awake()
    {
        particleSystem=GetComponentInChildren<ParticleSystem>();
        rightPippetPush.action.performed += OnPippet;
        leftPippetPush.action.performed += OnPippet;
    }

    private void OnDisable()
    {
        rightPippetPush.action.performed -= OnPippet;
        leftPippetPush.action.performed -= OnPippet;
    }

    void OnPippet(InputAction.CallbackContext context)
    {
        if (isSelected)
        {
            particleSystem.Play();
        }
    }

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelected = true;
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        isSelected = false;
    }
}
