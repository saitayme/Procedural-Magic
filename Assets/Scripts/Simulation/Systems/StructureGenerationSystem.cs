using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using UnityEngine;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial class StructureGenerationSystem : SystemBase
    {
        private EntityQuery _structureQuery;
        private EntityQuery _civilizationQuery;
        private EntityQuery _configQuery;
        private const float STRUCTURE_UPDATE_INTERVAL = 5.0f;
        private float _nextStructureUpdate;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _structureQuery = GetEntityQuery(ComponentType.ReadOnly<StructureData>());
            _civilizationQuery = GetEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            _configQuery = GetEntityQuery(ComponentType.ReadOnly<SimulationConfig>());
            _nextStructureUpdate = 0f;
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (!_configQuery.HasSingleton<SimulationConfig>())
            {
                Debug.Log("[StructureGenerationSystem] No SimulationConfig found");
                return;
            }

            var config = _configQuery.GetSingleton<SimulationConfig>();
            if (!config.EnableResourceSystem)
            {
                Debug.Log("[StructureGenerationSystem] Resource system is disabled");
                return;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            _nextStructureUpdate -= deltaTime;

            if (_nextStructureUpdate <= 0f)
            {
                _nextStructureUpdate = STRUCTURE_UPDATE_INTERVAL;
                Debug.Log("[StructureGenerationSystem] Attempting to update structures");

                // First, create structures for civilizations that don't have any
                var civCount = _civilizationQuery.CalculateEntityCount();
                Debug.Log($"[StructureGenerationSystem] Found {civCount} civilizations");

                if (civCount > 0)
                {
                    var job = new CreateStructuresJob
                    {
                        EntityCommandBuffer = _ecbSystem.CreateCommandBuffer().AsParallelWriter(),
                        Random = Unity.Mathematics.Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime),
                        Biome = Core.BiomeType.Plains // TODO: Use actual biome if available
                    };
                    Dependency = job.ScheduleParallel(_civilizationQuery, Dependency);
                    _ecbSystem.AddJobHandleForProducer(Dependency);
                }

                // Then update existing structures
                var updateJob = new StructureUpdateJob
                {
                    DeltaTime = deltaTime
                };
                Dependency = updateJob.Schedule(_structureQuery, Dependency);
                Dependency.Complete();

                var structCount = _structureQuery.CalculateEntityCount();
                Debug.Log($"[StructureGenerationSystem] Number of structures after update: {structCount}");
            }
        }

        [BurstCompile]
        private partial struct CreateStructuresJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;
            public Unity.Mathematics.Random Random;
            public Core.BiomeType Biome;

            private void Execute([ChunkIndexInQuery] int chunkIndex, in CivilizationData civilization)
            {
                if (!civilization.HasSpawned || !civilization.IsActive)
                    return;

                // Create a central structure for the civilization (city)
                var entity = EntityCommandBuffer.CreateEntity(chunkIndex);
                EntityCommandBuffer.AddComponent(chunkIndex, entity, new StructureData
                {
                    Position = civilization.Position,
                    Type = StructureType.City,
                    Health = 100f,
                    IsActive = true,
                    Owner = civilization.Name
                });
                EntityCommandBuffer.AddComponent<Components.EntityMarker>(chunkIndex, entity);
                EntityCommandBuffer.AddComponent<NameData>(chunkIndex, entity);
                var cityName = ProceduralWorld.Simulation.Utils.NameGenerator.GenerateCityName(
                    civilization.Position, Biome
                );
                EntityCommandBuffer.SetComponent(chunkIndex, entity, new Components.EntityMarker { Label = cityName });
                EntityCommandBuffer.SetComponent(chunkIndex, entity, new NameData {
                    Name = cityName,
                    Position = civilization.Position,
                    Type = NameType.City,
                    Significance = 1.0f,
                    SourceEntityId = entity
                });

                // Create some random structures around the civilization
                for (int i = 0; i < 5; i++)
                {
                    var offset = new float3(
                        Random.NextFloat(-20f, 20f),
                        0,
                        Random.NextFloat(-20f, 20f)
                    );
                    var structureEntity = EntityCommandBuffer.CreateEntity(chunkIndex);
                    var structureType = (StructureType)Random.NextInt(0, 3);
                    EntityCommandBuffer.AddComponent(chunkIndex, structureEntity, new StructureData
                    {
                        Position = civilization.Position + offset,
                        Type = structureType,
                        Health = Random.NextFloat(50f, 100f),
                        IsActive = true,
                        Owner = civilization.Name
                    });
                    EntityCommandBuffer.AddComponent<Components.EntityMarker>(chunkIndex, structureEntity);
                    EntityCommandBuffer.AddComponent<NameData>(chunkIndex, structureEntity);
                    FixedString128Bytes structureName;
                    if (structureType == StructureType.Monument)
                        structureName = ProceduralWorld.Simulation.Utils.NameGenerator.GenerateMonumentName(civilization.Position + offset);
                    else
                        structureName = ProceduralWorld.Simulation.Utils.NameGenerator.GenerateRegionName(civilization.Position + offset, Biome);
                    EntityCommandBuffer.SetComponent(chunkIndex, structureEntity, new Components.EntityMarker { Label = structureName });
                    EntityCommandBuffer.SetComponent(chunkIndex, structureEntity, new NameData {
                        Name = structureName,
                        Position = civilization.Position + offset,
                        Type = structureType == StructureType.Monument ? NameType.Monument : NameType.Structure,
                        Significance = 0.5f,
                        SourceEntityId = structureEntity
                    });

                    Debug.Log($"[StructureGenerationSystem] Spawned structure '{structureName}' of type {structureType} at {civilization.Position + offset}");
                }
            }
        }

        [BurstCompile]
        private partial struct StructureUpdateJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref StructureData structure)
            {
                if (!structure.IsActive)
                    return;

                var x = math.floor(structure.Position.x);
                var z = math.floor(structure.Position.z);
                structure.Position = new float3(x, 0, z);
                structure.Health += DeltaTime;
                Debug.Log($"[StructureGenerationSystem] Updated structure at {structure.Position} with new health {structure.Health}");
            }
        }
    }
} 