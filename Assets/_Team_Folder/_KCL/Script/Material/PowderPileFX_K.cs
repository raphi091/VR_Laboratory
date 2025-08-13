using UnityEngine;

public class PowderPileFX_K : MonoBehaviour
{
    [Tooltip("가루 더미로 쓸 메쉬(원기둥 등)의 Transform")]
    public Transform pileMesh;

    [Tooltip("1그램 당 높이 증가량(씬 스케일에 맞게 조절)")]
    public float gramsToHeight = 0.005f;

    [Tooltip("가루 더미 최대 높이")]
    public float maxHeight = 0.15f;

    private float totalGrams;

    /// <summary>가루가 추가될 때 호출 (그램 단위)</summary>
    public void AddAmount(float grams)
    {
        totalGrams += grams;
        float h = Mathf.Min(maxHeight, totalGrams * gramsToHeight);

        if (!pileMesh) return;

        var s = pileMesh.localScale;
        // Y만 키워서 위로 쌓이는 느낌
        pileMesh.localScale = new Vector3(s.x, Mathf.Max(h, 0.001f), s.z);
    }

    /// <summary>초기화/리셋용</summary>
    public void ResetPile()
    {
        totalGrams = 0f;
        if (!pileMesh) return;
        var s = pileMesh.localScale;
        pileMesh.localScale = new Vector3(s.x, 0.001f, s.z);
    }
}
