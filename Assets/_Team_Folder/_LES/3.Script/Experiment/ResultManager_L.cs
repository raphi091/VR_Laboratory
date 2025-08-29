using UnityEngine;

// PCR 실험의 시각적 결과물(텍스처)을 관리하고 제공합니다.
public class ResultManager_L : MonoBehaviour
{
    public static ResultManager_L Instance { get; private set; }

    [Header("PCR 결과")]
    [Tooltip("Gel Doc에 표시될 랜덤 결과 텍스처 배열")]
    public Texture[] pcrResultTextures;

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
}