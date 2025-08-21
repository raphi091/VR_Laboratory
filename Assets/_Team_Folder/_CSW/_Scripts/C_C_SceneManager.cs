using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class C_SceneManager : MonoBehaviour
{
    private bool isCompleted = false;
    public bool IsCompleted => isCompleted;
    private NpcController_G npc;

    private void Awake()
    {
        npc=FindObjectOfType<NpcController_G>();
    }

    void Start()
    {
        StartCoroutine(CheckTutorialCompleted());
    }
    
    IEnumerator CheckTutorialCompleted()
    {
        yield return new WaitUntil(()=>npc.isTutorialComplete);
        yield return new WaitForSeconds(3f);
        OnTutorialCompleted();
    }
    
    private void OnTutorialCompleted()
    {
        isCompleted = true;
        SceneManager.LoadScene("Main");
    }
}
