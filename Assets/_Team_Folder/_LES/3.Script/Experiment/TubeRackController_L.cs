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
        // 1. 손에 Tube가 있고, Tube Rack에 빈 슬롯이 있을 때
        Ch_VelocityInteractable grabInteractable = other.GetComponent<Ch_VelocityInteractable>();
        RackableTube rackableTube = other.GetComponent<RackableTube>();

        // 들어온 것이 잡을 수 있는 RackableTube이고, 현재 손에 들려있는 상태일 때
        if (rackableTube != null && grabInteractable != null && grabInteractable.isSelected)
        {
            PlaceTube(other.gameObject, grabInteractable);
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