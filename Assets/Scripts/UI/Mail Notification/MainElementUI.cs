using System;
using UnityEngine;
using UnityEngine.UI;

public class MainElementUI : MonoBehaviour
{
    [SerializeField] private MailTextConfig readConfig;
    [SerializeField] private MailTextConfig unreadConfig;
    [SerializeField] private bool isUnread = false;
    [SerializeField] private MailElementVisualUI visual;
    [SerializeField] private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if(btn)
            btn.onClick.AddListener(TestState);
    }

    private void OnDestroy()
    {
        if(btn)
            btn.onClick.RemoveListener(TestState);
    }

    private void TestState()
    {
        if (isUnread == false)
        {
            isUnread = true;
        }
        visual.SetConfig(isUnread ? readConfig : unreadConfig);
    }

    private void OnValidate()
    {
        if (visual == null)
        {
            visual = GetComponent<MailElementVisualUI>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (visual)
        {
            visual.SetConfig(isUnread ? unreadConfig : readConfig);
        }
    }
}