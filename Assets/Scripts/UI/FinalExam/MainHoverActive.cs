using UnityEngine;
using UnityEngine.EventSystems;

public class MainHoverActive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ExamInfoElement examInfoElement;
    private void Awake()
    {
        if (examInfoElement == null)
        {
            examInfoElement = GetComponentInParent<ExamInfoElement>();
        }

    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        examInfoElement.ActiveHover(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        examInfoElement.ActiveHover(false);
    }
}
