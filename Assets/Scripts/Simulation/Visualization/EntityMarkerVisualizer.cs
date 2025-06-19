using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using TMPro;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class EntityMarkerVisualizer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private GameObject markerPrefab;
        private EntityManager _entityManager;
        private Dictionary<Entity, GameObject> _markers = new();
        
        // Prevent flickering by updating less frequently
        private float _lastUpdate = 0f;
        private const float UPDATE_INTERVAL = 1f; // Update every 1 second instead of every frame

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }

        void Update()
        {
            if (_entityManager == null) return;
            
            // Only update periodically to prevent flickering
            if (Time.time - _lastUpdate < UPDATE_INTERVAL) return;
            _lastUpdate = Time.time;

            // Query for all civilizations and structures
            var civQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            var structQuery = _entityManager.CreateEntityQuery(typeof(StructureData));
            var civs = civQuery.ToEntityArray(Allocator.Temp);
            var structs = structQuery.ToEntityArray(Allocator.Temp);
            var allEntities = new List<Entity>(civs.Length + structs.Length);
            allEntities.AddRange(civs);
            allEntities.AddRange(structs);
            // Removed excessive logging to prevent console spam

            // Remove markers for destroyed entities
            var toRemove = new List<Entity>();
            foreach (var kvp in _markers)
            {
                if (!allEntities.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var e in toRemove) _markers.Remove(e);

            // Add/update markers
            int created = 0;
            foreach (var entity in allEntities)
            {
                Vector3 pos = Vector3.zero;
                string label = "";
                if (_entityManager.HasComponent<CivilizationData>(entity))
                {
                    var data = _entityManager.GetComponentData<CivilizationData>(entity);
                    pos = new Vector3(data.Position.x, data.Position.y, data.Position.z);
                    label = data.Name.ToString();
                }
                else if (_entityManager.HasComponent<StructureData>(entity))
                {
                    var data = _entityManager.GetComponentData<StructureData>(entity);
                    pos = new Vector3(data.Position.x, data.Position.y, data.Position.z);
                    label = $"Structure {entity.Index}";
                }
                // Future: Add religion/economy markers here
                if (!_markers.ContainsKey(entity))
                {
                    // Create a simple 3D text object instead of using the prefab
                    var marker = new GameObject($"Marker_{entity.Index}");
                    marker.transform.position = pos;
                    
                    // Add 3D TextMesh component
                    var textMesh = marker.AddComponent<TextMesh>();
                    textMesh.text = label;
                    textMesh.fontSize = 30;
                    textMesh.color = _entityManager.HasComponent<CivilizationData>(entity) ? Color.white : Color.green;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    
                    // Make it face the camera
                    if (mainCamera != null)
                    {
                        marker.transform.LookAt(marker.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                              mainCamera.transform.rotation * Vector3.up);
                    }
                    
                    _markers[entity] = marker;
                    created++;
                }
                else
                {
                    _markers[entity].transform.position = pos;
                    var text = _markers[entity].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text != null) text.text = label;
                }
            }
            civs.Dispose();
            structs.Dispose();
        }

        private void UpdateMarkerVisibility()
        {
            if (mainCamera == null) return;

            UnityEngine.Camera cam = mainCamera;
        }
    }

    // Helper component to make labels face the camera
    public class FaceCamera : MonoBehaviour
    {
        public UnityEngine.Camera mainCamera;
        
        void Start()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }
        
        void LateUpdate()
        {
            if (mainCamera != null)
            {
                // Make the canvas face the camera
                var canvas = GetComponentInChildren<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.transform.LookAt(canvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                          mainCamera.transform.rotation * Vector3.up);
                }
            }
        }
    }
} 