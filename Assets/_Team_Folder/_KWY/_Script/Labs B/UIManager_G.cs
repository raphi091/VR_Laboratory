using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager_G : MonoBehaviour
{
    public static UIManager_G Instance = null;

    [Header("연결 요소")]
    [Tooltip("안내 문구가 포함된 UI 패널")]
    public GameObject warningPanel;
    [Tooltip("안내 문구를 표시할 TextMeshPro 컴포넌트")]
    public TextMeshProUGUI warningText;
    [Tooltip("메시지가 표시될 시간(초)")]
    public float messageDuration = 3.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    public void ShowWarningMessage(string message)
    {
        if (warningPanel == null || warningText == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        warningText.text = message;
        warningPanel.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        warningPanel.SetActive(false);
    }
}
