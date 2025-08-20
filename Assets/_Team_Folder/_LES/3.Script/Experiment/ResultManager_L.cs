using UnityEngine;

// 모든 실험의 시각적 결과물(텍스처, 머티리얼)을 관리하고 제공합니다.
public class ResultManager_L : MonoBehaviour
{
    public static ResultManager_L Instance { get; private set; }

    [Header("PCR 결과")]
    [Tooltip("Gel Doc에 표시될 랜덤 결과 텍스처 배열")]
    public Texture[] pcrResultTextures;

    [Header("미생물 배양 결과")]
    [Tooltip("배양이 끝난 페트리 접시에 적용될 랜덤 결과 텍스처 배열")]
    public Texture[] culturingResultTextures; // 배열로 수정

    [Tooltip("배양 전 원본 액체 배지 머티리얼 (맑은 노랑)")]
    public Material flaskClearLiquidMaterial; // 추가된 부분
    [Tooltip("Shaking Incubator 배양 후 액체 배지 머티리얼 (탁한 노랑)")]
    public Material flaskCloudyLiquidMaterial;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // PCR 결과 텍스처 중 하나를 무작위로 반환합니다.    
    public Texture GetRandomPcrResult()
    {
        if (pcrResultTextures == null || pcrResultTextures.Length == 0) return null;
        int randomIndex = Random.Range(0, pcrResultTextures.Length);
        return pcrResultTextures[randomIndex];
    }
    
    // 미생물 배양 결과 텍스처 중 하나를 무작위로 반환합니다.    
    public Texture GetRandomCulturingResult()
    {
        if (culturingResultTextures == null || culturingResultTextures.Length == 0) return null;
        int randomIndex = Random.Range(0, culturingResultTextures.Length);
        return culturingResultTextures[randomIndex];
    }
}