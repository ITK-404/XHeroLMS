using UnityEngine;

public interface IWebOpener
{
    OpenResult Open(string url);
    void Close();
}


public struct OpenResult
{
    public bool IsCompleted;
}

public class OpenBrowserService : IWebOpener
{
    public OpenResult Open(string url)
    {
        Application.OpenURL(url);
        OpenResult result = new();
        result.IsCompleted = true;

        return result;
    }

    public void Close()
    {
    }
}