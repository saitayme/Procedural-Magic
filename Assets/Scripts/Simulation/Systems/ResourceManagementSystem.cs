using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using UnityEngine;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct ResourceManagementSystem : ISystem
    {
        private EntityQuery _resourceQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 1.0f;
        private ComponentTypeHandle<ResourceData> _resourceHandle;
        private MathRandom _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _resourceQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<ResourceData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _isInitialized = false;
            _nextUpdate = 0;
            _resourceHandle = state.GetComponentTypeHandle<ResourceData>(false);
            _random = MathRandom.CreateFromIndex(1234);

            Debug.Log("[ResourceManagementSystem] System created");
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableResourceSystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _resourceHandle.Update(ref state);

            var job = new UpdateResourceJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                ResourceHandle = _resourceHandle
            };

            state.Dependency = job.ScheduleParallel(_resourceQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateResourceJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<ResourceData> ResourceHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var resources = chunk.GetNativeArray(ref ResourceHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var resource = resources[i];

                    // Update resource values
                    resource.Amount = math.min(resource.Amount + Config.ResourceGrowthRate * DeltaTime, Config.MaxResourceAmount);
                    resource.Value = math.min(resource.Value + Config.ResourceValueRate * DeltaTime, Config.MaxResourceValue);

                    resources[i] = resource;
                }
            }
        }
    }
} 