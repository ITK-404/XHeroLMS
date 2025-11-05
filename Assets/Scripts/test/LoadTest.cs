using UnityEngine;
using UnityEngine.UI;

public class LoadTest: MonoBehaviour
{
    public string nameScene = "testScene";
    public Button button;

    public void Start()
    {
        button.onClick.AddListener(() => LoadingTransition.Load(nameScene));
    }
}
