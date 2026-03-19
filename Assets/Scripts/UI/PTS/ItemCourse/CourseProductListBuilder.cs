using System.Collections.Generic;
using UnityEngine;

public class CourseProductListBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private CourseProductItemUI itemPrefab;

    [Header("Behavior")]
    [SerializeField] private bool buildOnEnable = true;
    [SerializeField] private bool listenStoreChanged = true;
    [SerializeField] private bool clearOldItemsBeforeBuild = true;

    private readonly List<CourseProductItemUI> _spawnedItems = new();

    private void OnEnable()
    {
        if (listenStoreChanged)
            CourseDetailStaticStore.OnChanged += HandleStoreChanged;

        if (buildOnEnable)
            Rebuild();
    }

    private void OnDisable()
    {
        if (listenStoreChanged)
            CourseDetailStaticStore.OnChanged -= HandleStoreChanged;
    }

    private void HandleStoreChanged()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Products")]
    public void Rebuild()
    {
        if (contentParent == null)
        {
            Debug.LogWarning("[CourseProductListBuilder] contentParent is null.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning("[CourseProductListBuilder] itemPrefab is null.");
            return;
        }

        if (clearOldItemsBeforeBuild)
            ClearItems();

        if (!CourseDetailStaticStore.HasData)
        {
            Debug.Log("[CourseProductListBuilder] No course detail data in store.");
            return;
        }

        var course = CourseDetailStaticStore.CurrentCourse;
        if (course == null)
        {
            Debug.LogWarning("[CourseProductListBuilder] CurrentCourse is null.");
            return;
        }

        if (course.products == null || course.products.Count == 0)
        {
            Debug.Log("[CourseProductListBuilder] No products found in current course.");
            return;
        }

        foreach (var product in course.products)
        {
            if (product == null)
                continue;

            CourseProductItemUI item = Instantiate(itemPrefab, contentParent);
            item.gameObject.SetActive(true);

            item.Setup(
                product.productName,
                product.image,
                product.externalUrl
            );

            _spawnedItems.Add(item);
        }
    }

    public void ClearItems()
    {
        for (int i = _spawnedItems.Count - 1; i >= 0; i--)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}