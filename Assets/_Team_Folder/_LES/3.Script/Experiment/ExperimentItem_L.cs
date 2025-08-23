using UnityEngine;

// 아이템의 종류를 나타내기 위한 Enum (선택사항이지만 확장성에 좋음)
public enum ItemType { Generic, PCR_Tube, Agarose_Gel, Petri_Dish, Flask }

public class ExperimentItem_L : MonoBehaviour
{
    [Tooltip("이 아이템의 종류를 지정합니다.")]
    public ItemType itemType = ItemType.Generic;
}