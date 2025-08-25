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

    private bool isTutorialCompleted = false;
    private NpcController_G npc;
    
    public bool IsTutorialCompleted => isTutorialCompleted;

    protected override void Awake()
    {
        base.Awake();
        npc=FindObjectOfType<NpcController_G>();
    }

    private void Start()
    {
        C_DataManager.instance.LoadGameData();
    }

    private void OnEnable()
    {
        npc.OnExperimentEnd += OnExperimentCompleted;
    }
    
    private void OnDisable()
    {
        npc.OnExperimentEnd -= OnExperimentCompleted;
    }
    
    private void OnExperimentCompleted(NpcController_G.NpcMode mode)
    {
        if (mode.Equals(NpcController_G.NpcMode.Tutorial))
        {
            isTutorialCompleted = true;
            SceneManager.LoadScene("Main");
        }
    }
    
    void OnApplicationQuit()
    {
        C_DataManager.instance.SaveGameData();
    }
}
