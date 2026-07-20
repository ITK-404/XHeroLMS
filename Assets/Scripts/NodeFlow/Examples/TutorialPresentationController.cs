using System.Collections.Generic;
using UnityEngine;

public class TutorialPresentationController : MonoBehaviour
{
    [SerializeField]
    private List<TutorialStepPresentation> presentations;

    private void Init()
    {
        presentations.Clear();  
    }
    
    public void Show(string tutorialStepID)
    {
        foreach (var item in presentations)
        {
            if (item.StepId == tutorialStepID)
            {
                // show this
            }
            else
            {
                // hide this
            }
        }
    }
}