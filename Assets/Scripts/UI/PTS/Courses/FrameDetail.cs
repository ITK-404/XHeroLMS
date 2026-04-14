using UnityEngine;

public class FrameDetail : MonoBehaviour
{
    [SerializeField] private GameObject decorImg;
    [SerializeField] private GameObject emptyUI;

    public void ActiveEmptyUI(bool isEmpty)
    {
        emptyUI.gameObject.SetActive(isEmpty);
        decorImg.gameObject.SetActive(!isEmpty);
    }
}