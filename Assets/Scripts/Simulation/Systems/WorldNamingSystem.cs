using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    public partial class WorldNamingSystem : SystemBase
    {
        private EntityQuery _terrainQuery;
        private EntityQuery _civilizationQuery;
        private EntityQuery _resourceQuery;
        private EntityQuery _structureQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextNamingUpdate;
        private const float NAMING_UPDATE_INTERVAL = 5.0f;
        private Dictionary<int2, FixedString128Bytes> _biomeNames;
        private Dictionary<int2, FixedString128Bytes> _mountainNames;
        private Dictionary<int2, FixedString128Bytes> _riverNames;
        private Dictionary<int2, FixedString128Bytes> _forestNames;
        private Dictionary<int2, FixedString128Bytes> _desertNames;
        private Dictionary<int2, FixedString128Bytes> _oceanNames;
        private Dictionary<int2, FixedString128Bytes> _cityNames;
        private Dictionary<int2, FixedString128Bytes> _monumentNames;
        private Dictionary<int2, FixedString128Bytes> _structureNames;
        private Dictionary<int2, FixedString128Bytes> _ruinNames;
        private NativeList<NameData> _pendingNames;
        private Unity.Mathematics.Random _random;
        private FixedString128Bytes[] _forestNamesArray;
        private FixedString128Bytes[] _mountainNamesArray;
        private FixedString128Bytes[] _desertNamesArray;
        private FixedString128Bytes[] _tundraNamesArray;
        private FixedString128Bytes[] _swampNamesArray;
        private FixedString128Bytes[] _jungleNamesArray;
        private FixedString128Bytes[] _oceanNamesArray;
        private FixedString128Bytes[] _riverNamesArray;
        private FixedString128Bytes[] _lakeNamesArray;
        private FixedString128Bytes[] _plainsNamesArray;
        private NativeArray<FixedString128Bytes> _forestNamesArrayNative;
        private NativeArray<FixedString128Bytes> _mountainNamesArrayNative;
        private NativeArray<FixedString128Bytes> _desertNamesArrayNative;
        private NativeArray<FixedString128Bytes> _tundraNamesArrayNative;
        private NativeArray<FixedString128Bytes> _swampNamesArrayNative;
        private NativeArray<FixedString128Bytes> _jungleNamesArrayNative;
        private NativeArray<FixedString128Bytes> _oceanNamesArrayNative;
        private NativeArray<FixedString128Bytes> _riverNamesArrayNative;
        private NativeArray<FixedString128Bytes> _lakeNamesArrayNative;
        private NativeArray<FixedString128Bytes> _plainsNamesArrayNative;

        protected override void OnCreate()
        {
            _terrainQuery = GetEntityQuery(typeof(WorldTerrainData), typeof(NameData));
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadOnly<SimulationConfig>()
            );
            _resourceQuery = GetEntityQuery(typeof(ResourceData));
            _structureQuery = GetEntityQuery(typeof(StructureData));
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<SimulationConfig>()
            );
            _pendingNames = new NativeList<NameData>(Allocator.Persistent);
            _isInitialized = false;
            _nextNamingUpdate = 0;
            InitializeNameDictionaries();
            _random = Unity.Mathematics.Random.CreateFromIndex(1234);
            Debug.Log("[WorldNamingSystem] System created");

            // Initialize name arrays
            _forestNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Ancient Woods"),
                new FixedString128Bytes("Eternal Forest"),
                new FixedString128Bytes("Whispering Pines"),
                new FixedString128Bytes("Mystic Grove"),
                new FixedString128Bytes("Shadow Woods")
            };
            _forestNamesArrayNative = new NativeArray<FixedString128Bytes>(_forestNamesArray, Allocator.Persistent);

            _mountainNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Mighty Peaks"),
                new FixedString128Bytes("Thunder Mountains"),
                new FixedString128Bytes("Crystal Range"),
                new FixedString128Bytes("Dragon's Spine"),
                new FixedString128Bytes("Frost Giants")
            };
            _mountainNamesArrayNative = new NativeArray<FixedString128Bytes>(_mountainNamesArray, Allocator.Persistent);

            _desertNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Golden Sands"),
                new FixedString128Bytes("Eternal Dunes"),
                new FixedString128Bytes("Mirage Wastes"),
                new FixedString128Bytes("Sun's Embrace"),
                new FixedString128Bytes("Dust Plains")
            };
            _desertNamesArrayNative = new NativeArray<FixedString128Bytes>(_desertNamesArray, Allocator.Persistent);

            _tundraNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Frozen Wastes"),
                new FixedString128Bytes("Ice Plains"),
                new FixedString128Bytes("Arctic Fields"),
                new FixedString128Bytes("Frost Lands"),
                new FixedString128Bytes("Winter's Edge")
            };
            _tundraNamesArrayNative = new NativeArray<FixedString128Bytes>(_tundraNamesArray, Allocator.Persistent);

            _swampNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Misty Marshes"),
                new FixedString128Bytes("Bog of Shadows"),
                new FixedString128Bytes("Mire of Secrets"),
                new FixedString128Bytes("Fen of Whispers"),
                new FixedString128Bytes("Marsh of Dreams")
            };
            _swampNamesArrayNative = new NativeArray<FixedString128Bytes>(_swampNamesArray, Allocator.Persistent);

            _jungleNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Emerald Canopy"),
                new FixedString128Bytes("Wild Vines"),
                new FixedString128Bytes("Ancient Jungle"),
                new FixedString128Bytes("Primal Forest"),
                new FixedString128Bytes("Verdant Maze")
            };
            _jungleNamesArrayNative = new NativeArray<FixedString128Bytes>(_jungleNamesArray, Allocator.Persistent);

            _oceanNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Azure Depths"),
                new FixedString128Bytes("Sapphire Sea"),
                new FixedString128Bytes("Crystal Waters"),
                new FixedString128Bytes("Deep Blue"),
                new FixedString128Bytes("Ocean's Heart")
            };
            _oceanNamesArrayNative = new NativeArray<FixedString128Bytes>(_oceanNamesArray, Allocator.Persistent);

            _riverNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Silver Stream"),
                new FixedString128Bytes("Crystal Flow"),
                new FixedString128Bytes("River of Life"),
                new FixedString128Bytes("Ancient Current"),
                new FixedString128Bytes("Eternal Waters")
            };
            _riverNamesArrayNative = new NativeArray<FixedString128Bytes>(_riverNamesArray, Allocator.Persistent);

            _lakeNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Mirror Lake"),
                new FixedString128Bytes("Crystal Pool"),
                new FixedString128Bytes("Serene Waters"),
                new FixedString128Bytes("Mystic Basin"),
                new FixedString128Bytes("Tranquil Depths")
            };
            _lakeNamesArrayNative = new NativeArray<FixedString128Bytes>(_lakeNamesArray, Allocator.Persistent);

            _plainsNamesArray = new FixedString128Bytes[]
            {
                new FixedString128Bytes("Golden Fields"),
                new FixedString128Bytes("Endless Plains"),
                new FixedString128Bytes("Vast Meadows"),
                new FixedString128Bytes("Grassland Expanse"),
                new FixedString128Bytes("Open Prairie")
            };
            _plainsNamesArrayNative = new NativeArray<FixedString128Bytes>(_plainsNamesArray, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_pendingNames.IsCreated)
            {
                _pendingNames.Dispose();
            }
            if (_forestNamesArrayNative.IsCreated) _forestNamesArrayNative.Dispose();
            if (_mountainNamesArrayNative.IsCreated) _mountainNamesArrayNative.Dispose();
            if (_desertNamesArrayNative.IsCreated) _desertNamesArrayNative.Dispose();
            if (_tundraNamesArrayNative.IsCreated) _tundraNamesArrayNative.Dispose();
            if (_swampNamesArrayNative.IsCreated) _swampNamesArrayNative.Dispose();
            if (_jungleNamesArrayNative.IsCreated) _jungleNamesArrayNative.Dispose();
            if (_oceanNamesArrayNative.IsCreated) _oceanNamesArrayNative.Dispose();
            if (_riverNamesArrayNative.IsCreated) _riverNamesArrayNative.Dispose();
            if (_lakeNamesArrayNative.IsCreated) _lakeNamesArrayNative.Dispose();
            if (_plainsNamesArrayNative.IsCreated) _plainsNamesArrayNative.Dispose();
        }

        private void InitializeNameDictionaries()
        {
            _biomeNames = new Dictionary<int2, FixedString128Bytes>();
            _mountainNames = new Dictionary<int2, FixedString128Bytes>();
            _riverNames = new Dictionary<int2, FixedString128Bytes>();
            _forestNames = new Dictionary<int2, FixedString128Bytes>();
            _desertNames = new Dictionary<int2, FixedString128Bytes>();
            _oceanNames = new Dictionary<int2, FixedString128Bytes>();
            _cityNames = new Dictionary<int2, FixedString128Bytes>();
            _monumentNames = new Dictionary<int2, FixedString128Bytes>();
            _structureNames = new Dictionary<int2, FixedString128Bytes>();
            _ruinNames = new Dictionary<int2, FixedString128Bytes>();
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<SimulationConfig>();
                if (!config.EnableWorldNamingSystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextNamingUpdate)
                return;

            _nextNamingUpdate = currentTime + NAMING_UPDATE_INTERVAL;

            var job = new GenerateNamesJob
            {
                NameHandle = GetComponentTypeHandle<NameData>(),
                TerrainHandle = GetComponentTypeHandle<WorldTerrainData>(true),
                ForestNames = _forestNamesArrayNative,
                MountainNames = _mountainNamesArrayNative,
                DesertNames = _desertNamesArrayNative,
                TundraNames = _tundraNamesArrayNative,
                SwampNames = _swampNamesArrayNative,
                JungleNames = _jungleNamesArrayNative,
                OceanNames = _oceanNamesArrayNative,
                RiverNames = _riverNamesArrayNative,
                LakeNames = _lakeNamesArrayNative,
                PlainsNames = _plainsNamesArrayNative
            };

            Dependency = job.ScheduleParallel(_terrainQuery, Dependency);
        }

        [BurstCompile]
        private struct GenerateNamesJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<NameData> NameHandle;
            [ReadOnly] public ComponentTypeHandle<WorldTerrainData> TerrainHandle;
            [ReadOnly] public NativeArray<FixedString128Bytes> ForestNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> MountainNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> DesertNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> TundraNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> SwampNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> JungleNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> OceanNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> RiverNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> LakeNames;
            [ReadOnly] public NativeArray<FixedString128Bytes> PlainsNames;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var names = chunk.GetNativeArray(ref NameHandle);
                var terrains = chunk.GetNativeArray(ref TerrainHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var nameData = names[i];
                    var terrain = terrains[i];

                    // Generate a seed based on the chunk index and entity index
                    var seed = (uint)(unfilteredChunkIndex * 1000 + i);
                    var random = Unity.Mathematics.Random.CreateFromIndex(seed);
                    
                    // Generate biome-specific names
                    nameData.BiomeName = GenerateBiomeName(terrain.BiomeMap[0], random);
                    nameData.MountainName = GenerateMountainName(random);
                    nameData.RiverName = GenerateRiverName(random);
                    nameData.ForestName = GenerateForestName(random);
                    nameData.DesertName = GenerateDesertName(random);
                    nameData.OceanName = GenerateOceanName(random);
                    
                    names[i] = nameData;
                }
            }

            private FixedString128Bytes GenerateBiomeName(Core.BiomeType biome, Unity.Mathematics.Random random)
            {
                switch (biome)
                {
                    case Core.BiomeType.Forest:
                        return ForestNames[random.NextInt(0, ForestNames.Length)];
                    case Core.BiomeType.Mountains:
                        return MountainNames[random.NextInt(0, MountainNames.Length)];
                    case Core.BiomeType.Desert:
                        return DesertNames[random.NextInt(0, DesertNames.Length)];
                    case Core.BiomeType.Tundra:
                        return TundraNames[random.NextInt(0, TundraNames.Length)];
                    case Core.BiomeType.Swamp:
                        return SwampNames[random.NextInt(0, SwampNames.Length)];
                    case Core.BiomeType.Jungle:
                        return JungleNames[random.NextInt(0, JungleNames.Length)];
                    case Core.BiomeType.Ocean:
                        return OceanNames[random.NextInt(0, OceanNames.Length)];
                    case Core.BiomeType.Grassland:
                        return PlainsNames[random.NextInt(0, PlainsNames.Length)];
                    default:
                        return new FixedString128Bytes("Unknown Region");
                }
            }

            private FixedString128Bytes GenerateMountainName(Unity.Mathematics.Random random)
            {
                return MountainNames[random.NextInt(0, MountainNames.Length)];
            }

            private FixedString128Bytes GenerateRiverName(Unity.Mathematics.Random random)
            {
                return RiverNames[random.NextInt(0, RiverNames.Length)];
            }

            private FixedString128Bytes GenerateForestName(Unity.Mathematics.Random random)
            {
                return ForestNames[random.NextInt(0, ForestNames.Length)];
            }

            private FixedString128Bytes GenerateDesertName(Unity.Mathematics.Random random)
            {
                return DesertNames[random.NextInt(0, DesertNames.Length)];
            }

            private FixedString128Bytes GenerateOceanName(Unity.Mathematics.Random random)
            {
                return OceanNames[random.NextInt(0, OceanNames.Length)];
            }
        }

        // PUBLIC: Generate a civilization name based on biome and position
        public static FixedString128Bytes GenerateCivilizationName(Core.BiomeType biome, float3 position)
        {
            // Use a static random for now (could be improved for determinism)
            var seed = (uint)(((int)position.x * 73856093) ^ ((int)position.y * 19349663) ^ ((int)position.z * 83492791));
            var random = Unity.Mathematics.Random.CreateFromIndex(seed);
            string[] prefixes = { "New", "Old", "Great", "Grand", "Lost", "Ancient", "Free", "Holy", "Royal", "Northern", "Southern", "Eastern", "Western" };
            string[] civSuffixes = { "ia", "land", "ton", "grad", "polis", "stan", "vale", "heim", "mark", "dell", "mere", "hold", "reach", "shire", "port", "gate", "crest", "ford", "burg", "stead", "field", "ridge", "cliff", "bay", "coast", "plain", "wood", "grove", "peak", "fall", "rock", "point", "hollow", "moor", "wold", "barrow", "fen", "marsh", "waste", "wilds" };
            string biomeRoot = biome switch {
                Core.BiomeType.Forest => "Sylva",
                Core.BiomeType.Mountains => "Mont",
                Core.BiomeType.Desert => "Sahara",
                Core.BiomeType.Tundra => "Tundra",
                Core.BiomeType.Swamp => "Mire",
                Core.BiomeType.Jungle => "Junga",
                Core.BiomeType.Ocean => "Maris",
                Core.BiomeType.Grassland => "Prae",
                _ => "Terra"
            };
            string prefix = prefixes[random.NextInt(0, prefixes.Length)];
            string suffix = civSuffixes[random.NextInt(0, civSuffixes.Length)];
            string name = $"{prefix} {biomeRoot}{suffix}";
            return new FixedString128Bytes(name);
        }
    }
} 