using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using System.Collections.Generic;
using ProceduralWorld.Simulation.Systems;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using Unity.Entities;
using Unity.Collections;

namespace ProceduralWorld.Simulation.UI
{
    public class WorldNameDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject nameLabelPrefab;
        [SerializeField] private Canvas worldCanvas;
        [Header("Camera Settings")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private float labelScale = 1f;
        [SerializeField] private float minDistance = 10f;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float fadeStartDistance = 80f;

        [Header("Name Display Settings")]
        [SerializeField] private bool showBiomeNames = true;
        [SerializeField] private bool showMountainNames = true;
        [SerializeField] private bool showRiverNames = true;
        [SerializeField] private bool showForestNames = true;
        [SerializeField] private bool showDesertNames = true;
        [SerializeField] private bool showOceanNames = true;
        [SerializeField] private bool showCityNames = true;
        [SerializeField] private bool showMonumentNames = true;
        [SerializeField] private bool showStructureNames = true;
        [SerializeField] private bool showRuinNames = true;

        private EntityQuery worldNameQuery;
        private EntityQuery civilizationQuery;
        private Dictionary<int2, GameObject> _nameLabels;
        private List<GameObject> _activeLabels;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                worldNameQuery = world.EntityManager.CreateEntityQuery(typeof(WorldName));
                civilizationQuery = world.EntityManager.CreateEntityQuery(typeof(CivilizationData));
            }
            _nameLabels = new Dictionary<int2, GameObject>();
            _activeLabels = new List<GameObject>();
            InitializeNameLabels();
        }

        private void Update()
        {
            UpdateNameLabels();
        }

        private void InitializeNameLabels()
        {
            // Create name labels for all named locations
            if (showBiomeNames)
                CreateBiomeLabels();
            if (showMountainNames)
                CreateMountainLabels();
            if (showRiverNames)
                CreateRiverLabels();
            if (showForestNames)
                CreateForestLabels();
            if (showDesertNames)
                CreateDesertLabels();
            if (showOceanNames)
                CreateOceanLabels();
            if (showCityNames)
                CreateCityLabels();
            if (showMonumentNames)
                CreateMonumentLabels();
            if (showStructureNames)
                CreateStructureLabels();
            if (showRuinNames)
                CreateRuinLabels();
        }

        private void CreateBiomeLabels()
        {
            // Implementation to create labels for biomes
        }

        private void CreateMountainLabels()
        {
            // Implementation to create labels for mountains
        }

        private void CreateRiverLabels()
        {
            // Implementation to create labels for rivers
        }

        private void CreateForestLabels()
        {
            // Implementation to create labels for forests
        }

        private void CreateDesertLabels()
        {
            // Implementation to create labels for deserts
        }

        private void CreateOceanLabels()
        {
            // Implementation to create labels for oceans
        }

        private void CreateCityLabels()
        {
            // Implementation to create labels for cities
        }

        private void CreateMonumentLabels()
        {
            // Implementation to create labels for monuments
        }

        private void CreateStructureLabels()
        {
            // Implementation to create labels for structures
        }

        private void CreateRuinLabels()
        {
            // Implementation to create labels for ruins
        }

        private void UpdateNameLabels()
        {
            var cameraPosition = mainCamera.transform.position;
            _activeLabels.Clear();

            foreach (var kvp in _nameLabels)
            {
                var position = new Vector3(kvp.Key.x, 0, kvp.Key.y);
                var distance = Vector3.Distance(cameraPosition, position);

                if (distance < minDistance || distance > maxDistance)
                {
                    kvp.Value.SetActive(false);
                    continue;
                }

                kvp.Value.SetActive(true);
                _activeLabels.Add(kvp.Value);

                // Update position
                var screenPoint = mainCamera.WorldToScreenPoint(position);
                kvp.Value.transform.position = screenPoint;

                // Update scale based on distance
                var scale = Mathf.Lerp(labelScale, labelScale * 0.5f, (distance - minDistance) / (maxDistance - minDistance));
                kvp.Value.transform.localScale = Vector3.one * scale;

                // Update alpha based on distance
                var alpha = 1f;
                if (distance > fadeStartDistance)
                {
                    alpha = Mathf.Lerp(1f, 0f, (distance - fadeStartDistance) / (maxDistance - fadeStartDistance));
                }
                var canvasGroup = kvp.Value.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
            }
        }

        private GameObject CreateNameLabel(string name, Vector3 position)
        {
            var label = Instantiate(nameLabelPrefab, worldCanvas.transform);
            label.GetComponentInChildren<TextMeshProUGUI>().text = name;
            label.transform.position = mainCamera.WorldToScreenPoint(position);
            label.transform.localScale = Vector3.one * labelScale;
            return label;
        }

        public void ToggleBiomeNames(bool show) => showBiomeNames = show;
        public void ToggleMountainNames(bool show) => showMountainNames = show;
        public void ToggleRiverNames(bool show) => showRiverNames = show;
        public void ToggleForestNames(bool show) => showForestNames = show;
        public void ToggleDesertNames(bool show) => showDesertNames = show;
        public void ToggleOceanNames(bool show) => showOceanNames = show;
        public void ToggleCityNames(bool show) => showCityNames = show;
        public void ToggleMonumentNames(bool show) => showMonumentNames = show;
        public void ToggleStructureNames(bool show) => showStructureNames = show;
        public void ToggleRuinNames(bool show) => showRuinNames = show;

        // DISABLED: Now managed by SimulationUIManager
        /*
        void OnGUI()
        {
            if (!worldNameQuery.HasSingleton<WorldName>())
                return;

            var worldName = worldNameQuery.GetSingleton<WorldName>();
            var civilizations = civilizationQuery.ToEntityArray(Allocator.Temp);

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"World Name: {worldName.Value}");
            GUILayout.Label($"Civilizations: {civilizations.Length}");
            GUILayout.EndArea();

            civilizations.Dispose();
        }
        */
    }
} 