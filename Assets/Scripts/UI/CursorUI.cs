using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.UI
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CursorInteractionSystem : ISystem
    {
        private EntityQuery _hoverQuery;
        private EntityQuery _interactionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _hoverQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EntityReference>()
            );

            _interactionQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EntityReference>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ProcessCursorJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.Schedule(_hoverQuery, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        private partial struct ProcessCursorJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(in EntityReference entityRef)
            {
                // Process cursor interactions
                if (entityRef.Entity != Entity.Null)
                {
                    // Handle hover and interaction logic
                }
            }
        }
    }
} 