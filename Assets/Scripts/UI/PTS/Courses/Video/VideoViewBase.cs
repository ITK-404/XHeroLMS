using UnityEngine;
using UnityEngine.UI;

public abstract class VideoViewBase : MonoBehaviour
{
    [SerializeField] protected RawImage targetRawImage;

    private VideoPlayerCore _core;
    protected VideoPlayerCore Core => _core;

    // ─────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────

    public void SetCore(VideoPlayerCore core)
    {
        _core = core;
    }

    // ─────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────

    protected virtual void OnEnable()
    {
        if (_core != null)
            Bind();
    }

    protected virtual void OnDisable()
    {
        Unbind();
    }

    // ─────────────────────────────────────────
    // Bind / Unbind
    // ─────────────────────────────────────────

    private void Bind()
    {
        _core.OnTextureReady  += HandleTextureReady;
        _core.OnStateChanged  += HandleStateChanged;
        _core.OnBannerLoaded  += HandleBannerLoaded;
        _core.OnVideoFinished += HandleVideoFinished;

        // Sync trạng thái hiện tại ngay khi bind
        var model = _core.GetCurrentModel();
        if (model.IsPrepared)
            HandleTextureReady(_core.GetRenderTexture());

        HandleStateChanged(model);
    }

    private void Unbind()
    {
        if (_core == null) return;

        _core.OnTextureReady  -= HandleTextureReady;
        _core.OnStateChanged  -= HandleStateChanged;
        _core.OnBannerLoaded  -= HandleBannerLoaded;
        _core.OnVideoFinished -= HandleVideoFinished;
    }

    // ─────────────────────────────────────────
    // Handlers — override nếu cần
    // ─────────────────────────────────────────

    protected virtual void HandleTextureReady(RenderTexture rt)
    {
        if (targetRawImage != null)
            targetRawImage.texture = rt;
    }

    protected virtual void HandleBannerLoaded(Texture banner)
    {
        if (targetRawImage != null)
            targetRawImage.texture = banner;
    }

    protected virtual void HandleVideoFinished() { }

    // Subclass bắt buộc implement
    protected abstract void HandleStateChanged(VideoPlayerModel model);
}