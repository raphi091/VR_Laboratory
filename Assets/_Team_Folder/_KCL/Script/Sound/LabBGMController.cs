using UnityEngine;

public class LabBGMController : MonoBehaviour
{
    // 다른 스크립트(퀘스트/실험/미니게임 로직)에서 이 메서드들을 호출만 하면 됨.
    public void OnExperimentStart()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.ExperimentPhase, true);
    }

    public void OnExperimentSuccess()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.Success, false);
    }

    public void OnExperimentFailure()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.Failure, false);
    }

    public void OnDiscovery()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.Discovery, false);
    }

    public void OnBackToIdle()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.LabIdle, true);
    }

    public void OnGameEnding()
    {
        SoundManager.Instance?.PlayBGM(BGMTrackName.Ending, false);
    }
}
