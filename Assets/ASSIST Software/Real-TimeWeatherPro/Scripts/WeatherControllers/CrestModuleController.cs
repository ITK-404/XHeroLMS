// 
// Copyright(c) 2022 Real-Time Weather ASSIST-Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if (CREST_HDRP_PRESENT || CREST_URP_PRESENT) && UNITY_2020_3_OR_NEWER
using Crest;
#endif

using RealTimeWeather.Data;
using RealTimeWeather.Managers;

using UnityEngine;

namespace RealTimeWeather.WeatherControllers
{
    public class CrestModuleController : MonoBehaviour
    {
        #region Constants
        private const string kCrestInstanceName = "Crest Ocean";
        //Crest shader properties
        private const string kSmoothnessFar = "_SmoothnessFar";
        private const string kOceanColorHDRP = "_ScatterColourBase";
        private const string kOceanColorURP = "_Diffuse";
        private const float kSlowWindTurbulence = 5f;
        private const float kVisibilityToMaterial = 15f;
        private const float kFullColor = 255f;
        private const float kLowTurbidity = 31f;
        private const float kMediumTurbidity = 11f;
        private const float kHighTurbidity = 0f;
        private const float kAirPressureConvert = 1000f;
        #endregion

        #region Private Variables
        private GameObject _crestInstance;
        #endregion

#if (CREST_HDRP_PRESENT || CREST_URP_PRESENT) && UNITY_2020_3_OR_NEWER
        #region Public Variables
        public OceanRenderer _crestOceanRenderer;
        public ShapeFFT _crestShapeFFT;
        #endregion
#endif //Crest HDRP || Crest URP

        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate += OnWaterDataUpdate;
            }
        }

        private void OnDestroy()
        {
            if(RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentMaritimeUpdate -= OnWaterDataUpdate;
            }
        }
        #endregion

#if (CREST_HDRP_PRESENT || CREST_URP_PRESENT) && UNITY_2020_3_OR_NEWER
        #region Public Methods
        /// <summary>
        /// This function creates the crest manager and adds it as a child to the "RealTimeWeatherManager" manager. 
        /// </summary>
        public void CreateCrestManagerInstance()
        {
#if UNITY_EDITOR
            _crestOceanRenderer = FindObjectOfType<OceanRenderer>();
            _crestShapeFFT = FindObjectOfType<ShapeFFT>();

           if(_crestOceanRenderer == null || _crestShapeFFT == null)
            {
                GameObject _crestInstance = new GameObject();
                _crestInstance.AddComponent<OceanRenderer>();
                _crestInstance.AddComponent<ShapeFFT>();
                _crestInstance.transform.SetParent(this.transform);
                _crestInstance.name = kCrestInstanceName;

               _crestOceanRenderer = _crestInstance.GetComponentInChildren<OceanRenderer>();
                _crestShapeFFT = _crestInstance.GetComponentInChildren<ShapeFFT>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif // Unity Editor
        }
        #endregion
#endif //Crest HDRP || Crest URP

        #region Private Methods
        /// <summary>
        /// Handles the water data update event.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void OnWaterDataUpdate(WaterData waterData)
        {
#if (CREST_HDRP_PRESENT || CREST_URP_PRESENT) && UNITY_2020_3_OR_NEWER
            SetPrecipitation(waterData);
            SetTurbidity(waterData);
            SetAirPressureAtSea(waterData);
            SetWind(waterData);
            SetWaves(waterData);
            SetVisibility(waterData);
#endif
        }

#if (CREST_HDRP_PRESENT || CREST_URP_PRESENT) && UNITY_2020_3_OR_NEWER
        /// <summary>
        /// Sets the precipitation rate in Crest.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetPrecipitation(WaterData waterData)
        {
            _crestShapeFFT._windTurbulence = Mathf.Clamp(waterData.Precipitation / kSlowWindTurbulence, 0f, 1f);
        }

        /// <summary>
        /// Sets the ocean turbidity of the given area (depending on precipitation).
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetTurbidity(WaterData waterData)
        {
            Color turbidity = new Color();
#if UNITY_PIPELINE_HDRP
            turbidity = _crestOceanRenderer.OceanMaterial.GetColor(kOceanColorHDRP);
#endif
#if UNITY_PIPELINE_URP
            turbidity = _crestOceanRenderer.OceanMaterial.GetColor(kOceanColorURP);
#endif
            if (waterData.Precipitation > 1f && waterData.Precipitation <= 2.5f)
            {
                turbidity.b = kLowTurbidity / kFullColor;
            }
            if (waterData.Precipitation > 2.5f && waterData.Precipitation <= 10f)
            {
                turbidity.b = kMediumTurbidity / kFullColor;
            }
            if (waterData.Precipitation > 10f)
            {
                turbidity.b = kHighTurbidity;
            }
#if UNTIY_PIPELINE_HDRP
            _crestOceanRenderer.OceanMaterial.SetColor(kOceanColorHDRP, turbidity);
#endif
#if UNITY_PIPELINE_URP
            _crestOceanRenderer.OceanMaterial.SetColor(kOceanColorURP, turbidity);
#endif
        }

        /// <summary>
        /// Sets the atmoshperic pressure at sea in Crest.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetAirPressureAtSea(WaterData waterData)
        {
            _crestShapeFFT._weight = waterData.AirPressureAtSea / kAirPressureConvert;
        }

        /// <summary>
        /// Sets the wind parameters in Crest.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetWind(WaterData waterData)
        {
            _crestOceanRenderer._globalWindSpeed = waterData.Wind.Speed;
        }

        /// <summary>
        /// Sets the wave parameters in Crest.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetWaves(WaterData waterData)
        {
            _crestShapeFFT._waveDirectionHeadingAngle = Utilities.Vector2ToDegree(waterData.Wave.Direction != Vector2.zero ? waterData.Wave.Direction : waterData.Wind.Direction);
            _crestOceanRenderer._globalWindSpeed += (waterData.Wave.Height > 0.0f) ? waterData.Wave.Height : 0.0f;
        }

        /// <summary>
        /// Sets the ocean visibility at distance.
        /// </summary>
        /// <param name="waterData">A WaterData class instance that represents the received water data.</param>
        private void SetVisibility(WaterData waterData)
        {
            _crestOceanRenderer.OceanMaterial.SetFloat(kSmoothnessFar, (float)waterData.Visibility / kVisibilityToMaterial);
            if(_crestOceanRenderer.OceanMaterial.GetFloat(kSmoothnessFar) > 1f)
            {
                _crestOceanRenderer.OceanMaterial.SetFloat(kSmoothnessFar, 1f);
            }
        }
#endif
        #endregion
    }
}
