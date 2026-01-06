using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookViewUI : MonoBehaviour
{
    public Button enterCourseBtn;
    public Button buyCourseBtn;
    public Color leftColor;
    public Color rightColor;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI fullPriceText;

    private void Awake()
    {
        ShowEnterCourse();
    }
    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        
    }
    
    private void OnTextChanged(Object obj){
        if (obj == priceText)
        {
            RefreshColor();
        }
    }

    [ContextMenu("Refresh Color")]
    public void RefreshColor()
    {
        if (!isActiveAndEnabled) return;
        StartCoroutine(DelayOneFrame());
    }

    private IEnumerator DelayOneFrame()
    {
        yield return null;
        LocalRefreshColor();
    }

    private void LocalRefreshColor()
    {
        var tmp = priceText;
        tmp.ForceMeshUpdate();
        var textInfo = tmp.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
            var vertices = textInfo.meshInfo[meshIndex].colors32;

            float charPosX = (textInfo.characterInfo[i].bottomLeft.x + textInfo.characterInfo[i].topRight.x) / 2;
            float t = Mathf.InverseLerp(tmp.bounds.min.x, tmp.bounds.max.x, charPosX);
            Color32 color = Color32.Lerp(leftColor, rightColor, t);

            vertices[vertexIndex + 0] = color;
            vertices[vertexIndex + 1] = color;
            vertices[vertexIndex + 2] = color;
            vertices[vertexIndex + 3] = color;
        }

        for (int i = 0; i < tmp.textInfo.meshInfo.Length; i++)
        {
            tmp.textInfo.meshInfo[i].mesh.colors32 = tmp.textInfo.meshInfo[i].colors32;
            tmp.UpdateGeometry(tmp.textInfo.meshInfo[i].mesh, i);
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

    }

    public void ShowEnterCourse()
    {
        enterCourseBtn.gameObject.SetActive(true);
        buyCourseBtn.gameObject.SetActive(false);
    }

    public void ShowBuyCourseButton()
    {
        enterCourseBtn.gameObject.SetActive(false);
        buyCourseBtn.gameObject.SetActive(true);
    }
}
