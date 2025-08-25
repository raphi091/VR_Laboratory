using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class DynamicInfoUI_G : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [Tooltip("제목 텍스트")]
    public TextMeshProUGUI titleText;

    [Tooltip("실제 내용물 목록 텍스트")]
    public TextMeshProUGUI contentText;


    public void UpdateInfo(string title, string content)
    {
        if (titleText != null)
            titleText.text = title;

        if (contentText != null)
            contentText.text = content;
    }
}
