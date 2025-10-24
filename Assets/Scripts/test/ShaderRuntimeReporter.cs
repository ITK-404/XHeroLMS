using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

[DisallowMultipleComponent]
public class ShaderRuntimeReporter : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI output;

    [Header("What to check")]
    [Tooltip("Để trống sẽ tự tìm tất cả Renderer trong scene (bao gồm cả inactive).")]
    public Renderer[] renderersToCheck;
    [Tooltip("Có thể thêm trực tiếp các Material rời để kiểm tra.")]
    public Material[] extraMaterialsToCheck;

    [Header("Options")]
    [Tooltip("Tự động tìm tất cả Renderer nếu mảng renderersToCheck đang rỗng.")]
    public bool autoFindAllRenderers = true;
    [Tooltip("Tần suất cập nhật HUD (giây).")]
    [Min(0.1f)]
    public float refreshInterval = 1.0f;

    [Header("Hotkeys (Runtime)")]
    [Tooltip("Bật/tắt _BYPASS_STENCIL cho URP/BasicStencil")]
    public KeyCode toggleBypassKey = KeyCode.B;
    [Tooltip("Bật/tắt _SHOW_MASK_COLOR cho Custom/StencilMaskURP")]
    public KeyCode toggleShowMaskKey = KeyCode.M;

    float _nextTick;

    void Start()
    {
        if (!output)
            Debug.LogWarning("[ShaderRuntimeReporter] Chưa gán TextMeshProUGUI 'output'!");

        if ((renderersToCheck == null || renderersToCheck.Length == 0) && autoFindAllRenderers)
            renderersToCheck = FindObjectsOfType<Renderer>(true); // include inactive

        BuildAndShowReport();
    }

    void Update()
    {
        if (Time.unscaledTime >= _nextTick)
        {
            _nextTick = Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);
            BuildAndShowReport();
        }

        if (Input.GetKeyDown(toggleBypassKey))
            ToggleKeywordOnScene("URP/BasicStencil", "_BYPASS_STENCIL");

        if (Input.GetKeyDown(toggleShowMaskKey))
            ToggleKeywordOnScene("Custom/StencilMaskURP", "_SHOW_MASK_COLOR");
    }

    void ToggleKeywordOnScene(string shaderName, string keyword)
    {
        int count = 0;

        // Renderers trong scene
        if (renderersToCheck != null)
        {
            foreach (var r in renderersToCheck)
            {
                if (!r) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!m || m.shader == null) continue;
                    if (m.shader.name == shaderName)
                    {
                        if (m.IsKeywordEnabled(keyword)) m.DisableKeyword(keyword);
                        else m.EnableKeyword(keyword);
                        count++;
                    }
                }
            }
        }

        // Material rời
        if (extraMaterialsToCheck != null)
        {
            foreach (var m in extraMaterialsToCheck)
            {
                if (!m || m.shader == null) continue;
                if (m.shader.name == shaderName)
                {
                    if (m.IsKeywordEnabled(keyword)) m.DisableKeyword(keyword);
                    else m.EnableKeyword(keyword);
                    count++;
                }
            }
        }

        Debug.Log($"[ShaderRuntimeReporter] Toggled {keyword} on {count} material(s) using {shaderName}.");
        BuildAndShowReport();
    }

    void BuildAndShowReport()
    {
        var sb = new StringBuilder(2048);

        // Header
        var rp = GraphicsSettings.currentRenderPipeline;
        string rpName = rp ? rp.GetType().Name : "(Built-in)";
        sb.AppendLine("<b>Shader Runtime Report</b>");
        sb.AppendLine($"App: {Application.productName} {Application.version}");
        sb.AppendLine($"Device: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})");
        sb.AppendLine($"SRP: {rpName}");
        sb.AppendLine();

        // Renderers
        if (renderersToCheck != null && renderersToCheck.Length > 0)
        {
            foreach (var r in renderersToCheck)
            {
                if (!r) continue;
                sb.AppendLine($"<b>Renderer</b>: {r.name}  <size=80%>(layer {LayerMask.LayerToName(r.gameObject.layer)}, enabled {r.enabled})</size>");
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    string matName = m ? m.name : "null";
                    string shName = (m && m.shader) ? m.shader.name : "null";
                    bool supported = (m && m.shader) && m.shader.isSupported;
                    string ok = supported ? "<color=#00E676>[OK]</color>" : "<color=#FF5252>[BAD]</color>";
                    int queue = m ? m.renderQueue : 0;

                    sb.AppendLine($"  • Mat[{i}]: {matName}  |  Shader: {shName} {ok}  |  Queue: {queue}");

                    if (m && m.shader && (shName == "URP/BasicStencil" || shName == "Custom/StencilMaskURP"))
                    {
                        bool bypass = m.IsKeywordEnabled("_BYPASS_STENCIL");
                        bool gray   = m.IsKeywordEnabled("_GRAYSCALE_ON");
                        bool showM  = m.IsKeywordEnabled("_SHOW_MASK_COLOR");

                        sb.Append("      Keywords: ");
                        bool any = false;
                        if (bypass) { sb.Append("_BYPASS_STENCIL "); any = true; }
                        if (gray)   { sb.Append("_GRAYSCALE_ON ");   any = true; }
                        if (showM)  { sb.Append("_SHOW_MASK_COLOR ");any = true; }
                        if (!any) sb.Append("(none)");
                        sb.AppendLine();
                    }
                }
                sb.AppendLine();
            }
        }

        // Extra materials
        if (extraMaterialsToCheck != null && extraMaterialsToCheck.Length > 0)
        {
            sb.AppendLine("<b>Extra Materials</b>");
            foreach (var m in extraMaterialsToCheck)
            {
                string matName = m ? m.name : "null";
                string shName  = (m && m.shader) ? m.shader.name : "null";
                bool supported = (m && m.shader) && m.shader.isSupported;
                string ok = supported ? "<color=#00E676>[OK]</color>" : "<color=#FF5252>[BAD]</color>";
                int queue = m ? m.renderQueue : 0;

                sb.AppendLine($"  • {matName} | Shader: {shName} {ok} | Queue: {queue}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("<size=80%><i>Hints:</i> Shader = URP/BasicStencil mà [BAD] ⇒ compile fail/strip. " +
                      "Nếu [OK] mà không cắt được, bật _SHOW_MASK_COLOR ở StencilMaskURP (mask có vẽ chưa?) " +
                      "hoặc bật _BYPASS_STENCIL để xác nhận phần hiển thị hoạt động.</size>");

        if (output) output.text = sb.ToString();
        else Debug.Log(sb.ToString());
    }
}
