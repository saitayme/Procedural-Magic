using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Systems;
using ProceduralWorld.Simulation.UI.Components;
#if UNITY_EDITOR
using UnityEditor;
#endif
using CoreEventType = ProceduralWorld.Simulation.Core.EventType;
using CoreEventCategory = ProceduralWorld.Simulation.Core.EventCategory;
using Unity.Jobs;

namespace ProceduralWorld.Simulation.UI
{
    public class WorldHistoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject timelinePanel;
        [SerializeField] private GameObject eventDetailsPanel;
        [SerializeField] private GameObject figureDetailsPanel;
        [SerializeField] private GameObject periodDetailsPanel;
        [SerializeField] private GameObject mapViewPanel;
        [SerializeField] private GameObject eventMarkerPrefab;
        [SerializeField] private GameObject figureMarkerPrefab;
        [SerializeField] private GameObject periodMarkerPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform figuresContainer;
        [SerializeField] private Transform periodsContainer;
        [SerializeField] private GameObject figurePrefab;
        [SerializeField] private GameObject periodPrefab;

        [Header("Timeline UI")]
        [SerializeField] private RectTransform timelineContent;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private TextMeshProUGUI currentYearText;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private Button slowDownButton;

        [Header("Event Details")]
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private TextMeshProUGUI eventYearText;
        [SerializeField] private TextMeshProUGUI eventSignificanceText;
        [SerializeField] private Transform relatedFiguresContainer;
        [SerializeField] private Transform relatedCivilizationsContainer;
        [SerializeField] private GameObject relatedItemPrefab;

        [Header("Figure Details")]
        [SerializeField] private TextMeshProUGUI figureNameText;
        [SerializeField] private TextMeshProUGUI figureCivilizationText;
        [SerializeField] private TextMeshProUGUI figureLifespanText;
        [SerializeField] private TextMeshProUGUI figureLegacyText;
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private GameObject achievementPrefab;

        [Header("Period Details")]
        [SerializeField] private TextMeshProUGUI periodNameText;
        [SerializeField] private TextMeshProUGUI periodYearsText;
        [SerializeField] private TextMeshProUGUI periodDescriptionText;
        [SerializeField] private Transform periodEventsContainer;
        [SerializeField] private GameObject periodEventPrefab;

        [Header("Map View")]
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private float mapScale = 1f;
        [SerializeField] private float markerScale = 1f;

        [Header("New UI Elements")]
        [SerializeField] private Transform eventsContainer;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown filterDropdown;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_Text totalEventsText;
        [SerializeField] private TMP_Text totalFiguresText;
        [SerializeField] private TMP_Text totalPeriodsText;
        [SerializeField] private TMP_Text selectedEventTitle;
        [SerializeField] private TMP_Text selectedEventDescription;
        [SerializeField] private TMP_Text selectedEventYear;
        [SerializeField] private TMP_Text selectedEventType;
        [SerializeField] private TMP_Text selectedEventCategory;
        [SerializeField] private TMP_Text selectedEventSignificance;
        [SerializeField] private TMP_Text selectedEventLocation;
        [SerializeField] private TMP_Text selectedEventSize;
        [SerializeField] private TMP_Text selectedEventRelatedCivilization;
        [SerializeField] private TMP_Text selectedEventRelatedFigure;
        [SerializeField] private TMP_Text selectedFigureName;
        [SerializeField] private TMP_Text selectedFigureTitle;
        [SerializeField] private TMP_Text selectedFigureYear;
        [SerializeField] private TMP_Text selectedFigureLegacy;
        [SerializeField] private TMP_Text selectedFigureAchievements;
        [SerializeField] private TMP_Text selectedPeriodName;
        [SerializeField] private TMP_Text selectedPeriodDescription;
        [SerializeField] private TMP_Text selectedPeriodStartYear;
        [SerializeField] private TMP_Text selectedPeriodEndYear;
        [SerializeField] private TMP_Text selectedPeriodSignificance;
        [SerializeField] private TMP_Text selectedPeriodKeyEvents;
        [SerializeField] private TMP_Text selectedPeriodKeyFigures;
        [SerializeField] private TMP_Text selectedPeriodKeyCivilizations;
        [SerializeField] private TMP_Text selectedPeriodKeyTechnologies;
        [SerializeField] private TMP_Text selectedPeriodKeyConflicts;
        [SerializeField] private TMP_Text selectedPeriodKeyAlliances;
        [SerializeField] private TMP_Text selectedPeriodKeyTreaties;
        [SerializeField] private TMP_Text selectedPeriodKeyDiscoveries;
        [SerializeField] private TMP_Text selectedPeriodKeyInventions;
        [SerializeField] private TMP_Text selectedPeriodKeyArtifacts;
        [SerializeField] private TMP_Text selectedPeriodKeyMonuments;
        [SerializeField] private TMP_Text selectedPeriodKeyWritings;
        [SerializeField] private TMP_Text selectedPeriodKeyReligions;
        [SerializeField] private TMP_Text selectedPeriodKeyPhilosophies;
        [SerializeField] private TMP_Text selectedPeriodKeySciences;
        [SerializeField] private TMP_Text selectedPeriodKeyArts;
        [SerializeField] private TMP_Text selectedPeriodKeyCultures;
        [SerializeField] private TMP_Text selectedPeriodKeyLanguages;
        [SerializeField] private TMP_Text selectedPeriodKeyTraditions;
        [SerializeField] private TMP_Text selectedPeriodKeyBeliefs;
        [SerializeField] private TMP_Text selectedPeriodKeyValues;
        [SerializeField] private TMP_Text selectedPeriodKeyNorms;
        [SerializeField] private TMP_Text selectedPeriodKeyCustoms;
        [SerializeField] private TMP_Text selectedPeriodKeyRituals;
        [SerializeField] private TMP_Text selectedPeriodKeyCeremonies;
        [SerializeField] private TMP_Text selectedPeriodKeyFestivals;
        [SerializeField] private TMP_Text selectedPeriodKeyHolidays;
        [SerializeField] private TMP_Text selectedPeriodKeyCelebrations;
        [SerializeField] private TMP_Text selectedPeriodKeyObservances;
        [SerializeField] private TMP_Text selectedPeriodKeyPractices;

        private bool _isInitialized;
        private WorldHistorySystem _historySystem;
        private bool _isPlaying;
        private float _playbackSpeed = 1f;
        private HistoricalEventRecord _selectedEvent;
        private HistoricalFigure _selectedFigure;
        private HistoricalPeriod _selectedPeriod;
        private Dictionary<FixedString128Bytes, GameObject> _eventMarkers;
        private Dictionary<FixedString128Bytes, GameObject> _figureMarkers;
        private Dictionary<FixedString128Bytes, GameObject> _periodMarkers;
        private NativeList<HistoricalEventRecord> _displayedEvents;
        private Vector2 _scrollPosition;
        private bool _isExpanded;
        private Rect _windowRect;
        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 400f;
        private const float COLLAPSED_HEIGHT = 40f;

        private const int MAX_DISPLAYED_EVENTS = 10;
        private const float EVENT_DISPLAY_TIME = 5f;
        private const float FADE_DURATION = 0.5f;
        private const float SCROLL_SPEED = 50f;
        private const float EVENT_HEIGHT = 60f;
        private const float PADDING = 10f;

        private float _lastEventTime;
        private bool _isDragging;
        private Vector2 _dragOffset;

        private List<HistoricalEventRecord> _eventRecords;
        private List<HistoricalFigure> _historicalFigures;
        private List<HistoricalPeriod> _historicalPeriods;

        private bool[] _activeEventTypes;
        private bool[] _activeCategories;
        private int _currentYear;
        private int _startYear;
        private int _endYear;

        private string _searchQuery = "";
        [SerializeField] private int _filterCategoryIndex = 0; // Serializable int for EventCategory
        private string _sortBy = "Year";
        
        // Property to convert between int and EventCategory
        private EventCategory FilterCategory
        {
            get => (EventCategory)(1L << _filterCategoryIndex);
            set => _filterCategoryIndex = GetCategoryIndex(value);
        }
        
        private int GetCategoryIndex(EventCategory category)
        {
            // Convert EventCategory back to index for serialization
            if (category == EventCategory.All) return 0;
            
            // Find the bit position for single-bit categories
            long categoryValue = (long)category;
            for (int i = 0; i < 64; i++)
            {
                if ((categoryValue & (1L << i)) != 0)
                    return i;
            }
            return 0; // Default to All
        }

        private NativeList<HistoricalEventRecord> _events;
        private NativeList<HistoricalFigure> _figures;
        private NativeList<HistoricalPeriod> _periods;
        private Transform _eventContainer;
        private Transform _figureContainer;
        private Transform _periodContainer;
        private GameObject eventPrefab;
        private Slider _timeSlider;
        private List<HistoricalFigure> historicalFigures;
        private List<HistoricalPeriod> historicalPeriods;

        private void Awake()
        {
            _eventMarkers = new Dictionary<FixedString128Bytes, GameObject>();
            _figureMarkers = new Dictionary<FixedString128Bytes, GameObject>();
            _periodMarkers = new Dictionary<FixedString128Bytes, GameObject>();
            _displayedEvents = new NativeList<HistoricalEventRecord>(Allocator.Persistent);
            _events = new NativeList<HistoricalEventRecord>(Allocator.Persistent);
            _figures = new NativeList<HistoricalFigure>(Allocator.Persistent);
            _periods = new NativeList<HistoricalPeriod>(Allocator.Persistent);
            
            // Initialize filter arrays
            _activeEventTypes = new bool[System.Enum.GetValues(typeof(CoreEventType)).Length];
            _activeCategories = new bool[System.Enum.GetValues(typeof(CoreEventCategory)).Length];
            
            // Set all filters to true by default
            for (int i = 0; i < _activeEventTypes.Length; i++)
                _activeEventTypes[i] = true;
            for (int i = 0; i < _activeCategories.Length; i++)
                _activeCategories[i] = true;
        }

        private void OnDestroy()
        {
            if (_displayedEvents.IsCreated)
            {
                _displayedEvents.Dispose();
            }
            if (_events.IsCreated)
            {
                _events.Dispose();
            }
            if (_figures.IsCreated)
            {
                _figures.Dispose();
            }
            if (_periods.IsCreated)
            {
                _periods.Dispose();
            }
        }

        private void Start()
        {
            _timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            UpdateUI();

            _historySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WorldHistorySystem>();
            _isInitialized = false;
            _windowRect = new Rect(Screen.width - WINDOW_WIDTH - 20, 20, WINDOW_WIDTH, WINDOW_HEIGHT);
            _scrollPosition = Vector2.zero;
            _lastEventTime = Time.time;
            
            // Start with the window expanded so we can see events immediately
            _isExpanded = true;
            Debug.Log("[WorldHistoryUI] Start: Window set to expanded by default");

            InitializeUI();
            UpdateTimeline();
            UpdateMapView();
            SetupEventListeners();
        }

        private void SetupEventListeners()
        {
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(TogglePlayPause);
            if (speedUpButton != null)
                speedUpButton.onClick.AddListener(SpeedUp);
            if (slowDownButton != null)
                slowDownButton.onClick.AddListener(SlowDown);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseHistoryPanel);
            if (searchInput != null)
                searchInput.onValueChanged.AddListener(OnSearchInputChanged);
            if (filterDropdown != null)
                filterDropdown.onValueChanged.AddListener(OnFilterChanged);
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                if (_historySystem == null)
                {
                    Debug.LogWarning("[WorldHistoryUI] Update: _historySystem is null, cannot initialize");
                    return;
                }

                Debug.Log("[WorldHistoryUI] Update: Initializing UI");
                _isInitialized = true;
            }
            _currentYear = _historySystem.GetCurrentYear();

            if (currentYearText != null)
                currentYearText.text = $"Year: {_currentYear}";

            var periods = _historySystem.GetHistoricalPeriods(Allocator.Temp);
            var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
            var figures = _historySystem.GetHistoricalFigures(Allocator.Temp);

            if (events.IsCreated)
            {
                _events.Clear();
                _events.AddRange(events.AsArray());
                Debug.Log($"[WorldHistoryUI] Update: Retrieved {events.Length} events from history system");
                events.Dispose();
            }
            else
            {
                Debug.LogWarning("[WorldHistoryUI] Update: events was not created");
            }

            if (figures.IsCreated)
            {
                _figures.Clear();
                _figures.AddRange(figures.AsArray());
                figures.Dispose();
            }

            if (periods.IsCreated)
            {
                _periods.Clear();
                _periods.AddRange(periods.AsArray());
                periods.Dispose();
            }

            if (_isPlaying)
            {
                UpdateTimeline();
                UpdateMapView();
            }

            if (Time.time - _lastEventTime > EVENT_DISPLAY_TIME)
            {
                _lastEventTime = Time.time;
                if (_displayedEvents.Length > 0)
                {
                    _displayedEvents.RemoveAt(0);
                }
            }

            if (_eventContainer != null)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    Vector3 position = _eventContainer.localPosition;
                    position.y += scroll * SCROLL_SPEED;
                    _eventContainer.localPosition = position;
                }
            }
        }

        private void InitializeUI()
        {
            // Initialize timeline slider
            timelineSlider.onValueChanged.AddListener(OnTimelineValueChanged);
            playPauseButton.onClick.AddListener(TogglePlayPause);
            speedUpButton.onClick.AddListener(SpeedUp);
            slowDownButton.onClick.AddListener(SlowDown);

            // Initialize map view
            InitializeMapView();
        }

        private void InitializeMapView()
        {
            // Create markers for all events
            foreach (var evt in _historySystem.GetHistoricalEvents(Allocator.Temp))
            {
                CreateEventMarker(evt);
            }

            // Create markers for all figures
            foreach (var figure in _historySystem.GetHistoricalFigures(Allocator.Temp))
            {
                CreateFigureMarker(figure);
            }

            // Create markers for all periods
            foreach (var period in _historySystem.GetHistoricalPeriods(Allocator.Temp))
            {
                CreatePeriodMarker(period);
            }
        }

        private void CreateEventMarker(HistoricalEventRecord evt)
        {
            var marker = Instantiate(eventPrefab, mapContent);
            marker.transform.localPosition = new Vector3(
                evt.Location.x * mapScale,
                evt.Location.z * mapScale,
                0f
            );
            marker.transform.localScale = Vector3.one * markerScale;

            var button = marker.GetComponent<Button>();
            button.onClick.AddListener(() => ShowEventDetails(evt));

            _eventMarkers[evt.Title] = marker;
        }

        private void CreateFigureMarker(HistoricalFigure figure)
        {
            var marker = Instantiate(figurePrefab, mapContent);
            marker.transform.localPosition = new Vector3(
                figure.Location.x * mapScale,
                figure.Location.z * mapScale,
                0f
            );
            marker.transform.localScale = Vector3.one * markerScale;

            var button = marker.GetComponent<Button>();
            button.onClick.AddListener(() => ShowFigureDetails(figure));

            _figureMarkers[figure.Name] = marker;
        }

        private void CreatePeriodMarker(HistoricalPeriod period)
        {
            var marker = Instantiate(periodPrefab, mapContent);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * markerScale;

            var button = marker.GetComponent<Button>();
            button.onClick.AddListener(() => ShowPeriodDetails(period));

            _periodMarkers[period.Name] = marker;
        }

        private void UpdateTimeline()
        {
            var currentYear = _historySystem.GetCurrentYear();
            currentYearText.text = $"Year: {currentYear}";

            var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
            var figures = _historySystem.GetHistoricalFigures(Allocator.Temp);
            var periods = _historySystem.GetHistoricalPeriods(Allocator.Temp);

            // Update timeline slider
            var maxYear = periods.AsArray().Max(p => p.EndYear);
            timelineSlider.maxValue = maxYear;
            timelineSlider.value = currentYear;

            // Update event visibility
            foreach (var evt in events.AsArray())
            {
                if (_eventMarkers.TryGetValue(evt.Title, out var marker))
                {
                    marker.SetActive(evt.Year <= currentYear);
                }
            }

            // Update figure visibility
            foreach (var figure in figures.AsArray())
            {
                if (_figureMarkers.TryGetValue(figure.Name, out var marker))
                {
                    marker.SetActive(figure.BirthYear <= currentYear && figure.DeathYear >= currentYear);
                }
            }

            // Update period visibility
            foreach (var period in periods.AsArray())
            {
                if (_periodMarkers.TryGetValue(period.Name, out var marker))
                {
                    marker.SetActive(period.StartYear <= currentYear && period.EndYear >= currentYear);
                }
            }

            events.Dispose();
            figures.Dispose();
            periods.Dispose();
        }

        private void UpdateMapView()
        {
            // Update marker positions and visibility based on current year
            var currentYear = _historySystem.GetCurrentYear();

            foreach (var evt in _historySystem.GetHistoricalEvents(Allocator.Temp))
            {
                if (_eventMarkers.TryGetValue(evt.Title, out var marker))
                {
                    marker.transform.localPosition = new Vector3(
                        evt.Location.x * mapScale,
                        evt.Location.z * mapScale,
                        0f
                    );
                    marker.SetActive(evt.Year <= currentYear);
                }
            }

            foreach (var figure in _historySystem.GetHistoricalFigures(Allocator.Temp))
            {
                if (_figureMarkers.TryGetValue(figure.Name, out var marker))
                {
                    marker.transform.localPosition = new Vector3(
                        figure.Location.x * mapScale,
                        figure.Location.z * mapScale,
                        0f
                    );
                    marker.SetActive(figure.BirthYear <= currentYear && figure.DeathYear >= currentYear);
                }
            }
        }

        private void ShowEventDetails(HistoricalEventRecord evt)
        {
            _selectedEvent = evt;
            eventTitleText.text = evt.Title.ToString();
            eventDescriptionText.text = evt.Description.ToString();
            eventYearText.text = $"Year: {evt.Year}";
            eventSignificanceText.text = $"Significance: {evt.Significance:P0}";

            // Clear and populate related figures
            foreach (Transform child in relatedFiguresContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (var figureEntity in evt.RelatedFigures)
            {
                var figure = _historySystem.GetHistoricalFigure(figureEntity);
                if (!figure.Equals(default(HistoricalFigure)))
                {
                    var item = Instantiate(relatedItemPrefab, relatedFiguresContainer);
                    item.GetComponentInChildren<TextMeshProUGUI>().text = figure.Name.ToString();
                }
            }

            // Clear and populate related civilizations
            foreach (Transform child in relatedCivilizationsContainer)
            {
                Destroy(child.gameObject);
            }
            foreach (var civEntity in evt.RelatedCivilizations)
            {
                var civ = _historySystem.GetCivilization(civEntity);
                if (!civ.Equals(default(CivilizationData)))
                {
                    var item = Instantiate(relatedItemPrefab, relatedCivilizationsContainer);
                    item.GetComponentInChildren<TextMeshProUGUI>().text = civ.Name.ToString();
                }
            }

            eventDetailsPanel.SetActive(true);
        }

        private void ShowFigureDetails(HistoricalFigure figure)
        {
            _selectedFigure = figure;
            figureNameText.text = figure.Name.ToString();
            figureCivilizationText.text = $"Civilization: {figure.Civilization}";
            figureLifespanText.text = $"Lifespan: {figure.BirthYear} - {figure.DeathYear}";
            figureLegacyText.text = figure.Legacy.ToString();

            // Clear and populate achievements
            foreach (Transform child in achievementsContainer)
            {
                Destroy(child.gameObject);
            }
            var achievements = _historySystem.GetFigureAchievements(figure, Allocator.Temp);
            foreach (var achievement in achievements)
            {
                var item = Instantiate(achievementPrefab, achievementsContainer);
                item.GetComponentInChildren<TextMeshProUGUI>().text = achievement.ToString();
            }

            figureDetailsPanel.SetActive(true);
            achievements.Dispose();
        }

        private void ShowPeriodDetails(HistoricalPeriod period)
        {
            _selectedPeriod = period;
            periodNameText.text = period.Name.ToString();
            periodYearsText.text = $"Years: {period.StartYear} - {period.EndYear}";
            periodDescriptionText.text = period.Description.ToString();

            // Clear and populate period events
            foreach (Transform child in periodEventsContainer)
            {
                Destroy(child.gameObject);
            }
            var periodEvents = _historySystem.GetHistoricalEvents(Allocator.Temp)
                .Where(e => e.Year >= period.StartYear && e.Year <= period.EndYear)
                .OrderBy(e => e.Year);
            foreach (var evt in periodEvents)
            {
                var item = Instantiate(periodEventPrefab, periodEventsContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                texts[0].text = evt.Title.ToString();
                texts[1].text = $"Year: {evt.Year}";
            }

            periodDetailsPanel.SetActive(true);
        }

        private void OnTimelineValueChanged(float value)
        {
            _currentYear = (int)value;
            UpdateTimeline();
        }

        private void TogglePlayPause()
        {
            _isPlaying = !_isPlaying;
            playPauseButton.GetComponentInChildren<TextMeshProUGUI>().text = _isPlaying ? "Pause" : "Play";
        }

        private void SpeedUp()
        {
            _playbackSpeed *= 2f;
            _playbackSpeed = Mathf.Min(_playbackSpeed, 8f);
        }

        private void SlowDown()
        {
            _playbackSpeed *= 0.5f;
            _playbackSpeed = Mathf.Max(_playbackSpeed, 0.25f);
        }

        public void ToggleHistoryPanel()
        {
            historyPanel.SetActive(!historyPanel.activeSelf);
        }

        public void ToggleTimelinePanel()
        {
            timelinePanel.SetActive(!timelinePanel.activeSelf);
        }

        public void ToggleMapViewPanel()
        {
            mapViewPanel.SetActive(!mapViewPanel.activeSelf);
        }

        public void CloseEventDetails()
        {
            eventDetailsPanel.SetActive(false);
            _selectedEvent = default;
        }

        public void CloseFigureDetails()
        {
            figureDetailsPanel.SetActive(false);
            _selectedFigure = default;
        }

        public void ClosePeriodDetails()
        {
            periodDetailsPanel.SetActive(false);
            _selectedPeriod = default;
        }

        // DISABLED: Now managed by SimulationUIManager
        /*
        private void OnGUI()
        {
            // Add a simple test to verify UI is running
            if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                Debug.Log($"[WorldHistoryUI] OnGUI: Frame {Time.frameCount}, HistorySystem: {(_historySystem != null ? "Found" : "NULL")}, Events in cache: {_events.Length}");
            }

            if (_isExpanded)
        {
            _windowRect = GUI.Window(0, _windowRect, DrawWindow, "World History");
            }
            else
            {
                _windowRect = new Rect(_windowRect.x, _windowRect.y, WINDOW_WIDTH, COLLAPSED_HEIGHT);
                _windowRect = GUI.Window(0, _windowRect, DrawWindow, "History");
            }
            HandleWindowDrag();
        }
        */

        private void DrawWindow(int windowID)
        {
            if (_isExpanded)
            {
                DrawExpandedWindow();
            }
            else
            {
                DrawCollapsedWindow();
            }
            GUI.DragWindow();
        }

        private void DrawExpandedWindow()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            DrawSimpleHistoryDisplay();
            GUILayout.EndScrollView();

            if (GUILayout.Button("Collapse"))
            {
                _isExpanded = false;
                _windowRect.height = COLLAPSED_HEIGHT;
            }
        }

        private void DrawCollapsedWindow()
        {
            if (Time.frameCount % 120 == 0) // Log every 120 frames to avoid spam
            {
                Debug.Log($"[WorldHistoryUI] DrawCollapsedWindow: Window is collapsed. Click 'Expand' to see {_events.Length} events!");
            }
            
            if (GUILayout.Button("Expand"))
            {
                _isExpanded = true;
                _windowRect.height = WINDOW_HEIGHT;
                Debug.Log("[WorldHistoryUI] DrawCollapsedWindow: Window expanded by user");
            }
        }

        private void HandleWindowDrag()
        {
            if (Event.current.type == UnityEngine.EventType.MouseDown && Event.current.button == 0)
            {
                _isDragging = true;
                _dragOffset = new Vector2(
                    Event.current.mousePosition.x - _windowRect.x,
                    Event.current.mousePosition.y - _windowRect.y
                );
            }
            else if (Event.current.type == UnityEngine.EventType.MouseUp && Event.current.button == 0)
            {
                _isDragging = false;
            }
            else if (_isDragging && Event.current.type == UnityEngine.EventType.MouseDrag)
            {
                _windowRect.x = Event.current.mousePosition.x - _dragOffset.x;
                _windowRect.y = Event.current.mousePosition.y - _dragOffset.y;
            }
        }

        public void AddEvent(HistoricalEventRecord evt)
        {
            if (_eventRecords != null && _eventRecords.Count > 0)
            {
                // Handle event records
            }
        }

        private void UpdateEventDisplay()
        {
            if (eventsContainer == null || eventPrefab == null) return;

            // Clear existing events
            foreach (Transform child in eventsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add filtered events
            foreach (var evt in _events)
            {
                if (_activeEventTypes[(int)evt.Type] && _activeCategories[(int)evt.Category])
                {
                    var eventObj = Instantiate(eventPrefab, eventsContainer);
                    var text = eventObj.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = $"{evt.Year}: {evt.Name} - {evt.Description}";
                    }
                }
            }
        }

        public void ToggleEventType(int typeIndex)
        {
            if (typeIndex >= 0 && typeIndex < _activeEventTypes.Length)
            {
                _activeEventTypes[typeIndex] = !_activeEventTypes[typeIndex];
                UpdateEventDisplay();
            }
        }

        public void ToggleCategory(int categoryIndex)
        {
            if (categoryIndex >= 0 && categoryIndex < _activeCategories.Length)
            {
                _activeCategories[categoryIndex] = !_activeCategories[categoryIndex];
                UpdateEventDisplay();
            }
        }

        private void DrawEventTypeFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Event Type Filter:", GUILayout.Width(100));
            foreach (ProceduralWorld.Simulation.Core.EventType type in System.Enum.GetValues(typeof(ProceduralWorld.Simulation.Core.EventType)))
            {
                if (!_activeEventTypes[(int)type])
                    _activeEventTypes[(int)type] = true;

                _activeEventTypes[(int)type] = GUILayout.Toggle(_activeEventTypes[(int)type], type.ToString());
            }
            GUILayout.EndHorizontal();
        }

        private void DrawEventList()
        {
            if (_events.Length == 0)
            {
                GUILayout.Label("No historical events recorded yet.");
                return;
            }

            GUILayout.BeginScrollView(_scrollPosition);
            
            // Sort events by year (most recent first)
            var sortedEvents = new List<HistoricalEventRecord>();
            for (int i = 0; i < _events.Length; i++)
            {
                sortedEvents.Add(_events[i]);
            }
            sortedEvents.Sort((a, b) => b.Year.CompareTo(a.Year));
            
            foreach (var evt in sortedEvents)
            {
                if (_activeEventTypes != null && _activeEventTypes.Length > (int)evt.Type && 
                    !_activeEventTypes[(int)evt.Type])
                    continue;
                    
                if (_activeCategories != null && _activeCategories.Length > (int)evt.Category && 
                    !_activeCategories[(int)evt.Category])
                    continue;

                GUILayout.BeginVertical("box");
                GUILayout.Label($"Year {evt.Year}: {evt.Name}", EditorStyles.boldLabel);
                GUILayout.Label($"Type: {evt.Type}");
                GUILayout.Label($"Category: {evt.Category}");
                GUILayout.Label($"Description: {evt.Description}");
                if (evt.Significance > 0)
                    GUILayout.Label($"Significance: {evt.Significance}");
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
        }

        private void UpdateUI()
        {
            if (_historySystem != null)
            {
                var periods = _historySystem.GetHistoricalPeriods(Allocator.Temp);
                var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
                var figures = _historySystem.GetHistoricalFigures(Allocator.Temp);

                _events.Clear();
                _figures.Clear();
                _periods.Clear();

                foreach (var evt in events)
                {
                    _events.Add(evt);
                }

                foreach (var figure in figures)
                {
                    _figures.Add(figure);
                }

                foreach (var period in periods)
                {
                    _periods.Add(period);
                }

                events.Dispose();
                figures.Dispose();
                periods.Dispose();
            }
        }

        private void ClearContainers()
        {
            if (_eventContainer != null)
                foreach (Transform child in _eventContainer)
                    Destroy(child.gameObject);

            if (_figureContainer != null)
                foreach (Transform child in _figureContainer)
                    Destroy(child.gameObject);

            if (_periodContainer != null)
                foreach (Transform child in _periodContainer)
                    Destroy(child.gameObject);
        }

        private void DisplayEvents()
        {
            var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
            foreach (var evt in events)
            {
                var item = Instantiate(eventPrefab, _eventContainer);
                // Setup event item
            }
            events.Dispose();
        }

        private void DisplayFigures()
        {
            var figures = _historySystem.GetHistoricalFigures(Allocator.Temp);
            foreach (var figure in figures)
            {
                var item = Instantiate(figurePrefab, _figureContainer);
                // Setup figure item
            }
            figures.Dispose();
        }

        private void DisplayPeriods()
        {
            var periods = _historySystem.GetHistoricalPeriods(Allocator.Temp);
            foreach (var period in periods)
            {
                var item = Instantiate(periodPrefab, _periodContainer);
                // Setup period item
            }
            periods.Dispose();
        }

        private void OnTimeSliderChanged(float value)
        {
            _currentYear = (int)value;
            UpdateTimeline();
        }

        private void OnSearchInputChanged(string value)
        {
            _searchQuery = value;
            RefreshHistoryView();
        }

        private void OnFilterChanged(int index)
        {
            FilterCategory = (EventCategory)(1L << index);
            RefreshHistoryView();
        }

        private void OnSortChanged(int index)
        {
            _sortBy = sortDropdown.options[index].text;
            RefreshHistoryView();
        }

        private void RefreshHistoryView()
        {
            if (!_isInitialized || _historySystem == null)
                return;

            using (var events = _historySystem.GetHistoricalEvents(Allocator.Temp))
            using (var figures = _historySystem.GetHistoricalFigures(Allocator.Temp))
            using (var periods = _historySystem.GetHistoricalPeriods(Allocator.Temp))
            {
                var filteredEvents = FilterEvents(events.AsArray());
                var filteredFigures = FilterFigures(figures.AsArray());
                var filteredPeriods = FilterPeriods(periods.AsArray());

                UpdateEventsList(filteredEvents);
                UpdateFiguresList(filteredFigures);
                UpdatePeriodsList(filteredPeriods);

                UpdateStatistics(events.Length, figures.Length, periods.Length);
            }
        }

        private NativeArray<HistoricalEventRecord> FilterEvents(NativeArray<HistoricalEventRecord> events)
        {
            var filtered = new NativeList<HistoricalEventRecord>(Allocator.Temp);
            foreach (var evt in events)
            {
                if (MatchesSearch(evt) && MatchesFilter(evt))
                    filtered.Add(evt);
            }
            return filtered.AsArray();
        }

        private NativeArray<HistoricalFigure> FilterFigures(NativeArray<HistoricalFigure> figures)
        {
            var filtered = new NativeList<HistoricalFigure>(Allocator.Temp);
            foreach (var figure in figures)
            {
                if (MatchesSearch(figure))
                    filtered.Add(figure);
            }
            return filtered.AsArray();
        }

        private NativeArray<HistoricalPeriod> FilterPeriods(NativeArray<HistoricalPeriod> periods)
        {
            var filtered = new NativeList<HistoricalPeriod>(Allocator.Temp);
            foreach (var period in periods)
            {
                if (MatchesSearch(period))
                    filtered.Add(period);
            }
            return filtered.AsArray();
        }

        private bool MatchesSearch(HistoricalEventRecord evt)
        {
            return string.IsNullOrEmpty(_searchQuery) ||
                   evt.Name.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase) ||
                   evt.Description.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesSearch(HistoricalFigure figure)
        {
            return string.IsNullOrEmpty(_searchQuery) ||
                   figure.Name.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase) ||
                   figure.Title.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesSearch(HistoricalPeriod period)
        {
            return string.IsNullOrEmpty(_searchQuery) ||
                   period.Name.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase) ||
                   period.Description.ToString().Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesFilter(HistoricalEventRecord evt)
        {
            return FilterCategory == EventCategory.All || evt.Category == FilterCategory;
        }

        private void UpdateEventsList(NativeArray<HistoricalEventRecord> events)
        {
            if (!events.IsCreated) return;
            
            foreach (var evt in events)
            {
                if (_eventMarkers.TryGetValue(evt.Name, out var marker))
                {
                    marker.SetActive(evt.Year <= _historySystem.GetCurrentYear());
                }
            }
        }

        private void UpdateFiguresList(NativeArray<HistoricalFigure> figures)
        {
            if (!figures.IsCreated) return;
            
            foreach (var figure in figures)
            {
                if (_figureMarkers.TryGetValue(figure.Name, out var marker))
                {
                    marker.SetActive(figure.BirthYear <= _historySystem.GetCurrentYear() && 
                                   figure.DeathYear >= _historySystem.GetCurrentYear());
                }
            }
        }

        private void UpdatePeriodsList(NativeArray<HistoricalPeriod> periods)
        {
            if (!periods.IsCreated) return;
            
            foreach (var period in periods)
            {
                if (_periodMarkers.TryGetValue(period.Name, out var marker))
                {
                    marker.SetActive(period.StartYear <= _historySystem.GetCurrentYear() && 
                                   period.EndYear >= _historySystem.GetCurrentYear());
                }
            }
        }

        private void UpdateStatistics(int totalEvents, int totalFigures, int totalPeriods)
        {
            if (totalEventsText != null)
                totalEventsText.text = $"Total Events: {totalEvents}";
            if (totalFiguresText != null)
                totalFiguresText.text = $"Total Figures: {totalFigures}";
            if (totalPeriodsText != null)
                totalPeriodsText.text = $"Total Periods: {totalPeriods}";
        }

        private void OnEventSelected(HistoricalEventRecord evt)
        {
            if (evt.Title != default)
            {
                _selectedEvent = evt;
                UpdateEventDetails();
            }
        }

        private void OnFigureSelected(HistoricalFigure figure)
        {
            if (figure.Title != default)
            {
                _selectedFigure = figure;
                UpdateFigureDetails();
            }
        }

        private void OnPeriodSelected(HistoricalPeriod period)
        {
            _selectedPeriod = period;
            UpdatePeriodDetails();
        }

        private void UpdateEventDetails()
        {
            if (_selectedEvent.Title != default)
            {
                if (selectedEventTitle != null)
                    selectedEventTitle.text = _selectedEvent.Name.ToString();
                if (selectedEventDescription != null)
                    selectedEventDescription.text = _selectedEvent.Description.ToString();
                if (selectedEventYear != null)
                    selectedEventYear.text = $"Year: {_selectedEvent.Year}";
                if (selectedEventType != null)
                    selectedEventType.text = $"Type: {_selectedEvent.Type}";
                if (selectedEventCategory != null)
                    selectedEventCategory.text = $"Category: {_selectedEvent.Category}";
                if (selectedEventSignificance != null)
                    selectedEventSignificance.text = $"Significance: {_selectedEvent.Significance:F2}";
                if (selectedEventLocation != null)
                    selectedEventLocation.text = $"Location: {_selectedEvent.Location}";
                if (selectedEventSize != null)
                    selectedEventSize.text = $"Size: {_selectedEvent.Size:F2}";
            }
        }

        private void UpdateFigureDetails()
        {
            if (_selectedFigure.Title != default)
            {
                var achievements = _historySystem.GetFigureAchievements(_selectedFigure, Allocator.Temp);
                var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
                figureNameText.text = _selectedFigure.Name.ToString();
                figureCivilizationText.text = $"Civilization: {_selectedFigure.Civilization}";
                figureLifespanText.text = $"Lifespan: {_selectedFigure.BirthYear} - {_selectedFigure.DeathYear}";
                figureLegacyText.text = _selectedFigure.Legacy.ToString();

                // Clear and populate achievements
                foreach (Transform child in achievementsContainer)
                {
                    Destroy(child.gameObject);
                }
                foreach (var achievement in achievements)
                {
                    var item = Instantiate(achievementPrefab, achievementsContainer);
                    item.GetComponentInChildren<TextMeshProUGUI>().text = achievement.ToString();
                }

                figureDetailsPanel.SetActive(true);
                achievements.Dispose();
                events.Dispose();
            }
        }

        private void UpdatePeriodDetails()
        {
            if (_selectedPeriod.Equals(default(HistoricalPeriod)))
                return;

            if (selectedPeriodName != null)
                selectedPeriodName.text = _selectedPeriod.Name.ToString();
            if (selectedPeriodDescription != null)
                selectedPeriodDescription.text = _selectedPeriod.Description.ToString();
            if (selectedPeriodStartYear != null)
                selectedPeriodStartYear.text = $"Start Year: {_selectedPeriod.StartYear}";
            if (selectedPeriodEndYear != null)
                selectedPeriodEndYear.text = $"End Year: {_selectedPeriod.EndYear}";
            if (selectedPeriodSignificance != null)
                selectedPeriodSignificance.text = $"Significance: {_selectedPeriod.Significance:F2}";
        }

        public void OpenHistoryPanel()
        {
            if (historyPanel != null)
            {
                historyPanel.SetActive(true);
                RefreshHistoryView();
            }
        }

        public void CloseHistoryPanel()
        {
            if (historyPanel != null)
                historyPanel.SetActive(false);
        }

        private void DrawSimpleHistoryDisplay()
        {
            GUILayout.Label($"World History - Year {_currentYear}", GetBoldLabelStyle());
            GUILayout.Space(10);
            
            // Get fresh events directly from history system to ensure we have latest data
            NativeList<HistoricalEventRecord> currentEvents;
            if (_historySystem != null)
            {
                Debug.Log("[WorldHistoryUI] DrawSimpleHistoryDisplay: Getting events from history system");
                var tempEvents = _historySystem.GetHistoricalEvents(Allocator.Temp);
                currentEvents = new NativeList<HistoricalEventRecord>(tempEvents.Length, Allocator.Temp);
                if (tempEvents.IsCreated)
                {
                    currentEvents.AddRange(tempEvents.AsArray());
                    Debug.Log($"[WorldHistoryUI] DrawSimpleHistoryDisplay: Retrieved {currentEvents.Length} events");
                    
                    // Debug: Log first few events to verify data
                    for (int i = 0; i < math.min(3, currentEvents.Length); i++)
                    {
                        var evt = currentEvents[i];
                        Debug.Log($"[WorldHistoryUI] Event {i}: Name='{evt.Name}' | Description='{evt.Description}' | Year={evt.Year} | Title='{evt.Title}'");
                    }
                    
                    tempEvents.Dispose();
                }
                else
                {
                    Debug.LogWarning("[WorldHistoryUI] DrawSimpleHistoryDisplay: tempEvents was not created");
                }
            }
            else
            {
                Debug.LogWarning("[WorldHistoryUI] DrawSimpleHistoryDisplay: _historySystem is null");
                currentEvents = new NativeList<HistoricalEventRecord>(Allocator.Temp);
            }
            
            // Debug information
            GUILayout.Label($"History System: {(_historySystem != null ? "Found" : "NULL")}", GetMiniLabelStyle());
            GUILayout.Label($"Current Events Length: {currentEvents.Length}", GetMiniLabelStyle());
            GUILayout.Label($"UI Events Array Length: {_events.Length}", GetMiniLabelStyle());
            GUILayout.Label($"Is Initialized: {_isInitialized}", GetMiniLabelStyle());
            GUILayout.Space(5);
            
            if (currentEvents.Length == 0)
            {
                GUILayout.Label("No historical events recorded yet.");
                GUILayout.Label("Let the simulation run to generate history!");
                GUILayout.Label("Check that civilizations are spawning and interacting.");
                currentEvents.Dispose();
                return;
            }
            
            GUILayout.Label($"Total Events: {currentEvents.Length}", GetBoldLabelStyle());
            GUILayout.Space(5);
            
            // Show the most recent 20 events
            var recentEvents = new List<HistoricalEventRecord>();
            for (int i = 0; i < currentEvents.Length; i++)
            {
                recentEvents.Add(currentEvents[i]);
            }
            recentEvents.Sort((a, b) => b.Year.CompareTo(a.Year)); // Sort by year, newest first
            
            int eventsToShow = math.min(20, recentEvents.Count);
            GUILayout.Label($"Recent Events (showing {eventsToShow} of {recentEvents.Count}):", GetBoldLabelStyle());
            
            for (int i = 0; i < eventsToShow; i++)
            {
                var evt = recentEvents[i];
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Year {evt.Year}: {evt.Name}", GetBoldLabelStyle());
                GUILayout.Label($"{evt.Description}");
                GUILayout.Label($"Type: {evt.Type} | Category: {evt.Category} | Significance: {evt.Significance:F1}");
                GUILayout.EndVertical();
                GUILayout.Space(3);
            }
            
            currentEvents.Dispose();
        }
        
        // Helper methods for cross-platform GUI styles
        GUIStyle GetBoldLabelStyle()
        {
#if UNITY_EDITOR
            return EditorStyles.boldLabel;
#else
            return new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
#endif
        }
        
        GUIStyle GetMiniLabelStyle()
        {
#if UNITY_EDITOR
            return EditorStyles.miniLabel;
#else
            return new GUIStyle(GUI.skin.label) { fontSize = 10 };
#endif
        }
    }
} 