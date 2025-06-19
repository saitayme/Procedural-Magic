using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using UnityEngine;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    public partial class LegacySystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _legacyQuery;
        private WorldHistorySystem _historySystem;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 5f;
        private Unity.Mathematics.Random _random;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadWrite<AdaptivePersonalityData>()
            );
            
            _legacyQuery = GetEntityQuery(ComponentType.ReadWrite<LegacyData>());
            
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(42);
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<SimulationConfig>()) return;
            
            var config = SystemAPI.GetSingleton<SimulationConfig>();
            if (!config.EnableHistorySystem) return;
            
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate) return;
            _nextUpdate = currentTime + UPDATE_INTERVAL;
            
            var ecb = _ecbSystem.CreateCommandBuffer();
            
            // Create new legacies for eligible civilizations
            CreateNewLegacies(ecb);
            
            // Update existing legacies
            UpdateExistingLegacies(ecb);
            
            // Generate legacy-based events
            GenerateLegacyEvents(ecb);
        }

        private void CreateNewLegacies(EntityCommandBuffer ecb)
        {
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                // Create legacies for civilizations that have achieved significance
                if (ShouldCreateLegacy(civ, personality))
                {
                    CreateLegacy(civ, personality, entity, ecb);
                }
            }
            
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool ShouldCreateLegacy(CivilizationData civ, AdaptivePersonalityData personality)
        {
            // Criteria for legacy creation
            return (civ.Population > 8000f && civ.Culture > 6f) ||
                   (civ.Military > 7f && personality.SuccessfulWars > 2) ||
                   (civ.Technology > 7f && civ.Innovation > 6f) ||
                   (civ.Religion > 8f && civ.Influence > 6f) ||
                   (civ.Trade > 8f && civ.Wealth > 10000f);
        }

        private void CreateLegacy(CivilizationData civ, AdaptivePersonalityData personality, Entity civEntity, EntityCommandBuffer ecb)
        {
            var legacyType = DetermineLegacyType(civ, personality);
            var familyName = GenerateFamilyName(civ, legacyType);
            var motto = GenerateFamilyMotto(civ, personality, legacyType);
            
            var legacy = new LegacyData
            {
                FamilyName = familyName,
                FamilyMotto = motto,
                Type = legacyType,
                FoundingCivilization = civEntity,
                OriginLocation = civ.Position,
                ReputationScore = CalculateInitialReputation(civ, personality),
                Honor = personality.CurrentPersonality.Pride * 0.1f,
                Age = 0f,
                GenerationCount = 1,
                LivingDescendants = 1,
                LegacyStrength = 0.5f,
                IsActive = true
            };
            
            var legacyEntity = ecb.CreateEntity();
            ecb.AddComponent(legacyEntity, legacy);
            
            // Create founding event
            CreateLegacyFoundingEvent(legacy, civ, ecb);
        }

        private LegacyType DetermineLegacyType(CivilizationData civ, AdaptivePersonalityData personality)
        {
            if (civ.Military > 7f && personality.SuccessfulWars > 2) return LegacyType.Military;
            if (civ.Religion > 8f) return LegacyType.Religious;
            if (civ.Trade > 8f && civ.Wealth > 10000f) return LegacyType.Merchant;
            if (civ.Technology > 7f) return LegacyType.Scholar;
            if (civ.Culture > 7f) return LegacyType.Artisan;
            if (personality.CurrentPersonality.Hatred > 8f) return LegacyType.Criminal;
            if (civ.Stability < 0.3f) return LegacyType.Rebel;
            
            return LegacyType.Noble; // Default
        }

        private FixedString128Bytes GenerateFamilyName(CivilizationData civ, LegacyType type)
        {
            var civName = civ.Name.ToString();
            var prefix = type switch
            {
                LegacyType.Noble => "House",
                LegacyType.Military => "Clan",
                LegacyType.Religious => "Order",
                LegacyType.Merchant => "Company",
                LegacyType.Scholar => "Academy",
                LegacyType.Artisan => "Guild",
                LegacyType.Criminal => "Brotherhood",
                LegacyType.Rebel => "Movement",
                _ => "House"
            };
            
            // Extract distinctive part of civilization name
            var parts = civName.Split(' ');
            var distinctivePart = parts.Length > 1 ? parts[parts.Length - 1] : civName;
            
            return new FixedString128Bytes($"{prefix} of {distinctivePart}");
        }

        private FixedString512Bytes GenerateFamilyMotto(CivilizationData civ, AdaptivePersonalityData personality, LegacyType type)
        {
            string motto = type switch
            {
                LegacyType.Noble => "Honor above all, duty eternal",
                LegacyType.Military => "Victory through strength, strength through unity",
                LegacyType.Religious => "Faith lights the path, wisdom guides the way",
                LegacyType.Merchant => "Prosperity through wisdom, wealth through honor",
                LegacyType.Scholar => "Knowledge is power, understanding is wisdom",
                LegacyType.Artisan => "Beauty in purpose, perfection in craft",
                LegacyType.Criminal => "In shadows we thrive, in silence we strike",
                LegacyType.Rebel => "Freedom through struggle, justice through action",
                _ => "Legacy endures, honor prevails"
            };
            
            return new FixedString512Bytes(motto);
        }

        private float CalculateInitialReputation(CivilizationData civ, AdaptivePersonalityData personality)
        {
            float reputation = 0.5f;
            
            if (civ.Culture > 6f) reputation += 0.2f;
            if (civ.Military > 7f) reputation += 0.15f;
            if (civ.Trade > 6f) reputation += 0.1f;
            if (personality.CurrentPersonality.Pride > 7f) reputation += 0.15f;
            if (personality.CurrentPersonality.Pride > 8f) reputation += 0.1f;
            
            return math.clamp(reputation, 0.1f, 1.0f);
        }

        private void UpdateExistingLegacies(EntityCommandBuffer ecb)
        {
            var legacies = _legacyQuery.ToComponentDataArray<LegacyData>(Allocator.Temp);
            var legacyEntities = _legacyQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < legacies.Length; i++)
            {
                var legacy = legacies[i];
                var entity = legacyEntities[i];
                
                if (!legacy.IsActive) continue;
                
                // Age the legacy
                legacy.Age += UPDATE_INTERVAL;
                
                // Update legacy strength based on founding civilization
                UpdateLegacyStrength(ref legacy);
                
                // Chance for generational events
                if (_random.NextFloat() < 0.1f) // 10% chance per update
                {
                    GenerateGenerationalEvent(legacy, entity, ecb);
                }
                
                EntityManager.SetComponentData(entity, legacy);
            }
            
            legacies.Dispose();
            legacyEntities.Dispose();
        }

        private void UpdateLegacyStrength(ref LegacyData legacy)
        {
            if (EntityManager.HasComponent<CivilizationData>(legacy.FoundingCivilization))
            {
                var civ = EntityManager.GetComponentData<CivilizationData>(legacy.FoundingCivilization);
                
                // Legacy strength based on civilization's current state
                var strengthBonus = 0f;
                if (civ.Culture > 7f) strengthBonus += 0.1f;
                if (civ.Stability > 0.7f) strengthBonus += 0.1f;
                if (civ.Population > 10000f) strengthBonus += 0.05f;
                
                legacy.LegacyStrength = math.clamp(legacy.LegacyStrength + strengthBonus * 0.1f, 0.1f, 1.0f);
            }
            else
            {
                // Civilization is gone, legacy slowly fades
                legacy.LegacyStrength *= 0.98f;
                if (legacy.LegacyStrength < 0.1f)
                {
                    legacy.IsActive = false;
                }
            }
        }

        private void GenerateGenerationalEvent(LegacyData legacy, Entity legacyEntity, EntityCommandBuffer ecb)
        {
            var eventType = _random.NextInt(0, 4);
            
            string eventTitle = "";
            string eventDescription = "";
            
            switch (eventType)
            {
                case 0: // New generation
                    legacy.GenerationCount++;
                    eventTitle = $"{legacy.FamilyName}: New Generation";
                    eventDescription = $"The {legacy.GenerationCount}th generation of {legacy.FamilyName} rises to prominence, carrying forward their ancestral legacy.";
                    break;
                    
                case 1: // Honor gained
                    legacy.Honor += _random.NextFloat(0.1f, 0.3f);
                    eventTitle = $"{legacy.FamilyName}: Honor Restored";
                    eventDescription = $"Through noble deeds, {legacy.FamilyName} has enhanced their reputation and honor.";
                    break;
                    
                case 2: // Reputation event
                    var reputationChange = _random.NextFloat(-0.2f, 0.3f);
                    legacy.ReputationScore += reputationChange;
                    eventTitle = reputationChange > 0 ? $"{legacy.FamilyName}: Rising Star" : $"{legacy.FamilyName}: Scandal";
                    eventDescription = reputationChange > 0 ? 
                        $"{legacy.FamilyName} achieves new heights of recognition." :
                        $"{legacy.FamilyName} faces challenges that test their resolve.";
                    break;
                    
                case 3: // Legacy expansion
                    legacy.LivingDescendants += _random.NextInt(1, 3);
                    eventTitle = $"{legacy.FamilyName}: Growing Influence";
                    eventDescription = $"The influence of {legacy.FamilyName} grows as their descendants spread across the land.";
                    break;
            }
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = legacy.LegacyStrength * 0.8f,
                CivilizationId = legacy.FoundingCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void GenerateLegacyEvents(EntityCommandBuffer ecb)
        {
            // Generate cross-legacy interactions and conflicts
            var legacies = _legacyQuery.ToComponentDataArray<LegacyData>(Allocator.Temp);
            
            if (legacies.Length > 1 && _random.NextFloat() < 0.05f) // 5% chance
            {
                var legacy1 = legacies[_random.NextInt(0, legacies.Length)];
                var legacy2 = legacies[_random.NextInt(0, legacies.Length)];
                
                if (!legacy1.FamilyName.Equals(legacy2.FamilyName))
                {
                    GenerateInterLegacyEvent(legacy1, legacy2, ecb);
                }
            }
            
            legacies.Dispose();
        }

        private void GenerateInterLegacyEvent(LegacyData legacy1, LegacyData legacy2, EntityCommandBuffer ecb)
        {
            var eventTypes = new string[] { "Alliance", "Rivalry", "Marriage", "Feud", "Competition" };
            var eventType = eventTypes[_random.NextInt(0, eventTypes.Length)];
            
            var eventTitle = $"{legacy1.FamilyName} and {legacy2.FamilyName}: {eventType}";
            var eventDescription = eventType switch
            {
                "Alliance" => $"{legacy1.FamilyName} and {legacy2.FamilyName} form a powerful alliance, uniting their strengths.",
                "Rivalry" => $"Ancient rivalry between {legacy1.FamilyName} and {legacy2.FamilyName} flares into open competition.",
                "Marriage" => $"The houses of {legacy1.FamilyName} and {legacy2.FamilyName} are united through marriage.",
                "Feud" => $"A bitter feud erupts between {legacy1.FamilyName} and {legacy2.FamilyName}.",
                "Competition" => $"{legacy1.FamilyName} and {legacy2.FamilyName} compete for dominance in their field.",
                _ => $"{legacy1.FamilyName} and {legacy2.FamilyName} interact in unexpected ways."
            };
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Social,
                Category = EventCategory.Cultural,
                Significance = (legacy1.LegacyStrength + legacy2.LegacyStrength) * 0.5f
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void CreateLegacyFoundingEvent(LegacyData legacy, CivilizationData civ, EntityCommandBuffer ecb)
        {
            var foundingEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"Foundation of {legacy.FamilyName}"),
                Description = new FixedString512Bytes($"The esteemed {legacy.FamilyName} is founded in {civ.Name}, destined to leave their mark on history. Their motto: '{legacy.FamilyMotto}'"),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = legacy.LegacyStrength,
                CivilizationId = legacy.FoundingCivilization
            };
            
            _historySystem.AddEvent(foundingEvent);
        }
    }
} 