using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContainClassGroupUI : MonoBehaviour
{
    [SerializeField] private Button attandBtn;
    [SerializeField] private TextMeshProUGUI attendDateTmp;
    [SerializeField] private Transform noteText;

    public void Show(bool classContainDate)
    {
        gameObject.SetActive(true);
        attandBtn.gameObject.SetActive(classContainDate == false);
        noteText.gameObject.SetActive(classContainDate == false);
        attendDateTmp.gameObject.SetActive(classContainDate);
    } 
    public void Hide() => gameObject.SetActive(false);
}