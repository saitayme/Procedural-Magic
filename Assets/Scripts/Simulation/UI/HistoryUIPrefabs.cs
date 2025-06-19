using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProceduralWorld.Simulation.UI
{
    public class HistoryUIPrefabs : MonoBehaviour
    {
        [Header("Event Marker")]
        [SerializeField] private GameObject eventMarkerPrefab;
        [SerializeField] private Image eventIcon;
        [SerializeField] private TextMeshProUGUI eventTitle;
        [SerializeField] private TextMeshProUGUI eventYear;

        [Header("Figure Marker")]
        [SerializeField] private GameObject figureMarkerPrefab;
        [SerializeField] private Image figureIcon;
        [SerializeField] private TextMeshProUGUI figureName;
        [SerializeField] private TextMeshProUGUI figureLifespan;

        [Header("Period Marker")]
        [SerializeField] private GameObject periodMarkerPrefab;
        [SerializeField] private Image periodIcon;
        [SerializeField] private TextMeshProUGUI periodName;
        [SerializeField] private TextMeshProUGUI periodYears;

        [Header("Event Details")]
        [SerializeField] private GameObject eventDetailsPrefab;
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private TextMeshProUGUI eventYearText;
        [SerializeField] private TextMeshProUGUI eventSignificanceText;
        [SerializeField] private Transform relatedFiguresContainer;
        [SerializeField] private Transform relatedCivilizationsContainer;
        [SerializeField] private GameObject relatedItemPrefab;

        [Header("Figure Details")]
        [SerializeField] private GameObject figureDetailsPrefab;
        [SerializeField] private TextMeshProUGUI figureNameText;
        [SerializeField] private TextMeshProUGUI figureCivilizationText;
        [SerializeField] private TextMeshProUGUI figureLifespanText;
        [SerializeField] private TextMeshProUGUI figureLegacyText;
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private GameObject achievementPrefab;

        [Header("Period Details")]
        [SerializeField] private GameObject periodDetailsPrefab;
        [SerializeField] private TextMeshProUGUI periodNameText;
        [SerializeField] private TextMeshProUGUI periodYearsText;
        [SerializeField] private TextMeshProUGUI periodDescriptionText;
        [SerializeField] private Transform periodEventsContainer;
        [SerializeField] private GameObject periodEventPrefab;

        private void Start()
        {
            InitializePrefabs();
        }

        private void InitializePrefabs()
        {
            // Initialize Event Marker
            if (eventMarkerPrefab != null)
            {
                var button = eventMarkerPrefab.GetComponent<Button>();
                if (button == null)
                {
                    button = eventMarkerPrefab.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = eventIcon;
            }

            // Initialize Figure Marker
            if (figureMarkerPrefab != null)
            {
                var button = figureMarkerPrefab.GetComponent<Button>();
                if (button == null)
                {
                    button = figureMarkerPrefab.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = figureIcon;
            }

            // Initialize Period Marker
            if (periodMarkerPrefab != null)
            {
                var button = periodMarkerPrefab.GetComponent<Button>();
                if (button == null)
                {
                    button = periodMarkerPrefab.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = periodIcon;
            }

            // Initialize Related Item Prefab
            if (relatedItemPrefab != null)
            {
                var text = relatedItemPrefab.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null)
                {
                    Debug.LogError("Related Item Prefab is missing TextMeshProUGUI component");
                }
            }

            // Initialize Achievement Prefab
            if (achievementPrefab != null)
            {
                var text = achievementPrefab.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null)
                {
                    Debug.LogError("Achievement Prefab is missing TextMeshProUGUI component");
                }
            }

            // Initialize Period Event Prefab
            if (periodEventPrefab != null)
            {
                var texts = periodEventPrefab.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length < 2)
                {
                    Debug.LogError("Period Event Prefab is missing required TextMeshProUGUI components");
                }
            }
        }

        public GameObject GetEventMarkerPrefab() => eventMarkerPrefab;
        public GameObject GetFigureMarkerPrefab() => figureMarkerPrefab;
        public GameObject GetPeriodMarkerPrefab() => periodMarkerPrefab;
        public GameObject GetEventDetailsPrefab() => eventDetailsPrefab;
        public GameObject GetFigureDetailsPrefab() => figureDetailsPrefab;
        public GameObject GetPeriodDetailsPrefab() => periodDetailsPrefab;
        public GameObject GetRelatedItemPrefab() => relatedItemPrefab;
        public GameObject GetAchievementPrefab() => achievementPrefab;
        public GameObject GetPeriodEventPrefab() => periodEventPrefab;
    }
} 