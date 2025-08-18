using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ActionType
{
    Move,
    Speak,
    WaitForPlayer,
    ListenForCompletion
}

[System.Serializable]
public class NpcAction
{
    [Tooltip("이 행동의 종류 (이동, 말하기, 듣기 등)")]
    public ActionType Type;

    [Tooltip("Move 타입일 때, 이동해야 할 목표 지점의 ID")]
    public string LocationID;

    [Tooltip("Speak 타입일 때, NPC가 말할 안내 대사")]
    [TextArea(3, 5)]
    public string Instruction;

    [Tooltip("ListenForCompletion 타입일 때, 플레이어가 말할 완료 키워드 목록")]
    public List<string> CompletionKeywords;
}

[CreateAssetMenu(fileName = "New Sample Data", menuName = "NPC/Sample Data")]
public class SampleData_G : ScriptableObject
{
    [Tooltip("샘플의 이름 (예: Sample A, Sample B)")]
    public string SampleName;

    [Tooltip("이 샘플의 실험을 구성하는 행동들의 순차적인 목록")]
    public NpcAction[] Actions;
}
