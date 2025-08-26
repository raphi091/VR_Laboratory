using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;


public class PlayerMovementController_G : MonoBehaviour
{
    [Header("필수 연결 요소")]
    [Tooltip("XR Origin 오브젝트")]
    public Transform xrOrigin;

    [Tooltip("Locomotion System 컴포넌트")]
    public CharacterController characterController;

    [Tooltip("비활성화할 이동 관련 컴포넌트")]
    public MonoBehaviour moveProvider;

    [Tooltip("목표 위치")]
    public Transform snapTarget;


    [Header("설정")]
    [Tooltip("스냅이 완료되기까지 걸리는 시간(초)")]
    public float snapDuration = 0.5f;

    [Tooltip("플레이어 태그")]
    public string playerTag = "Player";

    private bool isLocked = false;
    private Coroutine snappingCoroutine;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || isLocked) return;

        isLocked = true;

        if (moveProvider != null) 
            moveProvider.enabled = false;

        if (snappingCoroutine != null)
        {
            StopCoroutine(snappingCoroutine);
        }

        snappingCoroutine = StartCoroutine(SnapToPosition());
    }

    private IEnumerator SnapToPosition()
    {
        if (characterController != null) 
            characterController.enabled = false;

        float elapsedTime = 0f;
        Vector3 startPosition = xrOrigin.position;
        Quaternion startRotation = xrOrigin.rotation;

        while (elapsedTime < snapDuration)
        {
            xrOrigin.position = Vector3.Lerp(startPosition, snapTarget.position, elapsedTime / snapDuration);
            xrOrigin.rotation = Quaternion.Slerp(startRotation, snapTarget.rotation, elapsedTime / snapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        xrOrigin.position = snapTarget.position;
        xrOrigin.rotation = snapTarget.rotation;

        if (characterController != null) 
            characterController.enabled = true;

        snappingCoroutine = null;
    }

    public void EnablePlayerMovement()
    {
        isLocked = false;

        if (moveProvider != null) 
            moveProvider.enabled = true;

        Debug.Log("플레이어 이동이 활성화되었습니다.");
    }
}
