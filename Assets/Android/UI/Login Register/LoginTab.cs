using UnityEngine;
using UnityEngine.UI;

public class LoginTab : MonoBehaviour
{
    public LoginTabType loginTabType;
    public Button btn;
    public Image unSelectSprite;
    public Image selectSprite;

    private LoginTabManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<LoginTabManager>();
        btn.onClick.AddListener(OnSelectThis);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnSelectThis);
    }

    private void OnSelectThis()
    {
        if (manager != null)
        {
            Debug.Log("On Item changed");
            manager.Select(this);
        }
    }

    public void Select()
    {
        selectSprite.gameObject.SetActive(true);

    }

    public void UnSelect()
    {
        selectSprite.gameObject.SetActive(false);
    }

    public bool isSelect = false;
}