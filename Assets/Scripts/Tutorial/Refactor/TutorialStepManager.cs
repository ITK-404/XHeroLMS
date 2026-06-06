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

        StartTutorial();
    }


    private void MappingObjects()
    {
        var originalTemps = FindObjectsByType<TutorialStepObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
    
        Debug.Log($"Total TutorialStepObject in scene: {originalTemps.Count}");
        Debug.Log($"Config instance ID: {config.GetInstanceID()}");
    
        foreach (var item in originalTemps)
            Debug.Log($"{item.name} - config ID: {item.ParentTutorialConfig?.GetInstanceID()}");

        var temps = originalTemps.Where(item => item.ParentTutorialConfig == config).ToList();
        Debug.Log($"Filtered by config: {temps.Count}");

        stepObjects = new TutorialStepObject[config.GetStepCount()];
    
        for (int i = 0; i < temps.Count; i++)
        {
            var tempStep = temps[i];
        
            if (tempStep == null)
            {
                Debug.LogWarning($"temps[{i}] is null, skipping");
                continue;
            }

            var correctIndex = config.GetIndexOfStep(tempStep.GetStepGuidId());
        
            if (correctIndex == TutorialConfig.NON_EXIT_INDEX)
            {
                Debug.LogWarning($"NON_EXIT_INDEX: {tempStep.name} | StepId: {tempStep.GetStepId()}", tempStep.gameObject);
                continue;
            }

            if (stepObjects[correctIndex] != null)
            {
                Debug.LogWarning($"Index {correctIndex} bị overwrite! Old: {stepObjects[correctIndex].name} → New: {tempStep.name}");
            }

            stepObjects[correctIndex] = tempStep;
        }

        for (int i = 0; i < stepObjects.Length; i++)
        {
            if (stepObjects[i] == null)
                Debug.LogWarning($"stepObjects[{i}] is null after mapping");
            else
                Debug.Log($"stepObjects[{i}] = {stepObjects[i].name}");
        }
    }
    
    public void StartTutorial()
    {
        currentStepIndex = 0;
        EnterCurrentStep();
    }

    private void EnterCurrentStep()
    {
        if (currentStepIndex >= maxTutorialStepCount)
        {
            // CompleteTutorial();
            Debug.Log("Complete");
            return;
        }

        var step = stepObjects[currentStepIndex];

        Debug.Log($"[Tutorial] Enter Step {currentStepIndex}");
        if (step == null)
        {
            Debug.Log($"Step is null, please checking");
        }
        step.OnEnter();

        step.StartListening(OnStepCompleted);
    }

    private void OnStepCompleted()
    {
        var step = stepObjects[currentStepIndex];

        Debug.Log($"[Tutorial] Exit Step {currentStepIndex}");

        step.StopListening();
        step.OnExit();

        currentStepIndex++;

        EnterCurrentStep();
    }

    // public void ShowNextStep(int currentStepIndex)
    // {
    //     if (currentStepIndex > maxTutorialStepCount - 1)
    //     {
    //         Debug.Log("Complete");
    //         return;
    //     }
    //
    //     // info to view to show next step ma
    //     Debug.Log("Start Step: " + currentStepIndex);
    //     var stepObject = stepObjects[currentStepIndex];
    //     stepObject.OnEnter();
    //     stepObject.StartListening(() =>
    //     {
    //         stepObject.StopListening();
    //         stepObject.OnExit();
    //         ShowNextStep(currentStepIndex + 1);
    //     });
    // }

    public void SetTutorialConfig(TutorialConfig _config)
    {
        config = _config;
        currentStepIndex = 0;
    }
}