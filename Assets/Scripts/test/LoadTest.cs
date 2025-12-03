using UnityEngine;
using UnityEngine.UI;

public class LoadTest: MonoBehaviour
{
    public string nameScene = "testScene";
    public Button button;

    public void Start()
    {
        button.onClick.AddListener(LoadScene);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(LoadScene);
    }

    private void LoadScene()
    {
        LoadingTransition.Load(nameScene);
    }
}
