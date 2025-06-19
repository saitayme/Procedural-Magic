using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    [BurstCompile]
    public partial class ReligionManagementSystem : SystemBase
    {
        private EntityQuery _religionQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextReligionUpdate;
        private const float RELIGION_UPDATE_INTERVAL = 1.0f;

        protected override void OnCreate()
        {
            _religionQuery = GetEntityQuery(typeof(ReligionData));
            _configQuery = GetEntityQuery(typeof(SimulationConfig));
            _isInitialized = false;
            _nextReligionUpdate = 0;
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<SimulationConfig>();
                if (!config.EnableReligionSystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextReligionUpdate)
                return;

            _nextReligionUpdate = currentTime + RELIGION_UPDATE_INTERVAL;

            var job = new UpdateReligionsJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            Dependency = job.ScheduleParallel(_religionQuery, Dependency);
        }

        [BurstCompile]
        private partial struct UpdateReligionsJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref ReligionData religion)
            {
                // Update religion values here
                religion.Influence = math.clamp(religion.Influence + religion.Growth * DeltaTime - religion.Decline * DeltaTime, 0f, 1f);
                religion.Stability = math.clamp(religion.Stability + (religion.Influence * 0.1f - religion.Decline * 0.2f) * DeltaTime, 0f, 1f);
            }
        }
    }
} 