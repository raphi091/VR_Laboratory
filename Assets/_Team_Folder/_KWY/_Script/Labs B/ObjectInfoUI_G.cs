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
    private Transform cameraTransform;


    private void OnEnable()
    {
        infoSource = GetComponentInParent<InfoDisplayable_G>();
        UpdateText();
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }
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
