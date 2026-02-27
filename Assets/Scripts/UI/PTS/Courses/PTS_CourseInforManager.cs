using UnityEngine;
using UnityEngine.UI;

public class PTS_CourseInforManager : MonoBehaviour
{
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private PanelBaseUI[] panelBaseUIList;

    private void Start()
    {
        Binding();
        ShowDefault();
    }

    public void ShowDefault()
    {
        toggles[0].isOn = true;
    }
    
    private void Binding()
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            var toggle = toggles[i];
            var index = i;
            toggle.onValueChanged.AddListener((isOn) =>
            {
                ShowPanel(index);
            });
        }
    }
    
    private void ShowPanel(int index)
    {
        var newPanel = panelBaseUIList[index];
        newPanel.Show();

        foreach (var item in panelBaseUIList)
        {
            if (item != newPanel)
            {
                item.Hide();
            }
        }
    }
}