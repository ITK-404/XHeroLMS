using UnityEngine;

public class DemoWebview : MonoBehaviour
{
    public UniWebView uniWebView;
    IWebOpener opener;

    private void Awake()
    {
        opener = new OpenWebviewService(uniWebView);
    }
    
    [ContextMenu("Open")]
    private void Open()
    {
        opener.Open("https://daotao.phongthuydainam.vn");
    }
}