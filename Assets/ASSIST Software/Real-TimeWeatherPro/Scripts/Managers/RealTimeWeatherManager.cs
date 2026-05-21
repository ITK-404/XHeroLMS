//
// Copyright(c) 2020 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

#if EXPANSE_PRESENT
using Expanse;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

using System;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using RealTimeWeather.UI;
using RealTimeWeather.Enums;
using RealTimeWeather.Classes;
using RealTimeWeather.Simulation;
using RealTimeWeather.WeatherAtlas;
using RealTimeWeather.WeatherForYou;
using RealTimeWeather.WeatherUnderground;
using RealTimeWeather.Data;
using RealTimeWeather.WeatherControllers;
using RealTimeWeather.WeatherProvider.Metocean;
using RealTimeWeather.WeatherProvider.OpenWeather;
using RealTimeWeather.WeatherProvider;
using RealTimeWeather.WeatherProvider.Stormglass;
using RealTimeWeather.WeatherProvider.Tomorrow;
using RealTimeWeather.WeatherProvider.WeatherStation;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor;
#endif


namespace RealTimeWeather.Managers
{
    /// <summary>
    /// Class that represents the main Real-Time Weather manager, this allows the weather data request from WeatherAtlasModule, WeatherUndergroundModule, WeatherForYouModule
    /// also it manages the automatic weather data update and whether simulation using third-party support components: Enviro or Tenkoku.
    /// </summary>
    public class RealTimeWeatherManager : MonoBehaviour
    {
        #region Static Variables
        [SerializeField] public string RelativePath;
        #endregion

        #region Enums
        /// <summary>
        /// <para>
        /// RequestMode is an enumeration which specifies what service will be requesteFupdatd to get weather data.
        /// </para>
        /// <para>
        /// RequestMode enum values are:
        /// <br>0: RTW mode</br>
        /// <br>1: Tomorrow mode</br>
        /// <br>2: OpenWeatherMap mode</br>
        /// </para>
        /// </summary>
        public enum WeatherRequestMode
        {
            [Description("1: RTW mode")] RtwMode,
            [Description("2: Tomorrow mode")] TomorrowMode,
            [Description("3: OpenWeatherMap mode")] OpenWeatherMapMode,
            [Description("0: None")] None
        }

        public enum WaterRequestMode
        {
            [Description("1: Metocean mode")] MetoceanMode,
            [Description("2: Stormglass mode")] StormglassMode,
            [Description("3: Tomorrow mode")] TomorrowMode,
            [Description("0: None")] None,
        }

        public enum WeatherSystems
        {
            None = 0,
            Enviro,
            Tenkoku,
            Atmos,
            Expanse,
            EasySky
        }

        public enum WaterSystems
        {
            None = 0,
            KWS,
            Crest
        }
        #endregion

        #region Real-Time Weather Manager Instance
        private static RealTimeWeatherManager _instance;
        public static RealTimeWeatherManager instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<RealTimeWeatherManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Public Delegates
        public delegate void CurrentWeatherUpdate(WeatherData weatherData);
        public delegate void HourlyWeatherUpdate(List<WeatherData> weatherData);
        public delegate void DailyWeatherUpdate(List<WeatherData> weatherData);
        public delegate void CurrentWaterUpdate(WaterData waterData);
        public delegate void HourlyWaterUpdate(List<WaterData> waterData);
        public delegate void DailyWaterUpdate(List<WaterData> waterData);
        public delegate void RequestWeatherFromService(string city, string country);
        public delegate void RequestWeatherFromTomorrowService();

        public event CurrentWeatherUpdate OnCurrentWeatherUpdate;
        public event CurrentWeatherUpdate OnCurrentWeatherUpdateUI;
        public event HourlyWeatherUpdate OnHourlyWeatherUpdate;
        public event DailyWeatherUpdate OnDailyWeatherUpdate;
        public event CurrentWaterUpdate OnCurrentMaritimeUpdate;
        public event HourlyWaterUpdate OnHourlyMaritimeUpdate;
        public event DailyWaterUpdate OnDailyMaritimeUpdate;
        public event RequestWeatherFromService RequestAtlasWeather;
        public event RequestWeatherFromService RequestUndergroundWeather;
        public event RequestWeatherFromService RequestWeatherForYouWeather;
        #endregion

        #region Private Const Variables
        private const string kUpdateWeatherDataCoroutine = "UpdateWeatherData";
        private const string kMenuItemPath = "Real-time Weather/Real-Time Weather Manager";
        private const string kFeedbackFormMenuPath = "Real-time Weather/Help/Feedback Form";
        private const string kTutorialsMenuPath = "Real-time Weather/Help/Tutorials";
        private const string kAboutUsMenuPath = "Real-time Weather/Help/About Us";
        private const string kManagerObjectName = "RealTimeWeatherManager";
        private const string kPrefabExtension = ".prefab";
        private const string kPNGExtension = ".png";
        private const string kMaterialExtension = ".mat";
        private const string kAssetExtension = ".asset";
        private const string kHelp = "HELP";
        private const string kHelpUrl = "https://youtube.com/playlist?list=PLkbJAZKnBqf6zDp3NPI_1ek_wgEh3UgBo";
        private const string kAssist = "ASSIST";
        private const string kAssistUrl = "https://assist-software.net/about-us";
        // Atlas module constants
        private const string kAtlasServiceException = "Atlas service exception: ";
        private const string kAtlasObjectName = "WeatherAtlasModule";
        // Underground module constants
        private const string kUndergroundServiceException = "Underground service exception:";
        private const string kUndergroundObjectName = "WeatherUndergroundModule";
        // WeatherForYou module constants
        private const string kWeatherForYouServiceException = "Weather For You service exception:";
        private const string kWeatherForYouObjectName = "WeatherForYouModule";
        // OpenWeatherMap module constants
        private const string kOpenWeatherMapServiceException = "Open Weather Map service exception: ";
        private const string kOpenWeatherMapObjectName = "OpenWeatherMapModule";
        // Metocean module constants
        private const string kMetoceanServiceException = "Metocean service exception:";
        private const string kMetoceanModuleObjectName = "MetoceanModule";
        // Enviro module constants
        private const string kEnviroModulePrefabName = "EnviroModulePrefab";
        private const string kEnviroModuleObjName = "EnviroModule";
        // Tenkoku module constants
        private const string kTenkokuModulePrefabName = "TenkokuModulePrefab";
        private const string kTenkokuModuleObjectName = "TenkokuModule";
        private const string kTenkokuManagerPrefabName = "Tenkoku DynamicSky";
        // Atmos module constants
        private const string kAtmosModulePrefabName = "AtmosModulePrefab";
        private const string kAtmosModuleObjName = "AtmosModule";
        // Expanse module constants
        private const string kExpanseModulePrefabName = "ExpanseModulePrefab";
        private const string kExpanseModuleObjName = "ExpanseModule";
        // Crest module constants
        private const string kCrestModulePrefabName = "CrestModulePrefab";
        private const string kCrestModuleObjName = "CrestModule";
        // KWS module constants
        private const string kKWSModulePrefabName = "KWSModulePrefab";
        private const string kKWSModuleObjName = "KWSModule";
        // Easy Sky module constants
        private const string kEasySkyModulePrefabName = "EasySkyModulePrefab";
        private const string kEasySkyModuleObjName = "Easy Sky Module Controller";
        // Tomorrow module constants
        private const string kTomorrowServiceException = "Tomorrow.io service exception: ";
        // Alert module constants
        private const string kAlertModulePrefabName = "AlertModule_Prefab";
        private const string kAlertModuleObjectName = "Alert Module";
        // Geocoding
        private const string kIncorrectGeocodingData = "Latitude and longitude incorrect: ";
        private const string kUSStr = "US";
        private const string kCommaStr = ",";
        // UI
        private const string kWeatherUIModulePrefabName = "RealTimeWeatherUI_Prefab";
        private const string kWeatherUIModuleObjectName = "Real-Time Weather UI";
        //User-interface logs
        private const string kNoDataProviderSelected = "No data provider selected in Provider data!";
        private const string kIncorrectInputExceptionLog = "Invalid input data! (could not provide data for this location)";
        private const string kGeocodingExceptionLog = "Invalid input data! (could not provide location)";
        private const string kInvalidInputDataForbiddenLog = "Invalid input data! (server access forbidden)";
        private const string kInvalidInputDataUnauthorizedLog = "Invalid input data! (server access unauthorized)";
        private const string kWebPageParsingFailedLog = "Obtaining weather data failed!";
        private const string kServerConnectionFailedLog = "Server connection failed!";
        private const string kWebPageForbiddenAccessError = "403 Forbidden";
        private const string kApiUnauthorizedAccessError = "401 Unauthorized";
        private const string kApiBadRequestError = "400 Bad Request";
        private const string kApiNotFoundError = "404 Not Found";
        //Custom Slider
        private const string kSliderTexturesStr = "SliderTextures";
        private const string kForecastPresetsStr = "Weather Presets Folders";
        private const int kNumberOfIntervals = 24;
        #endregion

        #region Private Variables
        private WeatherStations _weatherStations;
        private WaitForSeconds _weatherUpdateWait;
        private WeatherAtlasModule _atlasModule;
        private WeatherUndergroundModule _undergroundModule;
        private WeatherForYouModule _weatherForYouModule;
        private ForecastModule _forecastModule;
        private AlertSystemModule _alertSystemModule;
        private ReverseGeocoding _reverseGeocoding;
        private EnviroModuleController _enviroModuleController;
        private TenkokuModuleController _tenkokuModule;
        private AtmosModuleController _atmosModuleController;
        private ExpanseModuleController _expanseModuleController;
        private EasySkyModuleController _easySkyModuleController;
        private CrestModuleController _crestModuleController;
        private KWSModuleController _kwsModuleController;

        private List<RequestWeatherFromService> _RTWWeatherProviders;
        private List<string> _RTWServiceException;

        [SerializeField] private RealTimeWeatherUI _realTimeWeatherUI;
        [SerializeField] private Canvas _weatherUICanvas;

        [SerializeField] private string _requestedCity;
        [SerializeField] private string _requestedState;
        [SerializeField] private string _requestedCountry;
        [SerializeField] private bool _unitedStatesLocation = false;
        [SerializeField] private bool _isCoroutineActive;
        [SerializeField] private bool _isAutoWeatherUpdateEnabled = true;
        [SerializeField] private bool _isWeatherSimulationEnabled;
        [SerializeField] private bool _dontDestroy = false;
        [SerializeField] private bool _visibleUI = true;
        [SerializeField] private int _autoUpdateRate = 1;
        [SerializeField] private int _typeOfSimulation;
        [SerializeField] private bool _isEnviroEnabled;
        [SerializeField] private bool _isTenkokuEnabled;
        [SerializeField] private bool _isAtmosEnabled;
        [SerializeField] private bool _isExpanseEnabled;
        [SerializeField] private bool _isEasySkyEnabled;
        [SerializeField] private bool _isCrestEnabled;

        [SerializeField] private WeatherRequestMode _weatherRequestMode = WeatherRequestMode.RtwMode;
        [SerializeField] private WeatherRequestMode _lastChosenRequestMode = WeatherRequestMode.RtwMode;
        [SerializeField] private WaterRequestMode _maritimeRequestMode = WaterRequestMode.MetoceanMode;
        [SerializeField] private WaterRequestMode _lastChosenWaterRequestMode = WaterRequestMode.MetoceanMode;
        [SerializeField] private List<string> _weatherProviders = new List<string> { "Weather Atlas", "Weather Underground", "Weather For You" };
        [SerializeField] private List<string> _weatherProvidersAddress = new List<string> { "https://www.weather-atlas.com", "https://www.wunderground.com", "https://www.weatherforyou.com" };
        [SerializeField] private List<int> _RTWWeatherProvidersIndexes = new List<int> { 0, 1, 2 };

        [SerializeField] private List<ForecastData> _simulationsDataProfiles = new List<ForecastData>();
        [SerializeField] private List<ForecastData> _providerSimulationsDataProfiles = new List<ForecastData>();
        [SerializeField] private List<string> _simulations = new List<string>();
        [SerializeField] private List<string> _providersSimulations = new List<string>();
        [SerializeField] private ForecastData _loadedSimulation;

        [SerializeField] private WeatherSystems _selectedWeatherSystem = WeatherSystems.None;
        [SerializeField] private WaterSystems _selectedWaterSystem = WaterSystems.None;
        [SerializeField]
        [Tooltip("It's a float value that represents a geographic coordinate that specifies the north–south position of a point on the Earth's surface.\nLatitude must be set according to ISO 6709.")]
        [Range(-90.0f, 90.0f)]
        private float _latitude;
        [SerializeField]
        [Tooltip("It's a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.\nLongitude must be set according to ISO 6709.")]
        [Range(-180.0f, 180.0f)]
        private float _longitude;
        [Range(0, 2)]
        private int _indexRTWWeatherProviders = 0;

        private ForecastData _currentSimulationSelected;
        private bool _isSavingTimelapse = false;
        private bool _hasRequestError;
        private string _errorMessage;
        #endregion

        #region Public Variables
        public int selectedSimulation;

        public bool showGeneralSettings;
        public bool showSimulationSettings;
        public bool showLocalizationSettings;
        public bool showAutoUpdateSettings;
        public bool showWeatherSimulationSettings;
        public bool showWaterSimulationSettings;
        public bool lastRequestMaritimeDataState;
        public bool isKWSEnabled;
        public bool popupOpen;

        public Vector2 mousePos;
        public Vector2 mouseLocalPos;
        public Vector2 mouseRealTimePosition;

        public TimelapsePopupTextures PopupTextures;
        public WeatherPresetFolders WeatherPresets;
        public ForecastWeatherData DefaultForecastData = new ForecastWeatherData();
        #if UNITY_EDITOR
        public RandomForecastPopupData RandomForecastPopupData;
        #endif
        public int DefaultForecastIndex = -1;
        public event Action<Timelapse, DateTime> UpdateTimelapse;

        #endregion

        #region Public Properties
        public string RequestedCity
        {
            get { return _requestedCity; }
            set { _requestedCity = value; }
        }

        public string RequestedCountry
        {
            get { return _requestedCountry; }
            set { _requestedCountry = value; }
        }

        public bool IsAutoWeatherEnabled
        {
            get { return _isAutoWeatherUpdateEnabled; }
            set
            {
                _isAutoWeatherUpdateEnabled = value;
                OnAutoWeatherStateChanged();
            }
        }

        public bool IsWeatherSimulationEnabled
        {
            get { return _isWeatherSimulationEnabled; }
            set { _isWeatherSimulationEnabled = value; }
        }

        public int AutoWeatherUpdateRate
        {
            get { return _autoUpdateRate; }
            set { _autoUpdateRate = value; }
        }

        public WeatherRequestMode WeatherDataRequestMode
        {
            get { return _weatherRequestMode; }
            set { _weatherRequestMode = value; }
        }

        public WaterRequestMode WaterDataRequestMode
        {
            get { return _maritimeRequestMode; }
            set { _maritimeRequestMode = value; }
        }

        public WeatherSystems SelectedWeatherSystem
        {
            get { return _selectedWeatherSystem; }
            set { _selectedWeatherSystem = value; }
        }

        public WaterSystems SelectedWaterSystem
        {
            get { return _selectedWaterSystem; }
            set { _selectedWaterSystem = value; }
        }

        public List<string> DataWeatherProviders
        {
            get { return _weatherProviders; }
            set { _weatherProviders = value; }
        }

        public List<string> DataWeatherProvidersAddress
        {
            get { return _weatherProvidersAddress; }
            set { _weatherProvidersAddress = value; }
        }

        public List<int> RTWDataWeatherProvidersIndexes
        {
            get { return _RTWWeatherProvidersIndexes; }
            set { _RTWWeatherProvidersIndexes = value; }
        }

        public WeatherRequestMode LastChosenRequestMode
        {
            get { return _lastChosenRequestMode; }
            set { _lastChosenRequestMode = value; }
        }

        public WaterRequestMode LastChosenWaterRequestMode
        {
            get { return _lastChosenWaterRequestMode; }
            set { _lastChosenWaterRequestMode = value; }
        }

        public float Latitude
        {
            get { return _latitude; }
            set { _latitude = value; }
        }

        public float Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

        public List<ForecastData> SimulationsDataProfiles
        {
            get { return _simulationsDataProfiles; }
            set { _simulationsDataProfiles = value; }
        }

        public List<ForecastData> ProviderSimulationsDataProfiles
        {
            get { return _providerSimulationsDataProfiles; }
            set { _providerSimulationsDataProfiles = value; }
        }

        public List<string> Simulations
        {
            get { return _simulations; }
            set { _simulations = value; }
        }

        public List<string> ProvidersSimulations
        {
            get { return _providersSimulations; }
            set { _providersSimulations = value; }
        }

        public ForecastData CurrentSimulationSelected
        {
            get { return _currentSimulationSelected; }
            set { _currentSimulationSelected = value; }
        }

        public ForecastData LoadedSimulation
        {
            get { return _loadedSimulation; }
            set { _loadedSimulation = value; }
        }

        public bool HasRequestError
        {
            get
            {
                return _hasRequestError;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
        }

        public bool IsEnviroEnabled
        {
            get { return _isEnviroEnabled; }
            set { _isEnviroEnabled = value; }
        }

        public bool IsTenkokuEnabled
        {
            get { return _isTenkokuEnabled; }
            set { _isTenkokuEnabled = value; }
        }

        public bool IsAtmosEnabled
        {
            get { return _isAtmosEnabled; }
            set { _isAtmosEnabled = value; }
        }

        public bool IsExpanseEnabled
        {
            get { return _isExpanseEnabled; }
            set { _isExpanseEnabled = value; }
        }

        public bool IsEasySkyEnabled
        {
            get { return _isEasySkyEnabled; }
            set { _isEasySkyEnabled = value; }
        }

        public bool IsCrestEnabled
        {
            get { return _isCrestEnabled; }
            set { _isCrestEnabled = value; }
        }

        public SimulationType WeatherSimulationType { get; set; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            _weatherStations = new WeatherStations();
            _reverseGeocoding = new ReverseGeocoding();
            _enviroModuleController = transform.GetComponentInChildren<EnviroModuleController>();
            _tenkokuModule = transform.GetComponentInChildren<TenkokuModuleController>();
            _atmosModuleController = transform.GetComponentInChildren<AtmosModuleController>();
            _alertSystemModule = GetComponentInChildren<AlertSystemModule>();
            _atlasModule = transform.GetComponentInChildren<WeatherAtlasModule>();
            _undergroundModule = transform.GetComponentInChildren<WeatherUndergroundModule>();
            _weatherForYouModule = transform.GetComponentInChildren<WeatherForYouModule>();
            RequestAtlasWeather += _atlasModule.StartWeatherServiceParser;
            RequestUndergroundWeather += _undergroundModule.StartWeatherServiceParser;
            RequestWeatherForYouWeather += _weatherForYouModule.StartWeatherServiceParser;

            _atlasModule.onWebPageParsed += OnReceivingAtlasWeatherData;
            _atlasModule.onExceptionRaised += OnRequestWeatherServiceExceptionRaised;
            _undergroundModule.onWebPageParsed += OnReceivingUndergroundWeatherData;
            _undergroundModule.onExceptionRaised += OnRequestWeatherServiceExceptionRaised;
            _weatherForYouModule.onWebPageParsed += OnReceivingWeatherForYouWeatherData;
            _weatherForYouModule.onExceptionRaised += OnRequestWeatherServiceExceptionRaised;

            _RTWWeatherProviders = new List<RequestWeatherFromService> { RequestWeatherFromAtlasService, RequestWeatherFromUndergroundService, RequestWeatherFromWeatherForYouService, };
            _RTWServiceException = new List<string> { kAtlasServiceException, kUndergroundServiceException, kWeatherForYouServiceException };

            SetUIVisibility(_visibleUI);

            if (LoadedSimulation == null) return;

            if (_loadedSimulation.WeatherSimulationType == SimulationType.UserData || IsForecastModeEnabled())
            {
                AddForecastComponent();
            }
        }

        private void Start()
        {
            if (Application.isPlaying && _dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
                RealTimeWeatherManager[] obj = FindObjectsOfType<RealTimeWeatherManager>();
                if (obj.Length != 1)
                {
                    Destroy(this.gameObject);
                }
            }

            if (Application.isPlaying && _loadedSimulation != null && _loadedSimulation.WeatherSimulationType != (int)SimulationType.UserData)
            {
                StartCoroutine(UpdateWeatherData());
                StartCoroutine(UpdateWaterData());
            }
            else
            {
                if (LoadedSimulation != null)
                {
                    if (LoadedSimulation.IsWeatherSimulationActive)
                    {
                        _realTimeWeatherUI.WeatherDataOn = true;
                    }

                    _forecastModule.OnUserDataReceived(LoadedSimulation);
                    StartCoroutine(UpdateWaterData());
                }
                else
                {
                    _realTimeWeatherUI.WeatherDataOn = true;
                    InterrogateExceptionForUIDisplay(ExceptionType.SystemException, "No simulation has been loaded!");
                }

            }
        }

        private void OnDestroy()
        {
            RequestAtlasWeather -= _atlasModule.StartWeatherServiceParser;
            RequestUndergroundWeather -= _undergroundModule.StartWeatherServiceParser;
            RequestWeatherForYouWeather -= _weatherForYouModule.StartWeatherServiceParser;

            _atlasModule.onWebPageParsed -= OnReceivingAtlasWeatherData;
            _atlasModule.onExceptionRaised -= OnRequestWeatherServiceExceptionRaised;
            _undergroundModule.onWebPageParsed -= OnReceivingUndergroundWeatherData;
            _undergroundModule.onExceptionRaised -= OnRequestWeatherServiceExceptionRaised;
            _weatherForYouModule.onWebPageParsed -= OnReceivingWeatherForYouWeatherData;
            _weatherForYouModule.onExceptionRaised -= OnRequestWeatherServiceExceptionRaised;
        }
        #endregion

        #region Public Methods

        #region General Settings Methods
#if UNITY_EDITOR
        /// <summary>
        /// This function adds the "Real-Time Weather Manager Instance" option in the "Assets / Create / ..." menu.
        /// When the option is activated, a new GameObject will be instantiated with the RealTimeWeatherManager component on it.
        /// </summary>
        [MenuItem(kMenuItemPath, false, 1)]
        public static void CreateManagerInstance()
        {
            if (RealTimeWeatherManager.instance == null)
            {
                GameObject weatherManagerObj = new GameObject();
                weatherManagerObj.name = kManagerObjectName;
                weatherManagerObj.AddComponent<RealTimeWeatherManager>();

                GameObject atlasModuleObj = new GameObject();
                atlasModuleObj.name = kAtlasObjectName;
                atlasModuleObj.AddComponent<WeatherAtlasModule>();
                atlasModuleObj.transform.SetParent(weatherManagerObj.transform);

                GameObject undergroundModule = new GameObject();
                undergroundModule.name = kUndergroundObjectName;
                undergroundModule.AddComponent<WeatherUndergroundModule>();
                undergroundModule.transform.SetParent(weatherManagerObj.transform);

                GameObject weatherForYouModuleObj = new GameObject();
                weatherForYouModuleObj.name = kWeatherForYouObjectName;
                weatherForYouModuleObj.AddComponent<WeatherForYouModule>();
                weatherForYouModuleObj.transform.SetParent(weatherManagerObj.transform);
                Undo.RegisterCreatedObjectUndo(weatherManagerObj, weatherManagerObj.name);
                Selection.activeObject = weatherManagerObj;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

                GameObject alertModulePrefab = GetPrefab(kAlertModulePrefabName);

                if (alertModulePrefab != null)
                {
                    GameObject alertModuleInstance = Instantiate(alertModulePrefab, Vector3.zero, Quaternion.identity);
                    alertModuleInstance.transform.SetParent(weatherManagerObj.transform);
                    alertModuleInstance.name = kAlertModuleObjectName;
                    alertModuleInstance.SetActive(true);
                }

                GameObject weatherUIModulePrefab = GetPrefab(kWeatherUIModulePrefabName);

                if (weatherUIModulePrefab != null)
                {
                    GameObject weatherUIModuleInstance = Instantiate(weatherUIModulePrefab, Vector3.zero, Quaternion.identity);
                    weatherUIModuleInstance.transform.SetParent(weatherManagerObj.transform);
                    weatherUIModuleInstance.name = kWeatherUIModuleObjectName;
                    weatherUIModuleInstance.SetActive(true);
                }
            }
            RealTimeWeatherManager.instance.LoadScriptableObjects();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        [MenuItem(kFeedbackFormMenuPath, false, 100)]
        public static void ShowFeedbackFormWindow()
        {
            FeedbackWindow.InitializeForm();
        }

        [MenuItem(kTutorialsMenuPath, false, 100)]
        public static void ShowTutorials()
        {
            Application.OpenURL(kHelpUrl);
        }

        [MenuItem(kAboutUsMenuPath, false, 111)]
        public static void ShowAboutUs()
        {
            Application.OpenURL(kAssistUrl);
        }
#endif // UNITY_EDITOR
        #endregion

        #region Simulation Settings Methods
        public void UpdateSimulationList()
        {
            Simulations.Clear();
            foreach (ForecastData profile in SimulationsDataProfiles)
            {
                Simulations.Add(profile.SimulationName);
            }

            ProvidersSimulations.Clear();
            foreach (ForecastData profile in ProviderSimulationsDataProfiles)
            {
                ProvidersSimulations.Add(profile.SimulationName);
            }
        }

        /// <summary>
        /// Handles the simulation activation event using Enviro plug-in. An instance of EnviroModulePrefab is created.
        /// </summary>
        public void ActivateEnviroSimulation()
        {
#if ENVIRO_PRESENT || ENVIRO_3
            GameObject enviroPrefab = GetPrefab(kEnviroModulePrefabName);

            if (enviroPrefab != null)
            {
                GameObject enviroModuleInstance = Instantiate(enviroPrefab, Vector3.zero, Quaternion.identity);
                enviroModuleInstance.name = kEnviroModuleObjName;
                enviroModuleInstance.transform.SetParent(transform);
                enviroModuleInstance.SetActive(true);
                _enviroModuleController = enviroModuleInstance.GetComponent<EnviroModuleController>();
                _enviroModuleController.CreateEnviroManager();
                _enviroModuleController.SetupEnviro();
                IsEnviroEnabled = true;
            }
#endif
        }

        /// <summary>
        /// Handles the simulation activation event using Tenkoku plug-in. An instance of TenkokuModulePrefab is created.
        /// </summary>
        public void ActivateTenkokuSimulation()
        {
            GameObject tenkokuModulePrefab = GetPrefab(kTenkokuModulePrefabName);

            if (tenkokuModulePrefab != null)
            {
                GameObject tenkokuModuleInstance = Instantiate(tenkokuModulePrefab, Vector3.zero, Quaternion.identity);
                tenkokuModuleInstance.name = kTenkokuModuleObjectName;
                tenkokuModuleInstance.transform.SetParent(transform);
                tenkokuModuleInstance.SetActive(true);
                _tenkokuModule = tenkokuModuleInstance.GetComponent<TenkokuModuleController>();
                _tenkokuModule.CreateTenkokuManagerInstance(GetPrefab(kTenkokuManagerPrefabName));
                IsTenkokuEnabled = true;
            }
        }

        /// <summary>
        /// Handles the simulation activation event using Massive Clouds Atmos plug-in. An instance of AtmosModulePrefab is created.
        /// </summary>
        public void ActivateAtmosSimulation()
        {
#if ATMOS_PRESENT
            if(_atmosModuleController != null && !IsAtmosEnabled)
            {
                 DestroyImmediate(_atmosModuleController.gameObject);
                 _atmosModuleController = null;
            }

            GameObject atmosPrefab = GetPrefab(kAtmosModulePrefabName);

            if (atmosPrefab != null)
            {
                GameObject atmosModuleInstance = Instantiate(atmosPrefab, Vector3.zero, Quaternion.identity);
                atmosModuleInstance.name = kAtmosModuleObjName;
                atmosModuleInstance.transform.SetParent(transform);
                atmosModuleInstance.SetActive(true);

                _atmosModuleController = atmosModuleInstance.GetComponent<AtmosModuleController>();

                _atmosModuleController.CreateAtmosManagerInstance();
                _atmosModuleController.InitializeAtmos();
                IsAtmosEnabled = true;
            }
#endif
        }

        /// <summary>
        /// Handles the simulation activation event using Expanse plug-in. An instance of ExpanseModulePrefab is created.
        /// </summary>
        public void ActivateExpanseSimulation()
        {
#if EXPANSE_PRESENT
            GameObject expansePrefab = GetPrefab(kExpanseModulePrefabName);

            if (expansePrefab != null)
            {
                GameObject expanseModuleInstance = Instantiate(expansePrefab, Vector3.zero, Quaternion.identity);
                expanseModuleInstance.name = kExpanseModuleObjName;
                expanseModuleInstance.transform.SetParent(transform);

                _expanseModuleController = expanseModuleInstance.GetComponent<ExpanseModuleController>();
                _expanseModuleController.CreateExpanseManagerInstance();
                IsExpanseEnabled = true;
            }
#endif
        }

        public void ActivateEasySkySimulation()
        {
#if EASYSKY_PRESENT
            var easySkyPrefab = GetPrefab(kEasySkyModulePrefabName);

            if(easySkyPrefab)
            {
                GameObject easySkyModuleInstance = Instantiate(easySkyPrefab, Vector3.zero, Quaternion.identity);
                easySkyModuleInstance.name = kEasySkyModuleObjName;
                easySkyModuleInstance.transform.SetParent(transform);

                _easySkyModuleController = easySkyModuleInstance.GetComponent<EasySkyModuleController>();
                _easySkyModuleController.CreateEasySkyManagerInstance(GetPrefab("EasySkyWeatherManager"));
                IsEasySkyEnabled = true;
            }
#endif
        }

        public void ActivateKWSSimulation()
        {
#if KWS_URP_PRESENT || KWS_HDRP_PRESENT
            GameObject kwsPrefab = GetPrefab(kKWSModulePrefabName);

            if(kwsPrefab != null)
            {
                GameObject kwsModuleInstance = Instantiate(kwsPrefab, Vector3.zero, Quaternion.identity);
                kwsModuleInstance.name = kKWSModuleObjName;
                kwsModuleInstance.transform.SetParent(transform);
      
                _kwsModuleController = kwsModuleInstance.GetComponent<KWSModuleController>();
                _kwsModuleController.CreateKWSWaterSystemInstance();
                isKWSEnabled = true;
            }
#endif //KWS_URP_PRESENT || KWS_HDRP_PRESENT
        }

        /// <summary>
        /// Handles the simulation activation event using Crest plug-in. An instance of CrestModulePrefab is created.
        /// </summary>
        public void ActivateCrestSimulation()
        {
#if CREST_HDRP_PRESENT || CREST_URP_PRESENT
            GameObject crestPrefab = GetPrefab(kCrestModulePrefabName);

            if (crestPrefab != null)
            {
                GameObject crestModuleInstance = Instantiate(crestPrefab, Vector3.zero, Quaternion.identity);
                crestModuleInstance.name = kCrestModuleObjName;
                crestModuleInstance.transform.SetParent(transform);

                _crestModuleController = crestModuleInstance.GetComponent<CrestModuleController>();
                _crestModuleController.CreateCrestManagerInstance();
                IsCrestEnabled = true;
            }
#endif
        }

        public void DeactivateAllWeather()
        {
            DeactivateAtmosSimulation();
            DeactivateEnviroSimulation();
            DeactivateExpanseSimulation();
            DeactivateTenkokuSimulation();
            DeactivateEasySkySimulation();
        }

        public void DeactivateAllWater()
        {
            DeactivateCrestSimulation();
            DeactivateKWSSimulation();
        }

        /// <summary>
        /// Handles the simulation disable event. The created instance of EnviroModulePrefab is destroyed.
        /// </summary>
        public void DeactivateEnviroSimulation()
        {
            _enviroModuleController = transform.GetComponentInChildren<EnviroModuleController>();
            _isEnviroEnabled = false;
            if (_enviroModuleController != null)
            {
                DestroyImmediate(_enviroModuleController.gameObject);
            }
        }

        /// <summary>
        /// Handles the simulation disable event. The created instance of TenkokuModulePrefab is destroyed.
        /// </summary>
        public void DeactivateTenkokuSimulation()
        {
            _tenkokuModule = transform.GetComponentInChildren<TenkokuModuleController>();
            IsTenkokuEnabled = false;

            if (_tenkokuModule != null)
            {
                DestroyImmediate(_tenkokuModule.gameObject);
            }
        }

        /// <summary>
        /// Handles the simulation disable event. The created instance of AtmosModulePrefab is destroyed.
        /// </summary>
        public void DeactivateAtmosSimulation()
        {
#if ATMOS_PRESENT
            _atmosModuleController = transform.GetComponentInChildren<AtmosModuleController>();

            IsAtmosEnabled = false;
            if (_atmosModuleController != null)
            {
                _atmosModuleController.DestroyAtmosComponents();
                DestroyImmediate(_atmosModuleController.gameObject);
            }
#endif
        }

        /// <summary>
        /// Handles the simulation disable event. The created instance of ExpanseModulePrefab is destroyed.
        /// </summary>
        public void DeactivateExpanseSimulation()
        {
#if EXPANSE_PRESENT && UNITY_EDITOR
            _expanseModuleController = transform.GetComponentInChildren<ExpanseModuleController>();
            IsExpanseEnabled = false;

            if (_expanseModuleController != null)
            {
                _expanseModuleController.RemoveProfileTransitions();
                DestroyImmediate(_expanseModuleController.gameObject);
            }
#endif
        }

        public void DeactivateEasySkySimulation()
        {
#if EASYSKY_PRESENT
            _easySkyModuleController = transform.GetComponentInChildren<EasySkyModuleController>();

            IsEasySkyEnabled = false;
            if (_easySkyModuleController != null)
            {
                DestroyImmediate(_easySkyModuleController.gameObject);
            }
#endif
        }

        /// <summary>
        /// Handles the simulation disable event. The created instance of CrestModulePrefab is destroyed.
        /// </summary>
        public void DeactivateCrestSimulation()
        {
#if CREST_HDRP_PRESENT || CREST_URP_PRESENT
            _crestModuleController = transform.GetComponentInChildren<CrestModuleController>();
            IsCrestEnabled = false;
            if(_crestModuleController != null)
            {
                DestroyImmediate(_crestModuleController.gameObject);
            } 
#endif
        }


        public void DeactivateKWSSimulation()
        {
#if KWS_URP_PRESENT || KWS_HDRP_PRESENT
            _kwsModuleController = transform.GetComponentInChildren<KWSModuleController>();

            isKWSEnabled = false;
            if(_kwsModuleController != null)
            {
                DestroyImmediate(_kwsModuleController.gameObject);
            }
#endif // KWS_URP_PRESENT || KWS_HDRP_PRESENT
        }

        /// <summary>
        /// Finds and returns prefab specified by name.
        /// </summary>
        /// <param name="prefabName">A string value that represents the prefab name.</param>
        public static GameObject GetPrefab(string prefabName)
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets(prefabName);
            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[idx]);
                if (path.Contains(kPrefabExtension))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
#endif
            return null;
        }

        /// <summary>
        /// Finds and returns the object specified by name.
        /// </summary>
        /// <param name="name">A string value that represents the object name.</param>
        /// <param name="extension">A string value that represents the object extension.</param>
        /// <returns></returns>
        public UnityEngine.Object GetObject(string name, string extension)
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets(name);
            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[idx]);
                if (path.Contains(extension))
                {
                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                }
            }
#endif
            return null;
        }
        #endregion
        #endregion

        #region Public Events Methods
        /// <summary>
        /// Request weather data using city and country names.
        /// </summary>
        /// <param name="city">A string value that represents the city for which the weather is requested.</param>
        /// <param name="country">A string value that represents the country.</param>
        public void RequestWeatherByCityAndCountry(string city, string country)
        {
            if (!city.Equals(string.Empty) && !country.Equals(string.Empty))
            {
                if (_RTWWeatherProviders.Count() > 0)
                {
                    _RTWWeatherProviders[_RTWWeatherProvidersIndexes[_indexRTWWeatherProviders]](city, country);
                }
            }
            else
            {
                InterrogateExceptionForUIDisplay(ExceptionType.InvalidInputData, kGeocodingExceptionLog);
            }
        }

        /// <summary>
        /// Request weather data using city and state names (exclusively for US locations).
        /// </summary>
        /// <param name="city">A string value that represents the city for which the weather is requested.</param>
        /// <param name="country">A string value that represents the state to which the city belongs.</param>
        public void RequestWeatherByCityAndState(string city, string state)
        {
            if (!city.Equals(string.Empty) && !state.Equals(string.Empty))
            {
                if (_RTWWeatherProviders.Count() > 0)
                {
                    _RTWWeatherProviders[_RTWWeatherProvidersIndexes[_indexRTWWeatherProviders]](state + ", " + city, "USA");
                }
            }
            else
            {
                InterrogateExceptionForUIDisplay(ExceptionType.InvalidInputData, kGeocodingExceptionLog);
            }
        }

        /// <summary>
        /// Requests weather data using geographical coordinates.
        /// </summary>
        /// <param name="latitude">Is a float value that represets a geographic coordinate that specifies the north–south position of a point on the Earth's surface.</param>
        /// <param name="longitude">Is a float value that represents a geographic coordinate that specifies the east-west position of a point on the Earth's surface.</param>
        public IEnumerator RequestWeatherByGeoCoordinates(float latitude, float longitude)
        {
            // Atlas, WeatherForYou, and Weather Underground services use city and country names to get weather data.
            // For this reason, reverse geocoding must be applied.
            // Reverse geocoding is a process that converts latitude and longitude to readable locality properties.
            int index = -1;
            string locality = string.Empty;
            GeocodingData reverseGeoData;
            double lat = latitude;
            double lon = longitude;

            WeatherStationData[] stationData = new WeatherStationData[0];
            double[] distances = new double[0];

            //Tries to find the country and location of a specified coordinate if it cant retrieve the data it tries to get the data from neighbouring locations
            do
            {
                CoroutineWithData reverseGeoCoroutine = new CoroutineWithData(this, _reverseGeocoding.RequestGeocodingInformation((float)lat, (float)lon));
                yield return reverseGeoCoroutine.Coroutine;

                reverseGeoData = (GeocodingData)reverseGeoCoroutine.Result;

                if (reverseGeoData != null)
                {
                    locality = string.Empty;
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.District))
                    {
                        locality = reverseGeoData.Address.District;
                    }
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.County))
                    {
                        locality = reverseGeoData.Address.County;
                    }
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.Municipality))
                    {
                        locality = reverseGeoData.Address.Municipality;
                    }
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.City))
                    {
                        locality = reverseGeoData.Address.City;
                    }
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.Town))
                    {
                        locality = reverseGeoData.Address.Town;
                    }
                    if (!string.IsNullOrEmpty(reverseGeoData.Address.Village))
                    {
                        locality = reverseGeoData.Address.Village;
                    }
                }

                //If the user coordinates return the right information we dont search the weahter station list
                if (index == -1 && (locality == string.Empty || reverseGeoData.Address.Country == string.Empty))
                {
                    _weatherStations.ClosestWeatherStations(lat, lon, out stationData, out distances);
                }

                index++;
                if (index < stationData.Length)
                {
                    lat = stationData[index].Lat;
                    lon = stationData[index].Lon;
                }
            }
            while ((locality == string.Empty || reverseGeoData.Address.Country == string.Empty) && index <= stationData.Length);

            //If the location has been change notify the user through the UI
            if (index > 0 && index <= stationData.Length)
            {
                _realTimeWeatherUI.SetReserveLocationMessage("Could not retrieve data from Latitude: " + latitude + ", Longitude: " + longitude +
                                                     "\nData retreived from " + Utilities.ToTwoDecimals((float)distances[index - 1]) + " KM away at Latitude: " +
                                                     Utilities.ToTwoDecimals((float)stationData[index - 1].Lat) + ", Longitude: " + Utilities.ToTwoDecimals((float)stationData[index - 1].Lon), true);
            }
            else
            {
                _realTimeWeatherUI.SetReserveLocationMessage("", false);
            }

            if (locality == "Sofiivka")
            {
                //:(
                reverseGeoData.Address.Country = "Russia";
            }

            //Checks again if a valid location has been found otherwise it throws an error
            if (locality != string.Empty && reverseGeoData.Address.Country != string.Empty)
            {
                _requestedCity = locality;
                _requestedCountry = reverseGeoData.Address.Country;
                if (reverseGeoData.Address.CountryCode.ToUpper() == kUSStr)
                {
                    _requestedState = reverseGeoData.Address.State;
                    RequestWeatherByCityAndState(_requestedCity, _requestedState);
                }
                else
                {
                    RequestWeatherByCityAndCountry(_requestedCity, _requestedCountry);
                }
            }
            else
            {
                LogFile.Write(kIncorrectGeocodingData + latitude + " lat , " + longitude + " long");
                InterrogateExceptionForUIDisplay(ExceptionType.InvalidInputData, kGeocodingExceptionLog);
            }
        }

        /// <summary>
        /// Handles the automatic weather state changed event and triggers the Start/Stop for the UpdateWeatherData coroutine.
        /// </summary>
        public void OnAutoWeatherStateChanged()
        {
            if (_isAutoWeatherUpdateEnabled && LoadedSimulation != null && IsForecastModeEnabled() == false)
            {
                if (!_isCoroutineActive && Application.isPlaying)
                {
                    StartCoroutine(UpdateWeatherData());
                }
            }
            else if (_isCoroutineActive)
            {
                StopCoroutine(kUpdateWeatherDataCoroutine);
                _isCoroutineActive = false;
            }
        }

        public bool IsForecastModeEnabled()
        {
            switch (_loadedSimulation.WeatherRequestMode)
            {
                case WeatherRequestMode.TomorrowMode:
                    {
                        return _loadedSimulation.TomorrowSimulationData.IsForecastModeEnabled;
                    }
                case WeatherRequestMode.OpenWeatherMapMode:
                    {
                        return LoadedSimulation.OpenWeatherSimulationData.IsForecastModeEnabled;
                    }
            }

            // Other modes do not have forecast functionality
            return false;
        }

        public bool IsForecastComponentEnabled()
        {
            _forecastModule = gameObject.GetComponent<ForecastModule>();
            return _forecastModule != null;
        }

        public void AddForecastComponent()
        {
            gameObject.AddComponent<ForecastModule>();
            _forecastModule = gameObject.GetComponent<ForecastModule>();
        }

        /// <summary>
        /// Load an already existing timelapse
        /// </summary>
        /// <param name="timelapse">The timelapse that is being loaded</param>
        public void LoadTimelapse(Timelapse timelapse)
        {
            if (CurrentSimulationSelected == null) return;

            timelapse.timelapseName = CurrentSimulationSelected.SimulationName;
            if (CurrentSimulationSelected.forecastData != null && CurrentSimulationSelected.forecastData.Count != 0)
            {
                timelapse.defaultIntervalData = CurrentSimulationSelected.GetDefaultWeatherData();
                CurrentSimulationSelected.forecastData.RemoveAt(CurrentSimulationSelected.forecastData.Count - 1);
                FromWeatherDataToTimelapse(CurrentSimulationSelected.forecastData, timelapse);
                timelapse.loopSim = CurrentSimulationSelected.LoopSimulation;
                timelapse.lerpSpeed = CurrentSimulationSelected.LerpStrength;
                timelapse.simulationSpeed = CurrentSimulationSelected.simulationSpeed;
                timelapse.timelapseLongitude = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastLongitude;
                timelapse.timelapseLatitude = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastLatitude;
                timelapse.city = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastCity;
                timelapse.country = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastCountry;

                DateTime dateTime = DateTime.Parse(CurrentSimulationSelected.forecastData[0].forecastDateTime);
                timelapse.timelapseDate = dateTime.ToString("d", new CultureInfo("en-GB"));

                for (int i = 0; i < CurrentSimulationSelected.TimelapseDayNames.Count; i++)
                {
                    timelapse.timelapseDays[i].dayName = CurrentSimulationSelected.TimelapseDayNames[i];
                }
            }
        }

        public void LoadForecastTimelapse(Timelapse timelapse)
        {
            timelapse.timelapseName = CurrentSimulationSelected.SimulationName;
            if (CurrentSimulationSelected.forecastData != null && CurrentSimulationSelected.forecastData.Count != 0)
            {
                timelapse.defaultIntervalData = CurrentSimulationSelected.GetDefaultWeatherData();
                CurrentSimulationSelected.IntervalStates.Add(CurrentSimulationSelected.IntervalStates[CurrentSimulationSelected.IntervalStates.Count - 1]);
                CurrentSimulationSelected.IntervalPresetName.Add(CurrentSimulationSelected.IntervalPresetName[CurrentSimulationSelected.IntervalPresetName.Count - 1]);
                CurrentSimulationSelected.IntervalName.Add(CurrentSimulationSelected.IntervalName[CurrentSimulationSelected.IntervalName.Count - 1]);
                FromWeatherDataToTimelapse(CurrentSimulationSelected.forecastData, timelapse);
                timelapse.loopSim = CurrentSimulationSelected.LoopSimulation;
                timelapse.lerpSpeed = CurrentSimulationSelected.LerpStrength;
                timelapse.simulationSpeed = CurrentSimulationSelected.simulationSpeed;
                timelapse.timelapseLongitude = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastLongitude;
                timelapse.timelapseLatitude = CurrentSimulationSelected.forecastData[0].forecastLocation.forecastLatitude;

                DateTime dateTime = DateTime.Parse(CurrentSimulationSelected.forecastData[0].forecastDateTime);
                timelapse.timelapseDate = dateTime.ToString("d", new CultureInfo("en-GB"));

                for (int i = 0; i < CurrentSimulationSelected.TimelapseDayNames.Count; i++)
                {
                    timelapse.timelapseDays[i].dayName = CurrentSimulationSelected.TimelapseDayNames[i];
                }
            }
        }

        /// <summary>
        /// Set the Real-Time Weather UI visibility 
        /// </summary>
        public void SetUIVisibility(bool state)
        {
            _visibleUI = state;
            if (_realTimeWeatherUI == null)
            {
                _realTimeWeatherUI = transform.GetComponentInChildren<RealTimeWeatherUI>();
                _weatherUICanvas = _realTimeWeatherUI.GetComponent<Canvas>();
            }
            _weatherUICanvas.enabled = state;
        }
        #endregion // Public Methods

        #region Private Methods

        #region Private Events Methods


        /// <summary>
        /// Converts a WeatherData list to a Timelapse object
        /// </summary>
        /// <param name="weatherDatas">The Weather Data list</param>
        /// <param name="timelapse">The output Timelapse object</param>
        private void FromWeatherDataToTimelapse(List<ForecastWeatherData> weatherDatas, Timelapse timelapse)
        {
            DateTime firstDay = DateTime.Parse(weatherDatas[0].forecastDateTime);

            int currentInterval;
            for (int i = 0; i < weatherDatas.Count; i++)
            {
                int currentDay = (DateTime.Parse(weatherDatas[i].forecastDateTime) - firstDay).Days;
                if (currentDay != 0)
                {
                    timelapse.AddNewDefaultDay();
                }

                timelapse.timelapseDays[currentDay].selectedSlider = -1;
                timelapse.timelapseDays[currentDay].startedInside = new List<bool>();
                timelapse.timelapseDays[currentDay].sliderRelPosX = new List<float>();
                timelapse.timelapseDays[currentDay].delimitersPos = new bool[kNumberOfIntervals + 1];
                timelapse.timelapseDays[currentDay].lastDelimiterPos = -1;
                timelapse.timelapseDays[currentDay].timelapseIntervals = new List<TimelapseInterval>();

                currentInterval = 0;
                while (i < weatherDatas.Count && (DateTime.Parse(weatherDatas[i].forecastDateTime) - firstDay).Days == currentDay)
                {
                    var interval = new TimelapseInterval(weatherDatas[i]);
                    interval.intervalState = CurrentSimulationSelected.IntervalStates[i];

                    interval.weatherDataProfileStr = CurrentSimulationSelected.IntervalPresetName[i];
                    interval.presetName = CurrentSimulationSelected.IntervalName[i];
                    interval.presetColor = weatherDatas[i].color;

                    if (timelapse.timelapseDays[currentDay].timelapseIntervals.Count != 0)
                    {
                        int hour = DateTime.Parse(weatherDatas[i].forecastDateTime).Hour;
                        timelapse.timelapseDays[currentDay].sliderRelPosX.Add((float)(hour) / kNumberOfIntervals);
                        timelapse.timelapseDays[currentDay].delimitersPos[hour] = true;
                        timelapse.timelapseDays[currentDay].startedInside.Add(false);
                        timelapse.timelapseDays[currentDay].AddInterval(currentInterval, interval);
                    }
                    else
                    {
                        timelapse.timelapseDays[currentDay].AddInterval(currentInterval, interval);
                    }
                    currentInterval++;
                    i++;
                }
                i--;
            }

        }

        /// <summary>
        /// Requests the weather data.
        /// </summary>
        private void RequestWeather()
        {
            if (!_loadedSimulation.IsWeatherSimulationActive) return;

            _realTimeWeatherUI.WeatherDataOn = true;
            _indexRTWWeatherProviders = 0;
            _hasRequestError = false;
            switch (_loadedSimulation.WeatherRequestMode)
            {
                case WeatherRequestMode.None:
                    LogFile.Write(kNoDataProviderSelected);
                    _realTimeWeatherUI.WeatherDataOn = false;
                    break;
                case WeatherRequestMode.RtwMode:
                    if (LoadedSimulation.RtwSimulationData.RequestedCity == string.Empty && LoadedSimulation.RtwSimulationData.RequestedCountry == string.Empty)
                    {
                        StartCoroutine(RequestWeatherByGeoCoordinates(LoadedSimulation.RtwSimulationData.Latitude, LoadedSimulation.RtwSimulationData.Longitude));
                    }
                    else if (LoadedSimulation.RtwSimulationData.IsUSALocation)
                    {
                        RequestWeatherByCityAndState(LoadedSimulation.RtwSimulationData.RequestedCity, LoadedSimulation.RtwSimulationData.RequestedCountry);
                    }
                    else
                    {
                        RequestWeatherByCityAndCountry(LoadedSimulation.RtwSimulationData.RequestedCity, LoadedSimulation.RtwSimulationData.RequestedCountry);
                    }
                    break;
                case WeatherRequestMode.TomorrowMode:
                    RequestWeatherDataFromTomorrowService();
                    break;
                case WeatherRequestMode.OpenWeatherMapMode:
                    RequestOpenWeatherMapData();
                    break;
                default:
                    break;
            }
        }

        private async void RequestOpenWeatherMapData()
        {
            var openWeatherRequest = new OpenWeatherRequest(LoadedSimulation.OpenWeatherSimulationData);
            openWeatherRequest.RaiseException += OnOpenWeatherMapServiceExceptionRaised;
            if (LoadedSimulation.OpenWeatherSimulationData.IsForecastModeEnabled)
            {
                var result = await openWeatherRequest.RequestOpenWeatherPeriodData();
                OnReceivingOpenWeatherMapOneCallAPIData(result);
            }
            else
            {
                var result = await openWeatherRequest.RequestOpenWeatherData();
                OnReceivingOpenWeatherMapData(result);
            }
            openWeatherRequest.RaiseException -= OnOpenWeatherMapServiceExceptionRaised;
        }

        private void RequestWater()
        {
            _realTimeWeatherUI.MaritimeDataOn = true;

            if (!_loadedSimulation.IsWaterSimulationActive)
            {
                _realTimeWeatherUI.MaritimeDataOn = false;
                return;
            }

            switch (_loadedSimulation.WaterRequestMode)
            {
                case WaterRequestMode.MetoceanMode:
                    RequestMetoceanData();
                    break;
                case WaterRequestMode.StormglassMode:
                    SendStormglassAPIRequests();
                    break;
                case WaterRequestMode.TomorrowMode:
                    RequestTomorrowWaterData();
                    break;
            }
        }

        private async void SendStormglassAPIRequests()
        {
            var stormglassWater = new StormglassWaterRequest(_loadedSimulation.StormglassSimulationData);
            stormglassWater.RaiseException += OnExceptionRaised;
            var response = await stormglassWater.SendStormglassAPIRequests();

            if (response != null)
            {
                OnReceiveStormglassWaterData(response);
            }
            stormglassWater.RaiseException -= OnExceptionRaised;
        }

        private async void RequestMetoceanData()
        {
            var metoceanWater = new MetoceanWaterDataRequest(_loadedSimulation.MetoceanWaterSimulationData);
            metoceanWater.RaiseException += OnMetoceanServiceExceptionRaised;
            var response = await metoceanWater.RequestMetoceanData();
            OnReceivingMetoceanWeatherData(response);
            metoceanWater.RaiseException -= OnMetoceanServiceExceptionRaised;
        }

        private async void RequestTomorrowWaterData()
        {
            var tomorrowWater = new TomorrowWaterDataRequest(_loadedSimulation.TomorrowWaterSimulationData);
            tomorrowWater.RaiseException += OnTomorrowServiceExceptionRaised;

            var respone = await tomorrowWater.RequestTomorrowData();
            OnReceivingTomorrowWaterData(respone);
            tomorrowWater.RaiseException -= OnTomorrowServiceExceptionRaised;
        }

        /// <summary>
        /// Send notification with updated current weather data to the components that listen to the OnCurrentWeatherUpdate event.
        /// </summary>
        /// <param name="currentWeatherData">A WeatherData class instance that represents the current weather data received from services.</param>
        private void NotifyCurrentWeatherChanged(WeatherData currentWeatherData)
        {
            OnCurrentWeatherUpdate?.Invoke(currentWeatherData);
        }

        /// <summary>
        /// Send notification with updated current weather data to the UI component that listens to the OnCurrentWeatherUpdate event.
        /// </summary>
        /// <param name="currentWeatherData">A WeatherData class instance that represents the current weather data received from services.</param>
        private void NotifyCurrentWeatherUIChanged(WeatherData currentWeatherData)
        {
            OnCurrentWeatherUpdateUI?.Invoke(currentWeatherData);
        }

        /// <summary>
        /// If hourly weather data is present, send notification with updated hourly weather data to the components that listen to the OnHourlyWeatherUpdate event.
        /// </summary>
        /// <param name="hourlyWeatherData">A WeatherData class instance list that represents the hourly weather data received from services.</param>
        private void NotifyHourlyWeatherChanged(List<WeatherData> hourlyWeatherData)
        {
            OnHourlyWeatherUpdate?.Invoke(hourlyWeatherData);
        }

        /// <summary>
        /// If daily weather data is present, send notification with updated daily weather data to the components that listen to the OnDailyWeatherUpdate event.
        /// </summary>
        /// <param name="dailyWeatherData">A WeatherData class instance list that represents the daily weather data received from services.</param>
        private void NotifyDailyWeatherChanged(List<WeatherData> dailyWeatherData)
        {
            OnDailyWeatherUpdate?.Invoke(dailyWeatherData);
        }

        /// <summary>
        /// Send notification with updated current maritime data to the components that listen to the OnCurrentMaritimeUpdate event.
        /// </summary>
        /// <param name="currentMaritimeData">A MaritimeData class instance that represents the current maritime data received from services.</param>
        private void NotifyCurrentWaterChanged(WaterData currentMaritimeData)
        {
            OnCurrentMaritimeUpdate?.Invoke(currentMaritimeData);
        }

        /// <summary>
        /// If hourly maritime data is present, send notification with updated hourly maritime data to the components that listen to the OnHourlyMaritimeUpdate event.
        /// </summary>
        /// <param name="hourlyMaritimeData">A MaritimeData class instance list that represents the hourly maritime data received from services.</param>
        private void NotifyHourlyMaritimeChanged(List<WaterData> hourlyMaritimeData)
        {
            OnHourlyMaritimeUpdate?.Invoke(hourlyMaritimeData);
        }

        /// <summary>
        /// If daily maritime data is present, send notification with updated daily maritime data to the components that listen to the OnDailyMaritimeUpdate event.
        /// </summary>
        /// <param name="dailyMaritimeData">A MaritimeData class instance list that represents the daily maritime data received from services.</param>
        private void NotifyDailyMaritimeChanged(List<WaterData> dailyMaritimeData)
        {
            OnDailyMaritimeUpdate?.Invoke(dailyMaritimeData);
        }

        /// Requests the weather data from the Atlas service.
        /// </summary>
        /// <param name="city">A string value that represents the city for which the weather is requested.</param>
        /// <param name="country">A string value that represents the country.</param>
        private void RequestWeatherFromAtlasService(string city, string country)
        {
            RequestAtlasWeather?.Invoke(city, country);
        }

        /// <summary>
        /// Requests the weather data from the Underground service.
        /// </summary>
        /// <param name="city">A string value that represents the city for which the weather is requested.</param>
        /// <param name="country">A string value that represents the country.</param>
        private void RequestWeatherFromUndergroundService(string city, string country)
        {
            RequestUndergroundWeather?.Invoke(city, country);
        }

        /// <summary>
        /// Requests the weather data from the Weather For You service.
        /// </summary>
        /// <param name="city">A string value that represents the city for which the weather is requested.</param>
        /// <param name="country">A string value that represents the country.</param>
        private void RequestWeatherFromWeatherForYouService(string city, string country)
        {
            RequestWeatherForYouWeather?.Invoke(city, country);
        }

        /// <summary>
        /// Requests the weather data from the Tomorrow service.
        /// </summary>
        private async void RequestWeatherDataFromTomorrowService()
        {
            var tomorrowWeatherDataRequest = new TomorrowWeatherDataRequest(_loadedSimulation.TomorrowSimulationData);
            tomorrowWeatherDataRequest.RaiseException += OnTomorrowServiceExceptionRaised;

            var respone = await tomorrowWeatherDataRequest.GetData();

            OnReceivingTomorrowWeatherData(respone);
            tomorrowWeatherDataRequest.RaiseException -= OnTomorrowServiceExceptionRaised;
        }

        /// <summary>
        /// Handles the data receive event from Atlas service.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the weather data received from the Atlas service.</param>
        private void OnReceivingAtlasWeatherData(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                NotifyCurrentWeatherChanged(weatherData);
                NotifyCurrentWeatherUIChanged(weatherData);
            }
        }

        /// <summary>
        /// Handles the data receive event from Underground service.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the weather data received from the Underground service.</param>
        private void OnReceivingUndergroundWeatherData(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                NotifyCurrentWeatherChanged(weatherData);
                NotifyCurrentWeatherUIChanged(weatherData);
            }
        }

        /// <summary>
        /// Handles the data receive event from Weather For You service.
        /// </summary>
        /// <param name="weatherData">A WeatherData class instance that represents the weather data received from the Weather For You service.</param>
        private void OnReceivingWeatherForYouWeatherData(WeatherData weatherData)
        {
            if (weatherData != null)
            {
                _realTimeWeatherUI.SetWeatherForecastType(ForecastType.Current);
                NotifyCurrentWeatherChanged(weatherData);
                NotifyCurrentWeatherUIChanged(weatherData);
            }
        }

        /// <summary>
        /// Handles the data receive event from Tomorrow service.
        /// </summary>
        /// <param name="tomorrowData">A TomorrowData class instance that represents the weather data received from the Tomorrow service.</param>
        private void OnReceivingTomorrowWeatherData(TomorrowCoreData tomorrowData)
        {
            if (tomorrowData == null)
            {
                return;
            }

            TomorrowDataConverter tomorrowDataConverter = new TomorrowDataConverter(tomorrowData);
            if (LoadedSimulation.TomorrowSimulationData.IsForecastModeEnabled == false)
            {
                _realTimeWeatherUI.SetWeatherForecastType(ForecastType.Current);
                NotifyCurrentWeatherChanged(tomorrowDataConverter.ConvertCurrentTomorrowDataToRtwData());
                NotifyCurrentWeatherUIChanged(tomorrowDataConverter.ConvertCurrentTomorrowDataToRtwData());
            }
            else
            {
                List<WeatherData> forecastData = new List<WeatherData>();

                _realTimeWeatherUI.SetWeatherForecastType(ForecastType.Hourly);
                forecastData = tomorrowDataConverter.ConvertHourlyTomorrowDataToRtwData();
                forecastData.RemoveRange(108 + 1, forecastData.Count - 108 - 1);

                OnHourlyWeatherUpdate?.Invoke(forecastData);
                _forecastModule.OnProvidersDataReceived(forecastData, hourlyForecast: true, _loadedSimulation.TomorrowSimulationData.SimulationSpeed, _loadedSimulation.TomorrowSimulationData.IntervalLerpSpeed, true);
            }
        }

        /// <summary>
        /// Handles the data receive event from the Metocean service.
        /// </summary>
        /// <param name="metoceanData">A MetoceanData class instance that represents the weather data received from the Metocean API.</param>
        private void OnReceivingMetoceanWeatherData(MetoceanData metoceanData)
        {
            if (metoceanData != null)
            {
                MetoceanDataConverter converter = new MetoceanDataConverter(metoceanData, _loadedSimulation.MetoceanWaterSimulationData.NumberOfIntervals);
                NotifyCurrentWaterChanged(converter.ConvertCurrentMetoceanDataToWaterData());
                if (_loadedSimulation.WeatherRequestMode == WeatherRequestMode.None)
                {
                    NotifyCurrentWeatherChanged(converter.ConvertCurrentMetoceanDataToWeatherData());
                }
                if (_loadedSimulation.MetoceanWaterSimulationData.NumberOfIntervals != 0)
                {
                    NotifyHourlyMaritimeChanged(converter.ConvertIntervalMetoceanDataToWaterData());
                    if (_loadedSimulation.WeatherRequestMode == WeatherRequestMode.None)
                    {
                        NotifyHourlyWeatherChanged(converter.ConvertIntervalMetoceanDataToWeatherData());
                    }
                }
            }
        }

        /// <summary>
        /// Handles the data receive event from the Stormglass service.
        /// </summary>
        /// <param name="stormglassData">A Stormglass class instance that represents the weather data received from the Stormglass service.</param>
        private void OnReceiveStormglassWaterData(StormglassWeatherData stormglassData)
        {
            if (stormglassData != null)
            {
                StormglassDataConverter converter = new StormglassDataConverter();
                NotifyCurrentWaterChanged(converter.ConvertCurrentStormglassDataToWaterData(stormglassData));
                NotifyHourlyMaritimeChanged(converter.ConvertHourlyStormglassDataToWaterData(stormglassData));
                if (_loadedSimulation.WeatherRequestMode == WeatherRequestMode.None)
                {
                    NotifyCurrentWeatherChanged(converter.ConvertCurrentStormglassDataToWeatherData(stormglassData));
                    NotifyHourlyWeatherChanged(converter.ConvertHourlyStormglassDataToWeatherData(stormglassData));
                }
            }
        }

        /// <summary>
        /// Handles the data receive event from the Tomorrow.io service.
        /// </summary>
        /// <param name="tomorrowData">A TomorrowData class instance that represents the water data received from the Tomorrow service.</param>
        private void OnReceivingTomorrowWaterData(TomorrowCoreData tomorrowData)
        {
            if (tomorrowData == null)
            {
                return;
            }

            TomorrowDataConverter tomorrowDataConverter = new TomorrowDataConverter(tomorrowData);
            NotifyCurrentWaterChanged(tomorrowDataConverter.ConvertCurrentTomorrowMaritimeDataToRtwData());
            if (_weatherRequestMode == WeatherRequestMode.None)
            {
                NotifyCurrentWeatherChanged(tomorrowDataConverter.ConvertCurrentTomorrowDataToRtwData());
            }
        }

        /// <summary>
        /// Handles the data receive event from OpenWeatherMap service.
        /// </summary>
        /// <param name="weatherData">A OpenWeatherMapData class instance that represents the weather data received from the OpenWeatherMap service.</param>
        private void OnReceivingOpenWeatherMapData(OpenWeatherMapData weatherData)
        {
            if (weatherData != null)
            {
                OpenWeatherMapConverter converter = new OpenWeatherMapConverter(weatherData);
                _realTimeWeatherUI.SetWeatherForecastType(ForecastType.Current);
                NotifyCurrentWeatherChanged(converter.ConvertToRealTimeManagerWeatherData());
                NotifyCurrentWeatherUIChanged(converter.ConvertToRealTimeManagerWeatherData());
            }
        }

        /// <summary>
        /// Handles the data receive event from OpenWeatherMap service for OneCallAPI.
        /// </summary>
        /// <param name="weatherData">A OpenWeatherOneCallAPIMapData class instance that represents the weather data received from the Open Weather Map One Call API service.</param>
        private void OnReceivingOpenWeatherMapOneCallAPIData(OpenWeatherOneCallAPIMapData weatherData)
        {
            if (weatherData != null)
            {
                var converter = new OpenWeatherOneCallAPIMapConverter(weatherData);
                _realTimeWeatherUI.SetWeatherForecastType(ForecastType.Hourly);
                var forecastData = converter.ConvertHourlyWeatherDataToRealTimeManagerWeatherListData();

                OnHourlyWeatherUpdate?.Invoke(forecastData);
                _forecastModule.OnProvidersDataReceived(forecastData, hourlyForecast: true, LoadedSimulation.OpenWeatherSimulationData.SimulationSpeed, LoadedSimulation.OpenWeatherSimulationData.IntervalLerpSpeed, true);
            }
        }

        /// <summary>
        /// Handles the Request Weather service exception event.
        /// </summary>
        /// <param name="exception">An ExceptionType value that represents the exception type.</param>
        /// <param name="message">A string value that represents the exception message.</param>
        private void OnRequestWeatherServiceExceptionRaised(ExceptionType exception, string message)
        {
            if(_indexRTWWeatherProviders >= _RTWWeatherProviders.Count())
            {
                return;
            }

            LogFile.Write(_RTWServiceException[_RTWWeatherProvidersIndexes[_indexRTWWeatherProviders]] + message);
            _indexRTWWeatherProviders++;
            if (_indexRTWWeatherProviders < _RTWWeatherProviders.Count())
            {
                if (_unitedStatesLocation)
                {
                    _RTWWeatherProviders[_RTWWeatherProvidersIndexes[_indexRTWWeatherProviders]](_requestedState + ", " + _requestedCity, "USA");
                }
                else
                {
                    _RTWWeatherProviders[_RTWWeatherProvidersIndexes[_indexRTWWeatherProviders]](_requestedCity, _requestedCountry);
                }
            }
            else
            {
                InterrogateExceptionForUIDisplay(exception, message);
            }
        }

        /// <summary>
        /// The message error contains too much technical data for an usual user
        /// The following method will change the message error with a more appropriate message
        /// </summary>
        /// <param name="exception">The type of the exception raised</param>
        /// <param name="message">The message error describing the exception</param>
        public void InterrogateExceptionForUIDisplay(ExceptionType exception, string message, bool weatherDataError = true)
        {
            switch (exception)
            {
                case ExceptionType.HTTPException:
                    if (message.Contains(kWebPageForbiddenAccessError))
                    {
                        message = kInvalidInputDataForbiddenLog;
                        exception = ExceptionType.InvalidInputData;
                    }
                    else if (message.Contains(kApiUnauthorizedAccessError))
                    {
                        message = kInvalidInputDataUnauthorizedLog;
                        exception = ExceptionType.InvalidInputData;
                    }
                    else if (message.Contains(kApiBadRequestError) || message.Contains(kApiNotFoundError))
                    {
                        message = kIncorrectInputExceptionLog;
                        exception = ExceptionType.InvalidInputData;
                    }
                    else
                    {
                        message = kServerConnectionFailedLog;
                    }
                    break;
                case ExceptionType.WebPageParseException:
                    message = kWebPageParsingFailedLog;
                    break;
                case ExceptionType.InvalidInputData:
                    message = kIncorrectInputExceptionLog;
                    break;
            }

            _hasRequestError = true;
            _errorMessage = message;
            _realTimeWeatherUI.SetDataErrorMessage(message, weatherDataError, exception);
        }

        /// <summary>
        /// Handles the Tomorrow service exception event.
        /// </summary>
        /// <param name="exception">An ExceptionType value that represents the exception type.</param>
        /// <param name="message">A string value that represents the exception message.</param>
        private void OnTomorrowServiceExceptionRaised(ExceptionType exception, string message)
        {
            LogFile.Write(kTomorrowServiceException + exception.ToString() + " => " + message);
            InterrogateExceptionForUIDisplay(exception, message);
        }

        /// <summary>
        /// Handles the OpenWeatherMap service exception event.
        /// </summary>
        /// <param name="exception">An ExceptionType value that represents the exception type.</param>
        /// <param name="message">A string value that represents the exception message.</param>
        private void OnOpenWeatherMapServiceExceptionRaised(ExceptionType exception, string message)
        {
            LogFile.Write(kOpenWeatherMapServiceException + exception.ToString() + " => " + message);
            InterrogateExceptionForUIDisplay(exception, message);
        }

        /// <summary>
        /// Handles the Metocean service exception event.
        /// </summary>
        /// <param name="exception">An ExceptionType value that represents the exception type.</param>
        /// <param name="message">A string value that represents the exception message.</param>
        private void OnMetoceanServiceExceptionRaised(ExceptionType exception, string message)
        {
            LogFile.Write(kMetoceanServiceException + exception.ToString() + " => " + message);
            InterrogateExceptionForUIDisplay(exception, message, weatherDataError: false);
        }

        /// <summary>
        /// Handles the exception event.
        /// </summary>
        /// <param name="exception">An ExceptionType value that represents the exception type.</param>
        /// <param name="message">A string value that represents the exception message.</param>
        private void OnExceptionRaised(ExceptionType exception, string message)
        {
            LogFile.Write(exception.ToString() + " => " + message);
            InterrogateExceptionForUIDisplay(exception, message, weatherDataError: false);
        }

        #endregion

        #region Auto Weather Data Update Methods
        /// <summary>
        /// This is the main functionality for the automatic weather data update co-routine.
        /// It requests the weather data from the services with the established frequency.
        /// </summary>
        private IEnumerator UpdateWaterData()
        {
            if (!_loadedSimulation.IsWaterSimulationActive)
            {
                _realTimeWeatherUI.MaritimeDataOn = false;
                yield break;
            }

            _isCoroutineActive = true;

            if (IsForecastModeEnabled() && _loadedSimulation.WaterRequestMode != WaterRequestMode.None)
            {
                // Fetch data once
                RequestWater();

                _isCoroutineActive = false;
                yield break;
            }

            if (_isAutoWeatherUpdateEnabled)
            {
                while (_isAutoWeatherUpdateEnabled)
                {
                    if (_maritimeRequestMode != WaterRequestMode.None)
                    {
                        RequestWater();
                    }
                    _weatherUpdateWait = new WaitForSeconds(Utils.ConvertMinutesToSeconds(_autoUpdateRate));
                    yield return _weatherUpdateWait;
                }
            }
            else
            {
                if (_maritimeRequestMode != WaterRequestMode.None)
                {
                    RequestWater();
                }
            }

            _isCoroutineActive = false;
            yield return null;
        }

        private IEnumerator UpdateWeatherData()
        {
            if (!_loadedSimulation.IsWeatherSimulationActive)
            {
                _realTimeWeatherUI.WeatherDataOn = false;
                yield break;
            }

            if (LoadedSimulation.WeatherSimulationType == SimulationType.UserData)
            {
                yield break;
            }

            _isCoroutineActive = true;

            if (IsForecastModeEnabled() && _loadedSimulation.WeatherRequestMode != WeatherRequestMode.None)
            {
                // Fetch data once
                RequestWeather();

                _isCoroutineActive = false;
                yield break;
            }

            if (_isAutoWeatherUpdateEnabled)
            {
                while (_isAutoWeatherUpdateEnabled)
                {
                    if (_weatherRequestMode != WeatherRequestMode.None)
                    {
                        RequestWeather();
                    }

                    _weatherUpdateWait = new WaitForSeconds(Utils.ConvertMinutesToSeconds(_autoUpdateRate));
                    yield return _weatherUpdateWait;
                }
            }
            else
            {
                if (_weatherRequestMode != WeatherRequestMode.None)
                {
                    RequestWeather();
                }
            }

            _isCoroutineActive = false;
            yield return null;
        }
        #endregion

        #region Forecast Methods
#if UNITY_EDITOR
        /// <summary>
        /// Saves the time lapse into a list.
        /// </summary>
        public async void SaveTimelapse(Timelapse timelapseData, DateTime startDate, bool shouldUpdateTimelapse = false)
        {
            _isSavingTimelapse = false;
            ForecastData selectedSim = CurrentSimulationSelected;
            await WaitUntillSavedTimelapse();
            _isSavingTimelapse = true;
            List<ForecastWeatherData> weatherSchedule = new List<ForecastWeatherData>();
            List<IntervalState> intervalStates = new List<IntervalState>();
            List<string> _weatherIntervalPresetName = new List<string>();
            List<string> _weatherPresetsName = new List<string>();

            for (int i = 0; i < timelapseData.timelapseDays.Count; i++)
            {
                if (i != 0 || !timelapseData.timelapseDays[i].delimitersPos[0])
                {
                    weatherSchedule.Add(AddWeatherDataToList(timelapseData, timelapseData.timelapseDays[i].timelapseIntervals[0].weatherData, GetIntervalDateTime(startDate, i, 0)));
                    intervalStates.Add(timelapseData.timelapseDays[i].timelapseIntervals[0].intervalState);
                    _weatherIntervalPresetName.Add(timelapseData.timelapseDays[i].timelapseIntervals[0].weatherDataProfileStr);
                    _weatherPresetsName.Add(timelapseData.timelapseDays[i].timelapseIntervals[0].presetName);
                }
                for (int j = 1; j < timelapseData.timelapseDays[i].timelapseIntervals.Count; j++)
                {
                    if (j == timelapseData.timelapseDays[i].timelapseIntervals.Count - 1 && timelapseData.timelapseDays[i].delimitersPos[InspectorUtils.kLabelDivisions])
                    {
                        weatherSchedule.Add(AddWeatherDataToList(timelapseData, timelapseData.timelapseDays[i].timelapseIntervals[j].weatherData, GetIntervalDateTime(startDate, i, 23)));
                    }
                    else
                    {
                        int hour = Mathf.RoundToInt(timelapseData.timelapseDays[i].sliderRelPosX[j - 1] * InspectorUtils.kLabelDivisions);
                        weatherSchedule.Add(AddWeatherDataToList(timelapseData, timelapseData.timelapseDays[i].timelapseIntervals[j].weatherData, GetIntervalDateTime(startDate, i, hour)));
                    }
                    intervalStates.Add(timelapseData.timelapseDays[i].timelapseIntervals[j].intervalState);
                    _weatherIntervalPresetName.Add(timelapseData.timelapseDays[i].timelapseIntervals[j].weatherDataProfileStr);
                    _weatherPresetsName.Add(timelapseData.timelapseDays[i].timelapseIntervals[j].presetName);
                }
            }
            //Creates a new WeatherData for the last interval of the last day
            weatherSchedule.Add(new ForecastWeatherData(weatherSchedule[weatherSchedule.Count - 1]));
            ForecastWeatherData lastForecastData = weatherSchedule[weatherSchedule.Count - 1];
            var date = DateTime.Parse(lastForecastData.forecastDateTime);
            lastForecastData.forecastDateTime = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59).ToString();

            if (selectedSim)
            {
                selectedSim.LoopSimulation = timelapseData.loopSim;
                selectedSim.IntervalPresetName = _weatherIntervalPresetName;
                selectedSim.IntervalName = _weatherPresetsName;
                selectedSim.IntervalStates = intervalStates;
                selectedSim.SetForecastData(weatherSchedule);
                selectedSim.simulationSpeed = (int)timelapseData.simulationSpeed;
                selectedSim.LerpStrength = timelapseData.lerpSpeed;
                selectedSim.SetDefaultWeatherData(timelapseData.defaultIntervalData);
                selectedSim.TimelapseDayNames = new List<string>();
                selectedSim.DaysOfSimulation = timelapseData.timelapseDays.Count;
                for (int i = 0; i < timelapseData.timelapseDays.Count; i++)
                {
                    selectedSim.TimelapseDayNames.Add(timelapseData.timelapseDays[i].dayName);
                }
                await CheckNameValid(timelapseData, selectedSim);
                selectedSim.SimulationName = timelapseData.timelapseName;
            }
            UpdateSimulationList();

            EditorUtility.SetDirty(selectedSim);
            EditorUtility.SetDirty(WeatherPresets);
            AssetDatabase.SaveAssets();
            _isSavingTimelapse = false;
            if (shouldUpdateTimelapse)
            {
                Timelapse timelapse = new Timelapse();
                LoadTimelapse(timelapse);
                UpdateTimelapse?.Invoke(timelapse, startDate);
            }
        }

        /// <summary>
        /// Checks to see if the inputed name is valid otherwise it renames the simulation preset
        /// </summary>
        private async Task<bool> CheckNameValid(Timelapse targetTimelapse, ForecastData selectedSim)
        {
            //Check if the timelapse name has exceeded the max length
            if (targetTimelapse.timelapseName.Length > Utilities.kMaxNameLength)
            {
                targetTimelapse.timelapseName = targetTimelapse.timelapseName.Substring(0, Utilities.kMaxNameLength);
            }

            string assetPath = AssetDatabase.GetAssetPath(selectedSim.GetInstanceID());
            int nr = 1;
            string timelapseName = String.IsNullOrEmpty(targetTimelapse.timelapseName) ? "Simulation" : targetTimelapse.timelapseName;
            if (timelapseName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                timelapseName = "Simulation";
                targetTimelapse.timelapseName = "Simulation";
            }
            AssetDatabase.RenameAsset(assetPath, selectedSim.name);
            EditorUtility.SetDirty(selectedSim);
            string succesfullyUpdated = AssetDatabase.RenameAsset(assetPath, timelapseName);

            while (!string.IsNullOrEmpty(succesfullyUpdated) && nr <= 1000)
            {
                timelapseName = targetTimelapse.timelapseName + " (" + nr + ")";
                nr++;
                succesfullyUpdated = AssetDatabase.RenameAsset(assetPath, timelapseName);
                await Task.Yield();
            }
            if (!string.IsNullOrEmpty(succesfullyUpdated))
            {
                Debug.LogError("Error When Saving The Simulation");
                ForecastPresetNameError(timelapseName, assetPath);
            }
            targetTimelapse.timelapseName = timelapseName;
            return true;
        }

        /// <summary>
        /// Calculates the date and time based on the values from an interval.
        /// </summary>
        /// <param name="days">The day of the interval</param>
        /// <param name="hour">The start hour of the interval</param>
        /// <returns>A DateTime with the adjusted day and hour</returns>
        private DateTime GetIntervalDateTime(DateTime date, int days, int hour)
        {
            DateTime currentDateTime = date.AddDays(days);
            currentDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, hour, 0, 0);
            return currentDateTime;
        }

        /// <summary>
        /// Creates a new location.
        /// </summary>
        /// <returns>A new location with the data from the latitude and longitude fields</returns>
        private Localization GetLocation(Timelapse timelapse)
        {
            return new Localization("User-Data", timelapse.timelapseName, timelapse.timelapseLatitude, timelapse.timelapseLongitude);
        }

        /// <summary>
        /// Adds the interval to the list.
        /// </summary>
        /// <param name="weatherData">The data of the interval</param>
        /// <param name="intervalDate">The date and time of the interval</param>
        private ForecastWeatherData AddWeatherDataToList(Timelapse timelapse, ForecastWeatherData weatherData, DateTime intervalDate)
        {
            weatherData.forecastDateTime = intervalDate.ToString();
            weatherData.forecastLocation = new ForecastLocationData(GetLocation(timelapse));

            try
            {
                weatherData.forecastTimeZone = Utilities.GetTimeZone(weatherData.forecastLocation.forecastLatitude, weatherData.forecastLocation.forecastLongitude);
                weatherData.UTCOffset = Utilities.GetUTCOffsetData(weatherData.forecastTimeZone).ToString();
            }
            catch (Exception)
            {
                weatherData.UTCOffset = new TimeSpan(0, 0, 0).ToString();
            }

            return new ForecastWeatherData(weatherData);
        }

        private async void ForecastPresetNameError(string timelapseName, string assetPath)
        {
            int nr = 0;
            timelapseName = "Congrats you broke it!";
            string succesfullyUpdated = AssetDatabase.RenameAsset(assetPath, "Congrats you broke it!");
            while (!string.IsNullOrEmpty(succesfullyUpdated) && nr <= 100)
            {
                nr++;
                timelapseName += ",again";
                succesfullyUpdated = AssetDatabase.RenameAsset(assetPath, timelapseName);
                await Task.Yield();
            }
        }

        /// <summary>
        /// Populates the Slider fields with the default values.
        /// </summary>
        public void LoadScriptableObjects()
        {
#if UNITY_EDITOR
            PopupTextures = (TimelapsePopupTextures)GetObject(kSliderTexturesStr, kAssetExtension);
            WeatherPresets = (WeatherPresetFolders)GetObject(kForecastPresetsStr, kAssetExtension);
#endif
        }

        private async Task<bool> WaitUntillSavedTimelapse()
        {
            while (_isSavingTimelapse)
            {
                await Task.Yield();
            }
            return true;
        }

#endif //UNITY_EDITOR
        #endregion
        #endregion
    }
}