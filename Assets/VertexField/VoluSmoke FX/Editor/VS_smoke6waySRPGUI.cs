using UnityEngine;
using UnityEditor;

namespace VF
{
    public class VF_smoke6waySRPGUI : ShaderGUI
    {
        // Flipbook (no textures here)
        MaterialProperty _Sprite_Animation_Speed;
        MaterialProperty _Sprite_Sheet_Width;
        MaterialProperty _Sprite_Sheet_Height;
        MaterialProperty _Add_Frame;

        // Textures
        MaterialProperty _RTB_texture;
        MaterialProperty _LBF_texture;
        MaterialProperty _Gradient;
        MaterialProperty _Noise_Texture;
        MaterialProperty _FlowTex;

        // Look (color/lighting/alpha)
        MaterialProperty _Albedo_Color;
        MaterialProperty _Use_Mesh_Vertex_Color;
        MaterialProperty _Albedo_Contrast;
        MaterialProperty _Light_Remap;
        MaterialProperty _Power_Light_Remap;
        MaterialProperty _Color_Absorption;
        MaterialProperty _Color_as_Emession;
        MaterialProperty _Ambient;
        MaterialProperty _Main_Alpha;
        MaterialProperty _Frensnel;
        MaterialProperty _Multiplyy_Frensle;
        MaterialProperty _Soft;
        MaterialProperty _Grow;

        // Gradient
        MaterialProperty _Use_Gradient;
        MaterialProperty _Gradient_Offset;
        MaterialProperty _Gradient_Intensity;
        MaterialProperty __; // literal prop name "_"

        // Motion / noise
        MaterialProperty _Speed; // UI label: Noise Motion
        MaterialProperty _Scale_Noise;
        MaterialProperty _Rotate_Noise;
        MaterialProperty _Alpha_Noise;
        MaterialProperty _Noise_Power;
        MaterialProperty _Noise_Smoothstep_A;
        MaterialProperty _Noise_Smoothstep_B;

        // Flowmap & interaction
        MaterialProperty _Use_FlowMap;
        MaterialProperty _Flow_Strenght;
        MaterialProperty _FLow_Speed;
        MaterialProperty _Flow_UV_Offset;
        MaterialProperty _FlowMap_Tilling;
        MaterialProperty _Flowmap_Offset;
        MaterialProperty _Offset_Flow;
        MaterialProperty _Phase_Offset;
        MaterialProperty _TEMPORAL_MODE;
        MaterialProperty _Noise_FlowMap_Strenght;
        MaterialProperty _Decay_Mask;
        MaterialProperty _Decay_Power;
        MaterialProperty _Decay_Color;

        // Soft Edge (lives in Look)
        MaterialProperty _Use_Soft_Edge;
        MaterialProperty _Soft_Edge_Strenght;

        // Depth
        MaterialProperty _Depth_Choise;   // 0=A/Alpha, 1=B/Depth Map
        MaterialProperty _Depth_Sharpness;
        MaterialProperty _Depth_Density;
        MaterialProperty _Depth_Contrast;
        MaterialProperty _MinY_MaxY;

        // Ignored auto prop (safe bind)
        MaterialProperty _Gradient_TexelSize;

        enum Tab { Flipbook, Look, Motion, Flow, Depth, Textures }
        static Tab activeTab = Tab.Flipbook;

        static GUIStyle title, sectionTitle, chipStyle;
        static Color onColor, offColor, defaultBG, defaultContent;

        static GUIContent GC(string n, string t = null) => new GUIContent(n, t);

        public override void OnGUI(MaterialEditor me, MaterialProperty[] props)
        {
            // Bind props
            FP(props, "_Sprite_Animation_Speed", ref _Sprite_Animation_Speed);
            FP(props, "_Sprite_Sheet_Width", ref _Sprite_Sheet_Width);
            FP(props, "_Sprite_Sheet_Height", ref _Sprite_Sheet_Height);
            FP(props, "_Add_Frame", ref _Add_Frame);

            FP(props, "_RTB_texture", ref _RTB_texture);
            FP(props, "_LBF_texture", ref _LBF_texture);
            FP(props, "_Gradient", ref _Gradient);
            FP(props, "_Noise_Texture", ref _Noise_Texture);
            FP(props, "_FlowTex", ref _FlowTex);

            FP(props, "_Albedo_Color", ref _Albedo_Color);
            FP(props, "_Use_Mesh_Vertex_Color", ref _Use_Mesh_Vertex_Color);
            FP(props, "_Albedo_Contrast", ref _Albedo_Contrast);
            FP(props, "_Light_Remap", ref _Light_Remap);
            FP(props, "_Power_Light_Remap", ref _Power_Light_Remap);
            FP(props, "_Color_Absorption", ref _Color_Absorption);
            FP(props, "_Color_as_Emession", ref _Color_as_Emession);
            FP(props, "_Ambient", ref _Ambient);
            FP(props, "_Main_Alpha", ref _Main_Alpha);
            FP(props, "_Frensnel", ref _Frensnel);
            FP(props, "_Multiplyy_Frensle", ref _Multiplyy_Frensle);
            FP(props, "_Soft", ref _Soft);
            FP(props, "_Grow", ref _Grow);

            FP(props, "_Use_Gradient", ref _Use_Gradient);
            FP(props, "_Gradient_Offset", ref _Gradient_Offset);
            FP(props, "_Gradient_Intensity", ref _Gradient_Intensity);
            FP(props, "_", ref __);

            FP(props, "_Speed", ref _Speed);
            FP(props, "_Scale_Noise", ref _Scale_Noise);
            FP(props, "_Rotate_Noise", ref _Rotate_Noise);
            FP(props, "_Alpha_Noise", ref _Alpha_Noise);
            FP(props, "_Noise_Power", ref _Noise_Power);
            FP(props, "_Noise_Smoothstep_A", ref _Noise_Smoothstep_A);
            FP(props, "_Noise_Smoothstep_B", ref _Noise_Smoothstep_B);

            FP(props, "_Use_FlowMap", ref _Use_FlowMap);
            FP(props, "_Flow_Strenght", ref _Flow_Strenght);
            FP(props, "_FLow_Speed", ref _FLow_Speed);
            FP(props, "_Flow_UV_Offset", ref _Flow_UV_Offset);
            FP(props, "_FlowMap_Tilling", ref _FlowMap_Tilling);
            FP(props, "_Flowmap_Offset", ref _Flowmap_Offset);
            FP(props, "_Offset_Flow", ref _Offset_Flow);
            FP(props, "_Phase_Offset", ref _Phase_Offset);
            FP(props, "_TEMPORAL_MODE", ref _TEMPORAL_MODE);
            FP(props, "_Noise_FlowMap_Strenght", ref _Noise_FlowMap_Strenght);
            FP(props, "_Decay_Mask", ref _Decay_Mask);
            FP(props, "_Decay_Power", ref _Decay_Power);
            FP(props, "_Decay_Color", ref _Decay_Color);

            FP(props, "_Use_Soft_Edge", ref _Use_Soft_Edge);
            FP(props, "_Soft_Edge_Strenght", ref _Soft_Edge_Strenght);

            FP(props, "_Depth_Choise", ref _Depth_Choise);
            FP(props, "_Depth_Sharpness", ref _Depth_Sharpness);
            FP(props, "_Depth_Density", ref _Depth_Density);
            FP(props, "_Depth_Contrast", ref _Depth_Contrast);
            FP(props, "_MinY_MaxY", ref _MinY_MaxY);

            FP(props, "_Gradient_TexelSize", ref _Gradient_TexelSize);

            InitStylesAndColors();
            DrawHeaderChips();
            DrawToolbar();

            EditorGUILayout.Space(4);

            switch (activeTab)
            {
                case Tab.Flipbook: DrawFlipbook(me); break;
                case Tab.Look: DrawLook(me); break;
                case Tab.Motion: DrawMotion(me); break;
                case Tab.Flow: DrawFlow(me); break;
                case Tab.Depth: DrawDepth(me); break;
                case Tab.Textures: DrawTextures(me); break;
            }
        }

        void InitStylesAndColors()
        {
            if (title != null) return;
            title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            sectionTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            chipStyle = new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold };

            ColorUtility.TryParseHtmlString("#6EC67A", out onColor);   // green from your image
            ColorUtility.TryParseHtmlString("#DD5069", out offColor);  // red   from your image
        }

        void WithTint(Color bg, Color content, System.Action body)
        {
            defaultBG = GUI.backgroundColor;
            defaultContent = GUI.contentColor;
            GUI.backgroundColor = bg;
            GUI.contentColor = content;
            body?.Invoke();
            GUI.backgroundColor = defaultBG;
            GUI.contentColor = defaultContent;
        }

        void DrawHeaderChips()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("6 Way Smoke", title);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_Use_Gradient != null)
                    {
                        bool on = _Use_Gradient.floatValue > 0.5f;
                        WithTint(on ? onColor : offColor, Color.white, () =>
                        {
                            string label = "Gradient " + (on ? "ON" : "OFF");
                            if (GUILayout.Button(new GUIContent(label, "Toggle gradient remapping."), chipStyle))
                                _Use_Gradient.floatValue = on ? 0f : 1f;
                        });
                    }
                    if (_Use_FlowMap != null)
                    {
                        bool on = _Use_FlowMap.floatValue > 0.5f;
                        WithTint(on ? onColor : offColor, Color.white, () =>
                        {
                            string label = "Flow " + (on ? "ON" : "OFF");
                            if (GUILayout.Button(new GUIContent(label, "Toggle UV flow warping."), chipStyle))
                                _Use_FlowMap.floatValue = on ? 0f : 1f;
                        });
                    }
                    if (_Use_Soft_Edge != null)
                    {
                        bool on = _Use_Soft_Edge.floatValue > 0.5f;
                        WithTint(on ? onColor : offColor, Color.white, () =>
                        {
                            string label = "Soft Edge " + (on ? "ON" : "OFF");
                            if (GUILayout.Button(new GUIContent(label, "Toggle soft edge feathering."), chipStyle))
                                _Use_Soft_Edge.floatValue = on ? 0f : 1f;
                        });
                    }
                    if (_Depth_Choise != null)
                    {
                        int idx = Mathf.RoundToInt(_Depth_Choise.floatValue);
                        WithTint(onColor, Color.white, () =>
                        {
                            string label = "Depth Map: " + (idx == 0 ? "A" : "B");
                            if (GUILayout.Button(new GUIContent(label, "Switch between Height A (alpha) and Height B (depth map)."), chipStyle))
                                _Depth_Choise.floatValue = idx == 0 ? 1f : 0f;
                        });
                    }
                }
            }
        }

        void DrawToolbar()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                activeTab = (Tab)GUILayout.Toolbar((int)activeTab, new[]
                {
                    GC("Flipbook"), GC("Look"), GC("Motion"), GC("Flow"), GC("Depth"), GC("Textures")
                });
            }
        }

        // --- Sections (no collapses) ---

        void DrawFlipbook(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Flipbook", sectionTitle);
            EditorGUI.indentLevel++;
            if (_Sprite_Animation_Speed != null) me.ShaderProperty(_Sprite_Animation_Speed, GC("Sprite Animation Speed", "Seconds per frame for the flipbook playback. Lower values play faster."));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_Sprite_Sheet_Width != null) me.ShaderProperty(_Sprite_Sheet_Width, GC("Sheet Columns", "Number of frames across the sprite sheet."));
                if (_Sprite_Sheet_Height != null) me.ShaderProperty(_Sprite_Sheet_Height, GC("Sheet Rows", "Number of frames down the sprite sheet."));
            }
            if (_Sprite_Sheet_Width != null && _Sprite_Sheet_Height != null)
            {
                int cols = Mathf.Max(1, Mathf.RoundToInt(_Sprite_Sheet_Width.floatValue));
                int rows = Mathf.Max(1, Mathf.RoundToInt(_Sprite_Sheet_Height.floatValue));
                EditorGUILayout.LabelField($"Frames: {cols * rows} ({cols} x {rows})", EditorStyles.miniLabel);
            }
            if (_Add_Frame != null)
            {
                EditorGUILayout.Space(2);
                int totalFrames = 1;
                if (_Sprite_Sheet_Width != null && _Sprite_Sheet_Height != null)
                {
                    int cols = Mathf.Max(1, Mathf.RoundToInt(_Sprite_Sheet_Width.floatValue));
                    int rows = Mathf.Max(1, Mathf.RoundToInt(_Sprite_Sheet_Height.floatValue));
                    totalFrames = Mathf.Max(1, cols * rows);
                }

                int maxFrameIndex = Mathf.Max(0, totalFrames - 1);
                int current = Mathf.Clamp(Mathf.RoundToInt(_Add_Frame.floatValue), 0, maxFrameIndex);
                GUIContent label = GC("Start Frame Offset", "Choose the first frame to display.\nWhen animation speed is zero this frame stays visible.");

                EditorGUI.BeginChangeCheck();
                if (maxFrameIndex > 0)
                    current = EditorGUILayout.IntSlider(label, current, 0, maxFrameIndex);
                else
                    current = EditorGUILayout.IntField(label, current);
                if (EditorGUI.EndChangeCheck())
                    _Add_Frame.floatValue = current;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawLook(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Look", sectionTitle);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Color & Opacity", EditorStyles.miniBoldLabel);
            ColorWithTooltip(me, _Albedo_Color, GC("Albedo Color", "Base tint applied before lighting response."));
            if (_Main_Alpha != null) me.ShaderProperty(_Main_Alpha, GC("Main Alpha", "Global opacity multiplier for this effect."));
            if (_Ambient != null) me.ShaderProperty(_Ambient, GC("Ambient", "Adds constant light so the smoke stays readable in shadow."));
            if (_Albedo_Contrast != null) me.ShaderProperty(_Albedo_Contrast, GC("Albedo Contrast", "Pushes the base color towards brighter highlights or deeper shadows."));
            if (_Color_Absorption != null) me.ShaderProperty(_Color_Absorption, GC("Color Absorption", "Controls how quickly the smoke tint absorbs incoming light."));
            if (_Use_Mesh_Vertex_Color != null)
            {
                bool useVertexColors = _Use_Mesh_Vertex_Color.floatValue > 0.5f;
                bool toggled = EditorGUILayout.Toggle(
                    GC("Use Mesh Vertex Color", "Multiply the material tint by per-vertex colors from the mesh."),
                    useVertexColors);
                if (toggled != useVertexColors) _Use_Mesh_Vertex_Color.floatValue = toggled ? 1f : 0f;
            }
            if (_Color_as_Emession != null)
            {
                bool on = _Color_as_Emession.floatValue > 0.5f;
                bool next = EditorGUILayout.Toggle(GC("Color as Emission", "Treat the albedo color as emissive light contribution."), on);
                if (next != on) _Color_as_Emession.floatValue = next ? 1f : 0f;
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Lighting Response", EditorStyles.miniBoldLabel);
            if (_Light_Remap != null) me.ShaderProperty(_Light_Remap, GC("Light Remap", "Remap lighting to balance shadowed and lit areas."));
            if (_Power_Light_Remap != null) me.ShaderProperty(_Power_Light_Remap, GC("Light Remap Power", "Sharpen or soften the light remap curve."));
            if (_Frensnel != null) me.ShaderProperty(_Frensnel, GC("Fresnel Power", "Edge highlight falloff. Higher values tighten the rim."));
            if (_Multiplyy_Frensle != null) me.ShaderProperty(_Multiplyy_Frensle, GC("Fresnel Boost", "Overall intensity multiplier for the fresnel highlight."));

            EditorGUILayout.Space(6);

            // Soft Edge (colored toggle + gated controls)
            EditorGUILayout.LabelField("Soft Edge", EditorStyles.miniBoldLabel);
            if (_Use_Soft_Edge != null)
            {
                bool on = _Use_Soft_Edge.floatValue > 0.5f;
                ColoredToggle(me, "Use Soft Edge", "Enable edge feathering around the sprite footprint.", _Use_Soft_Edge, on);
                if (_Use_Soft_Edge.floatValue > 0.5f)
                {
                    if (_Soft_Edge_Strenght != null) me.ShaderProperty(_Soft_Edge_Strenght, GC("Soft Edge Strength", "How wide the feathered edge reaches."));
                    if (_Soft != null) me.ShaderProperty(_Soft, GC("Soft Fill", "Fine-tune how softly the edge fades out."));
                }
            }

            EditorGUILayout.Space(6);

            // Gradient (colored toggle + gated controls)
            EditorGUILayout.LabelField("Gradient", EditorStyles.miniBoldLabel);
            if (_Use_Gradient != null)
            {
                bool on = _Use_Gradient.floatValue > 0.5f;
                ColoredToggle(me, "Use Gradient", "Enable gradient remapping along the height of the smoke.", _Use_Gradient, on);
                if (_Use_Gradient.floatValue > 0.5f)
                {
                    if (_Gradient != null) me.TexturePropertySingleLine(GC("Gradient Texture", "1D ramp sampled from bottom (black) to top (white)."), _Gradient);
                    if (_Gradient_Offset != null) me.ShaderProperty(_Gradient_Offset, GC("Gradient Offset", "Slides the gradient ramp up or down."));
                    if (_Gradient_Intensity != null) me.ShaderProperty(_Gradient_Intensity, GC("Gradient Intensity", "Strength of the gradient tint."));
                    if (__ != null) me.ShaderProperty(__, GC("Gradient Level", "Controls which slice of the gradient texture is sampled."));
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawMotion(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Motion", sectionTitle);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Noise Animation", EditorStyles.miniBoldLabel);
            if (_Speed != null) me.ShaderProperty(_Speed, GC("Time Multiplier", "Global animation speed for noise-driven motion."));
            Vector2WithTooltip(me, _Scale_Noise, GC("Noise Scale (XY)", "Tiling for the noise sample. Use X/Y to stretch horizontally or vertically."));
            if (_Rotate_Noise != null) me.ShaderProperty(_Rotate_Noise, GC("Noise Rotation (rad)", "Rotates the noise lookup around Z; measured in radians."));
            if (_Alpha_Noise != null) me.ShaderProperty(_Alpha_Noise, GC("Alpha Noise Weight", "Blends additional noise into the alpha to break up edges."));
            if (_Noise_Power != null) me.ShaderProperty(_Noise_Power, GC("Noise Power", "Exponent applied to the noise sample to adjust contrast."));
            if (_Noise_Smoothstep_A != null) me.ShaderProperty(_Noise_Smoothstep_A, GC("Noise Clip Start", "Lower threshold for the noise contrast remap."));
            if (_Noise_Smoothstep_B != null) me.ShaderProperty(_Noise_Smoothstep_B, GC("Noise Clip End", "Upper threshold for the noise contrast remap."));
            if (_Noise_FlowMap_Strenght != null) me.ShaderProperty(_Noise_FlowMap_Strenght, GC("Flow Noise Strength", "Controls how strongly noise influences the flowmap distortion."));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawFlow(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Flow", sectionTitle);
            EditorGUI.indentLevel++;
            if (_Use_FlowMap != null)
            {
                bool on = _Use_FlowMap.floatValue > 0.5f;
                ColoredToggle(me, "Use Flowmap", "Enable UV flow warping driven by a flow map.", _Use_FlowMap, on);
                if (_Use_FlowMap.floatValue > 0.5f)
                {
                    if (_FlowTex != null) me.TexturePropertySingleLine(GC("Flow Map", "RG = direction; brightness controls the amount of distortion."), _FlowTex);
                    if (_TEMPORAL_MODE != null) me.ShaderProperty(_TEMPORAL_MODE, GC("Temporal Mode", "Choose between global time or the flow map's packed time."));
                    if (_Flow_Strenght != null) me.ShaderProperty(_Flow_Strenght, GC("Flow Strength", "How far UVs are displaced by the flow map."));
                    if (_FLow_Speed != null) me.ShaderProperty(_FLow_Speed, GC("Flow Speed", "Speed multiplier for the flow animation."));
                    Vector2WithTooltip(me, _FlowMap_Tilling, GC("Flow Map Tiling", "Scales the flow map UVs (X = U, Y = V)."));
                    Vector2WithTooltip(me, _Flowmap_Offset, GC("Flow Map Offset", "Offsets the flow map UVs in texture space (X = U, Y = V)."));
                    if (_Phase_Offset != null) me.ShaderProperty(_Phase_Offset, GC("Phase Offset", "Offsets the flow sine wave to avoid syncing between instances."));
                    if (_Offset_Flow != null) me.ShaderProperty(_Offset_Flow, GC("Time Offset", "Shifts the start time of the flow map animation."));
                    VectorWithTooltip(me, _Flow_UV_Offset, GC("Flow UV Offset", "Offsets the flow map lookup (XY) to reposition the pattern."));

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Interaction", EditorStyles.miniBoldLabel);
                    if (_Grow != null) me.ShaderProperty(_Grow, GC("Grow", "Expands or contracts the volume before rendering."));
                    if (_Decay_Mask != null) me.ShaderProperty(_Decay_Mask, GC("Decay Mask", "Mask texture controlling where the smoke can decay."));
                    if (_Decay_Power != null) me.ShaderProperty(_Decay_Power, GC("Decay Power", "Strength of the decay effect driven by the mask."));
                    ColorWithTooltip(me, _Decay_Color, GC("Decay Color", "Tint applied where the decay mask erodes the smoke."));
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawDepth(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Depth", sectionTitle);
            EditorGUI.indentLevel++;
            if (_Depth_Choise != null)
            {
                int cur = Mathf.RoundToInt(_Depth_Choise.floatValue);
                int next = EditorGUILayout.Popup(GC("Height Source", "Choose the data that drives the depth fade."),
                                                 cur, new[] { "Height A (Alpha)", "Height B (Depth Map)" });
                if (next != cur) _Depth_Choise.floatValue = next;
            }
            if (_Depth_Sharpness != null) me.ShaderProperty(_Depth_Sharpness, GC("Depth Sharpness", "Sharpens the transition between near and far samples."));
            if (_Depth_Density != null) me.ShaderProperty(_Depth_Density, GC("Depth Density", "Blends how much density increases with depth."));
            if (_Depth_Contrast != null) me.ShaderProperty(_Depth_Contrast, GC("Depth Contrast", "Adds contrast to the depth mask."));
            VectorWithTooltip(me, _MinY_MaxY, GC("Height Min/Max", "Y range remapping (X = min, Y = max)."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawTextures(MaterialEditor me)
        {
            EditorGUILayout.LabelField("Textures", sectionTitle);
            EditorGUI.indentLevel++;
            if (_RTB_texture != null) me.TexturePropertySingleLine(GC("Flipbook (RTB)", "Primary flipbook texture (RGB channels)."), _RTB_texture);
            if (_LBF_texture != null) me.TexturePropertySingleLine(GC("Lookup (LBF)", "Lookup texture for lighting/behavior."), _LBF_texture);
            if (_Gradient != null) me.TexturePropertySingleLine(GC("Gradient", "Optional gradient ramp used when gradient mode is enabled."), _Gradient);
            if (_Noise_Texture != null) me.TexturePropertySingleLine(GC("Noise Texture", "Tiling noise used for breakup and flow modulation."), _Noise_Texture);
            if (_FlowTex != null) me.TexturePropertySingleLine(GC("Flow Map", "Flow map controlling UV distortion."), _FlowTex);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void ColoredToggle(MaterialEditor me, string label, string tooltip, MaterialProperty toggleProp, bool isOnNow)
        {
            Color bg = isOnNow ? onColor : offColor;
            WithTint(bg, Color.white, () =>
            {
                bool next = EditorGUILayout.Toggle(GC(label, tooltip), isOnNow);
                if (next != isOnNow) toggleProp.floatValue = next ? 1f : 0f;
            });
            GUI.backgroundColor = defaultBG;
            GUI.contentColor = defaultContent;
        }

        void VectorWithTooltip(MaterialEditor me, MaterialProperty prop, GUIContent label)
        {
            _ = me;
            if (prop == null) return;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUI.BeginChangeCheck();
                Vector4 value = EditorGUILayout.Vector4Field(GUIContent.none, prop.vectorValue);
                if (EditorGUI.EndChangeCheck()) prop.vectorValue = value;
            }
            EditorGUI.showMixedValue = false;
        }

        void Vector2WithTooltip(MaterialEditor me, MaterialProperty prop, GUIContent label)
        {
            _ = me;
            if (prop == null) return;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUI.BeginChangeCheck();
                Vector4 current = prop.vectorValue;
                Vector2 value = EditorGUILayout.Vector2Field(GUIContent.none, new Vector2(current.x, current.y));
                if (EditorGUI.EndChangeCheck())
                {
                    current.x = value.x;
                    current.y = value.y;
                    prop.vectorValue = current;
                }
            }
            EditorGUI.showMixedValue = false;
        }

        void ColorWithTooltip(MaterialEditor me, MaterialProperty prop, GUIContent label)
        {
            _ = me;
            if (prop == null) return;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUI.BeginChangeCheck();
                Color value = EditorGUILayout.ColorField(GUIContent.none, prop.colorValue, true, true, true);
                if (EditorGUI.EndChangeCheck()) prop.colorValue = value;
            }
            EditorGUI.showMixedValue = false;
        }

        static void FP(MaterialProperty[] props, string name, ref MaterialProperty prop)
            => prop = FindProperty(name, props, false);
    }
}
