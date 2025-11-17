using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[CreateAssetMenu(fileName = "Book Cover Info",menuName = "Book Cover Database")]
public class BookCoverDatabase : ScriptableObject
{
    public string bookTag = "book_cover_sku";
    public string[] bookCoverList;
    public string sourcePath = "Sach/Texture";
    public List<string> paths = new();

    // Call from inspector (right-click the asset) or it runs in editor when values change
    [ContextMenu("Find All Assets")]
    public void FindAllAsset()
    {
        var textures = Resources.LoadAll<Texture2D>(sourcePath) ?? new Texture2D[0];
        bookCoverList = textures.Select(t => t.name).ToArray();

        paths = new List<string>(textures.Length);
        foreach (var t in textures)
            paths.Add($"{sourcePath}/{t.name}");
    }

#if UNITY_EDITOR
    // Refresh automatically in editor when values change
    private void OnValidate()
    {
        // Avoid doing work at runtime
        if (!Application.isPlaying)
            FindAllAsset();
    }
#endif
}

