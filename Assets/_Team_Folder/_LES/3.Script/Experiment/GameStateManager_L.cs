using UnityEngine;

// 씬 전환 시에도 유지되어야 하는 게임의 전반적인 상태를 저장합니다.
// (예: Air Incubator 배양 시작 여부)
public class GameStateManager_L : MonoBehaviour
{
    public static GameStateManager_L Instance { get; private set; }

    // Air Incubator가 작동되어 다음 씬 로드 시 결과를 보여줘야 하는지 여부
    public bool IsCulturingOvernight { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
    }
}
