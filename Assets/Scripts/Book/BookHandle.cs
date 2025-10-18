using DG.Tweening;
using System.Collections;
using UnityEngine;

public class BookHandle : MonoBehaviour
{
    [SerializeField] private Transform container;

    private void OnMouseEnter()
    {
        if (container == null)
        {
            Debug.LogError("Container is null");
            return;
        }
        Debug.Log("On Mouse Enter");
    }

    private void OnMouseExit()
    {
        if (container == null)
        {
            Debug.LogError("Container is null");
            return;
        }

        Debug.Log("On Mouse Exit");
        StopAllCoroutines();
        StartCoroutine(WaitDelay());
    }

    private IEnumerator WaitDelay()
    {
        container.DOKill();
        yield return new WaitForSeconds(0.1f);

        container.DORotate(Vector3.zero, 1);
    }
}