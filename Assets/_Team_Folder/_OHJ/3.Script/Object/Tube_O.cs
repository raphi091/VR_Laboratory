using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Tube_O : MonoBehaviour
{
    public Fill_O fill;
    private Material mat;

    private GameObject ParticleObj;
    public ParticleSystem particle;
    public Transform childParticle;

    [Header("Tray 구조")]
    public Transform gelTray;
    public GameObject Dam;
    public GameObject gelLiquid;
    public GameObject gelSolid;

    public Rigidbody gel_rb;
    public Rigidbody tray_rb;

    public float MinThreshold = 120f;
    public float MaxThreshold = 240f;
    public float fillValue;
    public float PourValue = 0.001f; 

    public bool isPour = false;

    public Ch_VelocityInteractable gelGrabinteractable;
    public Ch_VelocityInteractable trayGrabinteractable;

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

        // Rigidbody 접근
        gel_rb = gelSolid.GetComponent<Rigidbody>();
        tray_rb = gelTray.GetComponent<Rigidbody>();
    }
    private void Start()
    {
        StartCoroutine(Pour_co());
    }

    private void OnEnable()
    {
        // 이벤트 등록
        if (gelGrabinteractable != null)
        {
            gelGrabinteractable.selectEntered.AddListener(OnGelGrabbed);
            gelGrabinteractable.selectExited.AddListener(OnGelReleased);
        }

        if (trayGrabinteractable != null)
        {
            trayGrabinteractable.selectEntered.AddListener(onTrayGrabbed);
            trayGrabinteractable.selectExited.AddListener(onTrayReleased);
        }
    }

    private void OnDisable()
    {
        // 이벤트 등록 해제
        if (gelGrabinteractable != null)
        {
            gelGrabinteractable.selectEntered.RemoveListener(OnGelGrabbed);
            gelGrabinteractable.selectExited.RemoveListener(OnGelReleased);
        }

        if (trayGrabinteractable != null)
        {
            trayGrabinteractable.selectEntered.RemoveListener(onTrayGrabbed);
            trayGrabinteractable.selectExited.RemoveListener(onTrayReleased);
        }
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

        // 가득 채워졌다면 굳기 시작
        if(fill.isfull)
        {
            StartCoroutine(Harden_co());
        }
    }

    // 굳기
    private IEnumerator Harden_co()
    {
        Debug.Log("굳고 있습니다.");
        yield return new WaitForSeconds(1f);

        if (gelLiquid.activeInHierarchy)
        {
            gelLiquid.SetActive(false);
        }

        if(!gelSolid.activeInHierarchy)
        {
            gelSolid.SetActive(true);
            Dam.GetComponent<Ch_VelocityInteractable>().enabled = true;    // Dam grab 활성화

            //부모로 부터 분리
            gelSolid.transform.SetParent(null);

        }
    }

    private void OnGelGrabbed(SelectEnterEventArgs args)
    {
        if(gel_rb != null)
        {
            gel_rb.isKinematic = true;
            gel_rb.useGravity = false;
            Debug.Log("gel 잡기");
        }
    }
    
    private void OnGelReleased(SelectExitEventArgs args)
    {
        gel_rb.isKinematic = false;
        gel_rb.useGravity = true;
        Debug.Log("gel 놓음");
    }

    private void onTrayGrabbed(SelectEnterEventArgs args)
    {
        if (tray_rb != null)
        {
            Dam.transform.SetParent(gelTray);
            Debug.Log("Tray 잡을 시 Dam 부모로 부터 넣기 잡기");
        }
    }

    private void onTrayReleased(SelectExitEventArgs args)
    {
        //if (tray_rb != null)
        //{
        //    Dam.transform.SetParent(gelTray);
        //    Debug.Log("dam의 kinematic 해제");
        //}
    }
}
