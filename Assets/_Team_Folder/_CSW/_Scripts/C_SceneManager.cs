using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class C_SceneManager : Ch_BehaviourSingleton<C_SceneManager>
{
    protected override bool IsDontdestroy()
    {
        return true;
    }
    
    private NpcController_G npc;

    protected override void Awake()
    {
        base.Awake();
        npc = FindObjectOfType<NpcController_G>();
    }

    private void OnEnable()
    {
        npc.OnExperimentEnd += OnExperimentCompleted;
        C_DataManager.I.LoadGameData();
    }
    
    private void OnDisable()
    {
        npc.OnExperimentEnd -= OnExperimentCompleted;
    }
    
    private void OnExperimentCompleted(NpcController_G.NpcMode mode)
    {
        if (mode.Equals(NpcController_G.NpcMode.Tutorial))
        {
            Debug.Log("Tutorial Completed");
            C_DataManager.I.gameData.IsTutorialCompleted = true;
            SceneManager.LoadScene("Main");
        }
    }
    
    void OnApplicationQuit()
    {
        C_DataManager.I.SaveGameData();
    }
}
