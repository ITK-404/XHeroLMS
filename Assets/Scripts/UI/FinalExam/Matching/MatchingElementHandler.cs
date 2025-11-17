using UnityEngine;

public class MatchingElementHandler : MonoBehaviour
{
    public static MatchingElementHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnDroppedOnto(MatchingElement source, MatchingElement target)
    {
        Debug.Log($"{source.name} dropped onto {target.name}");
    }
}