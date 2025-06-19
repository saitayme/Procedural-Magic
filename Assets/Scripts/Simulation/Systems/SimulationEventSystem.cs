using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst.Intrinsics;
using UnityEngine;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct SimulationEventSystem : ISystem
    {
        private bool _isInitialized;
        private EntityQuery _eventQuery;
        private EntityQuery _eventStateQuery;

        public void OnCreate(ref SystemState state)
        {
            Debug.Log("[SimulationEventSystem] System created");
            state.RequireForUpdate<EventProcessingState>();
            
            _eventQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<HistoricalEventRecord>(),
                ComponentType.ReadWrite<EventProcessingState>()
            );
            
            _eventStateQuery = state.GetEntityQuery(ComponentType.ReadWrite<EventProcessingState>());
        }

        public void OnDestroy(ref SystemState state)
        {
            // Cleanup if needed
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                Debug.Log("[SimulationEventSystem] Initializing event system");
                var initialEventState = state.EntityManager.GetComponentData<EventProcessingState>(_eventStateQuery.GetSingletonEntity());
                initialEventState.IsDirty = true;
                state.EntityManager.SetComponentData(_eventStateQuery.GetSingletonEntity(), initialEventState);
                _isInitialized = true;
                return;
            }

            var currentEventState = state.EntityManager.GetComponentData<EventProcessingState>(_eventStateQuery.GetSingletonEntity());
            if (!currentEventState.IsDirty)
                return;

            Debug.Log("[SimulationEventSystem] Processing events");

            // Process events in parallel
            var job = new ProcessEventsJob
            {
                EventType = state.GetComponentTypeHandle<HistoricalEventRecord>(true),
                EventStateType = state.GetComponentTypeHandle<EventProcessingState>(false)
            };

            job.ScheduleParallel(_eventQuery, state.Dependency).Complete();

            // Mark events as processed
            currentEventState.IsDirty = false;
            state.EntityManager.SetComponentData(_eventStateQuery.GetSingletonEntity(), currentEventState);

            Debug.Log("[SimulationEventSystem] Events processed");
        }
    }

    [BurstCompile]
    public partial struct ProcessEventsJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<HistoricalEventRecord> EventType;
        public ComponentTypeHandle<EventProcessingState> EventStateType;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var events = chunk.GetNativeArray(ref EventType);
            var eventStates = chunk.GetNativeArray(ref EventStateType);

            for (int i = 0; i < chunk.Count; i++)
            {
                var eventState = eventStates[i];
                eventState.IsProcessed = true;
                eventStates[i] = eventState;
            }
        }
    }
} 