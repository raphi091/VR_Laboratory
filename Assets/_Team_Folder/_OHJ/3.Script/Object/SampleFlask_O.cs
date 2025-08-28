using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SampleFlask_O : MonoBehaviour
{
    [Header("액체 담기")]
    public List<LiquidData_L> requiredLiquids = new List<LiquidData_L>();
    public List<LiquidData_L> receiveddLiquids = new List<LiquidData_L>();  //받은 액체

    [Header("염색약 넣기")]
    public LiquidData_L Dye;    // 염색약
    public LiquidData_L DNA_DYE;
    public LiquidData_L SYBR_DYE;
    public bool ispossibleMix = false;  //염색약 섞기 가능 여부
    public bool isFillSample = false; // 샘플 채우기 가능 여부
    public bool ispossiblePour = false; // 붓기 가능 여부
    public bool isAddsuccess;  // 액체 추가 성공 여부

    [Header("플라스크 여부")]
    public bool isGel;  //겔이 든 플라스크인가?

    [Header("시각 UI")]
    public DynamicInfoUI_G infoPanel;

    private void Start()
    {
        UpdateInfoPanel();
    }

    public void ReceiveLiquid(List<LiquidData_L> liquids)
    {
        isAddsuccess = false;

        int i = 0;
        while(i < liquids.Count)
        {
            var liquid = liquids[i];

            // 요구하지 않는 액체 거르기
            // 샘플 플라스크는 DNA_DYE, 겔 플라스크는 SYBR_DYE는 들어갈 수 있게 허용
            if (!requiredLiquids.Contains(liquid))
            {
                if ((isGel && liquid != SYBR_DYE) || (!isGel && liquid != DNA_DYE))
                {
                    Debug.LogWarning($". {liquid.name}는 요구되지 않는 액체입니다.");
                    liquids.RemoveAt(i);    // 인덱스(숫자)의 요소를 제거
                    continue;   // i 증가하지 않고 다음 요소 체크
                }
            }
           
            // 중복 불가
            if (receiveddLiquids.Contains(liquid))
            {
                Debug.LogWarning($"중복된 액체입니다. {liquid.name}는 이미 있습니다.");
                liquids.RemoveAt(i);    // 인덱스(숫자)의 요소를 제거
                continue;
            }

            //유효한 액체인 경우
            receiveddLiquids.Add(liquid);
            isAddsuccess = true;
            UpdateInfoPanel();
            Debug.Log($"{liquid.name} 플라스크 추가");

            i++;
        }

        if (IsComplete())
        {
            ispossibleMix = true;
            ispossiblePour = true;

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

    private void UpdateInfoPanel()
    {
        if (infoPanel == null) return;

        string contentList;
        if (receiveddLiquids != null && receiveddLiquids.Count > 0)
        {
            var contentNames = receiveddLiquids.Select(data => data.liquidName);
            contentList = "- " + string.Join("\n- ", contentNames);
        }
        else
        {
            contentList = "없음";
        }

        infoPanel.UpdateInfo(contentList);
    }
}