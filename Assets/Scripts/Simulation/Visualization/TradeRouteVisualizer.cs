using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class TradeRouteVisualizer : MonoBehaviour
    {
        public Material lineMaterial;
        private EntityManager _entityManager;
        private List<LineRenderer> _lines = new();

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            if (_entityManager == null || lineMaterial == null) return;

            // Remove old lines
            foreach (var line in _lines)
                Destroy(line.gameObject);
            _lines.Clear();

            var tradeRouteQuery = _entityManager.CreateEntityQuery(typeof(TradeRouteData));
            var tradeRoutes = tradeRouteQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var routeEntity in tradeRoutes)
            {
                var route = _entityManager.GetComponentData<TradeRouteData>(routeEntity);
                
                // Check if both markets exist and get their positions
                if (!_entityManager.HasComponent<MarketData>(route.SourceMarket) || 
                    !_entityManager.HasComponent<MarketData>(route.DestinationMarket))
                    continue;

                var sourceMarket = _entityManager.GetComponentData<MarketData>(route.SourceMarket);
                var destMarket = _entityManager.GetComponentData<MarketData>(route.DestinationMarket);

                var go = new GameObject($"TradeRouteLine_{routeEntity.Index}");
                var lr = go.AddComponent<LineRenderer>();
                lr.material = lineMaterial;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(sourceMarket.Position.x, sourceMarket.Position.y, sourceMarket.Position.z));
                lr.SetPosition(1, new Vector3(destMarket.Position.x, destMarket.Position.y, destMarket.Position.z));
                lr.widthMultiplier = 0.2f;
                _lines.Add(lr);
            }

            tradeRoutes.Dispose();
        }
    }
} 