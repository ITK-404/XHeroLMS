using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "Book Cover Info", menuName = "Book Cover Database")]
public class BookCoverDatabase : ScriptableObject
{
    [Serializable]
    public struct BookReferences
    {
        public string book_sku;
        public string bookPath;
    }

    public string bookTag = "book_cover_sku";
    public BookReferences[] bookCoverList;
    public string sourcePath = "Sach/Texture";
    private List<string> paths = new();

    // Call from inspector (right-click the asset) or it runs in editor when values change
    [ContextMenu("Find All Assets")]
    public void FindAllAsset()
    {
        var textures = Resources.LoadAll<Texture2D>(sourcePath) ?? new Texture2D[0];

        bookCoverList = textures
            .Select(t => new BookReferences
            {
                book_sku = t.name,
                bookPath = $"{sourcePath}/{t.name}"
            })
            .ToArray();

        paths = new List<string>(bookCoverList.Length);
        foreach (var br in bookCoverList)
            paths.Add(br.bookPath);
    }

    public string GetResourcePath(string sku)
    {
        if (string.IsNullOrEmpty(sku) || bookCoverList == null || bookCoverList.Length == 0)
            return null;

        var match = Array.Find(bookCoverList, b => string.Equals(b.book_sku, sku, StringComparison.OrdinalIgnoreCase));
        //return match.bookPath;
        return Path.Combine(sourcePath, sku);
    }
    [ContextMenu("DebugLog")]
    private void DebugLog()
    {
        foreach (var item in bookCoverList)
        {
            Debug.Log($"Path " + item.bookPath + " Valid " + IsValidResourcePath<Texture>(item.bookPath));
        }
    }

    bool IsValidResourcePath<T>(string path) where T : UnityEngine.Object
    {
        var obj = Resources.Load<T>(path);
        return obj != null;
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