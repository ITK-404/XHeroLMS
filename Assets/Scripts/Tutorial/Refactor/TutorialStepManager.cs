using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-99)]
public class TutorialStepManager : MonoBehaviour
{
    public static TutorialStepManager Instance;

    [SerializeField] private TutorialConfig config;
    [SerializeField] private int currentStepIndex = 0;

    private TutorialStepObject[] stepObjects;

    private int maxTutorialStepCount => config != null ? config.GetStepCount() : 0;

    [SerializeField] private Transform highlightParent;
    public Transform GetHighlightParent() => highlightParent;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        MappingObjects();

        currentStepIndex = 0;
        ShowNextStep(currentStepIndex);
    }


    private void MappingObjects()
    {
        var originalTemps = FindObjectsByType<TutorialStepObject>(FindObjectsSortMode.InstanceID).ToList();
        var temps = originalTemps.Where(item => item.ParentTutorialConfig == config).ToList();

        stepObjects = new TutorialStepObject[config.GetStepCount()];
        for (int i = 0; i < temps.Count; i++)
        {
            var tempStep = temps[i];
            var correctIndex = config.GetIndexOfStep(tempStep.GetStepId());
            if (correctIndex == TutorialConfig.NON_EXIT_INDEX)
            {
                continue;
            }

            stepObjects[correctIndex] = tempStep;
        }
    }


    public void ShowNextStep(int currentStepIndex)
    {
        if (currentStepIndex > maxTutorialStepCount - 1)
        {
            Debug.Log("Complete");
            return;
        }

        // info to view to show next step ma
        Debug.Log("Start Step: " + currentStepIndex);
        var stepObject = stepObjects[currentStepIndex];
        stepObject.OnEnter();
        stepObject.StartListening(() =>
        {
            stepObject.StopListening();
            stepObject.OnExit();
            ShowNextStep(currentStepIndex + 1);
        });
    }

    public void SetTutorialConfig(TutorialConfig _config)
    {
        config = _config;
        currentStepIndex = 0;
    }
}