using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [SerializeField] private Button chairButton;
    
    protected override FlowNode CreateFlow()
    {
        // // block all
        // var clickChair = new WaitForTutorialEventNode(TutorialStepId.ClickChair);
        // var moveToChair = new WaitForTutorialEventNode(TutorialStepId.MoveToChairComplete);
        // var sitDown = new WaitForTutorialEventNode(TutorialStepId.SitDown);
        // var openCourse = new WaitForTutorialEventNode(TutorialStepId.OpenCourse);
        // var wait15Seconds = new WaitForSecondsNode(15f);
        // var closeCourse = new WaitForTutorialEventNode(TutorialStepId.CloseCourse);
        // var standUp = new WaitForTutorialEventNode(TutorialStepId.StandUp);
        //
        // clickChair.AddTransition(NodeResult.Completed, moveToChair);
        // moveToChair.AddTransition(NodeResult.Completed, sitDown);
        // sitDown.AddTransition(NodeResult.Completed, openCourse);
        // sitDown.AddTransition(NodeResult.Completed, openCourse);
        // openCourse.AddTransition(NodeResult.Completed, wait15Seconds);
        // wait15Seconds.AddTransition(NodeResult.Completed, closeCourse);
        // closeCourse.AddTransition(NodeResult.Completed, standUp);

        return clickChair;
    }
}
