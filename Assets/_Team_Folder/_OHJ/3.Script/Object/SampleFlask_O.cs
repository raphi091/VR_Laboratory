using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SampleFlask_O : MonoBehaviour
{
    [Header("액체 담기")]
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
        while(i <liquids.Count)
        {
            var liquid = liquids[i];

            //겔 플라스크라면
            if(isGel)
            {
                if(liquid.type != PourableType.Agar && liquid != SYBR_DYE)
                {
                    Debug.LogWarning("겔 플라스크에는 넣을 수 없습니다");
                    liquids.RemoveAt(i);
                    continue;
                }
            }

            //샘플플라스크
            else
            {
                if(liquid.type == PourableType.Agar || liquid == SYBR_DYE)
                {
                    Debug.LogWarning("샘플 플라스크에는 넣을 수 없습니다.");
                    liquids.RemoveAt(i);
                    continue;
                }
            }

            //중복 불가능
            if(receiveddLiquids.Contains(liquid))
            {
                Debug.LogWarning($"중복된 액체입니다. {liquid.name}은 이미 있습니다");
                liquids.RemoveAt(i);
                continue;
            }

            // DNA 타입은 하나만 허용
            if(liquid.type == PourableType.DNA && receiveddLiquids.Any(ld => ld.type == PourableType.DNA))
            {
                Debug.LogWarning("DNA는 한 종류만 넣을 수 있습니다");
                liquids.RemoveAt(i);
                continue;
            }

            // 유효한 액체
            receiveddLiquids.Add(liquid);
            isAddsuccess = true;
            UpdateInfoPanel();
            Debug.Log($"{liquid.name} 플라스크에 추가");

            i++;
        }

        // 모든 액체 다 넣었는지 개수로 판별?
        //샘플 플라스크 => 염색약 섞기 가능
        if(!isGel && receiveddLiquids.Count >= 6)
        {
            ispossibleMix = true;
            Debug.Log("샘플 플라스크에 염색약을 섞을 수 있습니다.");
        }

        // 겔 플라스크 => 붓기, 염색약 섞기 가능
        else if(isGel && receiveddLiquids.Count >= 2)
        {
            ispossibleMix = true;
            ispossiblePour = true;
            Debug.Log("이제 염색약 섞기랑 붓기가 가능합니다");
        }

        else
        {
            ispossibleMix = false;
            ispossiblePour = false;
        }

        UpdateInfoPanel();
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