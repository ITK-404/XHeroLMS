using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        // Fill BookReferences array with sku (texture name) and resource path
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

    // Return resource path for given sku, or null if not found
    public string GetResourcePath(string sku)
    {
        if (string.IsNullOrEmpty(sku) || bookCoverList == null || bookCoverList.Length == 0)
            return null;

        var match = Array.Find(bookCoverList, b => string.Equals(b.book_sku, sku, StringComparison.OrdinalIgnoreCase));
        return match.bookPath;
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