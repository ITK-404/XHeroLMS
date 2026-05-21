//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if UNITY_EDITOR
using UnityEditor;
#endif
#if ATMOS_PRESENT
using Mewlist.MassiveClouds;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using RealTimeWeather.Managers;
using System.Collections.Generic;
using RealTimeWeather.Data;

namespace RealTimeWeather.WeatherControllers
{
    /// <summary>
    /// Class used to simulate weather using Massive Clouds Atmos plug-in.
    /// </summary>
    public class AtmosModuleController : MonoBehaviour
    {
#if ATMOS_PRESENT
        #region Private Const Variables
        private const string kAtmosPadHighStr = "AtmosPadHigh";
        private const string kAtmosPadMiddleStr = "AtmosPadMiddle";
        private const string kSunStr = "Sun";
        
        private const float kSunXrotation = 21.691f;
        private const float kSunYrotation = 64.48f;
        private const float kSunZrotation = -144.611f;

        private const int kSunIntensity = 2;
        #endregion

        #region Private Variables
        [Header("Atmos References")]
        [SerializeField] private GameObject _mainCamera;
        [SerializeField] private Light _sun;
        [SerializeField] private AtmosPad _atmosPad;
        [SerializeField] private MassiveCloudsPhysicsCloud _atmosProfile;
        [SerializeField] private MassiveCloudsCameraEffect _cameraEffect;

        [Header("Atmos Visuals")]
        [SerializeField] private float _planetTilt = 23.5f;
        [SerializeField] private Vector3 _sunPosition = new Vector3(0, 3, -100);
        [SerializeField] private Color _sunColor = new Color(0.90f, 0.66f, 0.12f);

        private Dictionary<WeatherState, float> _weatherStateY = new Dictionary<WeatherState, float>();
        #endregion

        #region Public Variables
        [Header("Profile Pad Y")]
        public float stateClearY = 1f;
        public float statePartlyClearY = 0.8f;
        public float stateSunnyY = 0.6f;
        public float statePartlySunnyY = 1f;
        public float stateCloudyY = 0.4f;
        public float statePartlyCloudyY = 0.6f;
        public float stateThunderstormsY = 0.2f;
        public float stateRainPrecipitationY = 0.2f;
        public float stateRainSnowPrecipitationY = 0.2f;
        public float stateSnowPrecipitationY = 0.2f;
        public float stateFairY = 0.5f;
        public float stateMistY = 0f;
        public float stateWindyY = 0.4f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (RealTimeWeatherManager.instance)
            {
                SetProfileY();
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate += OnWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick += OnForecastWeatherUpdate;
            }
        }

        private void OnDestroy()
        {
            if (RealTimeWeatherManager.instance)
            {
                RealTimeWeatherManager.instance.OnCurrentWeatherUpdate -= OnWeatherUpdate;
                ForecastModule.OnForecastProgressModuleTick -= OnForecastWeatherUpdate;
            }
        }
        #endregion

        #region Create Instance
        /// <summary>
        /// This function creates the "AtmosPad" instance and adds it as a child to the "RealTimeWeatherManager" manager.
        /// </summary>
        public void CreateAtmosManagerInstance()
        {
#if UNITY_EDITOR
            _atmosPad = FindObjectOfType<AtmosPad>();

            if (_atmosPad == null)
            {
                GameObject atmosPadPrefab = null;

#if UNITY_STANDALONE
                atmosPadPrefab = RealTimeWeatherManager.GetPrefab(kAtmosPadHighStr);
#endif //UNITY_STANDALONE

#if UNITY_ANDROID || UNITY_IOS
                atmosPadPrefab = RealTimeWeatherManager.GetPrefab(kAtmosPadMiddleStr);
#endif //UNITY_ANDROID || UNITY_IOS
                if (atmosPadPrefab)
                {
                    _atmosPad = Instantiate(atmosPadPrefab).GetComponent<AtmosPad>();
                    _atmosPad.transform.SetParent(transform);
                }
            }
            FixAtmosPreloadedShaders();
            FixAtmosPhysicsProfile();
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Initializes Massive Clouds Atmos components.
        /// </summary>
        public void InitializeAtmos()
        {
#if UNITY_EDITOR
            if (_atmosPad == null)
            {
                Debug.LogError("Could not create Atmos Instance");
                return;
            }
            SetPlanetTilt();
            SetSun();
            SetCamera();
#endif //UNITY_EDITOR
        }
        #endregion

        #region Events
        /// <summary>
        /// Handles the weather data update event.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the received weather data.</param>
        private void OnWeatherUpdate(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                SetHour(weatherData.DateTime.Hour, weatherData.DateTime.Minute);
                _atmosPad.SetVariation(_weatherStateY[weatherData.WeatherState]);
            }
        }

        /// <summary>
        /// Handles the forecast weather data update event.
        /// </summary>
        /// <param name="currentWeatherData">The current weather data</param>
        /// <param name="nextWeatherData">The next weather data</param>
        /// <param name="progressOfWeather">The progress between the current and next weather data transition</param>
        private void OnForecastWeatherUpdate(WeatherData currentWeatherData, WeatherData nextWeatherData, double progressOfWeather)
        {
            if (currentWeatherData != null && nextWeatherData != null)
            {
                SetHour(currentWeatherData.DateTime.Hour, currentWeatherData.DateTime.Minute);
                float atmosPadY = Mathf.Lerp(_weatherStateY[currentWeatherData.WeatherState], _weatherStateY[nextWeatherData.WeatherState], (float)progressOfWeather);
                _atmosPad.SetVariation(atmosPadY);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// This function destroys the Atmos components.
        /// </summary>
        public void DestroyAtmosComponents()
        {
            DestroyImmediate(_cameraEffect);
        }
        #endregion

        #region Private Methods
        private void SetHour(float hour, float minute)
        {
            float time = hour + (minute / 60);
            ///SetHour(1.5f) = 01:30 :)
            _atmosPad.SetHour(time);
        }

        private void SetPlanetTilt()
        {
#if UNITY_EDITOR
            SerializedObject serializedPadObj = new SerializedObject(_atmosPad);
            SerializedProperty massiveCloudsProfile = serializedPadObj.FindProperty("earthTilt");
            massiveCloudsProfile.floatValue = _planetTilt;
            serializedPadObj.ApplyModifiedProperties();
#endif //UNITY_EDITOR
        }

        public void SetPhysicsProfile(MassiveCloudsPhysicsCloud atmosProfile)
        {
#if UNITY_EDITOR
            _atmosProfile = atmosProfile;
            SerializedObject serializedPadObj = new SerializedObject(_atmosPad);
            SerializedProperty massiveCloudsProfile = serializedPadObj.FindProperty("massiveClouds");
            massiveCloudsProfile.objectReferenceValue = _atmosProfile;
            serializedPadObj.ApplyModifiedProperties();
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// This function sets the sun. If the Light component is not found in the scene, a new one is instantiated.
        /// </summary>
        private void SetSun()
        {
#if UNITY_EDITOR
            _sun = FindObjectOfType<Light>();

            if (_sun == null)
            {
                GameObject sunObject = new GameObject(kSunStr);
                sunObject.transform.SetParent(transform);
                sunObject.transform.position = _sunPosition;
                sunObject.transform.rotation = Quaternion.Euler(kSunXrotation, kSunYrotation, kSunZrotation);
                _sun = sunObject.AddComponent<Light>();
                _sun.type = LightType.Directional;
                _sun.color = _sunColor;
                _sun.lightmapBakeType = LightmapBakeType.Realtime;
                _sun.intensity = kSunIntensity;
                _sun.shadows = LightShadows.Soft;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            SerializedObject serializedPadObj = new SerializedObject(_atmosPad);
            SerializedProperty massiveCloudsPadSun = serializedPadObj.FindProperty("sun");
            massiveCloudsPadSun.objectReferenceValue = _sun;
            serializedPadObj.ApplyModifiedProperties();
#endif//UNITY_EDITOR
        }

        #region Setup Methods
        /// <summary>
        /// This function adds the MassiveCloudsCameraEffect component to the camera.
        /// </summary>
        private void SetCamera()
        {
#if UNITY_EDITOR
            _mainCamera = Camera.main.gameObject;
            if (_mainCamera != null)
            {
                _cameraEffect = _mainCamera.GetComponent<MassiveCloudsCameraEffect>();
                if (!_cameraEffect)
                {
                    _cameraEffect = _mainCamera.AddComponent<MassiveCloudsCameraEffect>();
                }

                SerializedObject serializedCameraObj = new SerializedObject(_cameraEffect);
                SerializedProperty massiveCloudsCameraSun = serializedCameraObj.FindProperty("sun");
                massiveCloudsCameraSun.objectReferenceValue = _sun;
                SerializedProperty massiveCloudsCameraProfile = serializedCameraObj.FindProperty("massiveClouds");
                massiveCloudsCameraProfile.objectReferenceValue = _atmosProfile;
                serializedCameraObj.ApplyModifiedProperties();
            }
#endif //UNITY_EDITOR
        }

        /// <summary>
        /// Adds the necessary references to the Preloaded Shaders
        /// </summary>
        private void FixAtmosPreloadedShaders()
        {
#if UNITY_EDITOR && ATMOS_PRESENT
            GraphicsSettings graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            SerializedObject serializedObject = new SerializedObject(graphicsSettingsObj);
            SerializedProperty arrayProp = serializedObject.FindProperty("m_PreloadedShaders");
            ShaderVariantCollection shaderVariantCollection = (ShaderVariantCollection)RealTimeWeatherManager.instance.GetObject("MassiveCloudsShaderVariants", "shadervariants");
            if (shaderVariantCollection)
            {
                int index = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(index);
                SerializedProperty item = arrayProp.GetArrayElementAtIndex(index);
                item.objectReferenceValue = shaderVariantCollection;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError("Could not load Shader Variant for Atmos");
            }
#endif
        }

        /// <summary>
        /// Adds the necessary references to the Physics Profile
        /// </summary>
        private void FixAtmosPhysicsProfile()
        {
#if ATMOS_PRESENT && UNITY_EDITOR
            MassiveCloudsPhysicsCloud atmosProfile = null;
#if UNITY_PIPELINE_HDRP
            atmosProfile = (MassiveCloudsPhysicsCloud)RealTimeWeatherManager.instance.GetObject("Physics HDRP", ".asset");
            if (atmosProfile == null)
            {
                atmosProfile = (MassiveCloudsPhysicsCloud)RealTimeWeatherManager.instance.GetObject("Physics StandardRP - Natural - High", ".asset");
            }
#else
            atmosProfile = (MassiveCloudsPhysicsCloud)RealTimeWeatherManager.instance.GetObject("Physics StandardRP - Natural - High", ".asset");
#endif
            if (atmosProfile)
            {
                SetPhysicsProfile(atmosProfile);
            }
            else
            {
                Debug.LogError("Could not assign the Atmos Physics Profile, please use the Atmos Setup Wizzard from Window/Massive Clouds Atmos");
            }
#endif
        }

        private void SetProfileY()
        {
            _weatherStateY = new Dictionary<WeatherState, float>
            {
                { WeatherState.Clear, stateClearY },
                { WeatherState.PartlyClear, statePartlyClearY },
                { WeatherState.Sunny, stateSunnyY },
                { WeatherState.PartlySunny, stateSunnyY },
                { WeatherState.Cloudy, stateCloudyY },
                { WeatherState.PartlyCloudy, statePartlyCloudyY },
                { WeatherState.Thunderstorms, stateThunderstormsY },
                { WeatherState.RainPrecipitation, stateRainPrecipitationY },
                { WeatherState.RainSnowPrecipitation, stateRainSnowPrecipitationY },
                { WeatherState.SnowPrecipitation, stateSnowPrecipitationY },
                { WeatherState.Fair, stateFairY },
                { WeatherState.Mist, stateMistY },
                { WeatherState.Windy, stateWindyY }
            };
        }
        #endregion
        #endregion
#endif //ATMOS_PRESENT
    }
}
