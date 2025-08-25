using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleFlask_O : MonoBehaviour
{
    [Header("액체 담기")]
    public List<LiquidData_L> requiredLiquids = new List<LiquidData_L>();
    public List<LiquidData_L> receiveddLiquids = new List<LiquidData_L>();  //받은 액체

    public LiquidData_L Dye;    // 염색약
    public bool ispossibleMix = false;  //염색약 섞기 가능 여부
    public bool isFillSample = false; // 샘플 채우기 가능 여부
    public bool ispossiblePour = false; // 붓기 가능 여부
    public bool isAddsuccess;  // 액체 추가 성공 여부

    [Header("플라스크 여부")]
    public bool isGel;  //겔이 든 플라스크인가?

    [Header("파티클")]
    public GameObject fullParticleObj;
    public ParticleSystem fullParticle;

    public void ReceiveLiquid(List<LiquidData_L> liquids)
    {
        isAddsuccess = false;
        foreach (var liquid in liquids)
        {
            //requiredliquids에 없는 액체는 무시
            if (!requiredLiquids.Contains(liquid))
            {
                Debug.LogWarning($". {liquid.name}는 요구되지 않는 액체입니다.");
                continue;
            }

            // 중복 불가
            if (receiveddLiquids.Contains(liquid))
            {
                Debug.LogWarning($"중복된 액체입니다. {liquid.name}는 이미 있습니다.");
                continue;
            }
            receiveddLiquids.Add(liquid);
            isAddsuccess = true;
            Debug.Log($"{liquid.name} 플라스크 추가");

        }

        if (IsComplete())
        {
            ispossibleMix = true;
            ispossiblePour = true;

            if(!fullParticleObj.activeInHierarchy)
            {
                fullParticleObj.SetActive(true);
            }

            if(!fullParticle.isPlaying)
            {
                fullParticle.Play();
            }

            Debug.Log("모두 들어있습니다");
        }

        else
        {
            ispossibleMix = false;
            ispossiblePour = false;
            Debug.Log($"충족안됨 {receiveddLiquids.Count}");
        }

    }

    private bool IsComplete()
    {
        if (requiredLiquids == null || requiredLiquids.Count == 0)
        {
            return false;
        }

        return requiredLiquids.TrueForAll(liquid => receiveddLiquids.Contains(liquid));
    }
}