// TTSTester.cs - 최종 버전
// TTSSpeaker를 사용하여 TTS 기능을 테스트하는 최종 스크립트입니다.
using UnityEngine;
using Meta.WitAi.TTS.Utilities; // TTSSpeaker를 사용하기 위해 필요합니다.
using UnityEngine.InputSystem;    // Unity의 새로운 입력 시스템을 사용합니다.

// 이 스크립트가 붙은 오브젝트에는 TTSSpeaker가 반드시 있도록 강제합니다.
[RequireComponent(typeof(TTSSpeaker))]
public class TTSTester : MonoBehaviour
{
    // 이제 TTSWit이 아니라 TTSSpeaker 변수만 있으면 됩니다.
    private TTSSpeaker ttsSpeaker;

    // 게임이 시작될 때 한 번만 호출됩니다.
    void Awake()
    {
        // Inspector에서 직접 연결할 필요 없이,
        // 이 스크립트가 붙어있는 게임 오브젝트에서 TTSSpeaker 컴포넌트를 자동으로 찾아옵니다.
        ttsSpeaker = GetComponent<TTSSpeaker>();
    }

    void Update()
    {
        // 키보드 'T' 키 입력 감지 (새로운 입력 시스템)
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("'T' 키 입력 감지! TTSSpeaker로 음성 합성을 요청합니다...");
            SpeakTestSentence("This is the final test from keyboard.");
        }

        // VR 컨트롤러 'A' 버튼 입력 감지 (Meta SDK의 OVRInput)
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            Debug.Log("VR 컨트롤러 'A' 버튼 입력 감지! TTSSpeaker로 음성 합성을 요청합니다...");
            SpeakTestSentence("This is the final test from a VR controller.");
        }
    }

    // 텍스트를 받아 음성 재생을 요청하는 함수
    void SpeakTestSentence(string text)
    {
        // ttsSpeaker가 성공적으로 찾아졌는지 확인합니다.
        if (ttsSpeaker != null)
        {
            // TTSSpeaker에게 직접 "말하기"를 시킵니다. 이것이 최신 버전의 올바른 방법입니다.
            ttsSpeaker.Speak(text);
        }
        else
        {
            Debug.LogError("TTSSpeaker 컴포넌트를 찾을 수 없습니다!");
        }
    }
}