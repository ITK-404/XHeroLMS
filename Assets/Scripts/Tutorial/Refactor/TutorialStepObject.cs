using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(TutorialStepObject), true)]
public class TutorialStepObjectEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        var isActiveProperty = serializedObject.FindProperty("isCustomStepId");
        var toggle = new PropertyField(isActiveProperty);
        root.Add(toggle);
        
        var parentTutoProp = serializedObject.FindProperty("parentTutorial");
        root.Add(new PropertyField(parentTutoProp));
        
        var stepIdProp = serializedObject.FindProperty("stepId");

        root.Add(new PropertyField(stepIdProp));
        if (parentTutoProp.objectReferenceValue != null)
        {
            var configData = parentTutoProp.objectReferenceValue as TutorialConfig;
            if (configData != null)
            {
                var options = configData.GetListStep();
                
                var dropdown = new DropdownField(
                    "Tutorial Sequence",
                    options,
                    0); // index mặc định

                root.Add(dropdown);
            }
        }
        return root;
    }
}
public abstract class TutorialStepObject : MonoBehaviour
{
    [SerializeField] private string stepId;
    [SerializeField] private bool isCustomStepId;
    [SerializeField] private TutorialConfig parentTutorial;
    public TutorialConfig ParentTutorialConfig => parentTutorial;
    public string GetStepId()
    {
        return stepId;
    }

    private void Awake() => OnCustomAwake();
    private void OnDestroy() => OnCustomDestroy();

    protected virtual void OnCustomAwake(){}

    protected virtual void OnCustomDestroy(){}

    public virtual void OnEnter(){}
    public virtual void OnExit(){}
    public virtual void StartListening(Action onComplete) { }
    public virtual void StopListening() { }
}