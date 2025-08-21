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
            Debug.Log("머터리얼이 없습니다");
        }

        if(fill == null)
        {
            Debug.Log("fill이 없습니다");
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

            // 일정 각도 도달 시 붓기
            if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
            {
                isPour = true;

                fillValue = mat.GetFloat("_Fill");
                fillValue -= PourValue;
                mat.SetFloat("_Fill", fillValue);


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
                Ray ray = new Ray();
                ray.origin = ParticleObj.transform.position;
                RaycastHit hit;
                ray.direction = Vector3.down;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    Debug.Log($"{hit.transform.name}이 찍혔습니다.");
                    if (hit.transform == gelTray)
                    {
                        parseEventArgs.fromTool = this.GetComponent<C_ExperimentTool>();
                        parseEventArgs.toTool = hit.transform.GetComponent<C_ExperimentTool>();
                        C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);

                        fill.isfilling = true;
                    }

                    else
                    {
                        fill.isfilling = false;
                    }
                }
            }

            else
            {
                StopPouring();
            }
            yield return null;
        }
    }

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
