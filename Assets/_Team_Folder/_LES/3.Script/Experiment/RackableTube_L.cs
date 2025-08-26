using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class RackableTube : MonoBehaviour
{
    private TubeRackController currentRack = null;
    private int currentSlotIndex = -1;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // 아이템을 잡았을 때와 놓았을 때(랙에서 뽑았을 때)의 이벤트를 구독합니다.
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 구독을 해제합니다.
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    // 랙이 이 Tube를 등록할 때 호출할 함수
    public void PlaceInRack(TubeRackController rack, int slotIndex)
    {
        currentRack = rack;
        currentSlotIndex = slotIndex;
        rb.isKinematic = true; // 랙에 있을 때는 물리적으로 고정
    }

    // 플레이어가 이 Tube를 잡았을 때 호출될 함수
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // 만약 랙에 꽂혀있는 상태에서 잡았다면
        if (currentRack != null)
        {
            // 랙에게 내가 뽑혔다고 알립니다.
            currentRack.RemoveTubeFromSlot(currentSlotIndex);
            currentRack = null;
            currentSlotIndex = -1;
        }
    }

    // 플레이어가 이 Tube를 놓았을 때 호출될 함수
    private void OnReleased(SelectExitEventArgs args)
    {
        // 랙에서 뽑은 직후이므로, 물리 효과를 다시 활성화합니다.
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
}