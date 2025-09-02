using UnityEngine;
using System.Collections;

public class AutomaticDoor_L : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("왼쪽 문 오브젝트의 Animator")]
    public Animator leftDoorAnimator;
    [Tooltip("오른쪽 문 오브젝트의 Animator")]
    public Animator rightDoorAnimator;

    [Header("설정")]
    [Tooltip("문이 자동으로 닫히기까지 걸리는 시간(초)")]
    public float autoCloseDelay = 3.0f;

    [Header("사운드 설정")]
    [Tooltip("문이 열릴 때 재생할 사운드")]
    public AudioClip doorOpenSound;
    [Tooltip("문이 닫힐 때 재생할 사운드")]
    public AudioClip doorCloseSound;
    private AudioSource audioSource;

    private Coroutine closeCoroutine;
    private readonly int isOpenHash = Animator.StringToHash("isOpen");

    private bool isReady = false;

    void Awake()
    {
        // AudioSource 컴포넌트를 자동으로 찾아오거나, 없으면 추가합니다.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    IEnumerator Start()
    {
        // 게임 시작 직후의 물리적 충돌을 무시하기 위해 한 프레임 대기합니다.
        yield return null;
        isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player, NPC"))
        {
            return;
        }

        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        // 문이 이미 열려있지 않을 때만 열림 사운드를 재생합니다.
        if (leftDoorAnimator != null && !leftDoorAnimator.GetBool(isOpenHash))
        {
            if (audioSource != null && doorOpenSound != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }
        }

        // 두 개의 문을 모두 엽니다.
        if (leftDoorAnimator != null) leftDoorAnimator.SetBool(isOpenHash, true);
        if (rightDoorAnimator != null) rightDoorAnimator.SetBool(isOpenHash, true);

        Debug.Log("문이 열립니다.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player, NPC"))
        {
            return;
        }

        if (closeCoroutine == null)
        {
            closeCoroutine = StartCoroutine(AutoClose());
        }
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        // 문이 닫히기 전에 닫힘 사운드를 재생합니다.
        if (leftDoorAnimator != null && leftDoorAnimator.GetBool(isOpenHash))
        {
            if (audioSource != null && doorCloseSound != null)
            {
                audioSource.PlayOneShot(doorCloseSound);
            }
        }

        // 두 개의 문을 모두 닫습니다.
        if (leftDoorAnimator != null) leftDoorAnimator.SetBool(isOpenHash, false);
        if (rightDoorAnimator != null) rightDoorAnimator.SetBool(isOpenHash, false);

        Debug.Log("문이 닫힙니다.");
        closeCoroutine = null;
    }
}