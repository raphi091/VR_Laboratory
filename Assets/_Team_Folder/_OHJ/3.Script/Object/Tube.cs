using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : MonoBehaviour
{
    public Fill fill;
    private Material mat;

    private GameObject ParticleObj;
    public ParticleSystem particle;
    public Transform childParticle;

    public Transform gelTray;

    public float MinThreshold = 120f;
    public float MaxThreshold = 240f;
    public float fillValue;
    public float PourValue = 0.001f; 

    public bool isPour = false;

    private void Awake()
    {
        mat = transform.Find("cotent").GetComponent<MeshRenderer>().material;

        if(mat == null)
        {
            Debug.Log("머터리얼 찾을 수 없음");
        }

        if(fill == null)
        {
            Debug.Log("fill이 연결되지 않았습니다");
        }

        ParticleObj = transform.Find("WaterPoint").GetChild(0).gameObject;
        ParticleObj.TryGetComponent(out particle);
        childParticle = ParticleObj.transform.GetChild(0);
    }

    private void Start()
    {
        StartCoroutine(Pour_co());
    }

    private IEnumerator Pour_co()
    {
        while(true)
        {
            float angle = transform.rotation.eulerAngles.z;

            // 각도가 기준을 넘으면 파티클 생성
            if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
            {
                isPour = true;

                fillValue = mat.GetFloat("_Fill");
                fillValue -= PourValue;
                mat.SetFloat("_Fill", fillValue);

                //Debug.Log($"현재 양 : {fillValue}");
                //Debug.Log($"액체 흘려나옵니다 angle = {angle}");


                if (!ParticleObj.activeInHierarchy)
                {
                    ParticleObj.SetActive(true);
                }

                if (!particle.isPlaying)
                {
                    particle.Play();
                }

                if (fillValue < -1f)
                {
                    fillValue = -1f;
                    if (ParticleObj.activeInHierarchy)
                    {
                        ParticleObj.SetActive(false);
                    }

                    fill.isfilling = false;
                    isPour = false;

                    particle.Stop();
                    yield break;
                }

                // Raycast 
                //Physics.Raycast(Ray ray, out RaycastHit hitInfo)
                //충돌이 발생 시 hitinfo에 저장 => hitinfo가 gelTray라면 fill 되도록
                Ray ray = new Ray();
                ray.origin = ParticleObj.transform.position;
                RaycastHit hit;
                ray.direction = Vector3.down;

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log($"{hit.transform.name}");
                    if (hit.transform == gelTray)
                    {
                        fill.isfilling = true;
                        Debug.Log("레이케스트작동 중");
                    }

                    else
                    {
                        fill.isfilling = false;
                    }
                }
            }// end  : 각도 충족할 때

            else
            {
                StopPouring();
            }
            yield return null;
        }
    }

    //private void Update()
    //{
    //    float angle = transform.rotation.eulerAngles.z;

    //    // 각도가 기준을 넘으면 파티클 생성
    //    if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
    //    {
    //        isPour = true;

    //        fillValue = mat.GetFloat("_Fill");
    //        Debug.Log($"현재 양 : {fillValue}");
    //        fillValue  -= PourValue;
    //        mat.SetFloat("_Fill", fillValue);
    //        Debug.Log($"fillValue  감소 중 : {fillValue}");
    //        //Debug.Log($"액체 흘려나옵니다 angle = {angle}");

            
    //        if(!ParticleObj.activeInHierarchy)
    //        {
    //            ParticleObj.SetActive(true);
    //        }

    //        if(!particle.isPlaying)
    //        {
    //            particle.Play();
    //        }

    //        if(fillValue < -1f)
    //        {
    //            fillValue = -1f;
    //            if (ParticleObj.activeInHierarchy)
    //            {
    //                ParticleObj.SetActive(false);
    //            }

    //            fill.isfilling = false;
    //            isPour = false;

    //            particle.Stop();
    //        }

    //        // Raycast 
    //        //Physics.Raycast(Ray ray, out RaycastHit hitInfo)
    //        //충돌이 발생 시 hitinfo에 저장 => hitinfo가 gelTray라면 fill 되도록
    //        Ray ray = new Ray();
    //        ray.origin = ParticleObj.transform.position;
    //        RaycastHit hit;
    //        ray.direction = Vector3.down;

    //        if(Physics.Raycast(ray, out hit))
    //        {
    //            if(hit.transform == gelTray)
    //            {
    //                fill.isfilling = true;
    //            }

    //            else
    //            {
    //                fill.isfilling = false;
    //            }
    //        }
    //    }// end  : 각도 충족할 때

    //    else
    //    {
    //        StopPouring();
    //    }

    //}

    // 붓기 멈추기
    private void StopPouring()
    {
        isPour = false;

        if (fill != null && fill.isfilling)
        {
            fill.isfilling = false;
        }

        if (ParticleObj.activeInHierarchy)
        {
            ParticleObj.SetActive(false);
        }

        if(particle.isPlaying)
        {
            particle.Stop();
        }

    }
}
