using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_TutorialManager : MonoBehaviour
{
    private bool isCompleted = false;
    private NpcController_G npc;

    private void Awake()
    {
        npc=FindObjectOfType<NpcController_G>();
    }
}
