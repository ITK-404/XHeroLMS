//
// Copyright(c) 2022 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather.Data
{
    [CreateAssetMenu(fileName = "Enviro Preset Data", menuName = "Real-Time Weather/Data/EnviroPresetsData", order = 0)]
    public class EnviroPresetData : ScriptableObject
    {
        public Gradient weatherFogMod;
        public float volumeLightIntensity;
        public float windStrength;
        public float wetnessLevel;
        public float snowLevel;
        public float fogDensity;
        public float fogScatteringIntensity;
        public float skyFogStart;
        public float skyFogHeight;
        public float skyFogIntensity;
        public float blurDistance;
        public float blurIntensity;
        public float blurSkyIntensity;

        public float scatteringCoef;
        public float ambientSkyColorIntensity;
        public float edgeDarkness;
        public float lightAbsorbtion;
        public float lightStepModifier;
        public float lightVariance;
        public float density;
        public float baseErosionIntensity;
        public float detailErosionIntensity;
        public float coverage;
        public float coverageType;
        public float cloudType;

        public float flatCoverage;
        public float flatCloudsDensity;
        public float flatCloudsAbsorbtion;
        public float flatCloudsDirectLightIntensity;
        public float flatCloudsAmbientLightIntensity;
        public float flatCloudsHGPhase;

        public float particleLayer1Alpha;
        public float particleLayer1Brightness;
        public float particleLayer1ColorPower;
        public float particleLayer2Alpha;
        public float particleLayer2Brightness;
        public float particleLayer2ColorPower;

        public float cirrusAlpha;
        public float cirrusCoverage;
        public float cirrusColorPow;
    }
}
