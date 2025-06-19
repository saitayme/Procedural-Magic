using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using UnityEngine;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct EconomyManagementSystem : ISystem
    {
        private EntityQuery _economyQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 1.0f;
        private ComponentTypeHandle<EconomyData> _economyHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _economyQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<EconomyData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _isInitialized = false;
            _nextUpdate = 0;
            _economyHandle = state.GetComponentTypeHandle<EconomyData>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableEconomySystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _economyHandle.Update(ref state);

            var job = new UpdateEconomyJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                EconomyHandle = _economyHandle
            };

            state.Dependency = job.ScheduleParallel(_economyQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateEconomyJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<EconomyData> EconomyHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var economies = chunk.GetNativeArray(ref EconomyHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var economy = economies[i];

                    // Update economy values
                    economy.Wealth = math.min(economy.Wealth + Config.EconomyGrowthRate * DeltaTime, Config.MaxWealth);
                    economy.Technology = math.min(economy.Technology + Config.EconomyGrowthRate * DeltaTime, Config.MaxTechnology);
                    economy.Trade = math.min(economy.Trade + Config.EconomyGrowthRate * DeltaTime, Config.MaxTrade);
                    economy.Production = math.min(economy.Production + Config.EconomyGrowthRate * DeltaTime, Config.MaxProduction);
                    economy.Population = math.min(economy.Population + Config.EconomyGrowthRate * DeltaTime, Config.MaxPopulation);

                    economies[i] = economy;
                }
            }
        }
    }
} 