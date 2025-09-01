using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TubeRackController_L : MonoBehaviour
{
    [Header("슬롯 설정")]
    [Tooltip("Tube가 꽂힐 위치(Transform)들의 배열")]
    public Transform[] slots;

    public enum PlacementMode
    {
        FindFirstAvailable, // 첫 번째 빈 슬롯에 순차적으로 배치
        PlaceAtClosest      // 가장 가까운 빈 슬롯에 배치
    }

    [Tooltip("이 랙이 허용하는 아이템의 종류(ItemType)")]
    public ItemType acceptedItemType = ItemType.Tube;

    [Header("배치 방식 설정")]
    [Tooltip("Tube를 배치하는 방식을 선택합니다.")]
    public PlacementMode placementMode = PlacementMode.FindFirstAvailable;

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

    void Start()
    {
        // 씬에 미리 배치된 Tube들을 확인하고 등록합니다.
        for (int i = 0; i < slots.Length; i++)
        {
            // i번째 슬롯에 자식 오브젝트가 있는지 확인합니다.
            if (slots[i].childCount > 0)
            {
                // 첫 번째 자식을 Tube로 간주합니다.
                GameObject prePlacedTube = slots[i].GetChild(0).gameObject;
                RackableTube_L rackableTube = prePlacedTube.GetComponent<RackableTube_L>();

                if (rackableTube != null)
                {
                    // 1. 랙의 내장 배열에 Tube를 등록합니다.
                    tubesInSlots[i] = prePlacedTube;

                    // 2. Tube에게 자신이 랙에 꽂혔다고 알려줘서 물리 상태 등을 고정시킵니다.
                    rackableTube.PlaceInRack(this, i);

//                    Debug.Log($"미리 배치된 Tube '{prePlacedTube.name}'를 {i}번 슬롯에 등록했습니다.");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이제 XRGrabInteractable을 직접 사용합니다.
        XRGrabInteractable grabInteractable = other.GetComponent<XRGrabInteractable>();
        RackableTube_L rackableTube = other.GetComponent<RackableTube_L>();
        ExperimentItem_L experimentItem = other.GetComponent<ExperimentItem_L>(); // 아이템 종류 확인용

        if (rackableTube != null && grabInteractable != null && grabInteractable.isSelected && experimentItem != null && experimentItem.itemType == acceptedItemType)
        {
            if (placementMode == PlacementMode.FindFirstAvailable)
            {
                PlaceInFirstAvailableSlot(other.gameObject, grabInteractable);
            }
            else // placementMode == PlacementMode.PlaceAtClosest
            {
                PlaceInClosestSlot(other.gameObject, grabInteractable);
            }
        }
    }

    // [모드 1] 첫 번째 빈 슬롯에 배치하는 함수
    private void PlaceInFirstAvailableSlot(GameObject tubeObject, XRGrabInteractable grabInteractable)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (tubesInSlots[i] == null)
            {
                PerformPlacement(tubeObject, grabInteractable, i);
                return; // 배치했으므로 함수 종료
            }
        }
        Debug.LogWarning("Tube Rack이 가득 찼습니다!");
    }

    // [모드 2] 가장 가까운 빈 슬롯에 배치하는 함수
    private void PlaceInClosestSlot(GameObject tubeObject, XRGrabInteractable grabInteractable)
    {
        float closestDistanceSqr = float.MaxValue;
        int closestSlotIndex = -1;

        // 1. 가장 가까운 '빈' 슬롯을 찾습니다.
        for (int i = 0; i < slots.Length; i++)
        {
            if (tubesInSlots[i] == null) // 슬롯이 비어있을 때만
            {
                float distanceSqr = (tubeObject.transform.position - slots[i].position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestSlotIndex = i;
                }
            }
        }

        // 2. 가장 가까운 빈 슬롯을 찾았다면, 거기에 배치합니다.
        if (closestSlotIndex != -1)
        {
            PerformPlacement(tubeObject, grabInteractable, closestSlotIndex);
        }
        else
        {
            Debug.LogWarning("Tube Rack이 가득 찼거나 가까운 빈 슬롯이 없습니다!");
        }
    }

    // [공통 로직] 실제 배치를 수행하는 함수
    private void PerformPlacement(GameObject tubeObject, XRGrabInteractable grabInteractable, int slotIndex)
    {
        // a. 플레이어가 Tube를 강제로 놓게 만듭니다.
        if (interactionManager != null && grabInteractable.isSelected)
        {
            interactionManager.SelectExit(grabInteractable.firstInteractorSelecting, grabInteractable);
        }

        // b. Tube를 해당 슬롯 위치로 이동시키고 고정합니다.
        tubeObject.transform.SetParent(slots[slotIndex]);
        tubeObject.transform.localPosition = Vector3.zero;
        tubeObject.transform.localRotation = Quaternion.identity;

        // c. Tube에게 자신이 어느 랙의 몇 번째 슬롯에 있는지 알려줍니다.
        RackableTube_L rackableTube = tubeObject.GetComponent<RackableTube_L>();
        if (rackableTube != null)
        {
            // Tube에게 원래 크기를 물어보고, 그 크기로 되돌립니다.
            tubeObject.transform.localScale = rackableTube.GetOriginalScale();

            rackableTube.PlaceInRack(this, slotIndex);
        }
        
        // d. 내부 배열에 Tube를 기록합니다.
        tubesInSlots[slotIndex] = tubeObject;
        //Debug.Log($"Tube '{tubeObject.name}'가 {slotIndex}번 슬롯에 배치되었습니다.");
    }

    public void RemoveTubeFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < tubesInSlots.Length)
        {
            if (tubesInSlots[slotIndex] != null)
            {
//                Debug.Log($"Tube '{tubesInSlots[slotIndex].name}'가 {slotIndex}번 슬롯에서 제거되었습니다.");
                tubesInSlots[slotIndex] = null;
            }
        }
    }
}