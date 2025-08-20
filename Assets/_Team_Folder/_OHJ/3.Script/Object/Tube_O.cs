using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube_O : MonoBehaviour
{
    public Fill_O fill;
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

    [SerializeField] private ParseEventArgs parseEventArgs = new ParseEventArgs();

    private void Awake()
    {
        mat = transform.Find("cotent").GetComponent<MeshRenderer>().material;

        if(mat == null)
        {
            Debug.Log("���͸��� ã�� �� ����");
        }

        if(fill == null)
        {
//            Debug.Log("fill�� ������� �ʾҽ��ϴ�");
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

            // ������ ������ ������ ��ƼŬ ����
            if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
            {
                isPour = true;

                fillValue = mat.GetFloat("_Fill");
                fillValue -= PourValue;
                mat.SetFloat("_Fill", fillValue);

                //Debug.Log($"���� �� : {fillValue}");
                //Debug.Log($"��ü ������ɴϴ� angle = {angle}");


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
                //�浹�� �߻� �� hitinfo�� ���� => hitinfo�� gelTray��� fill �ǵ���
                Ray ray = new Ray();
                ray.origin = ParticleObj.transform.position;
                RaycastHit hit;
                ray.direction = Vector3.down;

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log($"{hit.transform.name}");
                    if (hit.transform == gelTray)
                    {
                        parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
                        parseEventArgs.toTool = hit.transform.GetComponent<C_ExperimentTool>();
                        C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

                        fill.isfilling = true;
                        Debug.Log("�����ɽ�Ʈ�۵� ��");
                    }

                    else
                    {
                        fill.isfilling = false;
                    }
                }
            }// end  : ���� ������ ��

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

    //    // ������ ������ ������ ��ƼŬ ����
    //    if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
    //    {
    //        isPour = true;

    //        fillValue = mat.GetFloat("_Fill");
    //        Debug.Log($"���� �� : {fillValue}");
    //        fillValue  -= PourValue;
    //        mat.SetFloat("_Fill", fillValue);
    //        Debug.Log($"fillValue  ���� �� : {fillValue}");
    //        //Debug.Log($"��ü ������ɴϴ� angle = {angle}");

            
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
    //        //�浹�� �߻� �� hitinfo�� ���� => hitinfo�� gelTray��� fill �ǵ���
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
    //    }// end  : ���� ������ ��

    //    else
    //    {
    //        StopPouring();
    //    }

    //}

    // �ױ� ���߱�
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
