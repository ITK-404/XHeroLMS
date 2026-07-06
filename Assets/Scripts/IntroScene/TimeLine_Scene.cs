using UnityEngine;
using TMPro;

public class TimeLine_Scene : MonoBehaviour
{
    public TextMeshProUGUI LineSys;

    public void SetLineText(string text)
    {
        if (LineSys != null)
            LineSys.text = text;
    }

    public void ClearLineText()
    {
        SetLineText(string.Empty);
    }
}
