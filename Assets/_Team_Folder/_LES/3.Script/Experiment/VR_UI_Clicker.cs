using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 지정된 입력 액션(VR 컨트롤러 버튼)이 발생했을 때,
/// XR Ray Interactor가 가리키는 UI 버튼을 강제로 클릭합니다.
/// </summary>
public class VR_UI_Clicker : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("UI와 상호작용하는 레이 인터랙터")]
    [SerializeField] private XRRayInteractor rayInteractor;

    [Header("입력 설정")]
    [Tooltip("UI 클릭으로 사용할 입력 액션 (예: XRI RightHand/UI Press)")]
    [SerializeField] private InputActionReference uiClickAction;

    private void OnEnable()
    {
        // 입력 액션 활성화
        if (uiClickAction != null && uiClickAction.action != null)
        {
            uiClickAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // 입력 액션 비활성화
        if (uiClickAction != null && uiClickAction.action != null)
        {
            uiClickAction.action.Disable();
        }
    }

    void Update()
    {
        // 지정된 버튼이 '이번 프레임에' 눌렸는지 확인
        if (uiClickAction == null || !uiClickAction.action.WasPressedThisFrame())
        {
            return;
        }

        // 레이가 현재 UI 요소와 충돌하고 있는지 확인
        if (rayInteractor.TryGetCurrentUIRaycastResult(out var result))
        {
            // 충돌한 오브젝트에서 Button 컴포넌트를 찾아봄
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null)
            {
                // 버튼이 있다면, 강제로 클릭 이벤트를 실행!
                Debug.Log($"UI Click Action Triggered on Button: {button.name}");
                button.onClick.Invoke();
            }
        }
    }
}