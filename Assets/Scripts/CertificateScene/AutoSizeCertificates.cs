using UnityEngine;

public class AutoSizeCertificates : MonoBehaviour
{
    [SerializeField] private Transform frame;
    [SerializeField] private Transform certificates;
    private void Update()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        
        if (Mathf.Abs(aspectRatio - 1f) < 0.05f)
        {
            transform.localScale = Vector3.one * .7f;
        }
        else if (aspectRatio > 1f)
        {
            transform.localScale = Vector3.one * 0.6f;
        }

        certificates.transform.localScale = frame.gameObject.activeSelf ? Vector3.one * 1.8f: Vector3.one * 2.5f;
    }
}