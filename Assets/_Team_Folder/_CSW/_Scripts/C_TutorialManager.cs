using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C_TutorialManager : Ch_BehaviourSingleton<C_TutorialManager>
{
    protected override bool IsDontdestroy()
    {
        return true;
    }

    bool isCompleted = false;

    void Start()
    {
        ParseEventArgs parseEventArgs=new ParseEventArgs();
        parseEventArgs.fromTool = null;
        parseEventArgs.toTool = null;
        C_ExperimentDataParser.I.DataParsed.Invoke(parseEventArgs);
    }
}
