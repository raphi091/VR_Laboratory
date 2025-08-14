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

    [Tooltip("Move 타입일 때, NPC가 이동해야 할 목표 지점")]
    public Transform TargetTransform;

    [Tooltip("Speak 타입일 때, NPC가 말할 안내 대사")]
    [TextArea(3, 5)]
    public string Instruction;

    [Tooltip("ListenForCompletion 타입일 때, 플레이어가 말할 완료 키워드 목록")]
    public List<string> CompletionKeywords;
}


[CreateAssetMenu(fileName = "New Experiment Data", menuName = "NPC/Experiment Data")]
public class ExperimentData : ScriptableObject
{
    [Tooltip("실험의 이름 (예: PCR, 미생물 배양)")]
    public string ExperimentName;

    [Tooltip("실험을 구성하는 행동들의 순차적인 목록")]
    public NpcAction[] Actions;
}
