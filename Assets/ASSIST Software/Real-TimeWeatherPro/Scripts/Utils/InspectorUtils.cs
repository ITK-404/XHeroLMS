// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace RealTimeWeather
{
    public class InspectorUtils
    {
        private static int _simulationCount;

        //Spaces
        private static int _marginSpace = 5;
        private static int _elementsSpace = 5;
        private static int _innerSpace = 7;
        private static int _horizontalSpace = 40;
        private static int _borderSpace = 10;

        //Sizes
        public static float SelectionGridTabHeight = 21;
        public static float VectorLabelWidth = 20;
        public static int GoBackBtnWidth = 100;
        public static int GoBackBtnHeight = 20;
        public static float FieldHeight = 20;

        private static float _labelWidth = 200;

        //Colours
        public static Color addButtonColor = Color.white;
        public static Color labelTextColor = Color.white;
        public static Color backgroundColorGray = Color.gray;
        public static Color backgroundColorLightGray = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static Color backgroundColorWhite = Color.white;
        public static Color[] timelapsePopupBoxColours = new Color[] { new Color(0.1f, 0.1f, 0.1f, 1f), new Color(0.6f, 0.6f, 0.6f, 1f) };
        public static Color[] timelapsePopupDayOrderColours = new Color[] { new Color(0.16f, 0.16f, 0.16f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f) };
        public static Color[] timelapsePopupButtonColours = new Color[] { new Color(0.6f, 0.6f, 0.6f, 0.9f), new Color(0.8f, 0.8f, 0.8f, 1f) };
        public static Color deleteButtonColor = new Color(1f, 0f, 0f, 0.3f);
        public static Color saveButtonColor = new Color(0.4f, 0.6f, 0f, 0.3f);

        //Timelapse Popup
        public const int kNumOfPresetRowsWithoutScroll = 3;
        public const float kSaveTimelapseButtonHeight = 30f;
        public const float kSaveTimelapseButtonWidth = 400f;
        public const float kBottomClippingRectHeight = 5f;
        public const float kGlobalFieldTabWidth = 460.0f;
        public const float kNewDayButtonHeight = 30f;
        public const float kNewDayButtonWidth = 300f;
        public const float kCharacterWidth = 10;
        public const float kLeftTabWidth = 460;
        public const float kLineThickness = 8;
        public const float kCustomSliderHeight = 50;
        public const float kProviderSliderHeight = 25;
        public const float kColliderThickness = 13;
        public const float kLabelLineThickness = 2;
        public const float kSliderLabelHeight = 22;
        [Min(1)] public const int kLabelDivisions = 24;
        [Range(1, 50)] public const int kSliderSnapThreshold = 25;

        //Presets
        public const int kNumberOfDefaultPresets = 9;

        //Structs
        private struct kWarningTextColorValues
        {
            public const byte Red = 245;
            public const byte Green = 203;
            public const byte Blue = 110;
            public const byte Alpha = 204;
        }

        //GUIStyles
        private static GUIStyle _headerStyle;
        private static GUIStyle _outerBoxStyle;
        private static GUIStyle _innerBoxStyle;
        private static GUIStyle _labelInfoStyle;
        private static GUIStyle _labelWarningStyle;
        private static GUIStyle _tabButtonStyle;
        private static GUIStyle _customRadioButtonStyle;
        private static GUIStyle _goBackButtonStyle;
        private static GUIStyle _addButtonStyle;

        //GUIStyles Properties
        public static GUIStyle HeaderStyle
        {
            get
            {
                _headerStyle = new GUIStyle(EditorStyles.foldout);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = labelTextColor;
                _headerStyle.active.textColor = labelTextColor;
                _headerStyle.hover.textColor = labelTextColor;
                return _headerStyle;
            }
        }

        public static GUIStyle HeaderStyle2
        {
            get
            {
                _headerStyle = new GUIStyle(EditorStyles.foldout);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.fontSize = 15;
                _headerStyle.imagePosition = ImagePosition.ImageLeft;
                _headerStyle.normal.textColor = labelTextColor;
                _headerStyle.active.textColor = labelTextColor;
                _headerStyle.hover.textColor = labelTextColor;
                _headerStyle.fixedWidth = 15;
                return _headerStyle;
            }
        }

        public static GUIStyle OuterBoxStyle
        {
            get
            {
                _outerBoxStyle = new GUIStyle(GUI.skin.box);
                _outerBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _outerBoxStyle.alignment = TextAnchor.UpperLeft;
                _outerBoxStyle.fontStyle = FontStyle.Bold;
                return _outerBoxStyle;
            }
        }

        public static GUIStyle InnerBoxStyle
        {
            get
            {
                _innerBoxStyle = new GUIStyle(EditorStyles.helpBox);
                _innerBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _innerBoxStyle.alignment = TextAnchor.UpperLeft;
                _innerBoxStyle.fontStyle = FontStyle.Bold;
                _innerBoxStyle.fontSize = 11;
                return _innerBoxStyle;
            }
        }

        public static GUIStyle BoldLabelInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle LabelInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Normal;
                _labelInfoStyle.alignment = TextAnchor.UpperLeft;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle TitleInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                _labelInfoStyle.alignment = TextAnchor.UpperLeft;
                _labelInfoStyle.fontSize = 20;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle HugeTitleInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                _labelInfoStyle.alignment = TextAnchor.UpperLeft;
                _labelInfoStyle.fontSize = 30;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle SmallTitleInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                _labelInfoStyle.alignment = TextAnchor.UpperLeft;
                _labelInfoStyle.fontSize = 15;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle BoldTitleInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                _labelInfoStyle.fontSize = 13;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle TextFieldInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.textField);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Normal;
                _labelInfoStyle.alignment = TextAnchor.MiddleLeft;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle BigLabelInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Bold;
                _labelInfoStyle.alignment = TextAnchor.MiddleLeft;
                _labelInfoStyle.wordWrap = true;
                _labelInfoStyle.fontSize = 15;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle SmallLabelInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Normal;
                _labelInfoStyle.alignment = TextAnchor.MiddleCenter;
                _labelInfoStyle.fontSize = 10;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle SmallBlackLabelInfoStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.label);
                _labelInfoStyle.normal.textColor = Color.black;
                _labelInfoStyle.fontStyle = FontStyle.Normal;
                _labelInfoStyle.alignment = TextAnchor.MiddleCenter;
                _labelInfoStyle.fontSize = 10;
                _labelInfoStyle.clipping = TextClipping.Clip;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle ToggleStyle
        {
            get
            {
                _labelInfoStyle = new GUIStyle(GUI.skin.toggle);
                _labelInfoStyle.normal.textColor = labelTextColor;
                _labelInfoStyle.fontStyle = FontStyle.Normal;
                _labelInfoStyle.alignment = TextAnchor.UpperLeft;
                _labelInfoStyle.wordWrap = true;
                return _labelInfoStyle;
            }
        }

        public static GUIStyle LabelWarningStyle
        {
            get { _labelWarningStyle = new GUIStyle(GUI.skin.label);
                _labelWarningStyle.normal.textColor =
                    new Color32(
                        kWarningTextColorValues.Red,
                        kWarningTextColorValues.Green,
                        kWarningTextColorValues.Blue,
                        kWarningTextColorValues.Alpha);
                _labelWarningStyle.fontStyle = FontStyle.Normal;
                _labelWarningStyle.alignment = TextAnchor.UpperLeft;
                _labelWarningStyle.wordWrap = true;
                return _labelWarningStyle;
            }
        }

        public static GUIStyle LabelErrorStyle
        {
            get
            {
                _labelWarningStyle = new GUIStyle(GUI.skin.label);
                _labelWarningStyle.normal.textColor = Color.red;
                _labelWarningStyle.fontStyle = FontStyle.Normal;
                _labelWarningStyle.alignment = TextAnchor.UpperLeft;
                _labelWarningStyle.wordWrap = true;
                return _labelWarningStyle;
            }
        }

        public static GUIStyle LabelGreenStyle
        {
            get
            {
                _labelWarningStyle = new GUIStyle(GUI.skin.label);
                _labelWarningStyle.normal.textColor = Color.green;
                _labelWarningStyle.fontStyle = FontStyle.Normal;
                _labelWarningStyle.alignment = TextAnchor.UpperLeft;
                _labelWarningStyle.wordWrap = true;
                return _labelWarningStyle;
            }
        }

        public static GUIStyle InputLabelStyle
        {
            get
            {
                _innerBoxStyle = new GUIStyle(EditorStyles.helpBox);
                _innerBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _innerBoxStyle.alignment = TextAnchor.MiddleLeft;
                _innerBoxStyle.fontStyle = FontStyle.Normal;
                _innerBoxStyle.fontSize = 12;
                return _innerBoxStyle;
            }
        }

        public static GUIStyle TabButtonStyle
        {
            get {_tabButtonStyle = new GUIStyle(GUI.skin.button);
                _tabButtonStyle.alignment = TextAnchor.UpperCenter;
                _tabButtonStyle.fontSize = 12;

                 return _tabButtonStyle;
            }
        }

        public static GUIStyle CustomRadioButtonStyle
        {
            get {_customRadioButtonStyle = new GUIStyle(EditorStyles.radioButton);
                _customRadioButtonStyle.padding = new RectOffset(18, 0, 1, 0);
                return _customRadioButtonStyle;
            }
        }

        public static GUIStyle AddButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = addButtonColor;
                _addButtonStyle.fontStyle = FontStyle.Bold;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.wordWrap = true;
                _addButtonStyle.fixedHeight = 25;
                return _addButtonStyle;
            }
        }

        public static GUIStyle ColoredButtonStyleFixWidth
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.black;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.wordWrap = true;
                _addButtonStyle.fixedHeight = 25;
                _addButtonStyle.fixedWidth = 150;
                _addButtonStyle.fontSize = 15;
                _addButtonStyle.normal.background = MakeBackgroundTexture(10,10,new Color(0.149f, 0.8666f,0.6509f));
                return _addButtonStyle;
            }
        }

        public static GUIStyle ButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.white;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.fixedHeight = 30;
                _addButtonStyle.fontSize = 15;
                _addButtonStyle.padding = new RectOffset(20, 20, 0, 0);
                return _addButtonStyle;
            }
        }

        public static GUIStyle SmallerButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.white;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.fixedHeight = 25;
                _addButtonStyle.fontSize = 12;
                _addButtonStyle.padding = new RectOffset(20, 20, 0, 0);
                _addButtonStyle.margin = new RectOffset(0, 0, 0, 0);
                return _addButtonStyle;
            }
        }

        public static GUIStyle ColoredButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.black;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.fixedHeight = 28;
                _addButtonStyle.fontSize = 15;
                _addButtonStyle.normal.background = MakeBackgroundTexture(50, 32, new Color(0.149f, 0.8666f, 0.6509f));
                _addButtonStyle.hover.background = MakeBackgroundTexture(50, 32, new Color(0.4796f, 0.8784f, 0.7520f));
                _addButtonStyle.hover.textColor = Color.black;
                _addButtonStyle.onHover.background = MakeBackgroundTexture(50, 32, new Color(0.4796f, 0.8784f, 0.7520f));
                _addButtonStyle.onHover.textColor = Color.black;
                _addButtonStyle.padding = new RectOffset(20, 20, 0, 0);
                return _addButtonStyle;
            }
        }

        public static GUIStyle SmallerColoredButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.black;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.fixedHeight = 22;
                _addButtonStyle.fontSize = 12;
                _addButtonStyle.normal.background = MakeBackgroundTexture(50, 32, new Color(0.149f, 0.8666f, 0.6509f));
                _addButtonStyle.hover.background = MakeBackgroundTexture(50, 32, new Color(0.4796f, 0.8784f, 0.7520f));
                _addButtonStyle.hover.textColor = Color.black;
                _addButtonStyle.onHover.background = MakeBackgroundTexture(50, 32, new Color(0.4796f, 0.8784f, 0.7520f));
                _addButtonStyle.onHover.textColor = Color.black;
                _addButtonStyle.padding = new RectOffset(20, 20, 0, 0);
                return _addButtonStyle;
            }
        }

        public static GUIStyle WhiteColoredButtonStyle
        {
            get
            {
                _addButtonStyle = new GUIStyle(GUI.skin.button);
                _addButtonStyle.normal.textColor = Color.black;
                _addButtonStyle.alignment = TextAnchor.MiddleCenter;
                _addButtonStyle.fixedHeight = 22;
                _addButtonStyle.fontSize = 12;
                _addButtonStyle.normal.background = MakeBackgroundTexture(50, 32, new Color(1f, 1f, 1f));
                _addButtonStyle.hover.background = MakeBackgroundTexture(50, 32, new Color(1f, 1f, 1f));
                _addButtonStyle.hover.textColor = Color.black;
                _addButtonStyle.onHover.background = MakeBackgroundTexture(50, 32, new Color(1f, 1f, 1f));
                _addButtonStyle.onHover.textColor = Color.black;
                _addButtonStyle.padding = new RectOffset(20, 20, 0, 0);
                return _addButtonStyle;
            }
        }

        private static Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D backgroundTexture = new Texture2D(width, height);

            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();

            return backgroundTexture;
        }

        public static GUIStyle CenterAlignmentStyle
        {
            get
            {
                _goBackButtonStyle = new GUIStyle(GUI.skin.button);
                _goBackButtonStyle.alignment = TextAnchor.MiddleCenter;
                return _goBackButtonStyle;
            }
        }

        public static int MarginSpace 
        { 
            get 
            { 
                return _marginSpace; 
            } 
        }

        public static int InnerSpace 
        { 
            get 
            {
                return _innerSpace; 
            } 
        }

        public static float LabelWidth 
        { 
            get
            { 
                return _labelWidth;
            }
        }
        
        public static int ElementsSpace 
        { 
            get 
            { 
                return _elementsSpace; 
            } 
        }

        public static int HorizontalSpace
        {
            get
            {
                return _horizontalSpace;
            }
        }

        public static int SimulationCount 
        {
            get { return _simulationCount; }
            set { _simulationCount = value; }
        }

        public static int BorderSpace { get => _borderSpace; set => _borderSpace = value; }
    }
}
#endif //UNITY_EDITOR