using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using Random = Unity.Mathematics.Random;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial class ResourceGenerationSystem : SystemBase
    {
        private EntityQuery _resourceQuery;
        private EntityQuery _configQuery;
        private const float RESOURCE_UPDATE_INTERVAL = 1.0f;
        private float _nextResourceUpdate;

        protected override void OnCreate()
        {
            _resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceData>());
            _configQuery = GetEntityQuery(ComponentType.ReadOnly<SimulationConfig>());
            _nextResourceUpdate = 0f;
        }

        protected override void OnUpdate()
        {
            if (!_configQuery.HasSingleton<SimulationConfig>())
                return;

            var config = _configQuery.GetSingleton<SimulationConfig>();
            if (!config.EnableResourceSystem)
                return;

            var deltaTime = SystemAPI.Time.DeltaTime;
            _nextResourceUpdate -= deltaTime;

            if (_nextResourceUpdate <= 0f)
            {
                _nextResourceUpdate = RESOURCE_UPDATE_INTERVAL;

                var job = new ResourceUpdateJob
                {
                    DeltaTime = deltaTime,
                    RegenerationRate = 0.1f // Default regeneration rate
                };

                Dependency = job.Schedule(_resourceQuery, Dependency);
                Dependency.Complete();
            }
        }

        [BurstCompile]
        private partial struct ResourceUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public float RegenerationRate;

            private void Execute(ref ResourceData resource)
            {
                if (resource.Type == Core.ResourceType.Metal)
                {
                    resource.Amount += RegenerationRate * DeltaTime;
                }
            }
        }
    }
} 