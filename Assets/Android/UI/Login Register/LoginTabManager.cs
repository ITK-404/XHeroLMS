using UnityEngine;

public class LoginTabManager : MonoBehaviour
{
    private LoginTab[] loginTabs;
    public LoginTabType currentTabType;
    private void Awake()
    {
        loginTabs = GetComponentsInChildren<LoginTab>();
        Select(loginTabs[0]);
    }
    public void Select(LoginTab tab)
    {
        foreach(var item in loginTabs)
        {
            if(item.loginTabType == tab.loginTabType)
            {
                item.Select();
                item.isSelect = true;
            }
            else
            {
                item.UnSelect();
                item.isSelect = false;
            }
        }
        currentTabType = tab.loginTabType;
    }
}
public enum LoginTabType
{
    Login,
    Register,

}
