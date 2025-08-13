using UnityEngine;

public class PourableItem_K : MonoBehaviour
{
    public enum IngredientType
    {
        LB,        // LB 가루
        Water,     // 증류수
        Agar      
    }

    public IngredientType ingredient; // Inspector에서 종류 선택

    [Range(0,180)] public float pourAngle = 60f; // 이 각도 이상 기울이면 붓기

    public bool IsPouring()
    {
        // 통의 윗면(up)이 아래를 많이 향하면 붓는 중
        float angle = Vector3.Angle(Vector3.down, transform.up);
        return angle > pourAngle;
    }
}
