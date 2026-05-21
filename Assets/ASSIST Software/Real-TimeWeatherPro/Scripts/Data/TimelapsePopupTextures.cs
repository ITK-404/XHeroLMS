using System.Collections.Generic;
using UnityEngine;


namespace RealTimeWeather.Data
{
    [CreateAssetMenu(fileName = "SliderTextures", menuName = "Real-Time Weather/Timelapse/PopupTextures", order = 0)]
    public class TimelapsePopupTextures : ScriptableObject
    {
        public Texture2D duplicateButtonTexture;
        public Texture2D deleteButtonTexture;
        [Space]
        public Material intervalMaterial;
        [Space]
        public Texture2D intervalTexturesClear;
        public Texture2D intervalTexturesPartlyClear;
        public Texture2D intervalTexturesCloudy;
        public Texture2D intervalTexturesMist;
        public Texture2D intervalTexturesThunderstorms;
        public Texture2D intervalTexturesRainSnowPrecipitation;
        public Texture2D intervalTexturesRainPrecipitation;
        public Texture2D intervalTexturesSnowPrecipitation;
        public Texture2D intervalTexturesWindy;
        public Texture2D intervalTexturesSunny;
        [Space]
        public Texture2D weatherProviderIntervalTexture;
        public Texture2D waterProviderIntervalTexture;
        public Texture2D weatherProviderTexture;
        public Texture2D waterProviderTexture;
        public Texture2D PresetBackground;
        public Texture2D PresetForeground;
        public Texture2D TimelapsePresetForeground;
        public Texture2D PresetSelected;
        public Texture2D Stripes;
        public Texture2D InfoTexture;
        [Space]
        public Color[] intervalCloudyColours = new Color[] { new Color(0.18f, 0.19f, 0.2f, 1.0f), new Color(0.12f, 0.13f, 0.15f, 1.0f) };
        public Color[] intervalSunnyColours = new Color[] { new Color(1.0f, 0.95f, 0.53f, 1.0f), new Color(1.0f, 0.88f, 0.38f, 1.0f) };
        public Color selectedIntervalCloudyHighlight = new Color(0.46f, 0.51f, 0.55f, 0.11f);
        public Color selectedIntervalSunnyHighlight = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        public Color delimiterColCloudy = new Color(0.39f, 0.40f, 0.42f, 1f);
        public Color delimiterColSunny = new Color(0.99f, 1f, 0.87f, 1f);
        public Color updateButtonCol = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        public Color ProviderBackgroundColor;
        public Color WaterProviderBackgroundColor;
        [Space]
        public Color ClearDefaultColor;
        public Color PartlyClearDefaultColor;
        public Color SunnyDefaultColor;
        public Color WindyDefaultColor;
        public Color SnowPrecipitatioDefaultColor;
        public Color RainSnowPrecipitationDefaultColor;
        public Color RainPrecipitationDefaultColor;
        public Color ThunderstormsDefaultColor;
        public Color MistDefaultColor;
        public Color CloudyDefaultColor;
    }
}