using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Experiment Data", menuName = "NPC/Experiment Data")]
public class ExperimentData_G : ScriptableObject
{
    [Tooltip("실험의 이름 (예: PCR, 미생물 배양)")]
    public string ExperimentName;

    [Tooltip("이 실험에서 선택 가능한 샘플들의 목록")]
    public SampleData_G[] Samples;
}
