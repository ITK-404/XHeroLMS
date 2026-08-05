using UnityEngine;
using UnityEngine.UI;

public class StateVideo : MonoBehaviour
{
    public Button stateBtn;
    public Object panel;

    [SerializeField] private VideoPlayerControllerPro videoPlayerControllerPro;

    private bool _isPausedOverlayVisible;
    private bool _hasAppliedState;

    private void OnEnable()
    {
        _hasAppliedState = false;

        if (stateBtn != null)
            stateBtn.onClick.AddListener(PlayVideo);

        BindController();
        ApplyStateFromController();
    }

    private void OnDisable()
    {
        if (stateBtn != null)
            stateBtn.onClick.RemoveListener(PlayVideo);

        if (videoPlayerControllerPro != null && videoPlayerControllerPro.OnPlayStateChanged != null)
            videoPlayerControllerPro.OnPlayStateChanged.RemoveListener(OnPlayStateChanged);
    }

    private void Update()
    {
        if (videoPlayerControllerPro == null)
            BindController();

        // Poll as well as listening to the event because some external pause
        // paths call VideoPlayer.Pause() directly.
        ApplyStateFromController();
    }

    private void BindController()
    {
        if (videoPlayerControllerPro == null)
            videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();

        if (videoPlayerControllerPro == null)
            return;

        if (videoPlayerControllerPro.OnPlayStateChanged == null)
            return;

        // Remove first so OnEnable/late binding can never subscribe twice.
        videoPlayerControllerPro.OnPlayStateChanged.RemoveListener(OnPlayStateChanged);
        videoPlayerControllerPro.OnPlayStateChanged.AddListener(OnPlayStateChanged);
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        if (isPlaying)
        {
            ApplyState(false);
            return;
        }

        ApplyStateFromController();
    }

    private void ApplyStateFromController()
    {
        if (videoPlayerControllerPro == null)
        {
            ApplyState(false);
            return;
        }

        bool showPausedState = videoPlayerControllerPro.HasActiveVideo &&
                               !videoPlayerControllerPro.IsVideoPlaying;
        ApplyState(showPausedState);
    }

    private void ApplyState(bool showPausedState)
    {
        if (_hasAppliedState && _isPausedOverlayVisible == showPausedState)
            return;

        _hasAppliedState = true;
        _isPausedOverlayVisible = showPausedState;

        GameObject panelObject = GetPanelObject();
        if (panelObject != null && panelObject != gameObject)
            panelObject.SetActive(showPausedState);

        if (stateBtn != null && stateBtn.gameObject != gameObject)
            stateBtn.gameObject.SetActive(showPausedState);
        else if (stateBtn != null)
            stateBtn.interactable = showPausedState;
    }

    private GameObject GetPanelObject()
    {
        if (panel is GameObject gameObject)
            return gameObject;

        if (panel is Component component)
            return component.gameObject;

        return null;
    }

    private void PlayVideo()
    {
        if (videoPlayerControllerPro == null)
            BindController();

        videoPlayerControllerPro?.PlayVideo();
    }
}
