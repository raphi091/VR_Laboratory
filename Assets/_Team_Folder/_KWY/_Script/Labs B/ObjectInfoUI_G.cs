using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ObjectInfoUI_G : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    private InfoDisplayable_G infoSource;

    private void OnEnable()
    {
        infoSource = GetComponentInParent<InfoDisplayable_G>();
        UpdateText();
    }

    private void UpdateText()
    {
        if (infoSource != null && nameText != null && descriptionText != null)
        {
            nameText.text = infoSource.objectName;
            descriptionText.text = infoSource.objectDescription;
        }
    }
}
