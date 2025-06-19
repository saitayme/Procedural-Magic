using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Systems;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;
using System.Collections;

namespace ProceduralWorld.Simulation.UI
{
    // Helper class for organizing events into narrative chapters
    public class ChronicleChapter
    {
        public string Title { get; set; }
        public List<HistoricalEventRecord> Events { get; set; } = new List<HistoricalEventRecord>();
    }

    // Add this class before the ChronicleReaderUI class
    public class ScrollWheelHandler : MonoBehaviour, UnityEngine.EventSystems.IScrollHandler
    {
        private ChronicleReaderUI _chronicleReader;
        
        public void Initialize(ChronicleReaderUI chronicleReader)
        {
            _chronicleReader = chronicleReader;
        }
        
        public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_chronicleReader != null)
            {
                _chronicleReader.OnMouseWheelScroll(eventData.scrollDelta.y);
            }
        }
    }

    public class ChronicleReaderUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject chronicleReaderPanel;
        [SerializeField] private TMP_Text chronicleTitle;
        [SerializeField] private TMP_Text chronicleSubtitle;
        [SerializeField] private TMP_Text chronicleText;
        [SerializeField] private ScrollRect chronicleScrollRect;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text pageIndicator;
        
        [Header("Chronicle Selection")]
        [SerializeField] private TMP_Dropdown civilizationDropdown;
        [SerializeField] private TMP_Dropdown chronicleVolumeDropdown;
        [SerializeField] private Button refreshChroniclesButton;
        
        [Header("Reading Experience")]
        [SerializeField] private Slider readingSpeedSlider;
        [SerializeField] private Toggle autoScrollToggle;
        [SerializeField] private TMP_Text readingProgressText;
        [SerializeField] private Slider fontSizeSlider;
        
        [Header("Analysis Panel")]
        [SerializeField] private GameObject analysisPanel;
        [SerializeField] private TMP_Text narrativeQualityText;
        [SerializeField] private TMP_Text dramaticIntensityText;
        [SerializeField] private TMP_Text thematicElementsText;
        [SerializeField] private TMP_Text causalConnectionsText;
        [SerializeField] private TMP_Text characterDevelopmentText;
        
        [Header("Interactive Features")]
        [SerializeField] private Button showCausalityButton;
        [SerializeField] private Button showThemesButton;
        [SerializeField] private Button showCharacterArcsButton;
        [SerializeField] private Button exportChronicleButton;
        
        // System references
        private LivingChronicleSystem _chronicleSystem;
        private EntityManager _entityManager;
        
        // Current state
        private CompiledChronicle _currentChronicle;
        private Entity _selectedCivilization;
        private List<CompiledChronicle> _availableChronicles;
        private int _currentPage;
        private int _totalPages;
        private string[] _pages;
        
        // Reading experience
        private float _autoScrollSpeed = 50f;
        private bool _isAutoScrolling = false;
        private float _baseFontSize = 14f;
        
        // Analysis data
        private NarrativeIntelligenceData _currentAnalysis;
        
        // Backup text component for compatibility
        private Text _backupChronicleText;
        
        // NUCLEAR SOLUTION: Windowed Text Renderer for massive text content
        private List<string> _fullTextLines = new List<string>();
        private int _visibleLineStart = 0;
        private int _linesPerPage = 30;

        private bool _useWindowedRenderer = false;
        
        // Emergency OnGUI fallback system
        private bool _useEmergencyGUI = false;
        private string _emergencyText = "";
        private int _emergencyScrollOffset = 0;
        private int _emergencyLinesPerScreen = 30;
        
        // Pagination system for large texts
        private string _fullChronicleText = "";
        private int _currentChunkIndex = 0;
        private int _chunkSize = 8000;
        
        private void Awake()
        {
            Debug.Log($"[ChronicleReaderUI] Awake called on GameObject: {gameObject.name}");
            Debug.Log($"[ChronicleReaderUI] GameObject active: {gameObject.activeInHierarchy}");
            Debug.Log($"[ChronicleReaderUI] Component enabled: {enabled}");
            
            // Ensure this GameObject persists across scene loads
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ChronicleReaderUI] Made GameObject persistent across scene loads");
            }
        }

        // Static method to ensure ChronicleReaderUI exists in the scene
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureChronicleReaderExists()
        {
            Debug.Log("[ChronicleReaderUI] Checking if ChronicleReaderUI exists in scene...");
            
            var existing = FindFirstObjectByType<ChronicleReaderUI>();
            if (existing == null)
            {
                Debug.Log("[ChronicleReaderUI] No ChronicleReaderUI found, creating one...");
                
                var go = new GameObject("ChronicleReaderUI");
                var component = go.AddComponent<ChronicleReaderUI>();
                
                // Make it persistent across scene loads
                DontDestroyOnLoad(go);
                
                Debug.Log("[ChronicleReaderUI] Created ChronicleReaderUI GameObject");
            }
            else
            {
                Debug.Log($"[ChronicleReaderUI] Found existing ChronicleReaderUI on GameObject: {existing.gameObject.name}");
            }
        }

        private void Start()
        {
            Debug.Log("[ChronicleReaderUI] Starting initialization...");
            
            // UI will be created by InitializeUI()
            
            InitializeUI();
            SetupEventListeners();
            FindSystemReferences();
            
            // Debug UI component assignments
            Debug.Log($"[ChronicleReaderUI] UI Components Status:");
            Debug.Log($"  chronicleReaderPanel: {(chronicleReaderPanel != null ? "OK" : "NULL")}");
            Debug.Log($"  chronicleTitle: {(chronicleTitle != null ? "OK" : "NULL")}");
            Debug.Log($"  chronicleText: {(chronicleText != null ? "TMP_Text Found" : (_backupChronicleText != null ? "Regular Text Found" : "Not Found"))}");
            Debug.Log($"  chronicleScrollRect: {(chronicleScrollRect != null ? "OK" : "NULL")}");
            Debug.Log($"  civilizationDropdown: {(civilizationDropdown != null ? "OK" : "NULL")}");
            Debug.Log($"  chronicleVolumeDropdown: {(chronicleVolumeDropdown != null ? "OK" : "NULL")}");
            
            // Test setting text directly
            if (HasTextComponent())
            {
                SetChronicleText("*** CHRONICLE READER UI TEST ***\n\n> UI Components Successfully Created!\n> Text Rendering Working!\n> Panel is Active and Visible!\n\nThis is your Living Chronicle Reader interface.\n\n- Press F6 to toggle this interface\n- Click 'Load Chronicles' to view your civilization's stories\n- Use the close button to hide this panel\n\nIf you can read this message, the UI is working correctly!");
                Debug.Log("[ChronicleReaderUI] Set test text to chronicle component");
                
                // Show the chronicle reader immediately for testing
                if (chronicleReaderPanel != null)
                {
                    chronicleReaderPanel.SetActive(true);
                    Debug.Log("[ChronicleReaderUI] Activated chronicle reader panel for testing");
                    
                    // Ensure the panel is properly positioned and visible
                    var rectTransform = chronicleReaderPanel.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Debug.Log($"[ChronicleReaderUI] Panel position: {rectTransform.anchoredPosition}");
                        Debug.Log($"[ChronicleReaderUI] Panel size: {rectTransform.sizeDelta}");
                        Debug.Log($"[ChronicleReaderUI] Panel anchors: {rectTransform.anchorMin} to {rectTransform.anchorMax}");
                    }
                }
            }
            else
            {
                Debug.LogError("[ChronicleReaderUI] No text component available - will create fallback UI");
            }
            
            Debug.Log("[ChronicleReaderUI] Initialization complete");
        }

        private void InitializeUI()
        {
            Debug.Log("[ChronicleReaderUI] Initializing UI components...");
            
            // Instead of searching for non-existent components, let's create the proper UI structure
            CreateProperChronicleUI();
            
            _availableChronicles = new List<CompiledChronicle>();
            _currentPage = 0;
            _totalPages = 1;
            
            Debug.Log("[ChronicleReaderUI] UI initialization complete");
        }
        
        private void CreateProperChronicleUI()
        {
            Debug.Log("[ChronicleReaderUI] Creating proper chronicle UI...");
            
            // If UI already exists, don't recreate it
            if (chronicleReaderPanel != null)
            {
                Debug.Log("[ChronicleReaderUI] UI already exists, skipping creation");
                return;
            }
            
            try
            {
                // Find or create canvas
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.Log("[ChronicleReaderUI] Creating new Canvas");
                    var canvasGO = new GameObject("Canvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100; // Make sure it's on top
                    canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    
                    // Ensure the canvas persists across scene loads (only for root GameObjects)
                    DontDestroyOnLoad(canvasGO);
                }
                
                // Create the main chronicle reader panel
                var panelGO = new GameObject("ChronicleReaderPanel");
                panelGO.transform.SetParent(canvas.transform, false);
                
                var panelRect = panelGO.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.1f, 0.1f);
                panelRect.anchorMax = new Vector2(0.9f, 0.9f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                
                var panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                
                chronicleReaderPanel = panelGO;
                
                Debug.Log("[ChronicleReaderUI] Created main panel");
                
                // Create header with title
                var headerGO = new GameObject("Header");
                headerGO.transform.SetParent(panelGO.transform, false);
                var headerRect = headerGO.AddComponent<RectTransform>();
                headerRect.anchorMin = new Vector2(0, 0.9f);
                headerRect.anchorMax = new Vector2(1, 1f);
                headerRect.offsetMin = new Vector2(20, 0);
                headerRect.offsetMax = new Vector2(-20, 0);
                
                var headerImage = headerGO.AddComponent<UnityEngine.UI.Image>();
                headerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                // Title text
                var titleGO = new GameObject("ChronicleTitle");
                titleGO.transform.SetParent(headerGO.transform, false);
                var titleRect = titleGO.AddComponent<RectTransform>();
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.offsetMin = new Vector2(10, 0);
                titleRect.offsetMax = new Vector2(-10, 0);
                
                chronicleTitle = titleGO.AddComponent<TextMeshProUGUI>();
                chronicleTitle.text = "Living Chronicle Reader";
                chronicleTitle.fontSize = 20;
                chronicleTitle.color = Color.white;
                chronicleTitle.fontStyle = FontStyles.Bold;
                chronicleTitle.alignment = TextAlignmentOptions.Center;
                Debug.Log("[ChronicleReaderUI] Created title");
                
                // Create main content area with scroll
                var scrollViewGO = new GameObject("ScrollView");
                scrollViewGO.transform.SetParent(panelGO.transform, false);
                
                var scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
                scrollViewRect.anchorMin = new Vector2(0, 0.1f);
                scrollViewRect.anchorMax = new Vector2(1, 0.85f);
                scrollViewRect.offsetMin = new Vector2(20, 20);
                scrollViewRect.offsetMax = new Vector2(-20, -20);
                
                var scrollViewImage = scrollViewGO.AddComponent<UnityEngine.UI.Image>();
                scrollViewImage.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
                
                chronicleScrollRect = scrollViewGO.AddComponent<ScrollRect>();
                chronicleScrollRect.horizontal = false;
                chronicleScrollRect.vertical = true;
                chronicleScrollRect.movementType = ScrollRect.MovementType.Clamped;
                chronicleScrollRect.scrollSensitivity = 30f;
                chronicleScrollRect.inertia = true;
                chronicleScrollRect.decelerationRate = 0.135f;
                
                // Create viewport
                var viewportGO = new GameObject("Viewport");
                viewportGO.transform.SetParent(scrollViewGO.transform, false);
                
                var viewportRect = viewportGO.AddComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(10, 10);
                viewportRect.offsetMax = new Vector2(-10, -10);
                
                var viewportMask = viewportGO.AddComponent<UnityEngine.UI.Mask>();
                viewportMask.showMaskGraphic = false;
                
                var viewportImage = viewportGO.AddComponent<UnityEngine.UI.Image>();
                viewportImage.color = Color.clear;
                
                chronicleScrollRect.viewport = viewportRect;
                
                // Create vertical scrollbar
                var scrollbarGO = new GameObject("Scrollbar Vertical");
                scrollbarGO.transform.SetParent(scrollViewGO.transform, false);
                
                var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.offsetMin = new Vector2(-20, 0);
                scrollbarRect.offsetMax = new Vector2(0, 0);
                
                var scrollbarImage = scrollbarGO.AddComponent<UnityEngine.UI.Image>();
                scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                
                // Create scrollbar handle
                var handleGO = new GameObject("Sliding Area");
                handleGO.transform.SetParent(scrollbarGO.transform, false);
                
                var handleAreaRect = handleGO.AddComponent<RectTransform>();
                handleAreaRect.anchorMin = Vector2.zero;
                handleAreaRect.anchorMax = Vector2.one;
                handleAreaRect.offsetMin = new Vector2(5, 5);
                handleAreaRect.offsetMax = new Vector2(-5, -5);
                
                var handleSliderGO = new GameObject("Handle");
                handleSliderGO.transform.SetParent(handleGO.transform, false);
                
                var handleSliderRect = handleSliderGO.AddComponent<RectTransform>();
                handleSliderRect.anchorMin = Vector2.zero;
                handleSliderRect.anchorMax = Vector2.one;
                handleSliderRect.offsetMin = Vector2.zero;
                handleSliderRect.offsetMax = Vector2.zero;
                
                var handleImage = handleSliderGO.AddComponent<UnityEngine.UI.Image>();
                handleImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                
                scrollbar.handleRect = handleSliderRect;
                scrollbar.targetGraphic = handleImage;
                
                // Connect scrollbar to scroll rect
                chronicleScrollRect.verticalScrollbar = scrollbar;
                chronicleScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                
                // Create content area
                var contentGO = new GameObject("Content");
                contentGO.transform.SetParent(viewportGO.transform, false);
                
                var contentRect = contentGO.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, 1000);
                
                chronicleScrollRect.content = contentRect;
                
                // FIXED APPROACH: Use VerticalLayoutGroup to properly connect text size to content size
                var layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childControlHeight = true;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.padding = new RectOffset(0, 0, 0, 0);
                layoutGroup.spacing = 0;
                
                var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                
                // Create the main text component with simple, direct sizing
                var textGO = new GameObject("ChronicleText");
                textGO.transform.SetParent(contentGO.transform, false);
                
                var textRect = textGO.AddComponent<RectTransform>();
                // Let VerticalLayoutGroup handle positioning, just set margins
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.offsetMin = new Vector2(25, 25);     // Left, Bottom margins - increased from 15 to 25
                textRect.offsetMax = new Vector2(-25, -25);   // Right, Top margins - increased from 15 to 25
                
                chronicleText = textGO.AddComponent<TextMeshProUGUI>();
                chronicleText.text = "Welcome to the Living Chronicle Reader!\n\nClick 'Load Chronicles' to view your civilization's epic stories.";
                chronicleText.fontSize = 16;
                chronicleText.color = new Color(0.95f, 0.95f, 0.85f, 1f); // Cream-white for better readability
                chronicleText.textWrappingMode = TextWrappingModes.Normal;
                chronicleText.overflowMode = TextOverflowModes.Masking; // FIXED: Use Masking instead of Overflow to prevent clipping
                chronicleText.alignment = TextAlignmentOptions.TopLeft;
                chronicleText.lineSpacing = 1.2f; // Increase line spacing for better readability
                chronicleText.paragraphSpacing = 8f; // Add paragraph spacing
                chronicleText.richText = false; // Disable rich text to avoid formatting issues
                
                // Add ContentSizeFitter to text for height calculation
                var textContentSizeFitter = textGO.AddComponent<ContentSizeFitter>();
                textContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                textContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                
                Debug.Log("[ChronicleReaderUI] Created main text area");
                
                // Create control buttons
                CreateControlButtons(panelGO);
                
                // Start with panel hidden
                chronicleReaderPanel.SetActive(false);
                
                Debug.Log("[ChronicleReaderUI] Proper chronicle UI created successfully!");
                
                // Validate that all critical components were created
                if (chronicleReaderPanel == null)
                {
                    Debug.LogError("[ChronicleReaderUI] chronicleReaderPanel is null after creation!");
                }
                if (chronicleTitle == null)
                {
                    Debug.LogError("[ChronicleReaderUI] chronicleTitle is null after creation!");
                }
                if (chronicleText == null)
                {
                    Debug.LogError("[ChronicleReaderUI] chronicleText is null after creation!");
                }
                if (chronicleScrollRect == null)
                {
                    Debug.LogError("[ChronicleReaderUI] chronicleScrollRect is null after creation!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChronicleReaderUI] Failed to create proper UI: {e.Message}\n{e.StackTrace}");
                // Only fall back to simple UI if proper UI creation fails
                CreateFallbackUI();
            }
        }
        
        private void CreateControlButtons(GameObject parentPanel)
        {
            Debug.Log("[ChronicleReaderUI] Creating CLEAN control buttons...");
            
            // Create simple button bar at top
            var buttonBarGO = new GameObject("CleanButtonBar");
            buttonBarGO.transform.SetParent(parentPanel.transform, false);
            var buttonBarRect = buttonBarGO.AddComponent<RectTransform>();
            buttonBarRect.anchorMin = new Vector2(0, 0.92f);
            buttonBarRect.anchorMax = new Vector2(1, 1);
            buttonBarRect.offsetMin = new Vector2(10, 0);
            buttonBarRect.offsetMax = new Vector2(-10, -5);
            
            // Button bar background
            var buttonBarBg = buttonBarGO.AddComponent<UnityEngine.UI.Image>();
            buttonBarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Load Chronicles button (main action)
            CreateCleanButton(buttonBarGO, "LOAD CHRONICLES", new Vector2(0, 0), new Vector2(0.35f, 1), 
                new Color(0.2f, 0.6f, 0.2f, 0.9f), () => {
                    Debug.Log("[ChronicleReaderUI] Load Chronicles clicked");
                    ShowRealChronicles();
                });
            
            // Test button 
            CreateCleanButton(buttonBarGO, "TEST", new Vector2(0.37f, 0), new Vector2(0.52f, 1), 
                new Color(0.6f, 0.2f, 0.2f, 0.9f), () => {
                    Debug.Log("[ChronicleReaderUI] Test clicked");
                    TestWindowedRenderer();
                });
            
            // Close button
            CreateCleanButton(buttonBarGO, "CLOSE", new Vector2(0.85f, 0), new Vector2(1, 1), 
                new Color(0.5f, 0.2f, 0.2f, 0.9f), () => {
                    Debug.Log("[ChronicleReaderUI] Close clicked");
                    if (chronicleReaderPanel != null)
                        chronicleReaderPanel.SetActive(false);
                });
        }
        
        private void CreateCleanButton(GameObject parent, string text, Vector2 anchorMin, Vector2 anchorMax, Color color, System.Action onClick)
        {
            var buttonGO = new GameObject($"CleanButton_{text}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = new Vector2(2, 2);
            buttonRect.offsetMax = new Vector2(-2, -2);
            
            var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = color;
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 11;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;
        }

        private void SetupEventListeners()
        {
            if (previousPageButton != null)
                previousPageButton.onClick.AddListener(PreviousPage);
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(NextPage);
            if (refreshChroniclesButton != null)
                refreshChroniclesButton.onClick.AddListener(RefreshChronicles);
            
            if (civilizationDropdown != null)
                civilizationDropdown.onValueChanged.AddListener(OnCivilizationSelected);
            if (chronicleVolumeDropdown != null)
                chronicleVolumeDropdown.onValueChanged.AddListener(OnChronicleVolumeSelected);
            
            if (readingSpeedSlider != null)
                readingSpeedSlider.onValueChanged.AddListener(OnReadingSpeedChanged);
            if (fontSizeSlider != null)
                fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
            if (autoScrollToggle != null)
                autoScrollToggle.onValueChanged.AddListener(OnAutoScrollToggled);
            
            if (showCausalityButton != null)
                showCausalityButton.onClick.AddListener(ShowCausalityView);
            if (showThemesButton != null)
                showThemesButton.onClick.AddListener(ShowThemeAnalysis);
            if (showCharacterArcsButton != null)
                showCharacterArcsButton.onClick.AddListener(ShowCharacterArcs);
            if (exportChronicleButton != null)
                exportChronicleButton.onClick.AddListener(ExportChronicle);
        }

        private void FindSystemReferences()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _chronicleSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LivingChronicleSystem>();
            
            if (_chronicleSystem == null)
            {
                Debug.LogWarning("[ChronicleReaderUI] Could not find LivingChronicleSystem");
            }
        }

        private void Update()
        {
            // Handle keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("[ChronicleReaderUI] F6 pressed - toggling chronicle reader");
                ToggleChronicleReader();
            }
            
            // Auto-scroll functionality
            if (_isAutoScrolling && chronicleScrollRect != null)
            {
                AutoScrollChronicle();
            }
            
            // Update reading progress
            UpdateReadingProgress();
            
            // Update the display text if we're showing the initial screen
            if (_chronicleSystem == null && chronicleText != null && chronicleText.text.Contains("LOADING"))
            {
                FindSystemReferences();
                if (_chronicleSystem != null)
                {
                    chronicleText.text = GetInitialDisplayText();
                }
            }
        }

        // ==== CHRONICLE LOADING ====
        
        private void RefreshChronicles()
        {
            if (_chronicleSystem == null) 
            {
                Debug.LogWarning("[ChronicleReaderUI] Chronicle system not available for refresh");
                return;
            }
            
            _availableChronicles.Clear();
            PopulateCivilizationDropdown();
            
            Debug.Log($"[ChronicleReaderUI] Refreshed chronicle list. Chronicle system active: {_chronicleSystem != null}");
        }

        private void PopulateCivilizationDropdown()
        {
            if (civilizationDropdown == null)
            {
                Debug.LogWarning("[ChronicleReaderUI] Civilization dropdown is null");
                return;
            }
            
            civilizationDropdown.ClearOptions();
            
            var civQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            var civilizations = civQuery.ToEntityArray(Allocator.Temp);
            var civData = civQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            var options = new List<TMP_Dropdown.OptionData>();
            options.Add(new TMP_Dropdown.OptionData("Select Civilization..."));
            
            for (int i = 0; i < civilizations.Length; i++)
            {
                var civName = civData[i].Name.ToString();
                options.Add(new TMP_Dropdown.OptionData(civName));
            }
            
            civilizationDropdown.AddOptions(options);
            
            Debug.Log($"[ChronicleReaderUI] Found {civilizations.Length} civilizations for dropdown");
            
            civilizations.Dispose();
            civData.Dispose();
        }

        private void OnCivilizationSelected(int index)
        {
            if (index == 0) return; // "Select Civilization..." option
            
            var civQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            var civilizations = civQuery.ToEntityArray(Allocator.Temp);
            
            if (index - 1 < civilizations.Length)
            {
                _selectedCivilization = civilizations[index - 1];
                LoadChroniclesForCivilization(_selectedCivilization);
            }
            
            civilizations.Dispose();
        }

        private void LoadChroniclesForCivilization(Entity civilization)
        {
            if (_chronicleSystem == null) return;
            
            var chronicles = _chronicleSystem.CompileChroniclesForCivilization(civilization, Allocator.Temp);
            
            _availableChronicles.Clear();
            if (chronicleVolumeDropdown != null)
            {
                chronicleVolumeDropdown.ClearOptions();
                
                var options = new List<TMP_Dropdown.OptionData>();
                options.Add(new TMP_Dropdown.OptionData("Select Chronicle Volume..."));
                
                for (int i = 0; i < chronicles.Length; i++)
                {
                    var chronicle = chronicles[i];
                    _availableChronicles.Add(chronicle);
                    
                    var volumeTitle = $"Vol. {i + 1}: {chronicle.Title}";
                    options.Add(new TMP_Dropdown.OptionData(volumeTitle));
                }
                
                chronicleVolumeDropdown.AddOptions(options);
            }
            
            chronicles.Dispose();
            
            Debug.Log($"[ChronicleReaderUI] Loaded {_availableChronicles.Count} chronicles for civilization");
        }

        private void OnChronicleVolumeSelected(int index)
        {
            if (index == 0 || index - 1 >= _availableChronicles.Count) return;
            
            _currentChronicle = _availableChronicles[index - 1];
            DisplayChronicle(_currentChronicle);
        }

        // ==== CHRONICLE DISPLAY ====
        
        private void DisplayChronicle(CompiledChronicle chronicle)
        {
            chronicleReaderPanel.SetActive(true);
            
            // Set title and subtitle
            chronicleTitle.text = chronicle.Title.ToString();
            chronicleSubtitle.text = chronicle.Subtitle.ToString();
            
            // Display full chronicle text with scrolling instead of pagination
            var chronicleTextContent = chronicle.ChronicleText.ToString();
            if (chronicleText != null)
            {
                // Format the text for better readability
                var formattedText = FormatChronicleText(chronicleTextContent);
                chronicleText.text = formattedText;
                
                // Reset scroll to top
                if (chronicleScrollRect != null)
                    chronicleScrollRect.verticalNormalizedPosition = 1f;
            }
            
            // Hide pagination controls since we're using continuous scrolling
            if (pageIndicator != null) pageIndicator.gameObject.SetActive(false);
            if (previousPageButton != null) previousPageButton.gameObject.SetActive(false);
            if (nextPageButton != null) nextPageButton.gameObject.SetActive(false);
            
            // Load analysis data
            LoadChronicleAnalysis(chronicle);
            
            Debug.Log($"[ChronicleReaderUI] Displaying chronicle: {chronicle.Title}");
        }

        private void PrepareChroniclePages(string chronicleText)
        {
            // Strip HTML tags first since we disabled rich text support
            var cleanText = StripHtmlTags(chronicleText);
            
            // Enhanced text formatting WITHOUT rich text tags
            var formattedText = FormatChronicleTextPlain(cleanText);
            
            // Split into SAFE pages to avoid Unity's 65k vertex limit - reduced to 8000 characters per page
            _pages = SplitTextIntoPages(formattedText, 8000);
            _totalPages = _pages.Length;
            
            Debug.Log($"[ChronicleReaderUI] Created {_totalPages} pages from {formattedText.Length} characters");
            UpdatePageIndicator();
        }

        private string FormatChronicleText(string rawText)
        {
            var formatted = new StringBuilder();
            
            // Add rich formatting for better readability
            var lines = rawText.Split('\n');
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    formatted.AppendLine();
                    continue;
                }
                
                // Format main title
                if (line.StartsWith("THE CHRONICLES OF"))
                {
                    formatted.AppendLine($"<size=22><b><color=#8B4513>{line}</color></b></size>");
                    continue;
                }
                
                // Format chapter headers
                if (line.StartsWith("CHAPTER"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine($"<size=18><b><color=#8B4513>{line}</color></b></size>");
                    formatted.AppendLine();
                    continue;
                }
                
                // Format section dividers
                if (line.Contains("═══") || line.Contains("───"))
                {
                    formatted.AppendLine($"<color=#8B4513>{line}</color>");
                    continue;
                }
                
                // Format year markers and narrative openings
                if (line.Contains("In the year") || line.Contains("Soon after,") || line.Contains("In time,") || line.Contains("Meanwhile,"))
                {
                    // Add paragraph spacing before new events
                    if (line.Contains("In the year") && formatted.Length > 0)
                        formatted.AppendLine();
                    
                    formatted.AppendLine($"<b><color=#4A4A4A>{line}</color></b>");
                    continue;
                }
                
                // Format introductory text
                if (line.Contains("Here are recorded") || line.Contains("their triumphs and tragedies"))
                {
                    formatted.AppendLine($"<i><color=#666666>{line}</color></i>");
                    continue;
                }
                
                // Format chapter conclusions
                if (line.Contains("Thus ended this chapter") || line.Contains("And so the chronicles record"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine($"<i><color=#666666>{line}</color></i>");
                    continue;
                }
                
                // Regular paragraph formatting with better spacing
                if (!string.IsNullOrWhiteSpace(line))
                {
                    formatted.AppendLine($"<color=#2C2C2C>{line}</color>");
                }
            }
            
            return formatted.ToString();
        }

        private string FormatChronicleTextPlain(string rawText)
        {
            var formatted = new StringBuilder();
            
            // Plain text formatting without rich text tags
            var lines = rawText.Split('\n');
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    formatted.AppendLine();
                    continue;
                }
                
                // Format main title with simple caps and spacing
                if (line.StartsWith("THE CHRONICLES OF"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine("═══════════════════════════════════════");
                    formatted.AppendLine(line.ToUpper());
                    formatted.AppendLine("═══════════════════════════════════════");
                    formatted.AppendLine();
                    continue;
                }
                
                // Format chapter headers
                if (line.StartsWith("CHAPTER"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine("───────────────────────────────────────");
                    formatted.AppendLine(line.ToUpper());
                    formatted.AppendLine("───────────────────────────────────────");
                    formatted.AppendLine();
                    continue;
                }
                
                // Format section dividers
                if (line.Contains("═══") || line.Contains("───"))
                {
                    formatted.AppendLine(line);
                    continue;
                }
                
                // Format year markers and narrative openings with spacing
                if (line.Contains("In the year") || line.Contains("Soon after,") || line.Contains("In time,") || line.Contains("Meanwhile,"))
                {
                    // Add paragraph spacing before new events
                    if (line.Contains("In the year") && formatted.Length > 0)
                        formatted.AppendLine();
                    
                    formatted.AppendLine($">> {line}");
                    continue;
                }
                
                // Format introductory text
                if (line.Contains("Here are recorded") || line.Contains("their triumphs and tragedies"))
                {
                    formatted.AppendLine($"    {line}");
                    continue;
                }
                
                // Format chapter conclusions
                if (line.Contains("Thus ended this chapter") || line.Contains("And so the chronicles record"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine($"    {line}");
                    continue;
                }
                
                // Regular paragraph formatting with better spacing
                if (!string.IsNullOrWhiteSpace(line))
                {
                    formatted.AppendLine(line);
                }
            }
            
            return formatted.ToString();
        }

        private string ExtractYearMarker(string line)
        {
            var startIndex = line.IndexOf("In the year");
            var endIndex = line.IndexOf(',');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return line.Substring(startIndex, endIndex - startIndex);
            }
            return "In the year";
        }

        private string[] SplitTextIntoPages(string text, int charactersPerPage)
        {
            var pages = new List<string>();
            var paragraphs = text.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            
            var currentPage = new StringBuilder();
            
            foreach (var paragraph in paragraphs)
            {
                // Safe page splitting to avoid Unity vertex limit - split when approaching limit
                if (currentPage.Length + paragraph.Length > charactersPerPage * 1.2f && currentPage.Length > charactersPerPage * 0.5f)
                {
                    pages.Add(currentPage.ToString());
                    currentPage.Clear();
                }
                
                currentPage.AppendLine(paragraph);
                currentPage.AppendLine();
            }
            
            // Always add the last page, even if it's small
            if (currentPage.Length > 0)
            {
                pages.Add(currentPage.ToString());
            }
            
            // If we only have one very long page, split it more intelligently
            if (pages.Count == 1 && pages[0].Length > charactersPerPage * 2)
            {
                return SplitLongTextIntoPages(pages[0], charactersPerPage);
            }
            
            return pages.ToArray();
        }
        
        private string[] SplitLongTextIntoPages(string longText, int charactersPerPage)
        {
            var pages = new List<string>();
            var lines = longText.Split('\n');
            var currentPage = new StringBuilder();
            
            foreach (var line in lines)
            {
                // Only split at natural breaks (chapter headers, year markers, etc.)
                bool isNaturalBreak = line.StartsWith("CHAPTER") || 
                                    line.Contains("In the year") || 
                                    line.Contains("═══") || 
                                    line.Contains("───");
                
                if (currentPage.Length > charactersPerPage && isNaturalBreak && currentPage.Length > 0)
                {
                    pages.Add(currentPage.ToString());
                    currentPage.Clear();
                }
                
                currentPage.AppendLine(line);
            }
            
            if (currentPage.Length > 0)
            {
                pages.Add(currentPage.ToString());
            }
            
            return pages.ToArray();
        }
        
        private List<string> SplitLargeText(string text, int maxCharsPerChunk)
        {
            var chunks = new List<string>();
            
            // If text is small enough, return as single chunk
            if (text.Length <= maxCharsPerChunk)
            {
                chunks.Add(text);
                return chunks;
            }
            
            // Split by sentences first to maintain readability
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new StringBuilder();
            
            foreach (var sentence in sentences)
            {
                var fullSentence = sentence.Trim() + ". ";
                
                // If adding this sentence would exceed limit, start new chunk
                if (currentChunk.Length + fullSentence.Length > maxCharsPerChunk && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }
                
                // If single sentence is too long, force split it
                if (fullSentence.Length > maxCharsPerChunk)
                {
                    var words = fullSentence.Split(' ');
                    var wordChunk = new StringBuilder();
                    
                    foreach (var word in words)
                    {
                        if (wordChunk.Length + word.Length + 1 > maxCharsPerChunk && wordChunk.Length > 0)
                        {
                            chunks.Add(wordChunk.ToString());
                            wordChunk.Clear();
                        }
                        wordChunk.Append(word + " ");
                    }
                    
                    if (wordChunk.Length > 0)
                        currentChunk.Append(wordChunk.ToString());
                }
                else
                {
                    currentChunk.Append(fullSentence);
                }
            }
            
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
            }
            
            return chunks;
        }

        private void DisplayCurrentPage()
        {
            if (_pages == null || _currentPage >= _pages.Length) return;
            
            if (chronicleText != null)
            {
                var pageText = _pages[_currentPage];
                
                // Final safety check - if page is still too large for Unity's vertex limit, truncate it
                if (pageText.Length > 12000) // Conservative limit to avoid 65k vertex error
                {
                    pageText = pageText.Substring(0, 12000) + "\n\n[PAGE TRUNCATED - TOO LARGE FOR DISPLAY]\n[Use navigation to view more content]";
                    Debug.LogWarning($"[ChronicleReaderUI] Page {_currentPage + 1} truncated due to Unity vertex limit");
                }
                
                chronicleText.text = pageText;
            }
            
            UpdatePageIndicator();
            UpdateNavigationButtons();
            
            // Reset scroll position
            if (chronicleScrollRect != null)
                chronicleScrollRect.verticalNormalizedPosition = 1f;
        }

        // ==== NAVIGATION ====
        
        private void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                DisplayCurrentPage();
            }
        }

        private void NextPage()
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                DisplayCurrentPage();
            }
        }

        private void UpdatePageIndicator()
        {
            if (pageIndicator != null && _totalPages > 0)
            {
                pageIndicator.text = $"Page {_currentPage + 1} of {_totalPages}";
            }
        }

        private void UpdateNavigationButtons()
        {
            if (previousPageButton != null)
                previousPageButton.interactable = _currentPage > 0;
            if (nextPageButton != null)
                nextPageButton.interactable = _currentPage < _totalPages - 1;
        }

        // ==== READING EXPERIENCE ====
        
        private void OnReadingSpeedChanged(float value)
        {
            _autoScrollSpeed = Mathf.Lerp(10f, 100f, value);
        }

        private void OnFontSizeChanged(float value)
        {
            _baseFontSize = Mathf.Lerp(10f, 20f, value);
            if (chronicleText != null)
            {
                chronicleText.fontSize = _baseFontSize;
            }
        }

        private void OnAutoScrollToggled(bool isOn)
        {
            _isAutoScrolling = isOn;
        }

        private void AutoScrollChronicle()
        {
            if (chronicleScrollRect != null)
            {
                var currentPos = chronicleScrollRect.verticalNormalizedPosition;
                var newPos = currentPos - (_autoScrollSpeed * Time.deltaTime / 1000f);
                
                chronicleScrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
                
                // Auto-advance to next page when reaching bottom
                if (newPos <= 0f && _currentPage < _totalPages - 1)
                {
                    NextPage();
                }
            }
        }

        private void UpdateReadingProgress()
        {
            if (_totalPages > 0 && readingProgressText != null)
            {
                var progress = (_currentPage + 1f) / _totalPages * 100f;
                readingProgressText.text = $"Progress: {progress:F1}%";
            }
        }

        // ==== ANALYSIS FEATURES ====
        
        private void LoadChronicleAnalysis(CompiledChronicle chronicle)
        {
            // This would load actual analysis data from the chronicle system
            _currentAnalysis = new NarrativeIntelligenceData
            {
                StoryQuality = chronicle.HistoricalSignificance * 0.8f,
                DramaticPacing = chronicle.DramaticIntensity * 0.9f,
                CausalCoherence = 0.85f, // Placeholder
                ThematicConsistency = 0.75f, // Placeholder
                CharacterDevelopment = 0.8f, // Placeholder
                HistoricalAccuracy = 0.95f, // Placeholder
                ReadabilityScore = 0.88f, // Placeholder
                EmotionalResonance = chronicle.DramaticIntensity * 0.7f
            };
            
            UpdateAnalysisDisplay();
        }

        private void UpdateAnalysisDisplay()
        {
            if (narrativeQualityText != null)
                narrativeQualityText.text = $"Story Quality: {_currentAnalysis.StoryQuality:F2}";
            
            if (dramaticIntensityText != null)
                dramaticIntensityText.text = $"Dramatic Intensity: {_currentAnalysis.DramaticPacing:F2}";
            
            if (thematicElementsText != null)
                thematicElementsText.text = $"Thematic Consistency: {_currentAnalysis.ThematicConsistency:F2}";
            
            if (causalConnectionsText != null)
                causalConnectionsText.text = $"Causal Coherence: {_currentAnalysis.CausalCoherence:F2}";
            
            if (characterDevelopmentText != null)
                characterDevelopmentText.text = $"Character Development: {_currentAnalysis.CharacterDevelopment:F2}";
        }

        private void ShowCausalityView()
        {
            // This would show an interactive causality diagram
            Debug.Log("[ChronicleReaderUI] Showing causality view - feature to be implemented");
        }

        private void ShowThemeAnalysis()
        {
            // This would show thematic analysis
            Debug.Log("[ChronicleReaderUI] Showing theme analysis - feature to be implemented");
        }

        private void ShowCharacterArcs()
        {
            // This would show character development over time
            Debug.Log("[ChronicleReaderUI] Showing character arcs - feature to be implemented");
        }

        private void ExportChronicle()
        {
            if (_pages == null || _pages.Length == 0) return;
            
            var fullText = string.Join("\n\n", _pages);
            var fileName = $"Chronicle_{_currentChronicle.Title}_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            // Save to persistent data path
            var filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            System.IO.File.WriteAllText(filePath, fullText);
            
            Debug.Log($"[ChronicleReaderUI] Exported chronicle to: {filePath}");
        }

        // ==== UI MANAGEMENT ====
        
        private void UpdateUI()
        {
            // Update UI state based on current data
        }

        public void ToggleChronicleReader()
        {
            Debug.Log("[ChronicleReaderUI] ToggleChronicleReader called");
            
            if (chronicleReaderPanel == null)
            {
                Debug.LogWarning("[ChronicleReaderUI] Chronicle reader panel is null - creating simple UI");
                CreateSimpleReliableUI();
                return;
            }
            
            bool wasActive = chronicleReaderPanel.activeSelf;
            chronicleReaderPanel.SetActive(!wasActive);
            
            Debug.Log($"[ChronicleReaderUI] Chronicle reader toggled: {!wasActive}");
            
            if (chronicleReaderPanel.activeSelf)
            {
                // Load fresh chronicles every time we open
                LoadFreshChronicles();
            }
        }

        public void ToggleAnalysisPanel()
        {
            if (analysisPanel != null)
                analysisPanel.SetActive(!analysisPanel.activeSelf);
        }
        
        private void CreateSimpleReliableUI()
        {
            Debug.Log("[ChronicleReaderUI] Creating simple reliable UI");
            
            // Find or create canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create main panel
            var panelGO = new GameObject("ChronicleReaderPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            var panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            chronicleReaderPanel = panelGO;
            
            // Create title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, 0);
            
            var titleText = titleGO.AddComponent<UnityEngine.UI.Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.text = "LIVING CHRONICLE READER";
            titleText.fontSize = 20;
            titleText.color = new Color(1f, 0.8f, 0.4f, 1f);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            // Create text area
            var textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(panelGO.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0, 0.15f);
            textAreaRect.anchorMax = new Vector2(1, 0.85f);
            textAreaRect.offsetMin = new Vector2(20, 0);
            textAreaRect.offsetMax = new Vector2(-20, 0);
            
            var textAreaImage = textAreaGO.AddComponent<UnityEngine.UI.Image>();
            textAreaImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            
            // Create text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(40, 100); // Much larger margins to prevent clipping
            textRect.offsetMax = new Vector2(-40, -40); // Ensure text stays well within bounds
            
            var textComponent = textGO.AddComponent<UnityEngine.UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 16;
            textComponent.color = new Color(0.95f, 0.95f, 0.85f, 1f);
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.supportRichText = false;
            textComponent.text = "Loading chronicles...";
            
            // Store references for both Text and TextMeshPro compatibility
            if (chronicleText is TextMeshProUGUI)
            {
                // If we have a TextMeshPro component, we need to handle it differently
                // For now, just use the backup text component
            }
            _backupChronicleText = textComponent;
            
            // Create control buttons
            CreateSimpleControlButtons(panelGO);
            
            // Load chronicles immediately
            LoadFreshChronicles();
            
            Debug.Log("[ChronicleReaderUI] Simple reliable UI created successfully");
        }
        
        private void CreateSimpleControlButtons(GameObject parent)
        {
            // Load button
            CreateSimpleButton(parent, "📖 LOAD FRESH", new Vector2(0.02f, 0.02f), new Vector2(0.25f, 0.12f), () => LoadFreshChronicles());
            
            // Previous page button
            CreateSimpleButton(parent, "◀ PREV", new Vector2(0.27f, 0.02f), new Vector2(0.42f, 0.12f), () => {
                if (_currentChunkIndex > 0) {
                    _currentChunkIndex--;
                    UpdateSimpleText(_backupChronicleText);
                }
            });
            
            // Next page button
            CreateSimpleButton(parent, "NEXT ▶", new Vector2(0.44f, 0.02f), new Vector2(0.59f, 0.12f), () => {
                int totalPages = Mathf.CeilToInt((float)_fullChronicleText.Length / _chunkSize);
                if (_currentChunkIndex < totalPages - 1) {
                    _currentChunkIndex++;
                    UpdateSimpleText(_backupChronicleText);
                }
            });
            
            // Close button
            CreateSimpleButton(parent, "✕ CLOSE", new Vector2(0.75f, 0.02f), new Vector2(0.98f, 0.12f), () => {
                if (chronicleReaderPanel != null)
                    chronicleReaderPanel.SetActive(false);
            });
        }
        
        private void LoadFreshChronicles()
        {
            Debug.Log("[ChronicleReaderUI] Loading fresh chronicles...");
            
            try
            {
                var chronicleSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<LivingChronicleSystem>();
                if (chronicleSystem != null)
                {
                    // Get all civilizations and compile their chronicles
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    var civQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CivilizationData>());
                    var civEntities = civQuery.ToEntityArray(Allocator.Temp);
                    
                    var allChronicleText = new System.Text.StringBuilder();
                    
                    if (civEntities.Length > 0)
                    {
                        // Get chronicles from the first civilization (or all of them)
                        var chronicles = chronicleSystem.CompileChroniclesForCivilization(civEntities[0], Allocator.Temp);
                        
                        if (chronicles.Length > 0)
                        {
                            // Use the first chronicle's text
                            var firstChronicle = chronicles[0];
                            allChronicleText.Append(firstChronicle.ChronicleText.ToString());
                        }
                        else
                        {
                            allChronicleText.Append("No chronicles have been compiled yet. Let the simulation run to generate historical events.");
                        }
                        
                        chronicles.Dispose();
                    }
                    else
                    {
                        allChronicleText.Append("No civilizations found. Please ensure the simulation has started and civilizations have been created.");
                    }
                    
                    civEntities.Dispose();
                    
                    string chronicleText = allChronicleText.ToString();
                    
                    if (!string.IsNullOrEmpty(chronicleText))
                    {
                        _fullChronicleText = StripHtmlTags(chronicleText);
                        _currentChunkIndex = 0;
                        
                        Debug.Log($"[ChronicleReaderUI] Loaded {_fullChronicleText.Length} characters of EPIC chronicles!");
                        
                        if (_backupChronicleText != null)
                        {
                            UpdateSimpleText(_backupChronicleText);
                        }
                    }
                    else
                    {
                        _fullChronicleText = CreateEngagingFallbackText();
                        if (_backupChronicleText != null)
                        {
                            _backupChronicleText.text = _fullChronicleText;
                        }
                    }
                }
                else
                {
                    _fullChronicleText = CreateEngagingFallbackText();
                    if (_backupChronicleText != null)
                    {
                        _backupChronicleText.text = _fullChronicleText;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ChronicleReaderUI] Error loading chronicles: {ex.Message}");
                _fullChronicleText = CreateErrorFallbackText(ex.Message);
                if (_backupChronicleText != null)
                {
                    _backupChronicleText.text = _fullChronicleText;
                }
            }
        }

        // ==== KEYBOARD SHORTCUTS ====
        


        private void CreateFallbackUI()
        {
            Debug.Log("[ChronicleReaderUI] Starting CreateFallbackUI...");
            
            // Find or create canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("[ChronicleReaderUI] No Canvas found, creating new Canvas");
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("[ChronicleReaderUI] Created new Canvas");
            }
            else
            {
                Debug.Log("[ChronicleReaderUI] Found existing Canvas");
            }
            
            // Create the chronicle reader panel
            var panelGO = new GameObject("ChronicleReaderPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            Debug.Log("[ChronicleReaderUI] Created ChronicleReaderPanel");
            
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            var panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            chronicleReaderPanel = panelGO;
            Debug.Log("[ChronicleReaderUI] Set up panel background");
            
            // Create title
            var titleGO = new GameObject("ChronicleTitle");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, 0);
            
            chronicleTitle = titleGO.AddComponent<TextMeshProUGUI>();
            chronicleTitle.text = "Living Chronicle Reader";
            chronicleTitle.fontSize = 18;
            chronicleTitle.color = Color.white;
            chronicleTitle.fontStyle = FontStyles.Bold;
            chronicleTitle.alignment = TextAlignmentOptions.Center;
            Debug.Log("[ChronicleReaderUI] Created title component");
            
            // Create scroll view for main text area
            var scrollViewGO = new GameObject("ChronicleScrollView");
            scrollViewGO.transform.SetParent(panelGO.transform, false);
            
            var scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0.1f);
            scrollViewRect.anchorMax = new Vector2(1, 0.85f);
            scrollViewRect.offsetMin = new Vector2(20, 20);
            scrollViewRect.offsetMax = new Vector2(-20, -20);
            
            var scrollViewImage = scrollViewGO.AddComponent<UnityEngine.UI.Image>();
            scrollViewImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            
            chronicleScrollRect = scrollViewGO.AddComponent<ScrollRect>();
            chronicleScrollRect.horizontal = false;
            chronicleScrollRect.vertical = true;
            chronicleScrollRect.movementType = ScrollRect.MovementType.Clamped;
            chronicleScrollRect.scrollSensitivity = 20f;
            Debug.Log("[ChronicleReaderUI] Created scroll view");
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var viewportMask = viewportGO.AddComponent<UnityEngine.UI.Mask>();
            viewportMask.showMaskGraphic = false;
            
            var viewportImage = viewportGO.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = Color.clear;
            
            chronicleScrollRect.viewport = viewportRect;
            Debug.Log("[ChronicleReaderUI] Created viewport");
            
            // Create content area
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 1000); // Start with some height
            
            chronicleScrollRect.content = contentRect;
            Debug.Log("[ChronicleReaderUI] Created content area");
            
            // Create the actual text component
            var textGO = new GameObject("ChronicleText");
            textGO.transform.SetParent(contentGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            chronicleText = textGO.AddComponent<TextMeshProUGUI>();
            chronicleText.text = GetInitialDisplayText();
            chronicleText.fontSize = 14;
            chronicleText.color = Color.white;
            chronicleText.textWrappingMode = TextWrappingModes.Normal;
            chronicleText.overflowMode = TextOverflowModes.Overflow;
            Debug.Log("[ChronicleReaderUI] Created text component with initial text");
            
            // Add ContentSizeFitter to auto-resize content
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Debug.Log("[ChronicleReaderUI] Added ContentSizeFitter");
            
            // Create close button
            var closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            var closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.8f, 0.02f);
            closeButtonRect.anchorMax = new Vector2(0.98f, 0.08f);
            closeButtonRect.offsetMin = Vector2.zero;
            closeButtonRect.offsetMax = Vector2.zero;
            
            var closeButtonImage = closeButtonGO.AddComponent<UnityEngine.UI.Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            
            var closeButton = closeButtonGO.AddComponent<Button>();
            closeButton.onClick.AddListener(() => {
                if (chronicleReaderPanel != null)
                    chronicleReaderPanel.SetActive(false);
            });
            
            var closeButtonTextGO = new GameObject("Text");
            closeButtonTextGO.transform.SetParent(closeButtonGO.transform, false);
            var closeButtonTextRect = closeButtonTextGO.AddComponent<RectTransform>();
            closeButtonTextRect.anchorMin = Vector2.zero;
            closeButtonTextRect.anchorMax = Vector2.one;
            closeButtonTextRect.offsetMin = Vector2.zero;
            closeButtonTextRect.offsetMax = Vector2.zero;
            
            var closeButtonText = closeButtonTextGO.AddComponent<TextMeshProUGUI>();
            closeButtonText.text = "Close";
            closeButtonText.fontSize = 12;
            closeButtonText.color = Color.white;
            closeButtonText.alignment = TextAlignmentOptions.Center;
            Debug.Log("[ChronicleReaderUI] Created close button");
            
            // Create refresh button
            var refreshButtonGO = new GameObject("RefreshButton");
            refreshButtonGO.transform.SetParent(panelGO.transform, false);
            var refreshButtonRect = refreshButtonGO.AddComponent<RectTransform>();
            refreshButtonRect.anchorMin = new Vector2(0.02f, 0.02f);
            refreshButtonRect.anchorMax = new Vector2(0.2f, 0.08f);
            refreshButtonRect.offsetMin = Vector2.zero;
            refreshButtonRect.offsetMax = Vector2.zero;
            
            var refreshButtonImage = refreshButtonGO.AddComponent<UnityEngine.UI.Image>();
            refreshButtonImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            
            var refreshButton = refreshButtonGO.AddComponent<Button>();
            refreshButton.onClick.AddListener(() => {
                Debug.Log("[ChronicleReaderUI] Refresh button clicked");
                ShowRealChronicles();
            });
            
            var refreshButtonTextGO = new GameObject("Text");
            refreshButtonTextGO.transform.SetParent(refreshButtonGO.transform, false);
            var refreshButtonTextRect = refreshButtonTextGO.AddComponent<RectTransform>();
            refreshButtonTextRect.anchorMin = Vector2.zero;
            refreshButtonTextRect.anchorMax = Vector2.one;
            refreshButtonTextRect.offsetMin = Vector2.zero;
            refreshButtonTextRect.offsetMax = Vector2.zero;
            
            var refreshButtonText = refreshButtonTextGO.AddComponent<TextMeshProUGUI>();
            refreshButtonText.text = "Load Chronicles";
            refreshButtonText.fontSize = 10;
            refreshButtonText.color = Color.white;
            refreshButtonText.alignment = TextAlignmentOptions.Center;
            Debug.Log("[ChronicleReaderUI] Created refresh button");
            
            // Create fix button
            var fixButtonGO = new GameObject("FixButton");
            fixButtonGO.transform.SetParent(panelGO.transform, false);
            var fixButtonRect = fixButtonGO.AddComponent<RectTransform>();
            fixButtonRect.anchorMin = new Vector2(0.22f, 0.02f);
            fixButtonRect.anchorMax = new Vector2(0.4f, 0.08f);
            fixButtonRect.offsetMin = Vector2.zero;
            fixButtonRect.offsetMax = Vector2.zero;
            
            var fixButtonImage = fixButtonGO.AddComponent<UnityEngine.UI.Image>();
            fixButtonImage.color = new Color(0.8f, 0.8f, 0.2f, 0.8f);
            
            var fixButton = fixButtonGO.AddComponent<Button>();
            fixButton.onClick.AddListener(() => {
                Debug.Log("[ChronicleReaderUI] Fix button clicked");
                FixMissingAdaptivePersonalityData();
            });
            
            var fixButtonTextGO = new GameObject("Text");
            fixButtonTextGO.transform.SetParent(fixButtonGO.transform, false);
            var fixButtonTextRect = fixButtonTextGO.AddComponent<RectTransform>();
            fixButtonTextRect.anchorMin = Vector2.zero;
            fixButtonTextRect.anchorMax = Vector2.one;
            fixButtonTextRect.offsetMin = Vector2.zero;
            fixButtonTextRect.offsetMax = Vector2.zero;
            
            var fixButtonText = fixButtonTextGO.AddComponent<TextMeshProUGUI>();
            fixButtonText.text = "Fix Components";
            fixButtonText.fontSize = 10;
            fixButtonText.color = Color.white;
            fixButtonText.alignment = TextAlignmentOptions.Center;
            Debug.Log("[ChronicleReaderUI] Created fix button");
            
            // IMPORTANT: Activate the panel immediately for testing
            chronicleReaderPanel.SetActive(true);
            Debug.Log("[ChronicleReaderUI] Activated fallback UI panel");
            
            // Set initial test text
            if (chronicleText != null)
            {
                chronicleText.text = "FALLBACK UI CREATED SUCCESSFULLY!\n\n" + GetInitialDisplayText();
                Debug.Log("[ChronicleReaderUI] Set initial text on fallback UI");
            }
            
            Debug.Log("[ChronicleReaderUI] Created enhanced fallback UI with buttons - UI should now be visible!");
        }
        
        private string GetInitialDisplayText()
        {
            var text = new System.Text.StringBuilder();
            text.AppendLine("==========================================");
            text.AppendLine("           CHRONICLE READER");
            text.AppendLine("==========================================");
            text.AppendLine();
            text.AppendLine("CONTROLS:");
            text.AppendLine("- Press F6 to toggle this panel");
            text.AppendLine("- Use arrow keys to navigate pages");
            text.AppendLine("- Press A for analysis view");
            text.AppendLine("- Press ESC to close");
            text.AppendLine("- Click 'Load Chronicles' button to refresh");
            text.AppendLine();
            text.AppendLine("SYSTEM STATUS:");
            
            if (_chronicleSystem != null)
            {
                text.AppendLine("+ Living Chronicle System: ACTIVE");
                text.AppendLine("+ Advanced storytelling enabled");
            }
            else
            {
                text.AppendLine("- Living Chronicle System: LOADING...");
                text.AppendLine("- Waiting for system initialization");
            }
            
            text.AppendLine();
            text.AppendLine("INSTRUCTIONS:");
            text.AppendLine("1. Let the simulation run to generate events");
            text.AppendLine("2. Chronicles are compiled automatically");
            text.AppendLine("3. Click 'Load Chronicles' to view real data");
            text.AppendLine("4. Use the buttons to navigate content");
            text.AppendLine();
            text.AppendLine("NOTE: The Living Chronicle System transforms");
            text.AppendLine("raw simulation events into epic, readable");
            text.AppendLine("stories with rich context and narrative!");
            text.AppendLine();
            text.AppendLine("Click 'Load Chronicles' below to check for");
            text.AppendLine("new chronicle entries from your simulation.");
            
            return text.ToString();
        }

        private void ShowSampleChronicle()
        {
            if (chronicleTitle != null)
                chronicleTitle.text = "Sample Chronicle - The Dawn of Civilizations";
            if (chronicleSubtitle != null)
                chronicleSubtitle.text = "A Living Chronicle Generated from Simulation Events";
            
            var sampleText = new System.Text.StringBuilder();
            sampleText.AppendLine("═══════════════════════════════════════");
            sampleText.AppendLine("           CHAPTER I: FIRST LIGHT");
            sampleText.AppendLine("═══════════════════════════════════════");
            sampleText.AppendLine();
            sampleText.AppendLine("In the year 1, when the world was young and untamed,");
            sampleText.AppendLine("the first sparks of civilization began to flicker across");
            sampleText.AppendLine("the vast wilderness. The Living Chronicle System");
            sampleText.AppendLine("watches and records, transforming raw events into");
            sampleText.AppendLine("epic narratives that capture the essence of history.");
            sampleText.AppendLine();
            sampleText.AppendLine("───────────────────────────────────────");
            sampleText.AppendLine("              THE GREAT AWAKENING");
            sampleText.AppendLine("───────────────────────────────────────");
            sampleText.AppendLine();
            sampleText.AppendLine("As civilizations spawn and grow, their stories will");
            sampleText.AppendLine("be woven into rich tapestries of interconnected");
            sampleText.AppendLine("narratives. Wars will rage, heroes will rise,");
            sampleText.AppendLine("empires will fall, and through it all, the");
            sampleText.AppendLine("Chronicle System will capture every moment of");
            sampleText.AppendLine("drama, triumph, and tragedy.");
            sampleText.AppendLine();
            sampleText.AppendLine("FEATURES OF THE LIVING CHRONICLE:");
            sampleText.AppendLine("- Rich contextual storytelling");
            sampleText.AppendLine("- Dramatic narrative arcs");
            sampleText.AppendLine("- Character development tracking");
            sampleText.AppendLine("- Causal relationship analysis");
            sampleText.AppendLine("- Thematic pattern recognition");
            sampleText.AppendLine("- Adaptive folklore generation");
            sampleText.AppendLine();
            sampleText.AppendLine("Let the simulation run and watch as your");
            sampleText.AppendLine("world's history unfolds in epic chronicles!");
            
            PrepareChroniclePages(sampleText.ToString());
            _currentPage = 0;
            DisplayCurrentPage();
            
            Debug.Log("[ChronicleReaderUI] Showing sample chronicle content");
        }

        private void ShowRealChronicles()
        {
            Debug.Log("[ChronicleReaderUI] *** LOAD CHRONICLES BUTTON CLICKED ***");
            Debug.Log("[ChronicleReaderUI] Loading real chronicles...");
            
            if (_chronicleSystem == null)
            {
                Debug.LogError("[ChronicleReaderUI] Chronicle system is null!");
                if (HasTextComponent())
                {
                    SetChronicleText("ERROR: Chronicle System not found!\n\nThe Living Chronicle System is not running. Make sure the simulation is active.");
                }
                return;
            }
            
            // Generate a proper chronicle directly from the LivingChronicleSystem
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.EntityManager == null)
            {
                Debug.LogError("[ChronicleReaderUI] ECS World not found!");
                if (HasTextComponent())
                {
                    SetChronicleText("ERROR: ECS World not found!\n\nThe Entity Component System is not active.");
                }
                return;
            }
            
            var entityManager = world.EntityManager;
            
            try
            {
                // Find the first active civilization
                var civilizationQuery = entityManager.CreateEntityQuery(typeof(CivilizationData));
                var civilizationEntities = civilizationQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                Debug.Log($"[ChronicleReaderUI] Found {civilizationEntities.Length} civilizations");
                
                if (civilizationEntities.Length == 0)
                {
                    Debug.LogWarning("[ChronicleReaderUI] No civilizations found");
                    if (HasTextComponent())
                    {
                        SetChronicleText("No active civilizations found.\n\nWait for civilizations to spawn and create history, then try again.");
                    }
                    civilizationEntities.Dispose();
                    civilizationQuery.Dispose();
                    return;
                }
                
                // Generate chronicles for ALL civilizations, not just the first one
                var allChronicleTexts = new List<string>();
                var civilizationData = civilizationQuery.ToComponentDataArray<CivilizationData>(Unity.Collections.Allocator.Temp);
                
                Debug.Log($"[ChronicleReaderUI] Generating chronicles for ALL {civilizationEntities.Length} civilizations");
                
                for (int i = 0; i < civilizationEntities.Length; i++)
                {
                    var selectedCivilization = civilizationEntities[i];
                    var civData = civilizationData[i];
                    
                    Debug.Log($"[ChronicleReaderUI] Processing civilization {i+1}/{civilizationEntities.Length}: {civData.Name}");
                
                    // Try to compile chronicles for this civilization
                    var compiledChronicles = _chronicleSystem.CompileChroniclesForCivilization(selectedCivilization, Unity.Collections.Allocator.Temp);
                    
                    string chronicleText = "";
                    
                    if (compiledChronicles.Length > 0)
                    {
                        // Use the first compiled chronicle
                        var chronicle = compiledChronicles[0];
                        chronicleText = chronicle.ChronicleText.ToString();
                        
                        if (string.IsNullOrEmpty(chronicleText) || chronicleText.Contains("chronicles remain unwritten"))
                        {
                            chronicleText = GenerateChronicleForCivilization(selectedCivilization, civData, world);
                        }
                    }
                    else
                    {
                        // No compiled chronicles available, generate one on the fly
                        chronicleText = GenerateChronicleForCivilization(selectedCivilization, civData, world);
                    }
                    
                    if (!string.IsNullOrEmpty(chronicleText))
                    {
                        allChronicleTexts.Add(chronicleText);
                        Debug.Log($"[ChronicleReaderUI] Added chronicle for {civData.Name} ({chronicleText.Length} characters)");
                    }
                    
                    compiledChronicles.Dispose();
                }
                
                // Combine all chronicles
                string combinedChronicles = "";
                if (allChronicleTexts.Count > 0)
                {
                    combinedChronicles = string.Join("\n\n" + new string('═', 80) + "\n\n", allChronicleTexts);
                    Debug.Log($"[ChronicleReaderUI] Combined {allChronicleTexts.Count} chronicles into {combinedChronicles.Length} characters");
                }
                else
                {
                    combinedChronicles = "No chronicles could be generated.\n\nWait for civilizations to create history, then try again.";
                }
                
                // Handle large text - REMOVED TRUNCATION - let the pagination system handle it
                // No more artificial limits! Let the full chronicles show through pagination
                
                Debug.Log($"[ChronicleReaderUI] Final combined chronicles length: {combinedChronicles.Length}");
                Debug.Log($"[ChronicleReaderUI] First 200 chars: {combinedChronicles.Substring(0, Math.Min(200, combinedChronicles.Length))}");
                
                var formattedText = FormatChronicleText(combinedChronicles);
                Debug.Log($"[ChronicleReaderUI] Formatted text length: {formattedText.Length}");
                Debug.Log($"[ChronicleReaderUI] Calling SetChronicleText with formatted text...");
                
                SetChronicleText(formattedText);
                
                if (chronicleTitle != null)
                {
                    chronicleTitle.text = $"Chronicles of All Civilizations ({allChronicleTexts.Count} Realms)";
                }
                
                civilizationData.Dispose();
                
                civilizationEntities.Dispose();
                civilizationQuery.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChronicleReaderUI] Error during chronicle query: {e.Message}\n{e.StackTrace}");
                if (HasTextComponent())
                {
                    SetChronicleText($"ERROR: Failed to query chronicles!\n\nException: {e.Message}\n\nCheck console for details.");
                }
            }
        }
        
        private string GenerateChronicleForCivilization(Entity civilization, CivilizationData civData, World world)
        {
            var historySystem = world.GetExistingSystemManaged<WorldHistorySystem>();
            if (historySystem != null)
            {
                var allEvents = historySystem.GetHistoricalEvents(Unity.Collections.Allocator.Temp);
                
                if (allEvents.Length > 0)
                {
                    var chronicleText = GenerateNarrativeChronicle(allEvents, civData);
                    allEvents.Dispose();
                    return chronicleText;
                }
                
                allEvents.Dispose();
            }
            
            return $"The Chronicles of {civData.Name}\n\n" +
                   "In the beginning, there was silence. The world waits for the first chapter of history to be written...\n\n" +
                   "Let the simulation run for a while to generate historical events, then refresh this view.";
        }

        private string GenerateNarrativeChronicle(NativeList<HistoricalEventRecord> events, CivilizationData civData)
        {
            var narrative = new System.Text.StringBuilder();
            
            // Sort events by year
            var eventList = new List<HistoricalEventRecord>();
            for (int i = 0; i < events.Length; i++)
            {
                eventList.Add(events[i]);
            }
            eventList.Sort((a, b) => a.Year.CompareTo(b.Year));
            
            // Chronicle opening
            narrative.AppendLine($"THE CHRONICLES OF {civData.Name.ToString().ToUpper()}");
            narrative.AppendLine("═══════════════════════════════════════════════════════════");
            narrative.AppendLine();
            narrative.AppendLine("Here are recorded the deeds and destinies of a great civilization,");
            narrative.AppendLine("their triumphs and tragedies, their rise to glory and the trials");
            narrative.AppendLine("that shaped their eternal legacy...");
            narrative.AppendLine();
            
            // Group events into chapters
            var chapters = OrganizeEventsIntoChapters(eventList);
            
            for (int chapterIndex = 0; chapterIndex < chapters.Count; chapterIndex++)
            {
                var chapter = chapters[chapterIndex];
                
                narrative.AppendLine($"CHAPTER {chapterIndex + 1}: {chapter.Title}");
                narrative.AppendLine(new string('─', 50));
                narrative.AppendLine();
                
                // Handle mythological chapters (no events - pure lore)
                if (chapter.Events.Count == 0)
                {
                    var mythologyNarrative = GenerateMythologicalNarrative(civData.Name.ToString());
                    narrative.AppendLine(mythologyNarrative);
                    narrative.AppendLine();
                }
                else
                {
                    // Generate causal connection narrative that shows how events relate to each other
                    string chapterNarrative = GenerateCausalConnectionNarrative(chapter.Events, civData.Name.ToString());
                    narrative.Append(chapterNarrative);
                }
                
                // Add chapter conclusion
                if (chapterIndex < chapters.Count - 1)
                {
                    narrative.AppendLine("Thus ended this chapter of their story, but greater deeds were yet to come...");
                }
                else
                {
                    narrative.AppendLine("And so the chronicles record these deeds for all time, that future generations might remember the glory and wisdom of those who came before.");
                }
                
                narrative.AppendLine();
                narrative.AppendLine();
            }
            
            return narrative.ToString();
        }
        
        private List<ChronicleChapter> OrganizeEventsIntoChapters(List<HistoricalEventRecord> events)
        {
            var chapters = new List<ChronicleChapter>();
            if (events.Count == 0) return chapters;
            
            // FILTER OUT BORING EVENTS - Only keep lore-worthy, epic events!
            var filteredEvents = FilterForLoreWorthyEvents(events);
            
            if (filteredEvents.Count == 0)
            {
                // If no lore-worthy events, create mythological backstory
                return GenerateMythologicalChapters(events[0].Year);
            }
            
            // Group events by time periods (every 20-30 years or by significance)
            var currentChapter = new ChronicleChapter();
            int chapterStartYear = filteredEvents[0].Year;
            var currentEvents = new List<HistoricalEventRecord>();
            
            for (int i = 0; i < filteredEvents.Count; i++)
            {
                var evt = filteredEvents[i];
                
                // Start a new chapter if enough time has passed or we have enough events
                bool shouldStartNewChapter = (evt.Year - chapterStartYear > 25) || 
                                           (currentEvents.Count >= 6) ||  // Fewer events per chapter for more focused stories
                                           (evt.Significance >= 4.0f && currentEvents.Count > 0);
                
                if (shouldStartNewChapter && currentEvents.Count > 0)
                {
                    currentChapter.Events = currentEvents;
                    currentChapter.Title = GenerateChapterTitle(currentEvents, chapterStartYear, currentEvents[currentEvents.Count - 1].Year);
                    chapters.Add(currentChapter);
                    
                    // Start new chapter
                    currentChapter = new ChronicleChapter();
                    currentEvents = new List<HistoricalEventRecord>();
                    chapterStartYear = evt.Year;
                }
                
                currentEvents.Add(evt);
            }
            
            // Add the final chapter
            if (currentEvents.Count > 0)
            {
                currentChapter.Events = currentEvents;
                currentChapter.Title = GenerateChapterTitle(currentEvents, chapterStartYear, currentEvents[currentEvents.Count - 1].Year);
                chapters.Add(currentChapter);
            }
            
            return chapters;
        }
        
        private List<HistoricalEventRecord> FilterForLoreWorthyEvents(List<HistoricalEventRecord> events)
        {
            var loreWorthyEvents = new List<HistoricalEventRecord>();
            
            foreach (var evt in events)
            {
                // Only include events that are actually interesting for lore!
                if (IsLoreWorthy(evt))
                {
                    loreWorthyEvents.Add(evt);
                }
            }
            
            Debug.Log($"[ChronicleReaderUI] Filtered {events.Count} events down to {loreWorthyEvents.Count} lore-worthy events");
            return loreWorthyEvents;
        }
        
        private bool IsLoreWorthy(HistoricalEventRecord evt)
        {
            // EPIC EVENTS - Always include these amazing event categories your systems generate!
            
            // HEROES & LEGENDS - Your heroic leader system generates these!
            if (evt.Category == EventCategory.Hero) return true;
            
            // COALITION WARS - Your massive alliance vs empire conflicts!
            if (evt.Category == EventCategory.Coalition) return true;
            
            // HOLY WARS - Your religious warfare system!
            if (evt.Category == EventCategory.HolyWar) return true;
            
            // BETRAYALS - Your epic treachery events!
            if (evt.Category == EventCategory.Betrayal) return true;
            
            // CIVILIZATION COLLAPSES - Your dramatic fall events!
            if (evt.Category == EventCategory.Collapse) return true;
            
            // GOLDEN AGES - Your prosperity periods!
            if (evt.Category == EventCategory.Golden) return true;
            
            // DISASTERS & CATASTROPHES - Your world-shaking events!
            if (evt.Category == EventCategory.Disaster) return true;
            
            // REVOLUTIONS & CIVIL WARS - Your internal conflicts!
            if (evt.Category == EventCategory.Revolution) return true;
            
            // CASCADING EVENTS - Your chain reaction collapses!
            if (evt.Category == EventCategory.Cascade) return true;
            
            // SPIRITUAL AWAKENINGS - Your religious transformation events!
            if (evt.Category == EventCategory.Spiritual) return true;
            
            // MAJOR DISCOVERIES - Your technological breakthroughs!
            if (evt.Category == EventCategory.Discovery) return true;
            
            // ESCALATING CONFLICTS - Your war cascade events!
            if (evt.Category == EventCategory.Escalation) return true;
            
            // High significance events are always lore-worthy
            if (evt.Significance >= 3.0f) return true;
            
            // Religious events are ALWAYS lore-worthy (your mythology system!)
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Religious) return true;
            
            // Major military conflicts are lore-worthy
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Military && 
                evt.Category == EventCategory.Conflict && evt.Significance >= 2.0f) return true;
            
            // Cultural events with decent significance are lore-worthy (your folklore system!)
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Cultural && evt.Significance >= 1.5f) return true;
            
            // Social events with significance are lore-worthy
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Social && evt.Significance >= 2.0f) return true;
            
            // Political upheavals with significance are lore-worthy
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Political && evt.Significance >= 2.0f) return true;
            
            // Major diplomatic events are lore-worthy
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Diplomatic && evt.Significance >= 2.0f) return true;
            
            // Technological breakthroughs are lore-worthy
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Technological && evt.Significance >= 2.0f) return true;
            
            return false; // Default: not lore-worthy
        }
        
        private List<ChronicleChapter> GenerateMythologicalChapters(int startYear)
        {
            // If no real events are lore-worthy, create mythological/legendary backstory
            var chapters = new List<ChronicleChapter>();
            
            var mythChapter = new ChronicleChapter
            {
                Title = "The Age of Legends",
                Events = new List<HistoricalEventRecord>() // Empty - we'll generate pure mythology
            };
            
            chapters.Add(mythChapter);
            return chapters;
        }
        
        private string GenerateMythologicalNarrative(string civName)
        {
            // Generate pure mythology when no historical events are lore-worthy
            var random = new System.Random(civName.GetHashCode());
            
            var mythologies = new string[]
            {
                $"In the time before memory, when the world was young and magic flowed like rivers through the land, the ancestors of {civName} were chosen by the Star-Touched Gods to be guardians of ancient secrets. They built their first cities from crystallized starlight and learned to speak the True Language that could command the elements themselves.",
                
                $"Legend speaks of the Founding Prophecy of {civName}, inscribed on tablets of living stone by beings of pure light. It foretold that this people would become the bridge between the mortal realm and the divine planes, destined to face trials that would forge them into something beyond ordinary mortals.",
                
                $"The First Kings of {civName} were said to be born from the union of celestial beings and mortal heroes, their bloodline carrying the power to reshape reality with their will alone. They ruled from floating palaces that moved with the constellations, and their voices could heal the wounded earth or call down storms of liquid starfire.",
                
                $"Ancient texts tell of the Great Awakening, when the sleeping dragons beneath {civName} stirred and offered their wisdom to the people above. In exchange for protection of the sacred groves, these wyrms taught the mortals the arts of prophecy, alchemy, and the crafting of weapons that could cut through the fabric of time itself.",
                
                $"The mythology of {civName} speaks of the Eternal Library, a repository of all knowledge that exists simultaneously in every moment of time. Their greatest scholars were said to walk its infinite halls, learning the secrets of creation and destruction, returning to the mortal world with eyes that burned with the fire of pure understanding.",
                
                $"In the age of wonders, the people of {civName} discovered the Singing Stones - crystalline formations that resonated with the music of the spheres. Those who learned to harmonize with these stones gained the ability to heal any wound, see across vast distances, and commune with the spirits of their ancestors who dwelt in the realm between stars.",
                
                $"The creation myth of {civName} tells of the Weaver of Fates, a cosmic entity who spun their destiny from threads of liquid moonlight and crystallized dreams. Each thread represented a potential future, and the wisest among them learned to read these patterns, becoming oracles whose prophecies shaped the course of history itself."
            };
            
            return mythologies[random.Next(mythologies.Length)];
        }
        
        private string GenerateChapterTitle(List<HistoricalEventRecord> events, int startYear, int endYear)
        {
            if (events.Count == 0) return "The Silent Years";
            
            // Find the most significant event type in this chapter
            var eventTypes = new Dictionary<ProceduralWorld.Simulation.Core.EventType, int>();
            var eventCategories = new Dictionary<EventCategory, int>();
            float maxSignificance = 0f;
            
            foreach (var evt in events)
            {
                if (!eventTypes.ContainsKey(evt.Type)) eventTypes[evt.Type] = 0;
                eventTypes[evt.Type]++;
                
                if (!eventCategories.ContainsKey(evt.Category)) eventCategories[evt.Category] = 0;
                eventCategories[evt.Category]++;
                
                if (evt.Significance > maxSignificance)
                {
                    maxSignificance = evt.Significance;
                }
            }
            
            // Generate title based on dominant themes
            var dominantType = eventTypes.OrderByDescending(kvp => kvp.Value).First().Key;
            var dominantCategory = eventCategories.OrderByDescending(kvp => kvp.Value).First().Key;
            
            // Generate varied, dramatic chapter titles
            var random = new System.Random(startYear + endYear);
            
            string title = dominantType switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military when dominantCategory == EventCategory.Conflict => 
                    GetRandomTitle(random, new[] { "The Blood-Soaked Conquest", "The War of Shattered Crowns", "The Great Devastation", "The Crimson Campaign", "The Iron Storm" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Military when dominantCategory == EventCategory.Coalition => 
                    GetRandomTitle(random, new[] { "The Sacred Alliance", "The Brotherhood of Steel", "The Unbreakable Pact", "The Coalition of Destiny", "The Unity Forged" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Cultural => 
                    GetRandomTitle(random, new[] { "The Golden Renaissance", "The Age of Wonders", "The Flowering of Genius", "The Cultural Revolution", "The Dawn of Enlightenment" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Religious => 
                    GetRandomTitle(random, new[] { "The Divine Revelation", "The Sacred Reformation", "The Age of Prophets", "The Spiritual Awakening", "The Holy Transformation" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Economic => 
                    GetRandomTitle(random, new[] { "The Age of Gold", "The Merchant Kings", "The Great Prosperity", "The Golden Current", "The Wealth of Nations" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => 
                    GetRandomTitle(random, new[] { "The Game of Thrones", "The Web of Intrigue", "The Master's Gambit", "The Silent War", "The Dance of Diplomats" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Social when dominantCategory == EventCategory.Collapse => 
                    GetRandomTitle(random, new[] { "The Great Collapse", "The Shattered Foundation", "The Time of Reckoning", "The Fall from Grace", "The Dying of the Light" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Social => 
                    GetRandomTitle(random, new[] { "The Great Uprising", "The Social Revolution", "The Transformation", "The New Order", "The People's Awakening" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Political => 
                    GetRandomTitle(random, new[] { "The Throne Wars", "The Game of Crowns", "The Struggle for Dominion", "The Political Earthquake", "The Power Shift" }),
                    
                _ => GetRandomTitle(random, new[] { "The Turning Point", "The Winds of Change", "The Crossroads of Fate", "The Defining Moment", "The Age of Legends" })
            };
            
            // Add year range
            if (startYear == endYear)
                title += $" (Year {startYear})";
            else
                title += $" (Years {startYear}-{endYear})";
            
            return title;
        }
        
        private string GetRandomTitle(System.Random random, string[] titles)
        {
            return titles[random.Next(titles.Length)];
        }
        
        private string GenerateEventNarrative(HistoricalEventRecord evt, bool isChapterOpening, string civName)
        {
            var narrative = new System.Text.StringBuilder();
            
            // Opening phrase
            if (isChapterOpening)
            {
                narrative.Append($"In the year {evt.Year}, ");
            }
            else
            {
                var timeTransitions = new[] { 
                    "Soon after, ", "In time, ", "As the seasons passed, ", 
                    "Not long thereafter, ", "In those days, ", "Meanwhile, " 
                };
                var random = new System.Random();
                narrative.Append(timeTransitions[random.Next(0, timeTransitions.Length)]);
            }
            
            // Generate narrative based on event type and category
            string eventNarrative = GenerateEventSpecificNarrative(evt, civName);
            narrative.Append(eventNarrative);
            
            // Add consequence or impact if significant
            if (evt.Significance > 2.0f)
            {
                narrative.Append(" ");
                narrative.Append(GenerateEventConsequence(evt, civName));
            }
            
            return narrative.ToString();
        }
        
        private string GenerateCausalConnectionNarrative(List<HistoricalEventRecord> events, string civName)
        {
            var narrative = new System.Text.StringBuilder();
            
            for (int i = 0; i < events.Count; i++)
            {
                var currentEvent = events[i];
                bool isChapterOpening = i == 0;
                
                // Generate the main event narrative
                string eventText = GenerateEventNarrative(currentEvent, isChapterOpening, civName);
                narrative.AppendLine(eventText);
                
                // Look for causal connections to subsequent events
                if (i < events.Count - 1)
                {
                    var nextEvent = events[i + 1];
                    string causalLink = GenerateCausalLink(currentEvent, nextEvent, civName);
                    if (!string.IsNullOrEmpty(causalLink))
                    {
                        narrative.AppendLine();
                        narrative.AppendLine(causalLink);
                    }
                }
                
                narrative.AppendLine();
            }
            
            return narrative.ToString();
        }
        
        private string GenerateCausalLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName)
        {
            // Generate connections based on event types and timing
            int yearDifference = toEvent.Year - fromEvent.Year;
            
            // Only create causal links for events within reasonable time frames
            if (yearDifference > 20) return "";
            
            var linkType = DetermineCausalLinkType(fromEvent, toEvent);
            
            return linkType switch
            {
                CausalLinkType.DirectConsequence => GenerateDirectConsequenceLink(fromEvent, toEvent, civName, yearDifference),
                CausalLinkType.ResourceEffect => GenerateResourceEffectLink(fromEvent, toEvent, civName, yearDifference),
                CausalLinkType.PoliticalRipple => GeneratePoliticalRippleLink(fromEvent, toEvent, civName, yearDifference),
                CausalLinkType.CulturalInfluence => GenerateCulturalInfluenceLink(fromEvent, toEvent, civName, yearDifference),
                CausalLinkType.MilitaryEscalation => GenerateMilitaryEscalationLink(fromEvent, toEvent, civName, yearDifference),
                _ => ""
            };
        }
        
        private enum CausalLinkType
        {
            None,
            DirectConsequence,
            ResourceEffect,
            PoliticalRipple,
            CulturalInfluence,
            MilitaryEscalation
        }
        
        private CausalLinkType DetermineCausalLinkType(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent)
        {
            // Determine relationship between events based on their types and categories
            if (fromEvent.Type == ProceduralWorld.Simulation.Core.EventType.Military && 
                toEvent.Type == ProceduralWorld.Simulation.Core.EventType.Military)
                return CausalLinkType.MilitaryEscalation;
                
            if (fromEvent.Type == ProceduralWorld.Simulation.Core.EventType.Economic && 
                (toEvent.Type == ProceduralWorld.Simulation.Core.EventType.Military || 
                 toEvent.Type == ProceduralWorld.Simulation.Core.EventType.Diplomatic))
                return CausalLinkType.ResourceEffect;
                
            if (fromEvent.Type == ProceduralWorld.Simulation.Core.EventType.Diplomatic && 
                toEvent.Type == ProceduralWorld.Simulation.Core.EventType.Military)
                return CausalLinkType.PoliticalRipple;
                
            if (fromEvent.Type == ProceduralWorld.Simulation.Core.EventType.Cultural && 
                toEvent.Type == ProceduralWorld.Simulation.Core.EventType.Social)
                return CausalLinkType.CulturalInfluence;
                
            if (fromEvent.Significance > 3.0f && toEvent.Year - fromEvent.Year <= 5)
                return CausalLinkType.DirectConsequence;
                
            return CausalLinkType.None;
        }
        
        private string GenerateDirectConsequenceLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName, int yearDifference)
        {
            var links = new string[]
            {
                $"The reverberations from these events continued to shape {civName}'s destiny. Within {yearDifference} years, the consequences would become clear.",
                $"This pivotal moment set in motion a chain of events that would unfold over the next {yearDifference} years, fundamentally altering the course of {civName}.",
                $"The decisions made during this time created ripple effects that reached every corner of {civName}, culminating {yearDifference} years later in further developments."
            };
            var random = new System.Random((int)(fromEvent.Year + toEvent.Year));
            return links[random.Next(links.Length)];
        }
        
        private string GenerateResourceEffectLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName, int yearDifference)
        {
            var links = new string[]
            {
                $"The economic shifts from this period strained {civName}'s resources and treasury. {yearDifference} years later, these pressures would force difficult choices about military and diplomatic priorities.",
                $"Resource allocation changes following these events affected {civName}'s ability to maintain its previous policies. The economic consequences became apparent {yearDifference} years later.",
                $"Trade disruptions and resource shortages created by these developments would influence {civName}'s strategic decisions for {yearDifference} years, ultimately leading to new approaches."
            };
            var random = new System.Random((int)(fromEvent.Year + toEvent.Year));
            return links[random.Next(links.Length)];
        }
        
        private string GeneratePoliticalRippleLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName, int yearDifference)
        {
            var links = new string[]
            {
                $"The diplomatic ramifications of these negotiations created new tensions and opportunities. {yearDifference} years later, these political changes would manifest in more dramatic ways.",
                $"Alliance structures and international relationships shifted as a result of these diplomatic efforts. The full impact would become clear {yearDifference} years later.",
                $"Trust and reputation effects from these diplomatic developments influenced {civName}'s relationships with neighbors, leading to significant consequences {yearDifference} years later."
            };
            var random = new System.Random((int)(fromEvent.Year + toEvent.Year));
            return links[random.Next(links.Length)];
        }
        
        private string GenerateCulturalInfluenceLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName, int yearDifference)
        {
            var links = new string[]
            {
                $"The cultural transformations of this era gradually reshaped {civName}'s social fabric. {yearDifference} years later, these changes would influence how society responded to new challenges.",
                $"New ideas and artistic movements spread throughout {civName}, changing how people viewed themselves and their place in the world. The social implications became evident {yearDifference} years later.",
                $"Educational and intellectual developments from this period created a new generation of leaders and thinkers in {civName}. Their influence would be felt {yearDifference} years later."
            };
            var random = new System.Random((int)(fromEvent.Year + toEvent.Year));
            return links[random.Next(links.Length)];
        }
        
        private string GenerateMilitaryEscalationLink(HistoricalEventRecord fromEvent, HistoricalEventRecord toEvent, string civName, int yearDifference)
        {
            var links = new string[]
            {
                $"The military actions of this period established new patterns of conflict and defense for {civName}. {yearDifference} years later, these precedents would influence strategic decisions.",
                $"Veterans from these conflicts brought hard-won experience to {civName}'s military leadership. Their influence would shape military doctrine and tactics {yearDifference} years later.",
                $"The strategic lessons learned from these battles became part of {civName}'s military tradition. {yearDifference} years later, this knowledge would prove crucial in new conflicts."
            };
            var random = new System.Random((int)(fromEvent.Year + toEvent.Year));
            return links[random.Next(links.Length)];
        }
        
        private string GenerateEventSpecificNarrative(HistoricalEventRecord evt, string civName)
        {
            // Use actual simulation data as foundation, then enhance with epic storytelling
            var baseDescription = evt.Description.ToString();
            var random = new System.Random((int)(evt.Year * 1000 + evt.Significance * 100));
            
            // Handle specific epic event categories your systems generate
            return evt.Category switch
            {
                // EPIC HERO EVENTS - Your heroic leader system
                EventCategory.Hero => 
                    EnhanceHeroicEvent(evt, civName, baseDescription, random),
                    
                // COALITION WARS - Your massive alliance conflicts
                EventCategory.Coalition => 
                    EnhanceCoalitionWarEvent(evt, civName, baseDescription, random),
                    
                // HOLY WARS - Your religious conflicts
                EventCategory.HolyWar => 
                    EnhanceHolyWarEvent(evt, civName, baseDescription, random),
                    
                // BETRAYALS - Your treachery events
                EventCategory.Betrayal => 
                    EnhanceBetrayalEvent(evt, civName, baseDescription, random),
                    
                // CIVILIZATION COLLAPSES - Your dramatic falls
                EventCategory.Collapse => 
                    EnhanceCollapseEvent(evt, civName, baseDescription, random),
                    
                // GOLDEN AGES - Your prosperity periods
                EventCategory.Golden => 
                    EnhanceGoldenAgeEvent(evt, civName, baseDescription, random),
                    
                // DISASTERS - Your catastrophic events
                EventCategory.Disaster => 
                    EnhanceDisasterEvent(evt, civName, baseDescription, random),
                    
                // REVOLUTIONS - Your civil wars and upheavals
                EventCategory.Revolution => 
                    EnhanceRevolutionEvent(evt, civName, baseDescription, random),
                    
                // CASCADING EVENTS - Your chain reaction collapses
                EventCategory.Cascade => 
                    EnhanceCascadeEvent(evt, civName, baseDescription, random),
                    
                // SPIRITUAL AWAKENINGS - Your religious transformations
                EventCategory.Spiritual => 
                    EnhanceSpiritualEvent(evt, civName, baseDescription, random),
                    
                // MAJOR DISCOVERIES - Your technological breakthroughs
                EventCategory.Discovery => 
                    EnhanceDiscoveryEvent(evt, civName, baseDescription, random),
                    
                // Default handling by event type
                _ => evt.Type switch
                {
                    ProceduralWorld.Simulation.Core.EventType.Military when evt.Category == EventCategory.Conflict => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Cultural => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Religious => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Economic => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Diplomatic => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Social => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Political => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    ProceduralWorld.Simulation.Core.EventType.Technological => 
                        EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random),
                        
                    _ => $"{EnhanceSimulationEventWithEpicStorytelling(evt, civName, baseDescription, random)} The chronicles record that {baseDescription.ToLower()}"
                }
            };
        }
        
        private string EnhanceSimulationEventWithEpicStorytelling(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            // Generate epic context that leads into the actual simulation event
            return evt.Type switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military when evt.Category == EventCategory.Conflict => 
                    GenerateEpicMilitaryContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Military when evt.Category == EventCategory.Coalition => 
                    GenerateEpicAllianceContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Cultural => 
                    GenerateEpicCulturalContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Religious => 
                    GenerateEpicReligiousContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Economic => 
                    GenerateEpicEconomicContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => 
                    GenerateEpicDiplomaticContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Social when evt.Category == EventCategory.Collapse => 
                    GenerateEpicSocialCollapseContext(civName, random, evt.Significance, baseDescription),
                    
                ProceduralWorld.Simulation.Core.EventType.Social => 
                    GenerateEpicSocialContext(civName, random, evt.Significance, baseDescription),
                    
                _ => GenerateEpicGenericContext(civName, random, evt.Significance, baseDescription)
            };
        }
        
        private string GenerateEpicMilitaryContext(string civName, System.Random random, float significance, string actualEvent)
        {
            // Create epic context that leads to the actual military event
            var contexts = new string[]
            {
                $"As tensions reached a breaking point across the borderlands, the war drums of {civName} echoed through valleys and mountains. Ancient prophecies spoke of this moment, when steel would meet steel and the fate of nations would hang in the balance.",
                
                $"The military commanders of {civName} had spent months preparing for this inevitable clash. Supply lines were secured, alliances tested, and weapons blessed by the gods of war. The stage was set for a conflict that would reshape the political landscape.",
                
                $"Diplomatic efforts had failed, and the people of {civName} knew that only through strength of arms could their sovereignty be preserved. Veterans sharpened their blades while young recruits said farewell to their families, knowing that history would remember this moment.",
                
                $"The strategic importance of the contested territories could not be ignored by the military leadership of {civName}. Control of these lands would determine trade routes, resource access, and the balance of power for generations to come.",
                
                $"Intelligence reports had warned the generals of {civName} that their enemies were mobilizing. Swift action was required to protect their people and maintain their position in the complex web of regional politics."
            };
            
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicAllianceContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"The diplomatic halls of {civName} buzzed with activity as envoys from distant lands arrived bearing proposals of mutual cooperation. The balance of power was shifting, and wise leaders knew that isolation meant vulnerability.",
                $"Strategic necessity drove the leaders of {civName} to seek new partnerships. The changing political landscape demanded fresh alliances to secure trade routes and defend against common threats.",
                $"After careful deliberation, the council of {civName} recognized that their goals aligned with those of potential allies. Shared interests in prosperity and security formed the foundation for lasting cooperation.",
                $"The geopolitical situation had evolved to a point where {civName} could no longer stand alone. Pragmatic leadership recognized that strength came through unity with like-minded nations.",
                $"Intelligence networks had revealed opportunities for {civName} to forge beneficial relationships with other powers. The time was right to transform tentative contacts into formal agreements."
            };
            
            return contexts[random.Next(contexts.Length)];
        }
        

        

        
        private string GenerateEconomicNarrative(string civName, System.Random random, float significance)
        {
            var narratives = new string[]
            {
                $"Gold flowed through {civName} like liquid sunlight as merchant princes built trading empires that spanned continents. Caravans laden with exotic treasures became a common sight.",
                $"The discovery of legendary mines in {civName} sparked the greatest gold rush in recorded history. Fortune-seekers and adventurers flooded in from every corner of the world.",
                $"Merchant guilds in {civName} became so powerful they could make or break entire kingdoms with a single trade embargo. Commerce became the new form of warfare.",
                $"Revolutionary banking systems emerged in {civName}, creating financial instruments so sophisticated they seemed like magic to traditional traders. Wealth multiplied exponentially.",
                $"The great marketplace of {civName} became a wonder of the world, where impossible goods from mythical lands changed hands daily. Prosperity reached heights never before imagined."
            };
            
            return narratives[random.Next(narratives.Length)];
        }
        
        private string GenerateDiplomaticNarrative(string civName, System.Random random, float significance)
        {
            var narratives = new string[]
            {
                $"The master diplomats of {civName} orchestrated a game of thrones so complex that historians would spend centuries unraveling its intricacies. Words became weapons sharper than any blade.",
                $"Secret envoys from {civName} moved like shadows through enemy courts, weaving webs of intrigue and manipulation that would topple empires without a single battle being fought.",
                $"The peace summit hosted by {civName} became legendary for the impossible compromises achieved and the ancient feuds finally put to rest. Diplomacy triumphed over warfare.",
                $"Whispered conspiracies in the halls of {civName} led to political earthquakes that reshaped the balance of power across multiple continents. Nothing would ever be the same.",
                $"The great betrayal orchestrated by {civName} became the stuff of legend, as former allies discovered too late that they had been outmaneuvered in a game they didn't even know they were playing."
            };
            
            return narratives[random.Next(narratives.Length)];
        }
        
        private string GenerateSocialCollapseNarrative(string civName, System.Random random, float significance)
        {
            var narratives = new string[]
            {
                $"The very foundations of {civName} cracked and crumbled as ancient social structures collapsed like houses of cards. Chaos reigned supreme as the old order died screaming.",
                $"Revolutionary fervor swept through {civName} like a plague, as the oppressed masses rose up with fire in their hearts and vengeance in their souls. The streets ran red with the blood of change.",
                $"The great plague that struck {civName} was more than disease - it was a social apocalypse that shattered every institution and tradition, leaving only survivors to rebuild from the ashes.",
                $"Natural disasters became the catalyst for the complete transformation of {civName}, as survivors abandoned old ways and forged entirely new forms of society from the ruins.",
                $"The collapse came not with war or famine, but with the slow, inexorable unraveling of everything the people of {civName} had believed in. Trust died, and with it, civilization itself."
            };
            
            return narratives[random.Next(narratives.Length)];
        }
        
        private string GenerateSocialNarrative(string civName, System.Random random, float significance)
        {
            var narratives = new string[]
            {
                $"Great social uprisings transformed {civName} as the common people discovered their collective power. What began as whispers of discontent became roars of revolutionary change.",
                $"The social fabric of {civName} underwent a metamorphosis so complete that visitors from just a generation before would not recognize their homeland. Progress came with a heavy price.",
                $"New forms of governance emerged in {civName} as innovative thinkers challenged millennia of tradition. Democracy, meritocracy, and radical equality became more than just ideas.",
                $"The great migration to {civName} brought together peoples from dozens of different cultures, creating a melting pot of traditions that forged entirely new ways of living.",
                $"Social revolutionaries in {civName} pioneered concepts so advanced they wouldn't be understood by other civilizations for centuries. They became the architects of the future."
            };
            
            return narratives[random.Next(narratives.Length)];
        }
        
        private string GenerateGenericEpicNarrative(string civName, System.Random random, float significance)
        {
            var narratives = new string[]
            {
                $"Legends speak of the time when {civName} stood at the crossroads of destiny, and the choices made in those crucial moments would echo through eternity.",
                $"The chronicles of {civName} record this as the turning point when myth became reality and heroes walked among mortals as equals.",
                $"In the annals of history, the deeds of {civName} during this period would be remembered as the stuff of legend, inspiring countless generations to come.",
                $"The great transformation of {civName} began with events so extraordinary that many believed the gods themselves had intervened in mortal affairs.",
                $"What transpired in {civName} during those fateful days would become the foundation myths upon which entire cultures would build their identities."
            };
            
            return narratives[random.Next(narratives.Length)];
        }
        
        // Add the missing context methods
        private string GenerateEpicCulturalContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"A cultural renaissance was brewing in {civName} as artists, scholars, and craftsmen pushed the boundaries of their disciplines. New ideas flowed like rivers through the streets and marketplaces.",
                $"The intellectual climate of {civName} had reached a tipping point where innovation and creativity flourished. Cultural exchanges with other civilizations brought fresh perspectives and techniques.",
                $"Social movements within {civName} were driving changes in artistic expression and cultural values. The old ways were being questioned and new traditions were taking root.",
                $"Educational institutions in {civName} had begun producing a new generation of thinkers and creators. Knowledge was becoming more accessible to the common people.",
                $"Cultural festivals and gatherings in {civName} had evolved into platforms for sharing revolutionary ideas and artistic innovations that would define the civilization's identity."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicReligiousContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"Religious fervor had been building in {civName} as spiritual leaders proclaimed visions and prophecies that resonated deeply with the faithful. The divine seemed closer than ever before.",
                $"Theological debates in {civName} had reached a crescendo as different interpretations of sacred texts divided communities. The search for spiritual truth intensified.",
                $"Pilgrims from distant lands had begun arriving in {civName}, drawn by reports of miraculous events and holy manifestations. Religious significance was growing.",
                $"The priesthood of {civName} had been experiencing unprecedented unity in their spiritual practices, leading to powerful collective ceremonies that moved the hearts of believers.",
                $"Ancient religious sites in {civName} had become focal points of renewed devotion as archaeological discoveries revealed forgotten aspects of their faith's origins."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicEconomicContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"Economic pressures had been mounting in {civName} as trade routes shifted and resource demands evolved. Merchants and traders adapted to changing market conditions.",
                $"Innovation in commerce and industry was transforming the economic landscape of {civName}. New methods of production and distribution were emerging.",
                $"The merchant guilds of {civName} had been negotiating complex agreements that would reshape regional trade networks. Economic alliances were being forged.",
                $"Resource discoveries and technological advances in {civName} were creating unprecedented opportunities for wealth generation and economic expansion.",
                $"Market fluctuations and trade disruptions had forced the economic leaders of {civName} to develop new strategies for maintaining prosperity and stability."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicDiplomaticContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"The diplomatic corps of {civName} had been engaged in delicate negotiations as regional tensions required careful management of international relationships.",
                $"Intelligence networks had provided {civName} with crucial information about shifting alliances and potential conflicts, informing their diplomatic strategies.",
                $"Cultural exchanges and trade relationships had created opportunities for {civName} to strengthen diplomatic ties with both allies and neutral parties.",
                $"The geopolitical landscape was evolving rapidly, and the diplomatic leadership of {civName} recognized the need for adaptive and forward-thinking foreign policy.",
                $"Previous diplomatic successes had established {civName} as a trusted mediator in regional disputes, creating new opportunities for influence and cooperation."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicSocialCollapseContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"Social tensions had been building in {civName} as traditional structures struggled to adapt to changing circumstances. The old order was showing signs of strain.",
                $"Economic hardships and political uncertainties had created unrest among the population of {civName}. Calls for reform echoed through the streets.",
                $"Natural disasters and external pressures had tested the resilience of {civName}'s social institutions. Communities were forced to adapt or face collapse.",
                $"Generational conflicts and changing values had created deep divisions within {civName}. The social fabric was being stretched to its breaking point.",
                $"Leadership failures and institutional corruption had eroded public trust in {civName}. The people demanded accountability and change."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicSocialContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"Social reform movements in {civName} had gained momentum as citizens organized to address inequality and improve living conditions for all members of society.",
                $"Community leaders in {civName} had been working to strengthen social bonds and create more inclusive institutions that served the needs of diverse populations.",
                $"Educational initiatives and social programs in {civName} were beginning to show positive results in improving quality of life and social cohesion.",
                $"Cultural celebrations and civic ceremonies in {civName} had evolved to better reflect the values and aspirations of the people, strengthening social unity.",
                $"Grassroots organizations in {civName} had emerged to address local challenges and create networks of mutual support that would benefit future generations."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        private string GenerateEpicGenericContext(string civName, System.Random random, float significance, string actualEvent)
        {
            var contexts = new string[]
            {
                $"The winds of change were blowing through {civName} as circumstances aligned to create new possibilities and challenges for the civilization.",
                $"Historical forces had been building momentum in {civName}, setting the stage for developments that would influence the course of their future.",
                $"The leadership of {civName} had been preparing for significant changes as they recognized the need to adapt to evolving circumstances.",
                $"Social, economic, and political factors in {civName} had converged to create a moment of opportunity that would require decisive action.",
                $"The people of {civName} stood at a crossroads where their choices would determine the path forward for their civilization and its legacy."
            };
            return contexts[random.Next(contexts.Length)];
        }
        
        // EPIC EVENT ENHANCEMENT METHODS - For your amazing simulation systems!
        
        private string EnhanceHeroicEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var heroicContexts = new string[]
            {
                $"With {civName} facing military defeats and economic collapse, the people desperately needed strong leadership. From the ranks of the common soldiers emerged a figure who would change everything.",
                $"The old rulers of {civName} had failed spectacularly, leaving the nation vulnerable to enemies and internal strife. It was then that an extraordinary individual stepped forward to seize control.",
                $"Morale in {civName} had reached its lowest point after a series of devastating losses. The population was ready to follow anyone who could promise victory and restore their pride.",
                $"Political chaos gripped {civName} as competing factions tore the nation apart. In this power vacuum, a charismatic leader united the people under a single banner."
            };
            return $"{heroicContexts[random.Next(heroicContexts.Length)]} {baseDescription}";
        }
        
        private string EnhanceCoalitionWarEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var coalitionContexts = new string[]
            {
                $"Intelligence reports revealed that {civName} had grown too powerful for any single nation to challenge alone. Emergency diplomatic meetings were held in secret as former enemies agreed to put aside their differences.",
                $"Trade routes were being strangled and smaller nations faced annexation. Desperate ambassadors arrived with urgent proposals: unite now or fall one by one to {civName}'s expanding empire.",
                $"Military advisors calculated the grim mathematics of war - individually, they would be crushed. Together, they might have a chance. The decision was unanimous: form a grand alliance or face extinction.",
                $"Border skirmishes had escalated into full conquest campaigns. With {civName} claiming territory after territory, neighboring powers realized they were witnessing the birth of a superpower that would dominate them all."
            };
            return $"{coalitionContexts[random.Next(coalitionContexts.Length)]} {baseDescription}";
        }
        
        private string EnhanceHolyWarEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var holyWarContexts = new string[]
            {
                $"Religious fervor had reached a fever pitch as competing interpretations of divine will could no longer coexist peacefully. Sacred texts became battle cries.",
                $"The faithful of {civName} believed they fought not just for territory or resources, but for the very soul of their civilization and the favor of the divine.",
                $"Priests blessed weapons and soldiers while prophets proclaimed that the gods themselves would judge the righteous through trial by combat.",
                $"What began as theological debate had escalated beyond the ability of mortal diplomacy to resolve. Only divine judgment through warfare could determine truth."
            };
            return $"{holyWarContexts[random.Next(holyWarContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceBetrayalEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var betrayalContexts = new string[]
            {
                $"The alliance with {civName} had been profitable for years, with shared trade routes and mutual defense pacts. But behind closed doors, secret negotiations were already underway to divide their wealth.",
                $"Military intelligence revealed that {civName} possessed resources and territories that were simply too valuable to ignore. The temptation proved stronger than any oath of friendship.",
                $"Economic pressures forced a terrible choice: honor the alliance with {civName} and face slow decline, or strike first and seize their assets for survival.",
                $"Diplomatic messages between {civName} and their allies had been intercepted, revealing plans that threatened vital interests. The decision was made to act before they could be betrayed first."
            };
            return $"{betrayalContexts[random.Next(betrayalContexts.Length)]} {baseDescription}";
        }
        
        private string EnhanceCollapseEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var collapseContexts = new string[]
            {
                $"The mighty edifice of {civName} had been built over centuries, but even the grandest civilizations are not immune to the inexorable forces of decline and fall.",
                $"Warning signs had appeared for years, but pride and denial blinded the leaders of {civName} to the approaching catastrophe that would reshape their world.",
                $"Like a great tree that appears strong until the final storm reveals its rotted roots, the foundations of {civName} crumbled beneath pressures too great to bear.",
                $"The end came not with dramatic battles or natural disasters, but with the slow, inevitable unraveling of systems that had once seemed unshakeable."
            };
            return $"{collapseContexts[random.Next(collapseContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceGoldenAgeEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var goldenAgeContexts = new string[]
            {
                $"The stars aligned in perfect harmony as {civName} entered an era of unprecedented prosperity and achievement that would be remembered as their golden age.",
                $"Peace, prosperity, and progress converged in {civName} like three rivers joining to create a mighty flood of cultural and economic advancement.",
                $"Future historians would mark this as the moment when {civName} reached the pinnacle of their civilization, setting standards that others could only dream of achieving.",
                $"The combination of wise leadership, abundant resources, and favorable circumstances created the perfect conditions for {civName} to flourish beyond all expectations."
            };
            return $"{goldenAgeContexts[random.Next(goldenAgeContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceDisasterEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var disasterContexts = new string[]
            {
                $"The forces of nature, indifferent to human ambition and achievement, prepared to remind {civName} of their place in the cosmic order.",
                $"No amount of preparation could have readied {civName} for the catastrophe that was about to test their resilience and determination to survive.",
                $"The earth itself seemed to rebel as natural forces beyond human control unleashed devastation upon {civName} and all they had built.",
                $"In moments like these, the fragility of civilization becomes starkly apparent as {civName} faced powers that dwarf human understanding."
            };
            return $"{disasterContexts[random.Next(disasterContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceRevolutionEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var revolutionContexts = new string[]
            {
                $"The seeds of change had been planted long ago in {civName}, and now they bloomed into a revolution that would transform everything the people thought they knew about governance and society.",
                $"Old grievances and new ideas combined in {civName} like fire and gunpowder, creating an explosive force that would reshape the political landscape forever.",
                $"The voice of the people, long suppressed, finally found its strength in {civName} as citizens rose up to claim their destiny and forge a new path forward.",
                $"Revolutionary fervor swept through {civName} like wildfire, consuming the old order and clearing the ground for something entirely unprecedented to take root."
            };
            return $"{revolutionContexts[random.Next(revolutionContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceCascadeEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var cascadeContexts = new string[]
            {
                $"Like dominoes falling in sequence, the crisis in {civName} triggered a chain reaction that would spread far beyond their borders, affecting civilizations across the known world.",
                $"The interconnected nature of the modern world meant that when {civName} stumbled, the shockwaves would be felt by every nation that had dealings with them.",
                $"What began as a local problem in {civName} quickly revealed the fragile web of dependencies that linked all civilizations together in ways few had fully understood.",
                $"The collapse of one pillar in the structure of regional stability caused others to buckle and fall, as {civName} learned the true cost of interconnection."
            };
            return $"{cascadeContexts[random.Next(cascadeContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceSpiritualEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var spiritualContexts = new string[]
            {
                $"A great awakening was stirring in the hearts of the people of {civName} as spiritual insights and religious revelations transformed their understanding of the divine.",
                $"The boundary between the mortal realm and the sacred seemed to thin in {civName} as miraculous events and prophetic visions became increasingly common.",
                $"Religious leaders in {civName} reported unprecedented experiences of divine communion, leading to a spiritual renaissance that would reshape their entire culture.",
                $"The faithful of {civName} found themselves at the center of a spiritual transformation that would influence religious thought for generations to come."
            };
            return $"{spiritualContexts[random.Next(spiritualContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string EnhanceDiscoveryEvent(HistoricalEventRecord evt, string civName, string baseDescription, System.Random random)
        {
            var discoveryContexts = new string[]
            {
                $"The scholars and inventors of {civName} stood on the brink of a breakthrough that would revolutionize their understanding of the world and their place within it.",
                $"Years of research and experimentation in {civName} were about to bear fruit in a discovery that would change the course of technological development forever.",
                $"The accumulated knowledge and wisdom of {civName} had reached a critical mass where new insights became not just possible, but inevitable.",
                $"Innovation and inspiration converged in {civName} as brilliant minds unlocked secrets that had remained hidden since the dawn of civilization."
            };
            return $"{discoveryContexts[random.Next(discoveryContexts.Length)]} The chronicles record that {baseDescription.ToLower()}";
        }
        
        private string GenerateEventConsequence(HistoricalEventRecord evt, string civName)
        {
            // Generate consequence text that connects to actual simulation effects
            var consequences = evt.Type switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military => new string[]
                {
                    "This military action shifted the balance of power in the region, affecting trade relationships and diplomatic standings with neighboring civilizations.",
                    "The outcome influenced resource allocation and strategic planning for years to come, as military casualties and territorial changes reshaped political landscapes.",
                    "Victory or defeat in this conflict would determine the civilization's military reputation and influence future alliance negotiations."
                },
                ProceduralWorld.Simulation.Core.EventType.Economic => new string[]
                {
                    "The economic impact rippled through trade networks, affecting prosperity levels and resource availability across multiple civilizations.",
                    "Market fluctuations triggered by this event influenced diplomatic and military decisions as civilizations adapted to new economic realities.",
                    "Wealth distribution patterns were permanently altered, creating new opportunities for some while challenging others to adapt their strategies."
                },
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => new string[]
                {
                    "This diplomatic development altered alliance structures and established new precedents for international relations and conflict resolution.",
                    "Trust levels and reputation effects from this diplomacy shaped interactions for generations, influencing future negotiations and partnerships.",
                    "The agreement created ripple effects that would influence trade relationships, military cooperation, and cultural exchanges."
                },
                ProceduralWorld.Simulation.Core.EventType.Cultural => new string[]
                {
                    "Cultural innovations spread beyond borders, influencing artistic traditions and intellectual developments in neighboring civilizations.",
                    "The cultural shift created new forms of identity and social organization that would define the civilization's character for centuries.",
                    "Educational and artistic achievements raised the civilization's prestige and attracted scholars, artists, and traders from distant lands."
                },
                ProceduralWorld.Simulation.Core.EventType.Religious => new string[]
                {
                    "Religious changes influenced moral frameworks and social structures, affecting everything from law-making to international relations.",
                    "Spiritual developments created new forms of unity or division within the population, influencing political stability and social cohesion.",
                    "The religious transformation attracted pilgrims and missionaries, creating new cultural and economic connections with other civilizations."
                },
                _ => new string[]
                {
                    "The long-term effects of this event influenced the civilization's development trajectory and relationships with neighbors.",
                    "This development created ripple effects that would influence future decisions, opportunities, and challenges.",
                    "The consequences became woven into the fabric of the civilization's ongoing story, shaping its character and destiny."
                }
            };
            
            var random = new System.Random((int)(evt.Year * evt.Significance));
            return consequences[random.Next(consequences.Length)];
        }

        private void FixMissingAdaptivePersonalityData()
        {
            if (_entityManager == null)
            {
                Debug.LogError("[ChronicleReaderUI] EntityManager not available for component fix");
                return;
            }

            try
            {
                var entityManager = _entityManager;
                
                // Find all civilization entities
                var query = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<CivilizationData>()
                );
                
                var civilizations = query.ToEntityArray(Allocator.Temp);
                var civilizationData = query.ToComponentDataArray<CivilizationData>(Allocator.Temp);
                
                int fixedCount = 0;
                
                for (int i = 0; i < civilizations.Length; i++)
                {
                    var entity = civilizations[i];
                    var civData = civilizationData[i];
                    
                    // Check if AdaptivePersonalityData is missing
                    if (!entityManager.HasComponent<AdaptivePersonalityData>(entity))
                    {
                        // Create personality based on civilization data
                        var basePersonality = new PersonalityTraits
                        {
                            Aggressiveness = civData.Aggressiveness,
                            Defensiveness = civData.Defensiveness,
                            Greed = civData.Greed,
                            Paranoia = civData.Paranoia,
                            Ambition = civData.Ambition,
                            Desperation = civData.Desperation,
                            Hatred = civData.Hatred,
                            Pride = civData.Pride,
                            Vengefulness = civData.Vengefulness
                        };
                        
                        var adaptivePersonality = new AdaptivePersonalityData
                        {
                            BasePersonality = basePersonality,
                            CurrentPersonality = basePersonality,
                            TemporaryModifiers = new PersonalityTraits(),
                            SuccessfulWars = civData.SuccessfulWars,
                            DefensiveVictories = 0,
                            TradeSuccesses = 0,
                            Betrayals = civData.TimesBetrayed,
                            NaturalDisasters = 0,
                            CulturalAchievements = 0,
                            ReligiousEvents = 0,
                            DiplomaticVictories = 0,
                            PersonalityFlexibility = 0.5f,
                            CurrentStress = civData.ResourceStressLevel,
                            TraumaResistance = 0.6f,
                            Stage = PersonalityEvolutionStage.Developing,
                            PreviousPersonality = basePersonality,
                            LastPersonalityChangeYear = civData.LastAttackedYear
                        };
                        
                        entityManager.AddComponentData(entity, adaptivePersonality);
                        fixedCount++;
                        
                        Debug.Log($"[ChronicleReaderUI] Added AdaptivePersonalityData to {civData.Name}");
                    }
                }
                
                civilizations.Dispose();
                civilizationData.Dispose();
                query.Dispose();
                
                Debug.Log($"[ChronicleReaderUI] ✅ Fixed {fixedCount} civilizations with missing AdaptivePersonalityData components");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChronicleReaderUI] ❌ Error fixing AdaptivePersonalityData: {e}");
            }
        }

        private void SetChronicleText(string text)
        {
            Debug.Log($"[ChronicleReaderUI] BRAND NEW SetChronicleText with {text?.Length ?? 0} characters");
            
            if (string.IsNullOrEmpty(text))
            {
                text = "No chronicle data available.";
            }
            
            // DESTROY ALL EXISTING UI AND BUILD FRESH
            CreateBrandNewChronicleGUI(text);
        }
        
        private void CreateBrandNewChronicleGUI(string text)
        {
            Debug.Log($"[ChronicleReaderUI] CREATING COMPLETELY NEW GUI FROM SCRATCH");
            Debug.Log($"[ChronicleReaderUI] Received text length: {text?.Length ?? 0}");
            Debug.Log($"[ChronicleReaderUI] First 100 chars: {(text?.Length > 0 ? text.Substring(0, Math.Min(100, text.Length)) : "NULL OR EMPTY")}");
            
            // DESTROY ALL EXISTING UI ELEMENTS
            DestroyAllExistingUI();
            
            // Find or create canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("ChronicleCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create main panel
            var mainPanelGO = new GameObject("BrandNewChroniclePanel");
            mainPanelGO.transform.SetParent(canvas.transform, false);
            
            var mainRect = mainPanelGO.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.1f, 0.1f);
            mainRect.anchorMax = new Vector2(0.9f, 0.9f);
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;
            
            var mainBg = mainPanelGO.AddComponent<UnityEngine.UI.Image>();
            mainBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f); // Very dark background
            
            // Create close button
            CreateSimpleButton(mainPanelGO, "CLOSE", new Vector2(0.85f, 0.9f), new Vector2(1f, 1f), () => {
                mainPanelGO.SetActive(false);
            });
            
            // Create text area - SIMPLE, NO SCROLLING BULLSHIT
            var textAreaGO = new GameObject("SimpleTextArea");
            textAreaGO.transform.SetParent(mainPanelGO.transform, false);
            
            var textRect = textAreaGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.15f);
            textRect.anchorMax = new Vector2(0.95f, 0.85f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // Use basic Unity Text with proper formatting
            var textComponent = textAreaGO.AddComponent<UnityEngine.UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14; // Optimal size for readability
            textComponent.color = new Color(0.95f, 0.95f, 0.85f, 1f); // Cream-white for better readability
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.lineSpacing = 1.4f; // Better line spacing
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap; // Proper text wrapping
            textComponent.verticalOverflow = VerticalWrapMode.Truncate; // Prevent overflow
            textComponent.supportRichText = false; // Disable rich text to avoid HTML tag issues
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Strip HTML tags and chunk text to avoid vertex limit
            string cleanText = StripHtmlTags(text);
            string displayText = cleanText;
            // REMOVED 6000 character truncation - let pagination handle large text
            Debug.Log($"[ChronicleReaderUI] Using full text length: {cleanText.Length} characters");
            
            // Apply proper formatting to make text readable
            string formattedText = FormatChronicleForDisplay(displayText);
            
            Debug.Log($"[ChronicleReaderUI] Setting formatted text on component: {formattedText.Length} characters");
            textComponent.text = formattedText;
            Debug.Log($"[ChronicleReaderUI] Text successfully assigned to component");
            Debug.Log($"[ChronicleReaderUI] Component text length after assignment: {textComponent.text.Length}");
            
            // Create simple navigation for large text
            if (text.Length > 5000)
            {
                CreateSimpleNavigation(mainPanelGO, text, textComponent);
            }
            
            Debug.Log($"[ChronicleReaderUI] BRAND NEW GUI CREATED - Displaying {displayText.Length} characters");
        }
        
        private void DestroyAllExistingUI()
        {
            // Only destroy chronicle display panels, NOT the main UI
            try
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    // Only destroy specific chronicle panels
                    var chroniclePanels = canvas.GetComponentsInChildren<Transform>();
                    foreach (var transform in chroniclePanels)
                    {
                        if (transform != null && transform.gameObject != null && transform.name != null)
                        {
                            if (transform.name == "BrandNewChroniclePanel" || 
                                transform.name == "ProperTextArea" ||
                                transform.name == "EmergencyTextArea")
                            {
                                DestroyImmediate(transform.gameObject);
                                Debug.Log($"[ChronicleReaderUI] Destroyed: {transform.name}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ChronicleReaderUI] Error during UI cleanup: {ex.Message}");
            }
            
            // Clear references but don't destroy the main UI
            chronicleScrollRect = null;
            _backupChronicleText = null;
            _useEmergencyGUI = false;
            
            Debug.Log("[ChronicleReaderUI] SAFELY cleaned up chronicle panels only");
        }
        
        private void CreateSimpleButton(GameObject parent, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = new Vector2(5, 5);
            buttonRect.offsetMax = new Vector2(-5, -5);
            
            var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonText = buttonTextGO.AddComponent<UnityEngine.UI.Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.text = text;
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateSimpleNavigation(GameObject parent, string fullText, UnityEngine.UI.Text textComponent)
        {
            // Store for navigation
            _fullChronicleText = fullText;
            _currentChunkIndex = 0;
            _chunkSize = 6000;
            
            int totalPages = Mathf.CeilToInt((float)fullText.Length / _chunkSize);
            
            // Previous button
            CreateSimpleButton(parent, "< PREV", new Vector2(0.05f, 0.05f), new Vector2(0.25f, 0.12f), () => {
                if (_currentChunkIndex > 0)
                {
                    _currentChunkIndex--;
                    UpdateSimpleText(textComponent);
                }
            });
            
            // Next button
            CreateSimpleButton(parent, "NEXT >", new Vector2(0.75f, 0.05f), new Vector2(0.95f, 0.12f), () => {
                if (_currentChunkIndex < totalPages - 1)
                {
                    _currentChunkIndex++;
                    UpdateSimpleText(textComponent);
                }
            });
            
            // Page indicator
            var pageIndicatorGO = new GameObject("PageIndicator");
            pageIndicatorGO.transform.SetParent(parent.transform, false);
            var pageRect = pageIndicatorGO.AddComponent<RectTransform>();
            pageRect.anchorMin = new Vector2(0.25f, 0.05f);
            pageRect.anchorMax = new Vector2(0.75f, 0.12f);
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;
            
            var pageText = pageIndicatorGO.AddComponent<UnityEngine.UI.Text>();
            pageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pageText.fontSize = 11;
            pageText.color = new Color(0.8f, 0.8f, 0.6f, 1f);
            pageText.alignment = TextAnchor.MiddleCenter;
            pageText.text = $"Page 1 of {totalPages}";
        }
        
        private void UpdateSimpleText(UnityEngine.UI.Text textComponent)
        {
            if (string.IsNullOrEmpty(_fullChronicleText) || textComponent == null) return;
            
            int startIndex = _currentChunkIndex * _chunkSize;
            int remainingLength = _fullChronicleText.Length - startIndex;
            int chunkLength = Mathf.Min(_chunkSize, remainingLength);
            
            string chunk = _fullChronicleText.Substring(startIndex, chunkLength);
            string cleanChunk = StripHtmlTags(chunk); // Strip HTML tags from chunks too
            
            // Format the text for much better readability
            string formattedChunk = FormatChronicleForDisplay(cleanChunk);
            
            int totalPages = Mathf.CeilToInt((float)_fullChronicleText.Length / _chunkSize);
            string displayText = $"═══ PAGE {_currentChunkIndex + 1} OF {totalPages} ═══\n\n{formattedChunk}";
            
            textComponent.text = displayText;
            
            // Update page indicator
            var pageIndicator = textComponent.transform.parent.Find("PageIndicator");
            if (pageIndicator != null)
            {
                var pageText = pageIndicator.GetComponent<UnityEngine.UI.Text>();
                if (pageText != null)
                {
                    pageText.text = $"Page {_currentChunkIndex + 1} of {totalPages}";
                }
            }
            
            Debug.Log($"[ChronicleReaderUI] Updated to page {_currentChunkIndex + 1} of {totalPages}");
        }
        
        private string FormatChronicleForDisplay(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var formatted = new System.Text.StringBuilder();
            var lines = text.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    formatted.AppendLine(); // Preserve empty lines
                    continue;
                }
                
                // Format chronicle title
                if (line.StartsWith("THE CHRONICLES OF"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine();
                    formatted.AppendLine($"◆ {line} ◆");
                    formatted.AppendLine("═══════════════════════════════════════════════════════════");
                    formatted.AppendLine();
                    formatted.AppendLine();
                    continue;
                }
                
                // Format chapter headers with better styling
                if (line.StartsWith("CHAPTER"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine();
                    formatted.AppendLine($"✦ {line} ✦");
                    formatted.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    formatted.AppendLine();
                    continue;
                }
                
                // Format section dividers
                if (line.Contains("═══") || line.Contains("───") || line.Contains("━━━"))
                {
                    formatted.AppendLine(line);
                    formatted.AppendLine();
                    continue;
                }
                
                // Format year markers and story openings - make them stand out
                if (line.Contains("In the year") || line.Contains("Soon after,") || 
                    line.Contains("Meanwhile,") || line.Contains("Not long thereafter,") ||
                    line.Contains("As the seasons passed,") || line.Contains("In those days,"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine($"◈ {line}");
                    formatted.AppendLine();
                    continue;
                }
                
                // Format chapter conclusions
                if (line.Contains("Thus ended this chapter") || line.Contains("And so the chronicles"))
                {
                    formatted.AppendLine();
                    formatted.AppendLine();
                    formatted.AppendLine($"~ {line} ~");
                    formatted.AppendLine();
                    formatted.AppendLine();
                    continue;
                }
                
                // Format regular content with proper spacing
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // If the line is extremely long, break it up
                    if (line.Length > 300)
                    {
                        var sentences = SplitIntoSentences(line);
                        foreach (var sentence in sentences)
                        {
                            if (!string.IsNullOrWhiteSpace(sentence))
                            {
                                formatted.AppendLine($"  {sentence.Trim()}");
                            }
                        }
                        formatted.AppendLine(); // Add spacing after paragraph
                    }
                    else
                    {
                        // Regular line - just add with proper indentation
                        formatted.AppendLine($"  {line}");
                    }
                }
            }
            
            return formatted.ToString();
        }
        
        private string[] SplitIntoSentences(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];
            
            // If the text is short enough, return as-is
            if (text.Length < 200)
            {
                return new string[] { text };
            }
            
            // Split long text into sentences for better readability
            var sentences = new List<string>();
            var currentSentence = new System.Text.StringBuilder();
            var words = text.Split(' ');
            
            foreach (var word in words)
            {
                currentSentence.Append(word + " ");
                
                // End sentence at punctuation or when it gets too long
                if ((word.EndsWith(".") || word.EndsWith("!") || word.EndsWith("?") || 
                     word.EndsWith(":") || currentSentence.Length > 150) && 
                    currentSentence.Length > 50)
                {
                    sentences.Add(currentSentence.ToString().Trim());
                    currentSentence.Clear();
                }
            }
            
            // Add any remaining text
            if (currentSentence.Length > 0)
            {
                sentences.Add(currentSentence.ToString().Trim());
            }
            
            return sentences.ToArray();
        }
        
        private string CreateEngagingFallbackText()
        {
            var fallbackText = new System.Text.StringBuilder();
            
            fallbackText.AppendLine("◆ THE LIVING CHRONICLES AWAIT ◆");
            fallbackText.AppendLine("═══════════════════════════════════════════════════════════");
            fallbackText.AppendLine();
            fallbackText.AppendLine("    The ancient tomes lie open, their pages blank and waiting...");
            fallbackText.AppendLine();
            fallbackText.AppendLine("✦ THE WORLD STIRS ✦");
            fallbackText.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            fallbackText.AppendLine();
            fallbackText.AppendLine("    In the primordial silence before history begins, civilizations");
            fallbackText.AppendLine("    are taking their first breaths. Heroes are being born in humble");
            fallbackText.AppendLine("    villages, future kings are learning to walk, and the seeds of");
            fallbackText.AppendLine("    epic tales are being planted in the fertile soil of possibility.");
            fallbackText.AppendLine();
            fallbackText.AppendLine("◈ Soon, the great events will unfold...");
            fallbackText.AppendLine();
            fallbackText.AppendLine("    • Wars will rage across continents");
            fallbackText.AppendLine("    • Alliances will be forged in blood and gold");
            fallbackText.AppendLine("    • Prophets will arise with divine visions");
            fallbackText.AppendLine("    • Empires will rise from dust and fall to ruin");
            fallbackText.AppendLine("    • Heroes will emerge to face impossible odds");
            fallbackText.AppendLine("    • Betrayals will shatter the mightiest kingdoms");
            fallbackText.AppendLine("    • Love will bloom amidst the chaos of war");
            fallbackText.AppendLine("    • Legends will be born that echo through eternity");
            fallbackText.AppendLine();
            fallbackText.AppendLine("✦ WHAT YOU CAN DO ✦");
            fallbackText.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            fallbackText.AppendLine();
            fallbackText.AppendLine("    1. Let the simulation run to generate historical events");
            fallbackText.AppendLine("    2. Watch civilizations grow, interact, and create drama");
            fallbackText.AppendLine("    3. Click '📖 LOAD FRESH' to check for new chronicles");
            fallbackText.AppendLine("    4. Witness the transformation of dry events into epic tales!");
            fallbackText.AppendLine();
            fallbackText.AppendLine("~ The Living Chronicle System transforms boring simulation data ~");
            fallbackText.AppendLine("~ into dramatic, engaging stories worthy of the greatest epics! ~");
            fallbackText.AppendLine();
            fallbackText.AppendLine("◆ THE CHRONICLES WILL WRITE THEMSELVES... ◆");
            
            return fallbackText.ToString();
        }
        
        private string CreateErrorFallbackText(string errorMessage)
        {
            var errorText = new System.Text.StringBuilder();
            
            errorText.AppendLine("◆ THE CHRONICLES ARE TROUBLED ◆");
            errorText.AppendLine("═══════════════════════════════════════════════════════════");
            errorText.AppendLine();
            errorText.AppendLine("✦ A DISTURBANCE IN THE NARRATIVE ✦");
            errorText.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            errorText.AppendLine();
            errorText.AppendLine("    The ancient scribes report a disturbance in the flow of stories.");
            errorText.AppendLine("    The Living Chronicle System has encountered an obstacle that");
            errorText.AppendLine("    prevents the tales from reaching your eyes.");
            errorText.AppendLine();
            errorText.AppendLine("◈ Technical Details for the Wise:");
            errorText.AppendLine($"    {errorMessage}");
            errorText.AppendLine();
            errorText.AppendLine("✦ WHAT TO TRY ✦");
            errorText.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            errorText.AppendLine();
            errorText.AppendLine("    • Ensure the simulation is running");
            errorText.AppendLine("    • Wait for civilizations to generate events");
            errorText.AppendLine("    • Click '📖 LOAD FRESH' to try again");
            errorText.AppendLine("    • Check the Unity console for more details");
            errorText.AppendLine();
            errorText.AppendLine("~ Fear not - the chronicles will flow again soon! ~");
            
            return errorText.ToString();
        }
        
        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Remove common HTML/rich text tags
            string result = input;
            
            // Remove HTML tags like <color>, <b>, <i>, etc.
            result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]+>", "");
            
            // Remove any remaining angle brackets that might be malformed tags
            result = result.Replace("<", "").Replace(">", "");
            
            // Remove repetitive phrases that make text boring
            result = System.Text.RegularExpressions.Regex.Replace(result, @"The chronicles record that\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            result = System.Text.RegularExpressions.Regex.Replace(result, @"The records show that\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            result = System.Text.RegularExpressions.Regex.Replace(result, @"History tells us that\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Clean up multiple spaces and newlines
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\n\s*\n", "\n\n");
            
            Debug.Log($"[ChronicleReaderUI] Stripped HTML tags and repetitive phrases - Original: {input.Length}, Clean: {result.Length}");
            
            return result.Trim();
        }
        
        // Helper method to check if we have any text component
        private bool HasTextComponent()
        {
            return chronicleText != null || _backupChronicleText != null;
        }

        private void CreateSimpleTestUI()
        {
            Debug.Log("[ChronicleReaderUI] Creating simple test UI...");
            
            try
            {
                // Find or create canvas
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    var canvasGO = new GameObject("TestCanvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    Debug.Log("[ChronicleReaderUI] Created new canvas");
                }
                
                // Create a simple panel
                var panelGO = new GameObject("SimpleTestPanel");
                panelGO.transform.SetParent(canvas.transform, false);
                
                var rectTransform = panelGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
                rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                var image = panelGO.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0f, 0f, 0f, 0.8f);
                
                // Create text
                var textGO = new GameObject("SimpleTestText");
                textGO.transform.SetParent(panelGO.transform, false);
                
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(20, 20);
                textRect.offsetMax = new Vector2(-20, -20);
                
                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = "SIMPLE TEST UI WORKING!\n\nThis proves the UI system is functional.\n\nThe Chronicle Reader should appear soon...";
                text.fontSize = 16;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                
                Debug.Log("[ChronicleReaderUI] Simple test UI created successfully!");
                
                // Auto-hide after 5 seconds
                StartCoroutine(HideTestUIAfterDelay(panelGO, 5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChronicleReaderUI] Failed to create simple test UI: {e.Message}");
            }
        }
        
        private System.Collections.IEnumerator HideTestUIAfterDelay(GameObject panel, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (panel != null)
            {
                panel.SetActive(false);
                Debug.Log("[ChronicleReaderUI] Hidden simple test UI");
            }
        }
        
        private void CreateSimpleDebugText(GameObject parentPanel)
        {
            Debug.Log("[ChronicleReaderUI] Creating simple debug text directly on panel");
            
            // Remove any existing debug text
            var existingDebugText = parentPanel.transform.Find("DebugText");
            if (existingDebugText != null)
            {
                DestroyImmediate(existingDebugText.gameObject);
            }
            
            // Create simple text directly on the main panel (no scroll, no viewport)
            var debugTextGO = new GameObject("DebugText");
            debugTextGO.transform.SetParent(parentPanel.transform, false);
            
            var debugTextRect = debugTextGO.AddComponent<RectTransform>();
            debugTextRect.anchorMin = new Vector2(0.1f, 0.15f);
            debugTextRect.anchorMax = new Vector2(0.9f, 0.85f);
            debugTextRect.offsetMin = Vector2.zero;
            debugTextRect.offsetMax = Vector2.zero;
            
            var debugText = debugTextGO.AddComponent<TextMeshProUGUI>();
            debugText.text = "*** DEBUG TEXT TEST ***\n\nThis is a simple test to see if text can be displayed at all.\n\nIf you can see this BRIGHT YELLOW text, then the UI system is working and the problem is with the scroll rect setup.\n\nThe chronicle data is being generated correctly (121,000+ characters), but it's not visible due to a UI layout issue.\n\nPossible causes:\n- Scroll rect viewport masking\n- Content size fitting\n- Text positioning\n- Color/visibility issues";
            debugText.fontSize = 18;
            debugText.color = Color.yellow;
            debugText.fontStyle = FontStyles.Bold;
            debugText.textWrappingMode = TextWrappingModes.Normal;
            debugText.alignment = TextAlignmentOptions.TopLeft;
            
            Debug.Log("[ChronicleReaderUI] Created simple debug text - should be VERY visible!");
        }
        
        private void CreateBypassChronicleText(GameObject parentPanel)
        {
            Debug.Log("[ChronicleReaderUI] Creating chronicle text bypassing scroll view");
            
            // Remove any existing bypass text
            var existingBypassText = parentPanel.transform.Find("BypassChronicleText");
            if (existingBypassText != null)
            {
                DestroyImmediate(existingBypassText.gameObject);
            }
            
            // Get the latest chronicle data
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var civilizationQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            var civilizations = civilizationQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var civilizationData = civilizationQuery.ToComponentDataArray<CivilizationData>(Unity.Collections.Allocator.TempJob);
            
            string chronicleContent = "No chronicles available";
            
            if (civilizations.Length > 0)
            {
                // Get the first civilization's data
                var firstCiv = civilizationData[0];
                
                // Get historical events
                var worldHistorySystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WorldHistorySystem>();
                if (worldHistorySystem != null)
                {
                    var events = worldHistorySystem.GetHistoricalEvents(Unity.Collections.Allocator.TempJob);
                    chronicleContent = GenerateNarrativeChronicle(events, firstCiv);
                    events.Dispose();
                }
            }
            
            civilizations.Dispose();
            civilizationData.Dispose();
            civilizationQuery.Dispose();
            
            // Create text directly on panel (no scroll view, no viewport, no content area)
            var bypassTextGO = new GameObject("BypassChronicleText");
            bypassTextGO.transform.SetParent(parentPanel.transform, false);
            
            var bypassTextRect = bypassTextGO.AddComponent<RectTransform>();
            bypassTextRect.anchorMin = new Vector2(0.05f, 0.15f);
            bypassTextRect.anchorMax = new Vector2(0.95f, 0.85f);
            bypassTextRect.offsetMin = Vector2.zero;
            bypassTextRect.offsetMax = Vector2.zero;
            
            var bypassText = bypassTextGO.AddComponent<TextMeshProUGUI>();
            bypassText.text = chronicleContent.Length > 5000 ? chronicleContent.Substring(0, 5000) + "\n\n[TRUNCATED - First 5000 characters shown]" : chronicleContent;
            bypassText.fontSize = 14;
            bypassText.color = Color.cyan;
            bypassText.fontStyle = FontStyles.Normal;
            bypassText.textWrappingMode = TextWrappingModes.Normal;
            bypassText.alignment = TextAlignmentOptions.TopLeft;
            bypassText.overflowMode = TextOverflowModes.Overflow;
            
            Debug.Log($"[ChronicleReaderUI] Created bypass chronicle text - Length: {bypassText.text.Length} characters");
            Debug.Log("[ChronicleReaderUI] This text bypasses the scroll view entirely - if you can see cyan text, the problem is with the scroll view setup!");
        }
        
        private void InspectScrollViewText()
        {
            Debug.Log("[ChronicleReaderUI] ========== SCROLL VIEW TEXT INSPECTION ==========");
            
            if (chronicleText == null)
            {
                Debug.LogError("[ChronicleReaderUI] chronicleText is NULL!");
                return;
            }
            
            Debug.Log($"[ChronicleReaderUI] Text content length: {chronicleText.text.Length}");
            Debug.Log($"[ChronicleReaderUI] Text active: {chronicleText.gameObject.activeInHierarchy}");
            Debug.Log($"[ChronicleReaderUI] Text enabled: {chronicleText.enabled}");
            Debug.Log($"[ChronicleReaderUI] Text color: {chronicleText.color}");
            Debug.Log($"[ChronicleReaderUI] Text font size: {chronicleText.fontSize}");
            
            var textRect = chronicleText.GetComponent<RectTransform>();
            Debug.Log($"[ChronicleReaderUI] Text RectTransform:");
            Debug.Log($"  - anchorMin: {textRect.anchorMin}");
            Debug.Log($"  - anchorMax: {textRect.anchorMax}");
            Debug.Log($"  - anchoredPosition: {textRect.anchoredPosition}");
            Debug.Log($"  - sizeDelta: {textRect.sizeDelta}");
            Debug.Log($"  - rect: {textRect.rect}");
            Debug.Log($"  - pivot: {textRect.pivot}");
            
            var textLayoutElement = chronicleText.GetComponent<LayoutElement>();
            if (textLayoutElement != null)
            {
                Debug.Log($"[ChronicleReaderUI] Text LayoutElement:");
                Debug.Log($"  - flexibleHeight: {textLayoutElement.flexibleHeight}");
                Debug.Log($"  - preferredHeight: {textLayoutElement.preferredHeight}");
                Debug.Log($"  - minHeight: {textLayoutElement.minHeight}");
                Debug.Log($"  - ignoreLayout: {textLayoutElement.ignoreLayout}");
            }
            
            var textContentSizeFitter = chronicleText.GetComponent<ContentSizeFitter>();
            if (textContentSizeFitter != null)
            {
                Debug.Log($"[ChronicleReaderUI] Text ContentSizeFitter:");
                Debug.Log($"  - verticalFit: {textContentSizeFitter.verticalFit}");
                Debug.Log($"  - horizontalFit: {textContentSizeFitter.horizontalFit}");
            }
            
            // Check parent (Content)
            var contentParent = chronicleText.transform.parent;
            if (contentParent != null)
            {
                Debug.Log($"[ChronicleReaderUI] Content Parent: {contentParent.name}");
                Debug.Log($"  - active: {contentParent.gameObject.activeInHierarchy}");
                
                var contentRect = contentParent.GetComponent<RectTransform>();
                Debug.Log($"[ChronicleReaderUI] Content RectTransform:");
                Debug.Log($"  - anchorMin: {contentRect.anchorMin}");
                Debug.Log($"  - anchorMax: {contentRect.anchorMax}");
                Debug.Log($"  - anchoredPosition: {contentRect.anchoredPosition}");
                Debug.Log($"  - sizeDelta: {contentRect.sizeDelta}");
                Debug.Log($"  - rect: {contentRect.rect}");
                
                var contentLayoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
                if (contentLayoutGroup != null)
                {
                    Debug.Log($"[ChronicleReaderUI] Content VerticalLayoutGroup:");
                    Debug.Log($"  - enabled: {contentLayoutGroup.enabled}");
                    Debug.Log($"  - childControlHeight: {contentLayoutGroup.childControlHeight}");
                    Debug.Log($"  - childControlWidth: {contentLayoutGroup.childControlWidth}");
                    Debug.Log($"  - childForceExpandHeight: {contentLayoutGroup.childForceExpandHeight}");
                    Debug.Log($"  - childForceExpandWidth: {contentLayoutGroup.childForceExpandWidth}");
                }
                
                var contentSizeFitter = contentParent.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    Debug.Log($"[ChronicleReaderUI] Content ContentSizeFitter:");
                    Debug.Log($"  - verticalFit: {contentSizeFitter.verticalFit}");
                    Debug.Log($"  - horizontalFit: {contentSizeFitter.horizontalFit}");
                }
            }
            
            // Check scroll rect
            if (chronicleScrollRect != null)
            {
                Debug.Log($"[ChronicleReaderUI] ScrollRect:");
                Debug.Log($"  - content: {(chronicleScrollRect.content != null ? chronicleScrollRect.content.name : "NULL")}");
                Debug.Log($"  - viewport: {(chronicleScrollRect.viewport != null ? chronicleScrollRect.viewport.name : "NULL")}");
                Debug.Log($"  - verticalNormalizedPosition: {chronicleScrollRect.verticalNormalizedPosition}");
                Debug.Log($"  - content.rect.height: {(chronicleScrollRect.content != null ? chronicleScrollRect.content.rect.height : 0)}");
                Debug.Log($"  - viewport.rect.height: {(chronicleScrollRect.viewport != null ? chronicleScrollRect.viewport.rect.height : 0)}");
            }
            
            Debug.Log("[ChronicleReaderUI] ========== END INSPECTION ==========");
        }

        // NUCLEAR SOLUTION: Windowed Text Renderer for massive text content
        private void SetChronicleTextWindowed(string fullText)
        {
            Debug.Log($"[ChronicleReaderUI] WINDOWED RENDERER: Processing {fullText.Length} characters");
            
            // NUCLEAR APPROACH: Create completely separate text display that bypasses ScrollRect
            CreateStandaloneTextDisplay(fullText);
        }
        
        private void CreateStandaloneTextDisplay(string fullText)
        {
            Debug.Log("[ChronicleReaderUI] Creating CLEAN STANDALONE text display");
            
            // Ensure we have a valid panel first
            if (chronicleReaderPanel == null)
            {
                Debug.LogWarning("[ChronicleReaderUI] No chronicle panel found, creating one...");
                CreateProperChronicleUI();
                
                if (chronicleReaderPanel == null)
                {
                    Debug.LogError("[ChronicleReaderUI] CRITICAL: Still no chronicle panel after creation attempt!");
                    return;
                }
            }
            
            Debug.Log($"[ChronicleReaderUI] Panel found: {chronicleReaderPanel.name}, active: {chronicleReaderPanel.activeInHierarchy}");
            
            // CLEAN SLATE: Remove ALL existing content except title
            CleanupExistingUI();
            
            // Create main container
            var mainContainer = new GameObject("CleanTextDisplay");
            mainContainer.transform.SetParent(chronicleReaderPanel.transform, false);
            
            var containerRect = mainContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -60); // Leave space for title
            
            // Dark background with border
            var bgImage = mainContainer.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            
            Debug.Log($"[ChronicleReaderUI] Main container created: {mainContainer.name}");
            
            // Create text area with proper scrolling
            CreateCleanTextArea(mainContainer, fullText);
            
            // Create simple navigation
            CreateCleanNavigation(mainContainer);
            
            Debug.Log("[ChronicleReaderUI] CLEAN display created successfully!");
        }
        
        private void CleanupExistingUI()
        {
            Debug.Log("[ChronicleReaderUI] Cleaning up existing UI...");
            
            // Hide/remove redundant UI elements
            if (chronicleScrollRect != null)
                chronicleScrollRect.gameObject.SetActive(false);
                
            // Only cleanup if we have a valid panel
            if (chronicleReaderPanel != null)
            {
                // Remove old standalone displays
                var existingStandalone = chronicleReaderPanel.transform.Find("StandaloneTextDisplay");
                if (existingStandalone != null)
                {
                    Debug.Log("[ChronicleReaderUI] Removing existing StandaloneTextDisplay");
                    DestroyImmediate(existingStandalone.gameObject);
                }
                    
                var existingClean = chronicleReaderPanel.transform.Find("CleanTextDisplay");
                if (existingClean != null)
                {
                    Debug.Log("[ChronicleReaderUI] Removing existing CleanTextDisplay");
                    DestroyImmediate(existingClean.gameObject);
                }
            }
            else
            {
                Debug.LogWarning("[ChronicleReaderUI] No chronicle panel found during cleanup");
            }
        }
        
        private void CreateCleanTextArea(GameObject parent, string fullText)
        {
            Debug.Log($"[ChronicleReaderUI] Creating PROPER CLEAN GUI with {fullText.Length} characters");
            
            if (parent == null)
            {
                Debug.LogError("[ChronicleReaderUI] Parent is null!");
                return;
            }
            
            // BANISH THE EMERGENCY SYSTEM TO THE SHADOW REALM
            _useEmergencyGUI = false;
            
            // Create main text area
            var textAreaGO = new GameObject("ProperTextArea");
            textAreaGO.transform.SetParent(parent.transform, false);
            
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(20, 60);
            textAreaRect.offsetMax = new Vector2(-20, -20);
            
            // Dark background
            var textAreaBg = textAreaGO.AddComponent<UnityEngine.UI.Image>();
            textAreaBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f); // Very dark background
            
            // Create ScrollRect for proper scrolling
            var scrollRect = textAreaGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            
            // Create Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(textAreaGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10, 10);
            viewportRect.offsetMax = new Vector2(-10, -10);
            
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportImage = viewportGO.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = Color.clear;
            
            scrollRect.viewport = viewportRect;
            
            // Create Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            
            // Use BASIC Unity Text component - no TextMeshPro bullshit
            var textComponent = contentGO.AddComponent<UnityEngine.UI.Text>();
            
            // Configure with BRIGHT, READABLE settings
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 16;
            textComponent.color = new Color(0.95f, 0.95f, 0.7f, 1f); // Bright cream - VERY readable
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.lineSpacing = 1.2f;
            textComponent.supportRichText = true;
            
            Debug.Log($"[ChronicleReaderUI] Created proper Text component with bright cream color: {textComponent.color}");
            
            // Validate text
            if (string.IsNullOrEmpty(fullText))
            {
                fullText = "No chronicle text available. Please try loading chronicles again.";
            }
            
            // CHUNK THE TEXT to avoid Unity's 65k vertex limit
            string displayText = fullText;
            if (fullText.Length > 8000) // Safe limit to avoid vertex overflow
            {
                displayText = fullText.Substring(0, 8000);
                displayText += "\n\n=== TEXT TRUNCATED ===\n";
                displayText += $"Showing first 8,000 characters of {fullText.Length} total.\n";
                displayText += "This prevents Unity's vertex limit error.\n";
                displayText += "Full text display coming soon!";
                
                Debug.Log($"[ChronicleReaderUI] Truncated text from {fullText.Length} to {displayText.Length} characters");
            }
            
            // Assign chunked text
            textComponent.text = displayText;
            
            // Store references and setup pagination
            chronicleScrollRect = scrollRect;
            _backupChronicleText = textComponent;
            _fullChronicleText = fullText; // Store full text for pagination
            _currentChunkIndex = 0;
            _chunkSize = 8000;
            
            // Create navigation buttons if text was truncated
            if (fullText.Length > 8000)
            {
                CreatePaginationControls(textAreaGO, textComponent);
            }
            
            Debug.Log("[ChronicleReaderUI] PROPER GUI CREATED - Bright text on dark background with working scroll!");
        }
        
        private void CreatePaginationControls(GameObject parent, UnityEngine.UI.Text textComponent)
        {
            // Create navigation bar at bottom
            var navBarGO = new GameObject("PaginationBar");
            navBarGO.transform.SetParent(parent.transform, false);
            
            var navBarRect = navBarGO.AddComponent<RectTransform>();
            navBarRect.anchorMin = new Vector2(0, 0);
            navBarRect.anchorMax = new Vector2(1, 0);
            navBarRect.offsetMin = new Vector2(10, 5);
            navBarRect.offsetMax = new Vector2(-10, 40);
            
            var navBarBg = navBarGO.AddComponent<UnityEngine.UI.Image>();
            navBarBg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
            
            // Previous button
            CreatePaginationButton(navBarGO, "< PREVIOUS", new Vector2(0, 0), new Vector2(0.25f, 1), () => {
                if (_currentChunkIndex > 0)
                {
                    _currentChunkIndex--;
                    UpdateDisplayedChunk(textComponent);
                }
            });
            
            // Next button
            CreatePaginationButton(navBarGO, "NEXT >", new Vector2(0.75f, 0), new Vector2(1, 1), () => {
                int totalChunks = Mathf.CeilToInt((float)_fullChronicleText.Length / _chunkSize);
                if (_currentChunkIndex < totalChunks - 1)
                {
                    _currentChunkIndex++;
                    UpdateDisplayedChunk(textComponent);
                }
            });
            
            // Page indicator
            var pageIndicatorGO = new GameObject("PageIndicator");
            pageIndicatorGO.transform.SetParent(navBarGO.transform, false);
            var pageIndicatorRect = pageIndicatorGO.AddComponent<RectTransform>();
            pageIndicatorRect.anchorMin = new Vector2(0.25f, 0);
            pageIndicatorRect.anchorMax = new Vector2(0.75f, 1);
            pageIndicatorRect.offsetMin = Vector2.zero;
            pageIndicatorRect.offsetMax = Vector2.zero;
            
            var pageText = pageIndicatorGO.AddComponent<UnityEngine.UI.Text>();
            pageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pageText.fontSize = 12;
            pageText.color = new Color(0.8f, 0.8f, 0.6f, 1f);
            pageText.alignment = TextAnchor.MiddleCenter;
            
            int totalChunks = Mathf.CeilToInt((float)_fullChronicleText.Length / _chunkSize);
            pageText.text = $"Page {_currentChunkIndex + 1} of {totalChunks} | {_fullChronicleText.Length} chars total";
            
            Debug.Log($"[ChronicleReaderUI] Created pagination controls - {totalChunks} pages total");
        }
        
        private void CreatePaginationButton(GameObject parent, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = new Vector2(5, 5);
            buttonRect.offsetMax = new Vector2(-5, -5);
            
            var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonText = buttonTextGO.AddComponent<UnityEngine.UI.Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.text = text;
            buttonText.fontSize = 11;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void UpdateDisplayedChunk(UnityEngine.UI.Text textComponent)
        {
            if (string.IsNullOrEmpty(_fullChronicleText) || textComponent == null) return;
            
            int startIndex = _currentChunkIndex * _chunkSize;
            int remainingLength = _fullChronicleText.Length - startIndex;
            int chunkLength = Mathf.Min(_chunkSize, remainingLength);
            
            string chunk = _fullChronicleText.Substring(startIndex, chunkLength);
            
            // Add pagination info
            int totalChunks = Mathf.CeilToInt((float)_fullChronicleText.Length / _chunkSize);
            string displayText = $"=== PAGE {_currentChunkIndex + 1} OF {totalChunks} ===\n\n";
            displayText += chunk;
            
            if (_currentChunkIndex < totalChunks - 1)
            {
                displayText += "\n\n=== CONTINUED ON NEXT PAGE ===";
            }
            
            textComponent.text = displayText;
            
            // Update page indicator
            var pageIndicator = textComponent.transform.parent.parent.Find("PaginationBar/PageIndicator");
            if (pageIndicator != null)
            {
                var pageText = pageIndicator.GetComponent<UnityEngine.UI.Text>();
                if (pageText != null)
                {
                    pageText.text = $"Page {_currentChunkIndex + 1} of {totalChunks} | {_fullChronicleText.Length} chars total";
                }
            }
            
            // Reset scroll to top
            if (chronicleScrollRect != null)
            {
                chronicleScrollRect.verticalNormalizedPosition = 1f;
            }
            
            Debug.Log($"[ChronicleReaderUI] Updated to page {_currentChunkIndex + 1} of {totalChunks}");
        }
        
        private TextMeshProUGUI _currentTextComponent;
        
        public void OnMouseWheelScroll(float scrollDelta)
        {
            // Use the stored ScrollRect reference first
            if (chronicleScrollRect != null)
            {
                // Scroll the ScrollRect directly with better sensitivity
                float scrollAmount = scrollDelta * 0.05f; // Smoother scrolling
                chronicleScrollRect.verticalNormalizedPosition = Mathf.Clamp01(chronicleScrollRect.verticalNormalizedPosition + scrollAmount);
                Debug.Log($"[ChronicleReaderUI] Mouse wheel scroll: {scrollDelta}, new position: {chronicleScrollRect.verticalNormalizedPosition}");
            }
            else
            {
                // Fallback to finding ScrollRect
                var scrollHandler = GetComponentInChildren<ScrollRect>();
                if (scrollHandler != null)
                {
                    float scrollAmount = scrollDelta * 0.05f;
                    scrollHandler.verticalNormalizedPosition = Mathf.Clamp01(scrollHandler.verticalNormalizedPosition + scrollAmount);
                    Debug.Log($"[ChronicleReaderUI] Fallback mouse wheel scroll: {scrollDelta}");
                }
                else if (_useWindowedRenderer)
                {
                    // Last resort: windowed scrolling
                    int scrollLines = Mathf.RoundToInt(scrollDelta * 3);
                    int newStart = Mathf.Clamp(_visibleLineStart - scrollLines, 0, Mathf.Max(0, _fullTextLines.Count - _linesPerPage));
                    
                    if (newStart != _visibleLineStart)
                    {
                        _visibleLineStart = newStart;
                        UpdateCleanDisplay();
                    }
                }
            }
        }
        
        private void CreateCleanNavigation(GameObject parent)
        {
            // Simple navigation bar at bottom
            var navBarGO = new GameObject("Navigation");
            navBarGO.transform.SetParent(parent.transform, false);
            
            var navBarRect = navBarGO.AddComponent<RectTransform>();
            navBarRect.anchorMin = new Vector2(0, 0);
            navBarRect.anchorMax = new Vector2(1, 0);
            navBarRect.offsetMin = new Vector2(15, 10);
            navBarRect.offsetMax = new Vector2(-15, 45);
            
            // Navigation background
            var navBg = navBarGO.AddComponent<UnityEngine.UI.Image>();
            navBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Previous Page button
            CreateNavButton(navBarGO, "< PREV", new Vector2(0, 0), new Vector2(0.18f, 1), () => {
                if (_visibleLineStart > 0)
                {
                    _visibleLineStart = Mathf.Max(0, _visibleLineStart - _linesPerPage);
                    UpdateCleanDisplay();
                    Debug.Log($"[ChronicleReaderUI] Previous page - now showing lines {_visibleLineStart}-{_visibleLineStart + _linesPerPage}");
                }
            });
            
            // Next Page button  
            CreateNavButton(navBarGO, "NEXT >", new Vector2(0.2f, 0), new Vector2(0.38f, 1), () => {
                if (_visibleLineStart + _linesPerPage < _fullTextLines.Count)
                {
                    _visibleLineStart += _linesPerPage;
                    UpdateCleanDisplay();
                    Debug.Log($"[ChronicleReaderUI] Next page - now showing lines {_visibleLineStart}-{_visibleLineStart + _linesPerPage}");
                }
            });
            
            // Jump to Top button
            CreateNavButton(navBarGO, "TOP", new Vector2(0.62f, 0), new Vector2(0.75f, 1), () => {
                _visibleLineStart = 0;
                UpdateCleanDisplay();
                Debug.Log("[ChronicleReaderUI] Jumped to top");
            });
            
            // Jump to Bottom button  
            CreateNavButton(navBarGO, "END", new Vector2(0.77f, 0), new Vector2(0.9f, 1), () => {
                _visibleLineStart = Mathf.Max(0, _fullTextLines.Count - _linesPerPage);
                UpdateCleanDisplay();
                Debug.Log("[ChronicleReaderUI] Jumped to bottom");
            });
            
            // Status indicator
            var statusIndicatorGO = new GameObject("StatusInfo");
            statusIndicatorGO.transform.SetParent(navBarGO.transform, false);
            var statusIndicatorRect = statusIndicatorGO.AddComponent<RectTransform>();
            statusIndicatorRect.anchorMin = new Vector2(0.2f, 0);
            statusIndicatorRect.anchorMax = new Vector2(0.8f, 1);
            statusIndicatorRect.offsetMin = new Vector2(10, 0);
            statusIndicatorRect.offsetMax = new Vector2(-10, 0);
            
            var statusText = statusIndicatorGO.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 12;
            statusText.color = new Color(0.8f, 0.8f, 0.7f, 1f);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.text = "Use PREV/NEXT for pages • TOP/END to jump • Simple text display";
            
            _pageIndicatorText = statusText;
        }
        
        private TextMeshProUGUI _pageIndicatorText;
        
        private void CreateNavButton(GameObject parent, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = new Vector2(5, 5);
            buttonRect.offsetMax = new Vector2(-5, -5);
            
            var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 11;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
        }
        
        private void UpdateCleanDisplay()
        {
            if (!_useWindowedRenderer || _fullTextLines.Count == 0 || _currentTextComponent == null) 
            {
                Debug.Log("[ChronicleReaderUI] UpdateCleanDisplay: Cannot update - missing components");
                return;
            }
            
            // Get visible lines
            int endLine = Mathf.Min(_visibleLineStart + _linesPerPage, _fullTextLines.Count);
            var visibleLines = new List<string>();
            
            for (int i = _visibleLineStart; i < endLine; i++)
            {
                visibleLines.Add(_fullTextLines[i]);
            }
            
            // Set text with clear page header
            string displayText = $"=== PAGE {(_visibleLineStart / _linesPerPage) + 1} ===\n\n" + string.Join("\n", visibleLines);
            _currentTextComponent.text = displayText;
            
            Debug.Log($"[ChronicleReaderUI] Updated display: '{displayText.Substring(0, Math.Min(100, displayText.Length))}...'");
            Debug.Log($"[ChronicleReaderUI] Text component color: {_currentTextComponent.color}");
            Debug.Log($"[ChronicleReaderUI] Text component active: {_currentTextComponent.gameObject.activeInHierarchy}");
            
            // Update page indicator
            if (_pageIndicatorText != null)
            {
                int currentPage = (_visibleLineStart / _linesPerPage) + 1;
                int totalPages = Mathf.CeilToInt((float)_fullTextLines.Count / _linesPerPage);
                _pageIndicatorText.text = $"Page {currentPage} of {totalPages} | Lines {_visibleLineStart + 1}-{endLine} of {_fullTextLines.Count}";
            }
            
            Debug.Log($"[ChronicleReaderUI] UPDATED: Displaying lines {_visibleLineStart + 1}-{endLine} of {_fullTextLines.Count}");
        }
        
        private void CreateStandaloneControls(GameObject parent, TextMeshProUGUI textComponent)
        {
            // Create control bar
            var controlBarGO = new GameObject("ControlBar");
            controlBarGO.transform.SetParent(parent.transform, false);
            
            var controlBarRect = controlBarGO.AddComponent<RectTransform>();
            controlBarRect.anchorMin = new Vector2(0, 0);
            controlBarRect.anchorMax = new Vector2(1, 0);
            controlBarRect.offsetMin = new Vector2(10, 10);
            controlBarRect.offsetMax = new Vector2(-10, 50);
            
            // Previous button
            var prevButtonGO = new GameObject("PrevButton");
            prevButtonGO.transform.SetParent(controlBarGO.transform, false);
            var prevButtonRect = prevButtonGO.AddComponent<RectTransform>();
            prevButtonRect.anchorMin = new Vector2(0, 0);
            prevButtonRect.anchorMax = new Vector2(0.2f, 1);
            prevButtonRect.offsetMin = Vector2.zero;
            prevButtonRect.offsetMax = new Vector2(-5, 0);
            
            var prevButtonImage = prevButtonGO.AddComponent<UnityEngine.UI.Image>();
            prevButtonImage.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            
            var prevButton = prevButtonGO.AddComponent<Button>();
            prevButton.onClick.AddListener(() => {
                if (_visibleLineStart > 0)
                {
                    _visibleLineStart = Mathf.Max(0, _visibleLineStart - _linesPerPage);
                    UpdateStandaloneDisplay(textComponent);
                }
            });
            
            var prevButtonTextGO = new GameObject("Text");
            prevButtonTextGO.transform.SetParent(prevButtonGO.transform, false);
            var prevButtonTextRect = prevButtonTextGO.AddComponent<RectTransform>();
            prevButtonTextRect.anchorMin = Vector2.zero;
            prevButtonTextRect.anchorMax = Vector2.one;
            prevButtonTextRect.offsetMin = Vector2.zero;
            prevButtonTextRect.offsetMax = Vector2.zero;
            
            var prevButtonText = prevButtonTextGO.AddComponent<TextMeshProUGUI>();
            prevButtonText.text = "< PREV";
            prevButtonText.fontSize = 12;
            prevButtonText.color = Color.white;
            prevButtonText.alignment = TextAlignmentOptions.Center;
            
            // Next button
            var nextButtonGO = new GameObject("NextButton");
            nextButtonGO.transform.SetParent(controlBarGO.transform, false);
            var nextButtonRect = nextButtonGO.AddComponent<RectTransform>();
            nextButtonRect.anchorMin = new Vector2(0.8f, 0);
            nextButtonRect.anchorMax = new Vector2(1, 1);
            nextButtonRect.offsetMin = new Vector2(5, 0);
            nextButtonRect.offsetMax = Vector2.zero;
            
            var nextButtonImage = nextButtonGO.AddComponent<UnityEngine.UI.Image>();
            nextButtonImage.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            
            var nextButton = nextButtonGO.AddComponent<Button>();
            nextButton.onClick.AddListener(() => {
                if (_visibleLineStart + _linesPerPage < _fullTextLines.Count)
                {
                    _visibleLineStart += _linesPerPage;
                    UpdateStandaloneDisplay(textComponent);
                }
            });
            
            var nextButtonTextGO = new GameObject("Text");
            nextButtonTextGO.transform.SetParent(nextButtonGO.transform, false);
            var nextButtonTextRect = nextButtonTextGO.AddComponent<RectTransform>();
            nextButtonTextRect.anchorMin = Vector2.zero;
            nextButtonTextRect.anchorMax = Vector2.one;
            nextButtonTextRect.offsetMin = Vector2.zero;
            nextButtonTextRect.offsetMax = Vector2.zero;
            
            var nextButtonText = nextButtonTextGO.AddComponent<TextMeshProUGUI>();
            nextButtonText.text = "NEXT >";
            nextButtonText.fontSize = 12;
            nextButtonText.color = Color.white;
            nextButtonText.alignment = TextAlignmentOptions.Center;
            
            // Page indicator
            var pageIndicatorGO = new GameObject("PageIndicator");
            pageIndicatorGO.transform.SetParent(controlBarGO.transform, false);
            var pageIndicatorRect = pageIndicatorGO.AddComponent<RectTransform>();
            pageIndicatorRect.anchorMin = new Vector2(0.2f, 0);
            pageIndicatorRect.anchorMax = new Vector2(0.8f, 1);
            pageIndicatorRect.offsetMin = new Vector2(5, 0);
            pageIndicatorRect.offsetMax = new Vector2(-5, 0);
            
            var pageIndicatorText = pageIndicatorGO.AddComponent<TextMeshProUGUI>();
            pageIndicatorText.fontSize = 10;
            pageIndicatorText.color = Color.yellow;
            pageIndicatorText.alignment = TextAlignmentOptions.Center;
            pageIndicatorText.text = "STANDALONE TEXT DISPLAY ACTIVE";
        }
        
        private void UpdateStandaloneDisplay(TextMeshProUGUI textComponent)
        {
            if (!_useWindowedRenderer || _fullTextLines.Count == 0 || textComponent == null) return;
            
            // Get visible lines
            int endLine = Mathf.Min(_visibleLineStart + _linesPerPage, _fullTextLines.Count);
            var visibleLines = new List<string>();
            
            for (int i = _visibleLineStart; i < endLine; i++)
            {
                visibleLines.Add(_fullTextLines[i]);
            }
            
            // Create display text with indicators
            string displayText = "";
            
            if (_visibleLineStart > 0)
            {
                displayText += $"▲▲▲ MORE CONTENT ABOVE (Line {_visibleLineStart + 1} of {_fullTextLines.Count}) ▲▲▲\n\n";
            }
            
            displayText += string.Join("\n", visibleLines);
            
            if (endLine < _fullTextLines.Count)
            {
                displayText += $"\n\n>>> MORE CONTENT BELOW (Showing {endLine} of {_fullTextLines.Count} lines) <<<";
            }
            
            // Set text directly - no ScrollRect, no layout groups, no BS
            textComponent.text = displayText;
            
            Debug.Log($"[ChronicleReaderUI] STANDALONE: Displaying lines {_visibleLineStart + 1}-{endLine} of {_fullTextLines.Count}");
            
            // Update page indicator
            var pageIndicator = textComponent.transform.parent.parent.Find("ControlBar/PageIndicator");
            if (pageIndicator != null)
            {
                var pageText = pageIndicator.GetComponent<TextMeshProUGUI>();
                if (pageText != null)
                {
                    int currentPage = (_visibleLineStart / _linesPerPage) + 1;
                    int totalPages = Mathf.CeilToInt((float)_fullTextLines.Count / _linesPerPage);
                    pageText.text = $"PAGE {currentPage} OF {totalPages} | LINES {_visibleLineStart + 1}-{endLine} OF {_fullTextLines.Count}";
                }
            }
        }
        
        private void OnWindowedScroll(Vector2 scrollValue)
        {
            if (!_useWindowedRenderer) return;
            
            // Calculate which lines should be visible based on scroll position
            float scrollPercent = 1f - scrollValue.y; // Invert because Unity scroll is backwards
            int totalLines = _fullTextLines.Count;
            int maxStartLine = Mathf.Max(0, totalLines - _linesPerPage);
            
            int newStartLine = Mathf.RoundToInt(scrollPercent * maxStartLine);
            
            if (newStartLine != _visibleLineStart)
            {
                _visibleLineStart = newStartLine;
                UpdateWindowedDisplay();
            }
        }
        
        private void UpdateWindowedDisplay()
        {
            if (!_useWindowedRenderer || _fullTextLines.Count == 0) return;
            
            // Get visible lines
            int endLine = Mathf.Min(_visibleLineStart + _linesPerPage, _fullTextLines.Count);
            var visibleLines = new List<string>();
            
            for (int i = _visibleLineStart; i < endLine; i++)
            {
                visibleLines.Add(_fullTextLines[i]);
            }
            
            // Add scroll indicators
            string displayText = "";
            
            if (_visibleLineStart > 0)
            {
                displayText += $"[SCROLLED DOWN - Line {_visibleLineStart + 1} of {_fullTextLines.Count}]\n";
                displayText += "▲ ▲ ▲ MORE CONTENT ABOVE ▲ ▲ ▲\n\n";
            }
            
            displayText += string.Join("\n", visibleLines);
            
            if (endLine < _fullTextLines.Count)
            {
                displayText += "\n\n>>> MORE CONTENT BELOW <<<";
                displayText += $"\n[Showing lines {_visibleLineStart + 1}-{endLine} of {_fullTextLines.Count}]";
            }
            
            // Set the text directly
            if (chronicleText != null)
            {
                chronicleText.text = displayText;
                Debug.Log($"[ChronicleReaderUI] WINDOWED RENDERER: Displaying lines {_visibleLineStart + 1}-{endLine} of {_fullTextLines.Count}");
            }
            else if (_backupChronicleText != null)
            {
                _backupChronicleText.text = displayText;
                         }
         }
         
         private void TestWindowedRenderer()
         {
            Debug.Log("[ChronicleReaderUI] Testing NUCLEAR windowed renderer with sample data");
            
            // Generate a massive test text
            var testText = new StringBuilder();
            testText.AppendLine("NUCLEAR WINDOWED RENDERER TEST");
            testText.AppendLine("═══════════════════════════════════════════════════════════");
            testText.AppendLine();
            
            for (int i = 1; i <= 1000; i++)
            {
                testText.AppendLine($"Line {i:D4}: This is a test line to demonstrate the windowed text renderer. It should handle massive amounts of text without Unity's limitations.");
                
                if (i % 50 == 0)
                {
                    testText.AppendLine();
                    testText.AppendLine($"═══ SECTION {i/50} COMPLETE ═══");
                    testText.AppendLine();
                }
            }
            
            testText.AppendLine();
            testText.AppendLine("END OF NUCLEAR TEST - If you can scroll through this smoothly, the windowed renderer is working!");
            
            string fullTestText = testText.ToString();
            Debug.Log($"[ChronicleReaderUI] Generated test text with {fullTestText.Length} characters and {fullTestText.Split('\n').Length} lines");
            
            // Force windowed rendering
            SetChronicleTextWindowed(fullTestText);
            
            if (chronicleTitle != null)
            {
                chronicleTitle.text = "NUCLEAR WINDOWED RENDERER TEST";
            }
        }
        
        private void OnGUI()
        {
            if (!_useEmergencyGUI || string.IsNullOrEmpty(_emergencyText))
                return;
                
            // Create a large text area covering most of the screen
            Rect textArea = new Rect(50, 100, Screen.width - 100, Screen.height - 200);
            
            // Dark background box for better contrast
            GUIStyle backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = Texture2D.whiteTexture;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-gray background
            GUI.Box(textArea, "", backgroundStyle);
            GUI.backgroundColor = Color.white; // Reset background color
            
            // Split text into lines for scrolling
            string[] lines = _emergencyText.Split('\n');
            
            // Create scrollable text display
            Rect scrollArea = new Rect(textArea.x + 10, textArea.y + 10, textArea.width - 20, textArea.height - 60);
            
            // Calculate visible lines
            int startLine = _emergencyScrollOffset;
            int endLine = Mathf.Min(startLine + _emergencyLinesPerScreen, lines.Length);
            
            // Build visible text with bright header
            System.Text.StringBuilder visibleText = new System.Text.StringBuilder();
            visibleText.AppendLine($"=== BRIGHT EMERGENCY DISPLAY === (Lines {startLine + 1}-{endLine} of {lines.Length})");
            visibleText.AppendLine("Text should now be BRIGHT CREAM/YELLOW and easily readable!");
            visibleText.AppendLine();
            
            for (int i = startLine; i < endLine; i++)
            {
                visibleText.AppendLine(lines[i]);
            }
            
            // Display the text with BRIGHT, READABLE colors
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 16;
            textStyle.normal.textColor = new Color(1f, 1f, 0.8f, 1f); // Bright cream/yellow - same as UI version
            textStyle.wordWrap = true;
            textStyle.alignment = TextAnchor.UpperLeft;
            
            GUI.Label(scrollArea, visibleText.ToString(), textStyle);
            
            // Navigation buttons
            Rect buttonArea = new Rect(textArea.x, textArea.y + textArea.height - 40, textArea.width, 30);
            
            float buttonWidth = buttonArea.width / 4;
            
            if (GUI.Button(new Rect(buttonArea.x, buttonArea.y, buttonWidth, buttonArea.height), "PREV PAGE"))
            {
                _emergencyScrollOffset = Mathf.Max(0, _emergencyScrollOffset - _emergencyLinesPerScreen);
            }
            
            if (GUI.Button(new Rect(buttonArea.x + buttonWidth, buttonArea.y, buttonWidth, buttonArea.height), "NEXT PAGE"))
            {
                _emergencyScrollOffset = Mathf.Min(lines.Length - _emergencyLinesPerScreen, _emergencyScrollOffset + _emergencyLinesPerScreen);
            }
            
            if (GUI.Button(new Rect(buttonArea.x + buttonWidth * 2, buttonArea.y, buttonWidth, buttonArea.height), "TOP"))
            {
                _emergencyScrollOffset = 0;
            }
            
            if (GUI.Button(new Rect(buttonArea.x + buttonWidth * 3, buttonArea.y, buttonWidth, buttonArea.height), "CLOSE"))
            {
                _useEmergencyGUI = false;
                ToggleChronicleReader(); // Close the reader
            }
            
            // Handle scroll wheel
            if (Event.current.type == UnityEngine.EventType.ScrollWheel && textArea.Contains(Event.current.mousePosition))
            {
                int scrollDirection = Event.current.delta.y > 0 ? 1 : -1;
                _emergencyScrollOffset = Mathf.Clamp(_emergencyScrollOffset + scrollDirection * 3, 0, Mathf.Max(0, lines.Length - _emergencyLinesPerScreen));
                Event.current.Use();
            }
        }
     }
 } 