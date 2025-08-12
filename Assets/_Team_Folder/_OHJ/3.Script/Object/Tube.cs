using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : MonoBehaviour
{
    private GameObject ParticleObj;
    public ParticleSystem particle;
    public Transform childParticle;

    public float MinThreshold = 120f;
    public float MaxThreshold = 240f;
    public bool isPlaying;

    private void Awake()
    {
        //Transform leak = transform.Find("WaterPoint");
        //if (leak == null)
        //{
        //    Debug.Log("Cylinder 오브젝트를 찾을 수 없습니다.");
        //    return;
        //}
        ParticleObj = transform.Find("WaterPoint").GetChild(0).gameObject;
        ParticleObj.TryGetComponent(out particle);
        childParticle = ParticleObj.transform.GetChild(0);
    }

    private void Update()
    {
        float angle = transform.rotation.eulerAngles.z;

        // 각도가 기준을 넘으면 파티클 생성
        if((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
        {
            Debug.Log($"액체 흘려나옵니다 angle = {angle}");

            if(!ParticleObj.activeInHierarchy)
            {
                ParticleObj.SetActive(true);
            }
            particle.Stop();
            particle.Play();
        }

        else
        {
            Debug.Log("액체 안나옵니다");

            if(ParticleObj.activeInHierarchy)
            {
                ParticleObj.SetActive(false);
            }
            particle.Stop();
        }
    }
}
