using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sub_DDCG_1",menuName = "SRTCourseData")]
public class SRTCourseData : ScriptableObject
{
    public string seoUrl;
    public List<SrtEntry> srtEntries = new();
    public TextAsset srtAsset;
    public AudioClip voiceClip;
    [Header("Raw Data")]
    [TextArea] public string Description;
    public float time = 30;

    [ContextMenu("Convert Data")]
    private void CreateData()
    {
        srtEntries = SrtReader.Parse(srtAsset);
        Debug.Log($"srt entries: " + srtEntries.Count);
    }
}
