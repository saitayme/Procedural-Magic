using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.UI;
using ProceduralWorld.Simulation.Utils;
using CoreEventCategory = ProceduralWorld.Simulation.Core.EventCategory;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    [BurstCompile]
    public partial class CursorInteractionSystem : SystemBase
    {
        private EntityQuery _terrainQuery;
        private EntityQuery _cursorQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextInteractionUpdate;
        private const float INTERACTION_UPDATE_INTERVAL = 0.1f;
        private NativeList<CursorInteractionData> _pendingInteractions;
        private Unity.Mathematics.Random _random;
        private WorldHistorySystem _historySystem;
        private ComponentTypeHandle<LocalTransform> _transformHandle;
        private ComponentTypeHandle<CursorInteractionData> _interactionHandle;

        protected override void OnCreate()
        {
            _terrainQuery = GetEntityQuery(typeof(WorldTerrainData));
            _cursorQuery = GetEntityQuery(typeof(CursorInteractionData));
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<SimulationConfig>()
            );
            _pendingInteractions = new NativeList<CursorInteractionData>(Allocator.Persistent);
            _isInitialized = false;
            _nextInteractionUpdate = 0;
            _random = Unity.Mathematics.Random.CreateFromIndex(1234);
            _historySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<WorldHistorySystem>();
            _transformHandle = GetComponentTypeHandle<LocalTransform>(true);
            _interactionHandle = GetComponentTypeHandle<CursorInteractionData>();
            Debug.Log("[CursorInteractionSystem] System created");
        }

        protected override void OnDestroy()
        {
            if (_pendingInteractions.IsCreated)
            {
                _pendingInteractions.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<SimulationConfig>();
                if (!config.EnableCursorSystem)
                    return;

                _isInitialized = true;
            }

            if (!SystemAPI.HasSingleton<WorldTerrainData>())
                return;

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextInteractionUpdate)
                return;

            _nextInteractionUpdate = currentTime + INTERACTION_UPDATE_INTERVAL;

            _transformHandle.Update(this);
            _interactionHandle.Update(this);

            var terrain = SystemAPI.GetSingleton<WorldTerrainData>();

            var events = new NativeQueue<HistoricalEventRecord>(Allocator.TempJob);

            var job = new ProcessCursorJob
            {
                CursorHandle = _transformHandle,
                InteractionHandle = _interactionHandle,
                HeightMap = terrain.HeightMap,
                TemperatureMap = terrain.TemperatureMap,
                MoistureMap = terrain.MoistureMap,
                BiomeMap = terrain.BiomeMap,
                Resolution = terrain.Resolution,
                ElapsedTime = currentTime,
                Events = events.AsParallelWriter()
            };

            Dependency = job.ScheduleParallel(_cursorQuery, Dependency);
            Dependency.Complete();

            // Add events to history system on main thread
            while (events.TryDequeue(out var evt))
            {
                _historySystem.AddEvent(evt);
            }
            events.Dispose();
        }

        private void CreateInteractionEvent(float3 position, Entity targetEntity)
        {
            var eventRecord = new HistoricalEventRecord
            {
                Title = "User interaction with cursor",
                Description = $"User clicked at position {position}",
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.User,
                Category = CoreEventCategory.Interaction,
                Location = position,
                Significance = 1.0f,
                SourceEntityId = Entity.Null,
                Size = 1.0f
            };

            _historySystem.AddEvent(eventRecord);
        }

        [BurstCompile]
        private struct ProcessCursorJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<LocalTransform> CursorHandle;
            [ReadOnly] public ComponentTypeHandle<CursorInteractionData> InteractionHandle;
            [ReadOnly] public NativeArray<float> HeightMap;
            [ReadOnly] public NativeArray<float> TemperatureMap;
            [ReadOnly] public NativeArray<float> MoistureMap;
            [ReadOnly] public NativeArray<Core.BiomeType> BiomeMap;
            [ReadOnly] public int Resolution;
            [ReadOnly] public float ElapsedTime;
            public NativeQueue<HistoricalEventRecord>.ParallelWriter Events;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var cursors = chunk.GetNativeArray(ref CursorHandle);
                var interactions = chunk.GetNativeArray(ref InteractionHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var cursor = cursors[i];
                    // You may want to use a real interaction check here
                    if (cursor.Position.y > 0)
                    {
                        var position = cursor.Position;
                        int idx = GetIndexFromPosition(position, Resolution);
                        if (idx < 0 || idx >= HeightMap.Length) continue;
                        var biomeType = BiomeMap[idx];
                        var height = HeightMap[idx];
                        var temperature = TemperatureMap[idx];
                        var humidity = MoistureMap[idx];

                        var eventRecord = new HistoricalEventRecord
                        {
                            Title = "Terrain Interaction",
                            Description = $"Interacted with terrain at position {position}",
                            Year = (int)ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.User,
                            Category = CoreEventCategory.Interaction,
                            Location = position,
                            Significance = 1.0f,
                            SourceEntityId = Entity.Null,
                            Size = 1.0f
                        };
                        Events.Enqueue(eventRecord);
                    }
                }
            }

            private int GetIndexFromPosition(float3 position, int resolution)
            {
                var x = (int)((position.x + 0.5f) * resolution);
                var z = (int)((position.z + 0.5f) * resolution);
                if (x < 0 || x >= resolution || z < 0 || z >= resolution) return -1;
                return z * resolution + x;
            }
        }
    }
} 