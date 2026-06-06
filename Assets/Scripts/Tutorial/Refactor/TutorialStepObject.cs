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

        root.Add(new HelpBox(
            "Chọn Tutorial Config trước, sau đó chọn Step từ dropdown. Phải gán Tutorial Config để manager filter tutorial.",
            HelpBoxMessageType.Info));

        var isCustomProp = serializedObject.FindProperty("isCustomStepId");
        var isCustomField = new PropertyField(isCustomProp);
        root.Add(isCustomField);

        var parentTutoProp = serializedObject.FindProperty("parentTutorial");
        var parentField = new PropertyField(parentTutoProp);
        root.Add(parentField);

        // stepId chỉ hiện khi isCustom = true
        var stepIdProp = serializedObject.FindProperty("stepId");
        var stepIdField = new PropertyField(stepIdProp);
        root.Add(stepIdField);

        // stepGuid ẩn, lưu giá trị thật
        var stepGuidProp = serializedObject.FindProperty("stepGuid");

        var dropdownContainer = new VisualElement();
        root.Add(dropdownContainer);

        void RebuildDropdown()
        {
            dropdownContainer.Clear();
            serializedObject.Update();

            bool isCustom = isCustomProp.boolValue;

            // Ẩn/hiện stepId field theo isCustom
            stepIdField.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
            dropdownContainer.style.display = isCustom ? DisplayStyle.None : DisplayStyle.Flex;

            if (isCustom) return;
            if (parentTutoProp.objectReferenceValue == null)
            {
                dropdownContainer.Add(new HelpBox("Chưa assign Tutorial Config", HelpBoxMessageType.Warning));
                return;
            }

            var configData = parentTutoProp.objectReferenceValue as TutorialConfig;
            if (configData == null) return;

            var options = configData.GetListStep();
            if (options.Count == 0)
            {
                dropdownContainer.Add(new HelpBox("Config chưa có step nào", HelpBoxMessageType.Warning));
                return;
            }

            // Tìm index hiện tại theo guid đang lưu
            var currentGuid = stepGuidProp.stringValue;
            var currentIndex = configData.GetIndexOfGuid(currentGuid);

            if (currentIndex < 0 && !string.IsNullOrEmpty(currentGuid))
            {
                dropdownContainer.Add(new HelpBox("Step đã bị xóa khỏi config, vui lòng chọn lại", HelpBoxMessageType.Error));
            }

            var dropdown = new DropdownField("Tutorial Sequence", options, Mathf.Max(currentIndex, 0));
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = options.IndexOf(evt.newValue);
                if (selectedIndex < 0) return;

                // Lưu guid, không lưu stepId
                stepGuidProp.stringValue = configData.GetGuidAtIndex(selectedIndex);
                serializedObject.ApplyModifiedProperties();
            });

            dropdownContainer.Add(dropdown);
        }

        parentField.RegisterValueChangeCallback(_ => RebuildDropdown());
        isCustomField.RegisterValueChangeCallback(_ => RebuildDropdown());
        RebuildDropdown();

        return root;
    }
}
public abstract class TutorialStepObject : MonoBehaviour
{
    [SerializeField] private string stepId;
    [SerializeField] private string stepGuid;
    
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

    public string GetStepGuidId()
    {
        return stepGuid;
    }
}