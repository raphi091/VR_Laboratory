using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class C_PlayerSpeak : MonoBehaviour
{
    [Header("타겟 NPC 설정")]
    [Tooltip("플레이어가 상호작용할 NpcController.")]
    [SerializeField] private NpcController_G targetNpc;

    [Header("입력 액션 설정")]
    [Tooltip("음성 대화를 시작하기 위해 설정한 Input Action")]
    [SerializeField] private InputActionReference interactionAction;

    private void OnEnable()
    {
        if (interactionAction != null)
        {
            interactionAction.action.Enable();
            interactionAction.action.performed += OnInteractionPressed;
        }
    }

    private void OnDisable()
    {
        if (interactionAction != null)
        {
            interactionAction.action.performed -= OnInteractionPressed;
            interactionAction.action.Disable();
        }
    }

    private void OnInteractionPressed(InputAction.CallbackContext context)
    {
        if (targetNpc == null)
        {
            Debug.LogError("타겟 NPC가 연결되지 않았습니다!");
            return;
        }

        Debug.Log("[Player] NPC와 상호작용을 시도합니다.");
        targetNpc.OnPlayerInteraction();
    }
}
