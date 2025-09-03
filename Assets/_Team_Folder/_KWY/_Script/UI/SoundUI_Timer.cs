using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;


public class SoundUI_Timer : MonoBehaviour
{
    [Header("시간 텍스트")]
    [SerializeField] private TextMeshProUGUI timeText;

    private void Start()
    {
        StartCoroutine(UpdateTimeRoutine());
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
}
