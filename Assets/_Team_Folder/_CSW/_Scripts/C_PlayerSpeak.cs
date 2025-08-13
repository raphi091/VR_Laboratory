using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 새로운 입력 시스템을 사용하기 위해 필수입니다.

public class C_PlayerSpeak : MonoBehaviour
{
    [Header("타겟 NPC 설정")]
    [Tooltip("플레이어가 상호작용할 NpcController를 연결합니다.")]
    [SerializeField] private NpcController_G targetNpc;

    [Header("입력 액션 설정")]
    [Tooltip("음성 대화를 시작하기 위해 설정한 Input Action을 연결합니다.")]
    [SerializeField] private InputActionReference startConversationAction;

    private void OnEnable()
    {
        // 스크립트가 활성화될 때, 지정된 입력 액션에 이벤트 핸들러를 등록(구독)합니다.
        // 이렇게 하면 해당 버튼이 눌렸을 때만 OnStartConversationPressed 함수가 호출됩니다.
        if (startConversationAction != null)
        {
            startConversationAction.action.Enable();
            startConversationAction.action.performed += OnStartConversationPressed;
        }
    }

    private void OnDisable()
    {
        // 스크립트가 비활성화될 때, 등록했던 이벤트 핸들러를 해제하여 메모리 누수를 방지합니다.
        if (startConversationAction != null)
        {
            startConversationAction.action.performed -= OnStartConversationPressed;
            startConversationAction.action.Disable();
        }
    }

    /// <summary>
    /// startConversationAction에 지정된 버튼이 눌렸을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="context">입력 시스템이 전달하는 입력 정보입니다.</param>
    private void OnStartConversationPressed(InputAction.CallbackContext context)
    {
        // targetNpc가 제대로 연결되어 있는지 확인합니다.
        if (targetNpc == null)
        {
            Debug.LogError("타겟 NPC가 연결되지 않았습니다!");
            return;
        }

        Debug.Log("[Player] NPC에게 대화 시작을 요청합니다.");
        
        // NpcController에 있는 public 함수를 호출하여 "말을 걸었다"는 신호를 보냅니다.
        targetNpc.OnPlayerStartsConversation();
    }
}
