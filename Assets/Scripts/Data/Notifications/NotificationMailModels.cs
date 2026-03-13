using System;
using System.Collections.Generic;

[Serializable]
public class NotificationMailResponse
{
    public bool status;
    public NotificationMailDataWrap data;
}

[Serializable]
public class NotificationMailDataWrap
{
    public NotificationUnreadData totalUnread;
    public int total;
    public List<NotificationMailItem> data;
}

[Serializable]
public class NotificationUnreadData
{
    public string all;
    public string personal;
    public string system;
    public string merchant;
}

[Serializable]
public class NotificationMailItem
{
    public string _id;
    public string to;
    public string label;
    public string title;
    public string text;
    public string link;
    public string iconKey;
    public bool isRead;
    public NotificationMailTime time;
    public string icon;
}

[Serializable]
public class NotificationMailTime
{
    public string day;
    public string time;
    public string key;
}