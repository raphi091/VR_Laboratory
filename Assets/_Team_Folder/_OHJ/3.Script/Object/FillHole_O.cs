using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

// 겔의 구멍
// 어떤 데이터가 들어있는지를 저장한다
public class FillHole_O : MonoBehaviour
{
    [Header("구멍들")]
    public List<MeshRenderer> holerenders = new List<MeshRenderer>();
    
    [Header("구멍에 들어간 액체 데이터")]
    public List<LiquidData_L> receivedLiquids = new List<LiquidData_L>();

    // 구멍마다 들어간 액체 기록
    public List<LiquidData_L> holeLiquidData = new List<LiquidData_L>();

    private void Awake()
    {
        // 초기화 : 구멍 수만큼 리스트 맞춰주기
        holeLiquidData = new List<LiquidData_L>(new LiquidData_L[holerenders.Count]);
    }

    // 구멍에 액체 넣고 색 바꾸고 DNA가 들어있을 시 DNA 정보를 넣기
    public void ReceiveLiquidAtHole(int index, List<LiquidData_L> liquids)
    {
        if (index < 0 || index >= holerenders.Count)
        {
            Debug.LogError("인덱스 구멍이 잘못되었습니다.");
            return;
        }

        // 데이터 저장 (DNA가 다르기 때문에 DNA 타입이 저장되도록 하기)
        // FirstOrDefault : 첫번째 반환
        LiquidData_L DNA = liquids.FirstOrDefault(l => l.type == PourableType.DNA);

        //구멍에 데이터 저장
        //DNA가 있으면 DNA가 저장되도록 하기
        if (DNA != null)
        {
            holeLiquidData[index] = DNA;
        }

        else
        {
            holeLiquidData[index] = liquids[0];
            Debug.Log("DNA가 없기 때문에 0번째 데이터를 추가합니다.");
        }

        // 색 바꾸기
        holerenders[index].material.color = Color.blue;

        // 전체 received목록 갱신
        receivedLiquids.Clear();
        foreach (var l in holeLiquidData)
        {
            if (l != null && !receivedLiquids.Contains(l))
            {
                receivedLiquids.Add(l);
            }
        }
    }


    public void SaveLiquidData(List<LiquidData_L> liquid)
    {
        receivedLiquids = new List<LiquidData_L>(liquid);
    }
}