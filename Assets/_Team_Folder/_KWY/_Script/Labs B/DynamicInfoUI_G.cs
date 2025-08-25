using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class DynamicInfoUI_G : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public TextMeshProUGUI contentText;


    public void UpdateContent(string content)
    {
        if (contentText != null)
        {
            contentText.text = content;
        }
    }
}
