using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(Core.SimulationSystemGroup))]
    public partial class TerritorySpawnSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _territoryQuery;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        private Unity.Mathematics.Random _random;
        private bool _hasSpawnedInitialTerritories = false;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            _territoryQuery = GetEntityQuery(ComponentType.ReadOnly<TerritoryData>());
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _random = Unity.Mathematics.Random.CreateFromIndex(5678);
        }

        protected override void OnUpdate()
        {
            // Only run once to create initial territories for existing civilizations
            if (_hasSpawnedInitialTerritories)
                return;

            var civilizations = _civilizationQuery.ToEntityArray(Allocator.Temp);
            var civDataList = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var territories = _territoryQuery.ToComponentDataArray<TerritoryData>(Allocator.Temp);

            if (civilizations.Length == 0)
            {
                civilizations.Dispose();
                civDataList.Dispose();
                territories.Dispose();
                return;
            }

            var ecb = _ecbSystem.CreateCommandBuffer();

            // For each civilization, check if they have any territories
            for (int i = 0; i < civilizations.Length; i++)
            {
                var civ = civDataList[i];
                var civEntity = civilizations[i];
                
                // Check if this civilization already has territories
                bool hasTerritory = false;
                for (int t = 0; t < territories.Length; t++)
                {
                    if (territories[t].OwnerCivilization == civEntity)
                    {
                        hasTerritory = true;
                        break;
                    }
                }

                // If no territory, create a capital city
                if (!hasTerritory && civ.Population > 0)
                {
                    var capitalEntity = ecb.CreateEntity();
                    ecb.AddComponent(capitalEntity, new TerritoryData
                    {
                        OwnerCivilization = civEntity,
                        Position = civ.Position,
                        ControlRadius = 60f,
                        TerritoryName = new FixedString128Bytes($"{civ.Name} Capital"),
                        Type = TerritoryType.City,
                        DefenseStrength = civ.Military * 0.8f,
                        Population = civ.Population,
                        Wealth = civ.Wealth,
                        IsRuined = false
                    });
                }
            }

            _hasSpawnedInitialTerritories = true;

            civilizations.Dispose();
            civDataList.Dispose();
            territories.Dispose();
        }
    }
} 