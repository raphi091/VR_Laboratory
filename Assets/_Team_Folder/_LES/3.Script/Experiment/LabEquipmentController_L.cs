using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LabEquipmentController_L : MonoBehaviour
{
    public enum EquipmentType { Thermocycler, GelElectrophoresis, GelDoc, Autoclave, ShakingIncubator, AirIncubator, CleanBench }

    [Header("기기 식별")]
    public EquipmentType type;

    [Header("상태 및 설정")]
    [SerializeField] private float processingTime = 5.0f;
    public Transform itemPlacementPoint;
    public CanvasGroup interactionCanvasGroup;
    public TMPro.TextMeshProUGUI statusText;
    public string requiredItemTag = "ExperimentSample";
    
    [Header("Optional Visual Components")]
    public Renderer screenRenderer;
    public Renderer liquidRenderer;
    
    [Header("이벤트")]
    public UnityEvent OnProcessCompleted;

    private GameObject currentItem;
    private Collider itemCollider;
    private Rigidbody itemRigidbody;

    private void OnTriggerEnter(Collider other)
    {
        if (currentItem == null && other.CompareTag(requiredItemTag))
        {
            currentItem = other.gameObject;
            Debug.Log($"[Enter] {gameObject.name}: '{currentItem.name}' 아이템 감지.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentItem)
        {
            Debug.Log($"[Exit] {gameObject.name}: '{currentItem.name}' 아이템 벗어남.");
            currentItem = null;
        }
    }
    
    public void StartProcessing()
    {
        if (currentItem == null)
        {
            Debug.LogWarning($"{gameObject.name}: 상호작용할 아이템이 없어 StartProcessing을 실행할 수 없습니다.");
            return;
        }
        Debug.Log($"--- {gameObject.name}: 프로세스 시작 ---");

        LockItemInPlace();

        if (type == EquipmentType.ShakingIncubator)
        {
            StartCoroutine(AnimateLiquidChange());
        }
        else
        {
            Invoke(nameof(ProcessComplete), processingTime);
        }
    }

    private void ProcessComplete()
    {
        UnlockItem();
        Debug.Log($"--- {gameObject.name}: 프로세스 완료 ---");

        if (type != EquipmentType.ShakingIncubator)
        {
            HandleVisualResult();
        }

        Debug.Log($"{gameObject.name}: OnProcessCompleted 이벤트를 발생시킵니다.");
        OnProcessCompleted.Invoke();
    }

    private void LockItemInPlace()
    {
        // ... (내부 로직은 디버그 필요 없음)
        itemCollider = currentItem.GetComponent<Collider>();
        itemRigidbody = currentItem.GetComponent<Rigidbody>();
        if (itemRigidbody != null) itemRigidbody.isKinematic = true;
        if (itemCollider != null) itemCollider.enabled = false;
        currentItem.transform.SetParent(itemPlacementPoint);
        currentItem.transform.localPosition = Vector3.zero;
        currentItem.transform.localRotation = Quaternion.identity;
    }

    private void UnlockItem()
    {
        // ... (내부 로직은 디버그 필요 없음)
        if (currentItem == null) return;
        currentItem.transform.SetParent(null);
        if (itemRigidbody != null) itemRigidbody.isKinematic = false;
        if (itemCollider != null) itemCollider.enabled = true;
    }

    private void HandleVisualResult()
    {
        if (type == EquipmentType.GelDoc)
        {
            if(screenRenderer != null)
            {
                screenRenderer.material.mainTexture = ResultManager_L.Instance.GetRandomPcrResult();
                Debug.Log($"{gameObject.name}: GelDoc 스크린에 결과 텍스처를 적용했습니다.");
            }
        }
    }

    private IEnumerator AnimateLiquidChange()
    {
        Debug.Log($"{gameObject.name}: 액체 색상 변경 애니메이션 시작.");
        // ... (이하 코루틴 내부 로직은 디버그 필요 없음)
        float elapsedTime = 0f;
        if (liquidRenderer == null) { ProcessComplete(); yield break; }
        Color startColor = ResultManager_L.Instance.flaskClearLiquidMaterial.color;
        Color endColor = ResultManager_L.Instance.flaskCloudyLiquidMaterial.color;
        Material newMaterialInstance = new Material(ResultManager_L.Instance.flaskClearLiquidMaterial);
        liquidRenderer.material = newMaterialInstance;
        while (elapsedTime < processingTime)
        {
            float t = elapsedTime / processingTime;
            newMaterialInstance.color = Color.Lerp(startColor, endColor, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        newMaterialInstance.color = endColor;
        ProcessComplete();
    }
}