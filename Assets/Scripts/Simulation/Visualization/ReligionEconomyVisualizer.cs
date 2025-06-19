using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using TMPro;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class ReligionEconomyVisualizer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private GameObject religionMarkerPrefab;
        public GameObject economyMarkerPrefab;
        private EntityManager _entityManager;
        private Dictionary<Entity, GameObject> _markers = new();

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }

        void Update()
        {
            if (_entityManager == null) return;

            // Query for all religions and economies
            var religionQuery = _entityManager.CreateEntityQuery(typeof(ReligionData));
            var economyQuery = _entityManager.CreateEntityQuery(typeof(EconomyData));
            var religions = religionQuery.ToEntityArray(Allocator.Temp);
            var economies = economyQuery.ToEntityArray(Allocator.Temp);
            var allEntities = new List<Entity>(religions.Length + economies.Length);
            allEntities.AddRange(religions);
            allEntities.AddRange(economies);

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
            foreach (var entity in allEntities)
            {
                Vector3 pos = Vector3.zero;
                string label = "";
                GameObject prefab = null;
                if (_entityManager.HasComponent<ReligionData>(entity))
                {
                    var data = _entityManager.GetComponentData<ReligionData>(entity);
                    pos = new Vector3(data.Position.x, data.Position.y, data.Position.z);
                    label = data.Name.ToString();
                    prefab = religionMarkerPrefab;
                }
                else if (_entityManager.HasComponent<EconomyData>(entity))
                {
                    var data = _entityManager.GetComponentData<EconomyData>(entity);
                    pos = new Vector3(data.Location.x, data.Location.y, data.Location.z);
                    label = data.Name.ToString();
                    prefab = economyMarkerPrefab;
                }
                if (prefab == null) continue;
                if (!_markers.ContainsKey(entity))
                {
                    // Create a simple 3D text object instead of using the prefab
                    var marker = new GameObject($"ReligionEconomyMarker_{entity.Index}");
                    marker.transform.position = pos;
                    
                    // Add 3D TextMesh component
                    var textMesh = marker.AddComponent<TextMesh>();
                    textMesh.text = label;
                    textMesh.fontSize = 20;
                    textMesh.color = _entityManager.HasComponent<ReligionData>(entity) ? Color.cyan : Color.yellow;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    
                    // Make it face the camera
                    if (mainCamera != null)
                    {
                        marker.transform.LookAt(marker.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                              mainCamera.transform.rotation * Vector3.up);
                    }
                    
                    _markers[entity] = marker;
                }
                else
                {
                    _markers[entity].transform.position = pos;
                    var textMesh = _markers[entity].GetComponent<TextMesh>();
                    if (textMesh != null) textMesh.text = label;
                }
            }
            religions.Dispose();
            economies.Dispose();
        }
    }
} 