public class OpenWebviewService : IWebOpener
{
    private UniWebView uniWebView;

    public OpenWebviewService(UniWebView uniWebView)
    {
        this.uniWebView = uniWebView;
    }

    public OpenResult Open(string url)
    {
        uniWebView.Load(url);
        uniWebView.Show();
        OpenResult result = new();
        result.IsCompleted = true;
        return result;
    }

    public void Close()
    {
        uniWebView.Hide();
    }
}