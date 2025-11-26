using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    public string originalScene = "New Scene";
    public AudioSource musicSource;
    [Serializable]
    public struct BGMConfig
    {
        public AudioClip clip;
        public float volume;
    }
    public BGMConfig[] bgmClips;
    public int index = 0;

    [Tooltip("Default fade duration used by Pause/Resume if no duration supplied")]
    public float fadeDuration = 1f;

    // track intended volume for current clip so Resume can fade back to it
    private float targetVolume = 1f;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (SceneManager.GetActiveScene().name == originalScene)
        {
            PlayRandomMusic();
        }
    }

    private void PlayMusic(AudioClip clip, float volume)
    {
        if (musicSource == null) return;

        musicSource.clip = clip;
        targetVolume = volume;
        musicSource.volume = targetVolume;
        musicSource.Play();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Change next music");
            GoNextMusic();
        }
    }

    private void PlayRandomMusic()
    {
        if (bgmClips == null || bgmClips.Length == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, bgmClips.Length);
        var getRandomMusic = bgmClips[randomIndex];

        index = randomIndex;
        PlayMusic(getRandomMusic.clip, getRandomMusic.volume);
    }

    private void GoNextMusic()
    {
        if (bgmClips == null || bgmClips.Length == 0) return;

        index = (index + 1) % bgmClips.Length;

        var music = bgmClips[index];
        PlayMusic(music.clip, music.volume);
        // index already advanced and wrapped above
    }

    // Public API: pause with fade to 0 over duration seconds
    public void PauseWithFade(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(PauseFadeCoroutine(Mathf.Max(0f, duration)));
    }

    // overload: use default fadeDuration
    public void PauseWithFade() => PauseWithFade(fadeDuration);

    // Public API: resume and fade from current volume to targetVolume over duration seconds
    public void ResumeWithFade(float duration)
    {
        if (musicSource == null) return;

        // If clip not set, nothing to resume
        if (musicSource.clip == null) return;

        // If already playing, just ensure volume fades to target
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        // If paused, unpause first then fade in
        if (!musicSource.isPlaying && musicSource.time > 0f)
            musicSource.UnPause();
        else if (!musicSource.isPlaying)
            musicSource.Play();

        fadeCoroutine = StartCoroutine(ResumeFadeCoroutine(Mathf.Max(0f, duration)));
    }

    // overload: use default fadeDuration
    public void ResumeWithFade() => ResumeWithFade(fadeDuration);

    private IEnumerator PauseFadeCoroutine(float duration)
    {
        if (musicSource == null) yield break;

        float start = musicSource.volume;
        float t = 0f;
        if (duration <= 0f)
        {
            musicSource.volume = 0f;
            musicSource.Pause();
            fadeCoroutine = null;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Pause();
        fadeCoroutine = null;
    }

    private IEnumerator ResumeFadeCoroutine(float duration)
    {
        if (musicSource == null) yield break;

        float start = musicSource.volume;
        float t = 0f;
        if (duration <= 0f)
        {
            musicSource.volume = targetVolume;
            fadeCoroutine = null;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(start, targetVolume, t / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}

