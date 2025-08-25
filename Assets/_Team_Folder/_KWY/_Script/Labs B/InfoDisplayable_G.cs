using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoDisplayable_G : MonoBehaviour
{
    [Header("표시할 정보")]
    [Tooltip("오브젝트의 이름")]
    public string objectName = "오브젝트 이름";

    [Tooltip("오브젝트에 대한 간단한 설명")]
    [TextArea(3, 5)]
    public string objectDescription = "오브젝트에 대한 설명입니다.";
}
