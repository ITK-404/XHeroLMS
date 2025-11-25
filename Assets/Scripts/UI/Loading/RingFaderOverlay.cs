using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RingFaderOverlay : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite centerSprite;
    public Sprite satelliteSprite;

    [Header("Layout")]
    public Vector2 centerSize = new Vector2(160, 160);
    public int satelliteCount = 16;
    public float radius = 140f;
    public Vector2 satelliteSize = new Vector2(48, 48);
    public float startAngleDeg = 90f;
    public bool faceInward = false;

    [Header("Fade")]
    public float cycleSeconds = 1.2f;
    [Range(0f, 1f)] public float minAlpha = 0.15f;
    [Range(0f, 1f)] public float maxAlpha = 1f;
    [Range(0f, 1f)] public float phaseStep = 1f / 16f;

    private readonly List<CanvasGroup> _cgs = new();
    private readonly List<Coroutine> _running = new();
    private bool _built;

    Image _centerImage;

    public void BuildAndPlay()
    {
        StopFades();
        Rebuild();
        StartFades();
        _built = true;
    }

    void OnEnable()
    {
        if (_built)
        {
            StartFades();
        }
        else
        {
            if (centerSprite || satelliteSprite) BuildAndPlay();
        }
    }

    void OnDisable() => StopFades();

    public void Resume()
    {
        if (!_built || _cgs.Count == 0 || transform.childCount == 0)
            Rebuild();
        StartFades();
        _built = true;
    }

    public void Rebuild()
    {
        var trash = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            trash.Add(transform.GetChild(i).gameObject);
        foreach (var t in trash) Destroy(t);

        _cgs.Clear();

        // Center icon
        if (centerSprite)
        {
            var go = new GameObject("center", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            _centerImage = go.GetComponent<Image>();
            _centerImage.sprite = centerSprite;
            _centerImage.preserveAspect = true;

            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = centerSize;
        }

        // Satellite dots
        if (satelliteSprite)
        {
            float step = 360f / Mathf.Max(1, satelliteCount);
            for (int i = 0; i < satelliteCount; i++)
            {
                var go = new GameObject($"satellite_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                var rt = go.GetComponent<RectTransform>();
                var img = go.GetComponent<Image>();
                var cg = go.GetComponent<CanvasGroup>();

                go.transform.SetParent(transform, false);

                img.sprite = satelliteSprite;
                img.preserveAspect = true;
                rt.sizeDelta = satelliteSize;

                float angle = startAngleDeg + i * step;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;

                if (faceInward)
                {
                    Vector2 dirToCenter = -pos.normalized;
                    float ang = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg;
                    rt.localRotation = Quaternion.Euler(0, 0, ang + 90f);
                }
                else
                {
                    Vector2 lookOut = pos.normalized;
                    float ang = Mathf.Atan2(lookOut.y, lookOut.x) * Mathf.Rad2Deg;
                    rt.localRotation = Quaternion.Euler(0, 0, ang - 90f);
                }

                cg.alpha = minAlpha;
                _cgs.Add(cg);
            }
        }
    }

    public void StartFades()
    {
        StopFades();
        for (int i = 0; i < _cgs.Count; i++)
        {
            float phase = i * phaseStep;
            _running.Add(StartCoroutine(FadeLoop(_cgs[i], phase)));
        }
    }

    public void StopFades()
    {
        foreach (var c in _running) if (c != null) StopCoroutine(c);
        _running.Clear();
    }

    IEnumerator FadeLoop(CanvasGroup cg, float phase01)
    {
        float twoPi = Mathf.PI * 2f;
        while (true)
        {
            if (!cg) yield break;
            float t = (Time.time / cycleSeconds + phase01) % 1f;
            float s = (Mathf.Sin(t * twoPi) + 1f) * 0.5f;
            cg.alpha = Mathf.Lerp(minAlpha, maxAlpha, s);
            yield return null;
        }
    }
}
