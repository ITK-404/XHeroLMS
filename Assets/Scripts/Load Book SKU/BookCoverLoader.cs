using System.Collections.Generic;
using UnityEngine;

public class BookCoverLoader : MonoBehaviour
{
    private Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    public BookCoverDatabase database;
    public Texture2D LoadCover(string sku)
    {
        if (database == null)
        {
            Debug.Log("Book Cover Database is null");
            return null;
        } 
        
        var path = database.GetResourcePath(sku);
        if (cache.TryGetValue(sku, out var tex))
            return tex;

        var loaded = Resources.Load<Texture2D>(path);
        cache.Add(sku, loaded);
        return loaded;
    }

    public void UnloadAll()
    {
        cache.Clear();
        Resources.UnloadUnusedAssets();
    }

    private void OnDestroy()
    {
        UnloadAll();
    }
}