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
    public GameObject itemPrefab; // prefab: TMP_Text + child "Point" (Image)

    [Header("Line Parent (common parent cho line + items)")]
    public Transform lineParent;  // KÉO Panel/Root chung vào đây

    [Header("Camera dùng cho Matching (nếu Canvas không phải Overlay)")]
    [SerializeField] private Camera uiCamera;

    [Header("Line UI")]
    [Tooltip("Độ dày đường nối (pixel) - ứng với Width")]
    public float lineThickness = 3f;              // WIDTH = độ dày
    [Tooltip("Sprite dùng cho line (nếu để trống sẽ dùng Background mặc định)")]
    public Sprite lineSprite;

    [Header("Item Colors")]
    public Color normalColor   = Color.white;
    public Color selectedColor = new Color(1f, 0.92f, 0.55f); // vàng nhạt

    // ====== Exam Data ======
    private string _qid;
    private List<string> _leftTexts;
    private List<string> _rightTexts;
    private Action<Dictionary<int, int>> _onAnswerChanged;
    private readonly Dictionary<int, int> _pairs = new();

    // ====== Runtime ======
    private readonly List<Item> leftItems  = new();
    private readonly List<Item> rightItems = new();
    private Image   _bg;
    private Item    _selectedItem; // item đang chọn chờ nối

    private Canvas _canvas;
    private Camera _uiCamResolved;   // camera thực sự dùng cho Canvas (có thể null nếu Overlay)

    private class Item
    {
        public RectTransform rt;
        public TMP_Text      txt;
        public Image         point;
        public Image         bg;

        // line UI dùng Image + RectTransform
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
            {
                // Overlay: không dùng camera
                _uiCamResolved = null;
            }
            else
            {
                // ScreenSpaceCamera hoặc WorldSpace
                _uiCamResolved = _canvas.worldCamera != null ? _canvas.worldCamera : uiCamera;
            }
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
        Dictionary<int, int> savedPairs,
        Action<Dictionary<int, int>> callback)
    {
        _qid             = q.id;
        _onAnswerChanged = callback;

        _leftTexts  = (q.matchingLeft  != null) ? new List<string>(q.matchingLeft)  : new List<string>();
        _rightTexts = (q.matchingRight != null) ? new List<string>(q.matchingRight) : new List<string>();

        // Fallback nếu BE vẫn nhét vào options
        if (_leftTexts.Count == 0 && _rightTexts.Count == 0 &&
            q.options != null && q.options.Count > 0)
        {
            int half = q.options.Count / 2;
            for (int i = 0; i < q.options.Count; i++)
            {
                if (i < half) _leftTexts.Add(q.options[i]);
                else          _rightTexts.Add(q.options[i]);
            }
        }

        // Tách chuỗi theo '-'
        _leftTexts  = SplitColumnText(_leftTexts);
        _rightTexts = SplitColumnText(_rightTexts);

        Debug.Log($"[MATCHING] QID={_qid}, left={_leftTexts.Count}, right={_rightTexts.Count}");

        ClearUI();
        SpawnItems();
        RestorePairs(savedPairs);
        RaiseChanged();
    }

    // ---------- Render ----------
    private void ClearUI()
    {
        if (leftRoot != null)
            foreach (Transform c in leftRoot) Destroy(c.gameObject);
        if (rightRoot != null)
            foreach (Transform c in rightRoot) Destroy(c.gameObject);

        // Xóa line cũ nếu còn (phòng trường hợp để chung parent)
        if (lineParent != null)
        {
            foreach (Transform c in lineParent)
            {
                if (c.name.StartsWith("MatchLine"))
                    Destroy(c.gameObject);
            }
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
            leftItems.Add(CreateItem(leftRoot, _leftTexts[i]));

        for (int i = 0; i < _rightTexts.Count; i++)
            rightItems.Add(CreateItem(rightRoot, _rightTexts[i]));
    }

    private Item CreateItem(Transform root, string label)
    {
        // leftRoot/rightRoot nên là con của lineParent để tất cả cùng hệ toạ độ
        var go   = Instantiate(itemPrefab, root);
        var item = new Item
        {
            rt    = go.GetComponent<RectTransform>(),
            txt   = go.GetComponentInChildren<TMP_Text>(true),
            point = go.transform.Find("Point")?.GetComponent<Image>(),
            bg    = go.GetComponent<Image>()
        };
        if (item.txt != null) item.txt.text = label ?? "New Text";
        if (item.bg  != null) item.bg.color = normalColor;
        return item;
    }

    private void SetItemSelected(Item item, bool selected)
    {
        if (item?.bg == null) return;
        item.bg.color = selected ? selectedColor : normalColor;
    }

    // ---------- tạo Line Image (UI) ----------
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

        // gán sprite để image thật sự render được
        if (lineSprite != null)
        {
            img.sprite = lineSprite;
        }
        else
        {
            // dùng sprite UI mặc định nếu chưa set
            img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        }

        var rt = go.GetComponent<RectTransform>();
        // anchor giữa để dễ tính
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        // WIDTH = độ dày, HEIGHT sẽ được set = A→B
        rt.sizeDelta = new Vector2(lineThickness, 0f);

        return rt;
    }

    // =================== CONNECTION ===================
    private void TryConnect(Item a, Item b)
    {
        if (a == null || b == null) return;
        if (a == b) return;

        // CHẶN nối cùng 1 bên (cùng leftRoot / cùng rightRoot)
        if (!IsOppositeSide(a, b)) return;

        // clear các connection cũ (nếu có)
        ClearConnection(a);
        ClearConnection(b);

        var lineRt = CreateLineImage();

        a.connected = b;
        b.connected = a;
        a.lineRt    = lineRt;
        b.lineRt    = lineRt;

        UpdateLine(a);
        UpdateLine(b);

        int ai = leftItems.IndexOf(a);
        int bi = rightItems.IndexOf(b);
        if (ai >= 0 && bi >= 0)
            _pairs[ai] = bi;

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

        int ai1 = leftItems.IndexOf(x);
        int ai2 = leftItems.IndexOf(o);
        if (ai1 >= 0) _pairs.Remove(ai1);
        if (ai2 >= 0) _pairs.Remove(ai2);
    }

    // =================== LINE UPDATE (Canvas-aware) ===================
    private void UpdateLine(Item x)
    {
        if (x == null || x.connected == null) return;
        if (x.point == null || x.connected.point == null) return;
        if (x.lineRt == null) return;

        Transform parent = lineParent != null ? lineParent : transform.parent;
        if (parent == null) return;

        RectTransform parentRt = parent as RectTransform;
        if (parentRt == null)
        {
            Debug.LogWarning("[MATCHING] lineParent / parent không phải RectTransform (Canvas/UI)");
            return;
        }

        // 1) screen pos của 2 Point (dùng camera ĐÚNG của Canvas)
        Camera camForUI = _uiCamResolved;
        Vector2 screen1 = RectTransformUtility.WorldToScreenPoint(camForUI, x.point.rectTransform.position);
        Vector2 screen2 = RectTransformUtility.WorldToScreenPoint(camForUI, x.connected.point.rectTransform.position);

        // 2) Convert sang local trong parent (lineParent)
        Vector2 local1, local2;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screen1, camForUI, out local1))
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screen2, camForUI, out local2))
            return;

        // 3) Tính midpoint, độ dài, góc
        Vector2 dir    = local2 - local1;
        float   length = dir.magnitude;
        if (length <= 0.01f) return;

        Vector2 mid = (local1 + local2) * 0.5f;

        // alpha = góc để trục X trùng với dir
        float alpha = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // muốn trục Y (HEIGHT) trùng với dir => quay thêm -90°
        float angle = alpha - 90f;

        // 4) Set cho RectTransform của line
        var rt = x.lineRt;
        rt.anchoredPosition = mid;
        // WIDTH = độ dày, HEIGHT = A→B
        rt.sizeDelta        = new Vector2(lineThickness, length);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);    // chỉ xoay Z
    }

    private bool IsOppositeSide(Item a, Item b)
    {
        return (leftItems.Contains(a) && rightItems.Contains(b)) ||
               (leftItems.Contains(b) && rightItems.Contains(a));
    }

    // =================== POINTER (CLICK ĐỂ NỐI) ===================
    public void OnPointerDown(PointerEventData e)
    {
        var hit = FindHitItem(e);
        if (hit == null) return;

        // click lại chính nó => bỏ chọn
        if (_selectedItem == hit)
        {
            SetItemSelected(_selectedItem, false);
            _selectedItem = null;
            return;
        }

        // Chưa có item nào được chọn -> chọn item hiện tại
        if (_selectedItem == null)
        {
            _selectedItem = hit;
            SetItemSelected(_selectedItem, true);
            return;
        }

        // ĐÃ có 1 item được chọn trước đó
        if (!IsOppositeSide(_selectedItem, hit))
        {
            // Cùng bên -> chỉ đổi selection, không nối
            SetItemSelected(_selectedItem, false);
            _selectedItem = hit;
            SetItemSelected(_selectedItem, true);
            return;
        }

        // Khác bên -> TỰ ĐỘNG TẠO LINE NỐI 2 PREFAB
        TryConnect(_selectedItem, hit);

        // reset selection sau khi nối xong
        SetItemSelected(_selectedItem, false);
        SetItemSelected(hit, false);
        _selectedItem = null;
    }

    // Không dùng drag nhưng vẫn phải implement interface
    public void OnDrag(PointerEventData e) { }
    public void OnPointerUp(PointerEventData e) { }

    private Item FindHitItem(PointerEventData e)
    {
        Camera camForUI = _uiCamResolved; // cùng camera với line
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

        foreach (var kv in saved)
        {
            int li = kv.Key;
            int ri = kv.Value;
            if (li >= 0 && li < leftItems.Count &&
                ri >= 0 && ri < rightItems.Count)
            {
                TryConnect(leftItems[li], rightItems[ri]);
            }
        }
    }

    private void RaiseChanged()
    {
        _onAnswerChanged?.Invoke(new Dictionary<int, int>(_pairs));
    }

    // =================== REVIEW ===================
    public void ShowCorrect(Dictionary<int, int> correctPairs)
    {
        if (correctPairs == null) return;

        foreach (var kv in correctPairs)
        {
            int li = kv.Key;
            int ri = kv.Value;

            if (li < 0 || li >= leftItems.Count) continue;
            if (ri < 0 || ri >= rightItems.Count) continue;

            bool ok = _pairs.ContainsKey(li) && _pairs[li] == ri;

            if (leftItems[li].point != null)
                leftItems[li].point.color = ok ? Color.green : Color.red;
        }
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
}
