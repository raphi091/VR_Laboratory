using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TubeEvent : MonoBehaviour
{
    [Header("이벤트")]
    public UnityEvent PourintoTray;

    [Header("필요한 오브젝트")]
    public Transform gelTray;

    [Header("파티클")]
    public GameObject ParticleObj;
    public ParticleSystem particle;
    public Transform childParticle;
    public Material mat;

    [Header("수치")]
    public float MinThreshold = 120f;
    public float MaxThreshold = 240f;
    public float fillValue;
    public float PourValue = 0.001f;

    public bool isPour = false;

    private void Awake()
    {
        mat = transform.Find("cotent").GetComponent<MeshRenderer>().material;

        if (mat == null)
        {
            Debug.Log("머터리얼 찾을 수 없음");
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
        float angle = transform.rotation.eulerAngles.z;

        if ((angle > MinThreshold && angle < MaxThreshold) || (angle < -MinThreshold && angle > -MaxThreshold))
        {
            PourLiquid();
        }

        else
        {
            StopPouring();
        }
        yield return null;
    }

    private void PourLiquid()
    {
        if (mat == null || ParticleObj == null || particle == null)
        {
            return;
        }

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

            isPour = false;

            StopPouring();
            return;

        }
    }

    private void StopPouring()
    {
        if (ParticleObj.activeInHierarchy)
        {
            ParticleObj.SetActive(false);
        }

        if (particle.isPlaying)
        {
            particle.Stop();
        }

    }
}
