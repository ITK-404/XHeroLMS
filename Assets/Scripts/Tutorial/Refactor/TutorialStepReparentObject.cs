using UnityEngine;

public class TutorialStepReparentObject : TutorialStepObject
{
    [SerializeField] private Transform oldParent;
    [SerializeField] private RectTransform currentItem;
    [SerializeField] private Transform newParent;

    protected override void OnCustomAwake()
    {
        base.OnCustomAwake();
        oldParent = currentItem.transform.parent;
        newParent = TutorialStepManager.Instance.GetHighlightParent();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        currentItem.transform.SetParent(newParent,true);
    }

    public override void OnExit()
    {
        base.OnExit();
        currentItem.transform.SetParent(oldParent,true);
    }
}