using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(TutorialStepObject), true)]
[CanEditMultipleObjects] 
public class TutorialStepObjectEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        
        var helpBox = new HelpBox("Chọn Tutorial Config trước, sau đó chọn Step từ dropdown, phải gán tutorial config để manager filer tutorial", HelpBoxMessageType.Info);
        root.Add(helpBox);
        
        
        
        var isActiveProperty = serializedObject.FindProperty("isCustomStepId");
        var isActiveField = new PropertyField(isActiveProperty);
        root.Add(isActiveField);
        
        var parentTutoProp = serializedObject.FindProperty("parentTutorial");
        var parentField = new PropertyField(parentTutoProp);
        root.Add(parentField);
        
        var stepIdProp = serializedObject.FindProperty("stepId");
        root.Add(new PropertyField(stepIdProp));

        var dropdownContainer = new VisualElement();
        root.Add(dropdownContainer);

        void RebuildDropdown()
        {
            Debug.Log($"Editor rebuild dropdown");
            dropdownContainer.Clear();

            serializedObject.Update();
            if (isActiveProperty.boolValue)
            {
                stepIdProp.stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
                return;
            }
            if (parentTutoProp.objectReferenceValue == null) return;
            
            var configData = parentTutoProp.objectReferenceValue as TutorialConfig;
            if (configData == null) return;

            Debug.Log($"Editor Thử vẽ dropdown");
            
            var options = configData.GetListStep();

            var dropdown = new DropdownField("Tutorial Sequence", options, 0);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = options.IndexOf(evt.newValue);
                stepIdProp.stringValue = options[selectedIndex];
                serializedObject.ApplyModifiedProperties();
            });
            dropdownContainer.Add(dropdown);
        }
        parentField.RegisterValueChangeCallback(_ => RebuildDropdown());
        isActiveField.RegisterValueChangeCallback(_ => RebuildDropdown());
        // Chạy lần đầu
        RebuildDropdown();
        
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