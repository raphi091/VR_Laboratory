using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wobble_O : MonoBehaviour
{
    private Renderer rend;   //랜더링
    private Vector3 lastPos;    //마지막 위치 
    private Vector3 velocity;   //속도
    private Vector3 lastRot;    //마지막 회전
    private Vector3 angularVelocity;    //각속도

    public float MaxWobble; //흔들기 최대속도
    public float WobbleSpeed;   //흔들기 속도
    public float Recovery = 1f;

    [SerializeField] private float wobbleAmountX;    //흔들 때 X축
    [SerializeField] private float wobbleAmountZ;    //흔들 때 Z축
    [SerializeField] private float wobbleAmountToAddX;   //X축이 변경될 때 변수
    [SerializeField] private float wobbleAmountToAddZ;   //Y축 변경될 때 변수
    public float pulse;    // 진동 수
    private float time;

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    private void Update()
    {
        time += Time.deltaTime;

        //시간이 흐르면서  흔들기 감소
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * (Recovery));
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * (Recovery));

        // Mathf.sin : 부드럽게 진동하거나 움직이게 할 때 사용
        //Mathf.sin(시간변수);
        // 각도에 따라 -1, 1 사이를 반환 => 오브젝트의 위치를 주기적으로 변화시켜 부드러운 움직임을 구현
        // ex) Mathf.Sin(Time.time * frequency) * amplitude;    // frequency는 진동 수, amplitude는 움직임 수
        pulse = 2 * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        // 세이더
        rend.material.SetFloat("_WobbleX", wobbleAmountX);
        rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

        // 속도 = 거리 / 시간
        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = transform.rotation.eulerAngles - lastRot;

        // 흔들 때 속도
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

        // 마지막 위치
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }
}
