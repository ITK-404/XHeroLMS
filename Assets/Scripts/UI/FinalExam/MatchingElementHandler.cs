using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MatchingElementHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI Roots")]
    public Transform leftRoot;
    public Transform rightRoot;
    public GameObject itemPrefab; // prefab: TMP_Text + ExamMatchingElement

    [Header("Line Parent (common parent cho line + items)")]
    public Transform lineParent;

    [Header("Camera dùng cho Matching (nếu Canvas không phải Overlay)")]
    [SerializeField] private Camera uiCamera;

    [Header("Line UI")]
    [Tooltip("Độ dày đường nối (pixel) - ứng với Width")]
    public float lineThickness = 3f;              // WIDTH = độ dày
    [Tooltip("Sprite dùng cho line (nếu để trống sẽ dùng Background mặc định)")]
    public Sprite lineSprite;

    [Header("Item Colors")]
    public Color normalColor   = Color.white;
    public Color selectedColor = new Color(1f, 0.92f, 0.55f);

    [Header("Review")]
    public bool isReadOnlyReview = false;

    // ====== Exam Data ======
    private string _qid;

    // TEXT gốc (đúng thứ tự server / data)
    private List<string> _leftTextsOriginal;
    private List<string> _rightTextsOriginal;

    // TEXT sau khi split (và random order)
    private List<string> _leftTexts;
    private List<string> _rightTexts;
    private Action<Dictionary<int, int>> _onAnswerChanged;
    private readonly Dictionary<int, int> _pairs = new();

    // Mapping displayIndex -> originalIndex
    private readonly List<int> _leftDisplayToOrig  = new();
    private readonly List<int> _rightDisplayToOrig = new();

    // ====== Runtime ======
    private readonly List<Item> leftItems  = new();
    private readonly List<Item> rightItems = new();
    private Image   _bg;
    private Item    _selectedItem;

    private Canvas _canvas;
    private Camera _uiCamResolved;

    private class Item
    {
        public RectTransform      rt;
        public TMP_Text           txt;
        public Image              bg;
        public ExamMatchingElement elem;  // chứa topPoint/lowerPoint

        public RectTransform lineRt;
        public Item          connected;
    }

    private void Awake()
    {
        // tìm Canvas cha
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
        {
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                _uiCamResolved = null;
            else
                _uiCamResolved = _canvas.worldCamera != null ? _canvas.worldCamera : uiCamera;
        }
        else
        {
            _uiCamResolved = uiCamera;
        }

        _bg = GetComponent<Image>();
        if (_bg != null)
        {
            var c = _bg.color;
            c.a = 0f;
            _bg.color = c;
            _bg.raycastTarget = true;
        }
    }

    // =================== SETUP ===================
    public void SetupQuestion(
        ExamQuestion q,
        Dictionary<int, int> savedPairs,        // KEY = rightIndex (orig), VALUE = leftIndex (orig)
        Action<Dictionary<int, int>> callback)  // callback nhận (rightOrig -> leftOrig)
    {
        _qid             = q.id;
        _onAnswerChanged = callback;

        // Lấy text gốc từ question
        _leftTextsOriginal  = (q.matchingLeft  != null) ? new List<string>(q.matchingLeft)  : new List<string>();
        _rightTextsOriginal = (q.matchingRight != null) ? new List<string>(q.matchingRight) : new List<string>();

        if (_leftTextsOriginal.Count == 0 && _rightTextsOriginal.Count == 0 &&
            q.options != null && q.options.Count > 0)
        {
            int half = q.options.Count / 2;
            for (int i = 0; i < q.options.Count; i++)
            {
                if (i < half) _leftTextsOriginal.Add(q.options[i]);
                else          _rightTextsOriginal.Add(q.options[i]);
            }
        }

        // Split text (có thể tách "Kim-Thủy-..." thành list)
        _leftTextsOriginal  = SplitColumnText(_leftTextsOriginal);
        _rightTextsOriginal = SplitColumnText(_rightTextsOriginal);

        Debug.Log($"[MATCHING] QID={_qid}, left={_leftTextsOriginal.Count}, right={_rightTextsOriginal.Count}");

        ClearUI();

        // Tạo mapping displayIndex -> originalIndex và RANDOM order
        BuildAndShuffleDisplayMaps();

        // Tạo list text hiển thị theo order random
        BuildDisplayTextsFromOriginal();

        SpawnItems();
        RestorePairs(savedPairs);   // savedPairs đang dùng index gốc
        RaiseChanged();
    }

    private void BuildAndShuffleDisplayMaps()
    {
        _leftDisplayToOrig.Clear();
        _rightDisplayToOrig.Clear();

        for (int i = 0; i < _leftTextsOriginal.Count; i++)
            _leftDisplayToOrig.Add(i);

        for (int i = 0; i < _rightTextsOriginal.Count; i++)
            _rightDisplayToOrig.Add(i);

        // Seed ổn định theo QID để cùng 1 câu luôn ra cùng 1 random
        int baseSeed = GetStableSeed(_qid);

        Shuffle(_leftDisplayToOrig,  baseSeed ^ 397);
        Shuffle(_rightDisplayToOrig, baseSeed ^ 791);
    }

    private static void Shuffle(List<int> list, int seed)
    {
        var rng = new System.Random(seed);
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    private static int GetStableSeed(string s)
    {
        if (string.IsNullOrEmpty(s)) return 123456;
        unchecked
        {
            int h = 23;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h;
        }
    }

    private void BuildDisplayTextsFromOriginal()
    {
        _leftTexts  = new List<string>(_leftDisplayToOrig.Count);
        _rightTexts = new List<string>(_rightDisplayToOrig.Count);

        foreach (var origIdx in _leftDisplayToOrig)
            _leftTexts.Add(_leftTextsOriginal[origIdx]);

        foreach (var origIdx in _rightDisplayToOrig)
            _rightTexts.Add(_rightTextsOriginal[origIdx]);
    }
    // ---------- Render ----------
    private void ClearUI()
    {
        if (leftRoot != null)
            foreach (Transform c in leftRoot) Destroy(c.gameObject);
        if (rightRoot != null)
            foreach (Transform c in rightRoot) Destroy(c.gameObject);

        if (lineParent != null)
        {
            foreach (Transform c in lineParent)
                if (c.name.StartsWith("MatchLine"))
                    Destroy(c.gameObject);
        }

        leftItems.Clear();
        rightItems.Clear();
        _pairs.Clear();
        _selectedItem = null;
    }

    private void SpawnItems()
    {
        if (leftRoot == null || rightRoot == null || itemPrefab == null) return;

        for (int i = 0; i < _leftTexts.Count; i++)
            leftItems.Add(CreateItem(leftRoot, _leftTexts[i], true));

        for (int i = 0; i < _rightTexts.Count; i++)
            rightItems.Add(CreateItem(rightRoot, _rightTexts[i], false));
    }

    private Item CreateItem(Transform root, string label, bool isLeft)
    {
        var go   = Instantiate(itemPrefab, root);
        var item = new Item
        {
            rt   = go.GetComponent<RectTransform>(),
            txt  = go.GetComponentInChildren<TMP_Text>(true),
            bg   = go.GetComponent<Image>(),
            elem = go.GetComponent<ExamMatchingElement>()
        };

        // set side + ẩn/hiện point đúng cột
        if (item.elem != null)
        {
            var side = isLeft
                ? ExamMatchingElement.ElementSide.A
                : ExamMatchingElement.ElementSide.B;

            item.elem.Initialize(side);
        }

        if (item.txt != null) item.txt.text = label ?? "New Text";
        if (item.bg  != null) item.bg.color = normalColor;
        return item;
    }

    private void SetItemSelected(Item item, bool selected)
    {
        if (item?.bg == null) return;
        item.bg.color = selected ? selectedColor : normalColor;
    }

    private RectTransform CreateLineImage()
    {
        Transform parent = lineParent != null ? lineParent : transform.parent;
        if (parent == null) parent = transform;

        var go = new GameObject("MatchLine", typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        img.type = Image.Type.Simple;

        // Chỉ dùng sprite nếu bạn gán trong Inspector
        if (lineSprite != null)
        {
            img.sprite = lineSprite;
            img.type = Image.Type.Sliced;
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(lineThickness, 0f);   // width = độ dày
        return rt;
    }

    // =================== CONNECTION ===================
    private void TryConnect(Item a, Item b)
    {
        if (a == null || b == null) return;
        if (a == b) return;

        // phải khác cột
        if (!IsOppositeSide(a, b)) return;

        // clear cặp cũ (nếu có)
        ClearConnection(a);
        ClearConnection(b);

        // tạo line chung
        var lineRt = CreateLineImage();

        a.connected = b;
        b.connected = a;
        a.lineRt    = lineRt;
        b.lineRt    = lineRt;

        UpdateLine(a);
        UpdateLine(b);

        // ===== CHUẨN HOÁ: luôn encode RIGHT(orig) -> LEFT(orig) =====
        int leftDisplayIndex  = -1;
        int rightDisplayIndex = -1;

        if (leftItems.Contains(a) && rightItems.Contains(b))
        {
            leftDisplayIndex  = leftItems.IndexOf(a);
            rightDisplayIndex = rightItems.IndexOf(b);
        }
        else if (leftItems.Contains(b) && rightItems.Contains(a))
        {
            leftDisplayIndex  = leftItems.IndexOf(b);
            rightDisplayIndex = rightItems.IndexOf(a);
        }

        if (leftDisplayIndex >= 0 && rightDisplayIndex >= 0 &&
            leftDisplayIndex  < _leftDisplayToOrig.Count &&
            rightDisplayIndex < _rightDisplayToOrig.Count)
        {
            int leftOrig  = _leftDisplayToOrig[leftDisplayIndex];   // j (gốc)
            int rightOrig = _rightDisplayToOrig[rightDisplayIndex]; // i (gốc)

            // “dòng trái j nối với dòng phải i”  => _pairs[i] = j (index gốc)
            _pairs[rightOrig] = leftOrig;
        }

        RaiseChanged();
    }

    private void ClearConnection(Item x)
    {
        if (x == null || x.connected == null) return;
        var o = x.connected;

        if (x.lineRt != null)
            Destroy(x.lineRt.gameObject);

        x.connected = null;
        x.lineRt    = null;
        o.connected = null;
        o.lineRt    = null;

        // KEY trong _pairs là index cột PHẢI GỐC
        RemoveRightKeyByDisplayIndex(x);
        RemoveRightKeyByDisplayIndex(o);
    }

    private void RemoveRightKeyByDisplayIndex(Item item)
    {
        int d = rightItems.IndexOf(item);
        if (d < 0 || d >= _rightDisplayToOrig.Count) return;

        int rightOrig = _rightDisplayToOrig[d];   // index gốc
        _pairs.Remove(rightOrig);
    }

    private void UpdateLine(Item x)
    {
        if (x == null || x.connected == null) return;
        if (x.elem == null || x.connected.elem == null) return;
        if (x.lineRt == null) return;

        Transform parent = lineParent != null ? lineParent : transform.parent;
        if (parent == null) return;

        RectTransform parentRt = parent as RectTransform;
        if (parentRt == null)
        {
            Debug.LogWarning("[MATCHING] lineParent / parent không phải RectTransform");
            return;
        }

        // 1) world position của 2 MatchingPoint
        Vector3 world1 = x.elem.GetMatchingPoint().position;
        Vector3 world2 = x.connected.elem.GetMatchingPoint().position;

        // 2) convert sang local của lineParent
        Vector3 local1_3D = parentRt.InverseTransformPoint(world1);
        Vector3 local2_3D = parentRt.InverseTransformPoint(world2);

        Vector2 local1 = new Vector2(local1_3D.x, local1_3D.y);
        Vector2 local2 = new Vector2(local2_3D.x, local2_3D.y);

        Vector2 dir = local2 - local1;
        float length = dir.magnitude;
        if (length <= 0.01f) return;

        Vector2 mid   = (local1 + local2) * 0.5f;
        float alpha   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angle   = alpha - 90f; // trục Y của Image hướng A->B

        var rt = x.lineRt;
        rt.anchoredPosition = mid;
        rt.sizeDelta        = new Vector2(lineThickness, length);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsOppositeSide(Item a, Item b)
    {
        return (leftItems.Contains(a) && rightItems.Contains(b)) ||
               (leftItems.Contains(b) && rightItems.Contains(a));
    }

    // =================== POINTER (CLICK ĐỂ NỐI) ===================
    public void OnPointerDown(PointerEventData e)
    {
        if (isReadOnlyReview) return;

        var hit = FindHitItem(e);
        if (hit == null) return;

        if (_selectedItem == hit)
        {
            SetItemSelected(_selectedItem, false);
            _selectedItem = null;
            return;
        }

        if (_selectedItem == null)
        {
            _selectedItem = hit;
            SetItemSelected(_selectedItem, true);
            return;
        }

        if (!IsOppositeSide(_selectedItem, hit))
        {
            SetItemSelected(_selectedItem, false);
            _selectedItem = hit;
            SetItemSelected(_selectedItem, true);
            return;
        }

        TryConnect(_selectedItem, hit);

        SetItemSelected(_selectedItem, false);
        SetItemSelected(hit, false);
        _selectedItem = null;
    }

    public void OnDrag(PointerEventData e) { }
    public void OnPointerUp(PointerEventData e) { }

    private Item FindHitItem(PointerEventData e)
    {
        Camera camForUI = _uiCamResolved;

        foreach (var i in leftItems)
            if (RectTransformUtility.RectangleContainsScreenPoint(i.rt, e.position, camForUI))
                return i;

        foreach (var i in rightItems)
            if (RectTransformUtility.RectangleContainsScreenPoint(i.rt, e.position, camForUI))
                return i;

        return null;
    }

    private void LateUpdate()
    {
        foreach (var i in leftItems)  UpdateLine(i);
        foreach (var i in rightItems) UpdateLine(i);
    }

    // =================== SAVE / LOAD ===================
    private void RestorePairs(Dictionary<int, int> saved)
    {
        if (saved == null) return;

        // saved: KEY = rightOrigIndex (i), VALUE = leftOrigIndex (j)
        foreach (var kv in saved)
        {
            int rightOrig = kv.Key;
            int leftOrig  = kv.Value;

            int leftDisplay  = _leftDisplayToOrig.IndexOf(leftOrig);
            int rightDisplay = _rightDisplayToOrig.IndexOf(rightOrig);

            if (leftDisplay  >= 0 && leftDisplay  < leftItems.Count &&
                rightDisplay >= 0 && rightDisplay < rightItems.Count)
            {
                TryConnect(leftItems[leftDisplay], rightItems[rightDisplay]);
            }
        }
    }

    private void RaiseChanged()
    {
        // Trả ra đúng format: KEY = index cột PHẢI (orig), VALUE = index cột TRÁI (orig)
        _onAnswerChanged?.Invoke(new Dictionary<int, int>(_pairs));
    }

    // =================== SPLIT TEXT ===================
    private static List<string> SplitColumnText(List<string> src)
    {
        var result = new List<string>();
        if (src == null) return result;

        foreach (var raw in src)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var plain = raw.Trim()
                           .Replace('–', '-')
                           .Replace('—', '-');

            if (plain.Contains("-"))
            {
                var parts = plain.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var t = part.Trim();
                    if (!string.IsNullOrEmpty(t))
                        result.Add(t);
                }
            }
            else
            {
                result.Add(plain);
            }
        }

        return result;
    }

    public void SetReadOnly(bool readOnly)
    {
        isReadOnlyReview = readOnly;
    }
}
