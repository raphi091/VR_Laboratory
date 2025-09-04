using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;


public class SoundUI_Timer : MonoBehaviour
{
    [Header("시간 텍스트")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private GameObject slider;

    private XRInput input;


    private void Awake()
    {
        input = new XRInput();
    }

    private void Start()
    {
        StartCoroutine(UpdateTimeRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            if (input.XRIUI.enabled) return;

            input.XRIUI.Enable();

            SetSelectedUIElement(slider);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Right_Hand") || other.CompareTag("Left_Hand"))
        {
            if (!input.XRIUI.enabled) return;

            input.XRIUI.Disable();
        }
    }

    private IEnumerator UpdateTimeRoutine()
    {
        while (true)
        {
            DateTime currentTime = DateTime.Now;
            string formattedTime = currentTime.ToString("(ddd) tt hh:mm", new CultureInfo("ko-KR"));
            timeText.text = formattedTime;

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void SetSelectedUIElement(GameObject element)
    {
        EventSystem.current.SetSelectedGameObject(element);
    }
}
