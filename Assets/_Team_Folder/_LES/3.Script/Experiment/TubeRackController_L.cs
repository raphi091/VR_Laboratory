using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TubeRackController : MonoBehaviour
{
    [Header("슬롯 설정")]
    [Tooltip("Tube가 꽂힐 위치(Transform)들의 배열")]
    public Transform[] slots;

    // 각 슬롯에 어떤 Tube가 있는지 저장하는 내부 배열
    private GameObject[] tubesInSlots;
    private XRInteractionManager interactionManager;

    void Awake()
    {
        // 슬롯 개수만큼 내부 배열을 초기화합니다.
        tubesInSlots = new GameObject[slots.Length];
        interactionManager = FindObjectOfType<XRInteractionManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 참고: Ch_VelocityInteractable은 XRGrabInteractable을 대체하는 사용자 정의 스크립트로 보입니다.
        Ch_VelocityInteractable grabInteractable = other.GetComponent<Ch_VelocityInteractable>();
        RackableTube rackableTube = other.GetComponent<RackableTube>();

        // ▼▼▼ 디버깅을 위한 상세 로그 추가 ▼▼▼
        Debug.Log($"--- Tube Rack 감지 검사 시작: '{other.name}' ---");

        // 1. RackableTube 스크립트 검사
        if (rackableTube != null)
            Debug.Log("<color=green>1. RackableTube 스크립트: 있음 (성공)</color>");
        else
            Debug.Log("<color=red>1. RackableTube 스크립트: 없음 (실패!)</color>");

        // 2. Ch_VelocityInteractable 스크립트 검사
        if (grabInteractable != null)
            Debug.Log("<color=green>2. Ch_VelocityInteractable 스크립트: 있음 (성공)</color>");
        else
            Debug.Log("<color=red>2. Ch_VelocityInteractable 스크립트: 없음 (실패!)</color>");

        // 3. isSelected 상태 검사 (grabInteractable이 있을 때만)
        if (grabInteractable != null)
        {
            if (grabInteractable.isSelected)
                Debug.Log("<color=green>3. 손에 들려있는 상태(isSelected): 네 (성공)</color>");
            else
                Debug.Log("<color=red>3. 손에 들려있는 상태(isSelected): 아니오 (실패!)</color>");
        }
        else
        {
            Debug.Log("3. 손에 들려있는 상태(isSelected): (Ch_VelocityInteractable이 없어 확인 불가)");
        }

        Debug.Log("--- 감지 검사 종료 ---");
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        // 최종 조건문
        if (rackableTube != null && grabInteractable != null && grabInteractable.isSelected)
        {
            Debug.Log(3); // 성공적으로 모든 조건을 통과
                          // PlaceTube 함수가 Ch_VelocityInteractable 타입을 받도록 수정해야 할 수 있습니다.
                          // PlaceTube(other.gameObject, grabInteractable); 
        }
    }

    // Tube를 빈 슬롯에 배치하는 함수
    private void PlaceTube(GameObject tubeObject, XRGrabInteractable grabInteractable)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (tubesInSlots[i] == null)
            {
                // 이제 grabInteractable이 무엇인지 정확히 알고 있으므로 에러가 발생하지 않습니다.
                if (interactionManager != null && grabInteractable.isSelected)
                {
                    interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
                }

                tubeObject.transform.SetParent(slots[i]);
                tubeObject.transform.localPosition = Vector3.zero;
                tubeObject.transform.localRotation = Quaternion.identity;

                RackableTube rackableTube = tubeObject.GetComponent<RackableTube>();
                if (rackableTube != null)
                {
                    rackableTube.PlaceInRack(this, i);
                }

                tubesInSlots[i] = tubeObject;
                Debug.Log($"Tube '{tubeObject.name}'가 {i}번 슬롯에 배치되었습니다.");

                return;
            }
        }

        Debug.LogWarning("Tube Rack이 가득 찼습니다!");
    }


    // 3. Tube가 랙에서 뽑혔을 때 호출될 함수
    public void RemoveTubeFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < tubesInSlots.Length)
        {
            if (tubesInSlots[slotIndex] != null)
            {
                Debug.Log($"Tube '{tubesInSlots[slotIndex].name}'가 {slotIndex}번 슬롯에서 제거되었습니다.");
                tubesInSlots[slotIndex] = null; // 내부 배열에서 Tube 기록 삭제
            }
        }
    }
}