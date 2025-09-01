using System.Linq;
using UnityEngine;

[System.Serializable]
public class PcrResultMapping
{
    public ExperimentGroup group;
    public Texture resultTexture;
}

// PCR 실험의 시각적 결과물(텍스처)을 관리하고 제공합니다.
public class ResultManager_L : MonoBehaviour
{
    public static ResultManager_L Instance { get; private set; }

    [Header("PCR 결과")]
    [Tooltip("Gel Doc에 표시될 랜덤 결과 텍스처 배열")]
    public PcrResultMapping[] pcrResults;

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
    public Texture GetPcrResultForGroup(ExperimentGroup group)
    {
        if (pcrResults == null || pcrResults.Length == 0) return null;

        PcrResultMapping resultMapping = pcrResults.FirstOrDefault(r => r.group == group);

        if (resultMapping != null)
        {
            return resultMapping.resultTexture;
        }

        Debug.LogWarning($"'{group}' 해당회는 PCR 결과가 ResultManager에 설정되어 있지 않습니다.");
        return null;
    }
}