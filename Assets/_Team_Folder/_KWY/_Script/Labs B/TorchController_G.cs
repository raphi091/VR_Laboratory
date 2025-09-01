using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class TorchController_G : MonoBehaviour
{
    [Header("연결 요소")]
    [Tooltip("토치 끝에 붙어있는 불꽃 VFX")]
    public VisualEffect flameVFX;

    [Tooltip("상호작용에 사용할 컨트롤러 버튼")]
    public InputActionReference interactionAction;

    [Header("사운드 설정")]
    [Tooltip("불을 켤 때 나는 '딸깍' 소리 (한 번 재생)")]
    public AudioClip ignitionClickSound;

    [Tooltip("불이 켜져 있는 동안 나는 '타오르는' 소리 (반복 재생)")]
    public AudioClip burningLoopSound;

    [Header("오디오 소스 연결")]
    [Tooltip("한 번만 재생되는 효과음용 AudioSource")]
    public AudioSource effectsAudioSource;

    [Tooltip("계속 반복 재생되는 배경음용 AudioSource")]
    public AudioSource loopAudioSource;


    [Header("상태")]
    [Tooltip("현재 토치가 켜져 있는지 여부")]
    public bool isLit = false;

    private bool isHeld = false;

    private void OnEnable()
    {
        interactionAction.action.started += LightTorch;
        interactionAction.action.canceled += ExtinguishTorch;
    }

    private void OnDisable()
    {
        interactionAction.action.started -= LightTorch;
        interactionAction.action.canceled -= ExtinguishTorch;
    }

    private void Start()
    {
        if (flameVFX != null)
        {
            flameVFX.Stop();
            flameVFX.gameObject.SetActive(false);
        }
        isLit = false;
        
        // 반복 오디오 소스 설정
        if(loopAudioSource != null && burningLoopSound != null)
        {
            loopAudioSource.clip = burningLoopSound;
            loopAudioSource.loop = true;
        }
    }

    private void LightTorch(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        // 1. '딸깍' 소리를 효과음용 AudioSource에서 한 번 재생합니다.
        if (effectsAudioSource != null && ignitionClickSound != null)
        {
            effectsAudioSource.PlayOneShot(ignitionClickSound);
        }

        // 2. '타오르는' 소리를 반복음용 AudioSource에서 재생 시작합니다.
        if (loopAudioSource != null)
        {
            loopAudioSource.Play();
        }

        if (flameVFX != null)
        {
            flameVFX.gameObject.SetActive(true);
            flameVFX.SendEvent("OnPlay");
        }

        isLit = true;
    }

    private void ExtinguishTorch(InputAction.CallbackContext context)
    {
        if (!isHeld) return;

        // '타오르는' 소리를 멈춥니다.
        if (loopAudioSource != null)
        {
            loopAudioSource.Stop();
        }

        if (flameVFX != null)
        {
            flameVFX.SendEvent("OnStop");
        }

        isLit = false;
    }

    public void OnGrab()
    {
        isHeld = true;
    }

    public void OnRelease()
    {
        isHeld = false;
        
        // 토치를 놓으면 무조건 불과 소리를 끕니다.
        if (isLit)
        {
            if (loopAudioSource != null) loopAudioSource.Stop();
            if (flameVFX != null)
            {
                flameVFX.SendEvent("OnStop");
            }
            isLit = false;
        }
    }
}