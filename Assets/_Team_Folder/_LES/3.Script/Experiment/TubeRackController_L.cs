using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TubeRackController_L : MonoBehaviour
{
    [Header("슬롯 설정")]
    [Tooltip("Tube가 꽂힐 위치(Transform)들의 배열")]
    public Transform[] slots;

    private GameObject[] tubesInSlots;
    private XRInteractionManager interactionManager;

    void Awake()
    {
        tubesInSlots = new GameObject[slots.Length];
        interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogError("오류: 씬에서 XRInteractionManager를 찾을 수 없습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이제 XRGrabInteractable을 직접 사용합니다.
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();
        RackableTube_L rackableTube = other.GetComponent<RackableTube_L>();

        if (rackableTube != null && grabInteractable != null && grabInteractable.isSelected)
        {
            // 주석을 해제하여 PlaceTube 함수를 정상적으로 호출합니다.
            PlaceTube(other.gameObject, grabInteractable);
        }
    }

    private void PlaceTube(GameObject tubeObject, XRGrabInteractable grabInteractable)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (tubesInSlots[i] == null) // 이 슬롯이 비어있다면
            {
                // a. 플레이어가 Tube를 강제로 놓게 만듭니다.
                if (interactionManager != null && grabInteractable.isSelected)
                {
                    interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
                }

                // b. Tube를 해당 슬롯 위치로 이동시키고 고정합니다.
                tubeObject.transform.SetParent(slots[i]);
                tubeObject.transform.localPosition = Vector3.zero;
                tubeObject.transform.localRotation = Quaternion.identity;

                // c. Tube에게 자신이 어느 랙의 몇 번째 슬롯에 있는지 알려줍니다.
                RackableTube_L rackableTube = tubeObject.GetComponent<RackableTube_L>();
                if (rackableTube != null)
                {
                    rackableTube.PlaceInRack(this, i);
                }
                
                // d. 내부 배열에 Tube를 기록합니다.
                tubesInSlots[i] = tubeObject;
                Debug.Log($"Tube '{tubeObject.name}'가 {i}번 슬롯에 배치되었습니다.");

                return; // 배치했으므로 함수 종료
            }
        }
        
        Debug.LogWarning("Tube Rack이 가득 찼습니다!");
    }

    public void RemoveTubeFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < tubesInSlots.Length)
        {
            if (tubesInSlots[slotIndex] != null)
            {
                Debug.Log($"Tube '{tubesInSlots[slotIndex].name}'가 {slotIndex}번 슬롯에서 제거되었습니다.");
                tubesInSlots[slotIndex] = null;
            }
        }
    }
}