using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using Unity.Physics;
using Unity.Transforms;
using ProceduralWorld.Simulation.Systems;
using ProceduralWorld.Simulation.Utils;
using CoreEventType = ProceduralWorld.Simulation.Core.EventType;
using CoreEventCategory = ProceduralWorld.Simulation.Core.EventCategory;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.UI
{
    public struct CursorData : IComponentData
    {
        public float3 Position;
        public float3 LastPosition;
        public float3 Rotation;
        public bool IsActive;
    }

    [UpdateInGroup(typeof(CoreSimulationGroup))]
    [BurstCompile]
    public partial class CursorSystem : SystemBase
    {
        private EntityQuery _cursorQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private WorldHistorySystem _historySystem;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 0.1f;
        private ComponentTypeHandle<CursorData> _cursorHandle;
        private MathRandom _random;

        protected override void OnCreate()
        {
            _cursorQuery = GetEntityQuery(
                ComponentType.ReadWrite<CursorData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _isInitialized = false;
            _nextUpdate = 0;
            _cursorHandle = GetComponentTypeHandle<CursorData>(false);
            _random = MathRandom.CreateFromIndex(1234);

            Debug.Log("[CursorSystem] System created");
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableCursorSystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            var job = new UpdateCursorJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                Random = _random,
                CursorHandle = _cursorHandle,
                SystemHandle = SystemHandle
            };

            Dependency = job.ScheduleParallel(_cursorQuery, Dependency);
            _random = job.Random;
        }

        [BurstCompile]
        private struct UpdateCursorJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public MathRandom Random;
            public ComponentTypeHandle<CursorData> CursorHandle;
            public SystemHandle SystemHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var cursors = chunk.GetNativeArray(ref CursorHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var cursor = cursors[i];

                    // Update cursor position
                    var position = cursor.Position;
                    position += Config.CursorSpeed * DeltaTime;
                    position = math.clamp(position, 0, Config.MaxPosition);
                    cursor.Position = position;

                    var rotation = cursor.Rotation;
                    rotation += Config.CursorRotationSpeed * DeltaTime;
                    rotation = math.clamp(rotation, 0, Config.MaxRotation);
                    cursor.Rotation = rotation;

                    cursors[i] = cursor;
                }
            }
        }

        private void RecordUserInteraction(ref SystemState state, float3 position)
        {
            if (_historySystem != null)
            {
                var eventRecord = new HistoricalEventRecord
                {
                    Title = "User interaction with cursor",
                    Description = $"User clicked at position {position}",
                    Year = (int)SystemAPI.Time.ElapsedTime,
                    Type = CoreEventType.User,
                    Category = CoreEventCategory.Interaction,
                    Location = position,
                    Significance = 1.0f,
                    SourceEntityId = Entity.Null,
                    Size = 1.0f
                };
                _historySystem.AddEvent(eventRecord);
            }
        }
    }
} 