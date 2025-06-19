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
    public partial class CulturalInfluenceSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _mythologyQuery;
        private EntityQuery _folkloreQuery;
        private EntityQuery _legacyQuery;
        private EntityQuery _narrativeArcQuery;
        private WorldHistorySystem _historySystem;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 3f; // Cultural influence is gradual but persistent
        private Unity.Mathematics.Random _random;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadWrite<AdaptivePersonalityData>()
            );
            
            _mythologyQuery = GetEntityQuery(ComponentType.ReadOnly<MythologyData>());
            _folkloreQuery = GetEntityQuery(ComponentType.ReadOnly<FolkloreGenerationData>());
            _legacyQuery = GetEntityQuery(ComponentType.ReadOnly<LegacyData>());
            _narrativeArcQuery = GetEntityQuery(ComponentType.ReadOnly<NarrativeArcData>());
            
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(999);
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
            
            // Apply cultural influences to civilizations
            ApplyMythologicalInfluences(ecb);
            ApplyFolkloreInfluences(ecb);
            ApplyLegacyInfluences(ecb);
            ApplyNarrativeArcInfluences(ecb);
            
            // Generate cultural resonance events
            GenerateCulturalResonanceEvents(ecb);
        }

        private void ApplyMythologicalInfluences(EntityCommandBuffer ecb)
        {
            var mythologies = _mythologyQuery.ToComponentDataArray<MythologyData>(Allocator.Temp);
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                bool modified = false;
                
                // Apply influences from relevant myths
                for (int m = 0; m < mythologies.Length; m++)
                {
                    var myth = mythologies[m];
                    
                    // Check if this myth affects this civilization
                    if (ShouldMythInfluenceCivilization(myth, civ, entity))
                    {
                        ApplySingleMythInfluence(ref civ, ref personality, myth);
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EntityManager.SetComponentData(entity, civ);
                    EntityManager.SetComponentData(entity, personality);
                }
            }
            
            mythologies.Dispose();
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool ShouldMythInfluenceCivilization(MythologyData myth, CivilizationData civ, Entity civEntity)
        {
            // Direct influence if this civ created the myth
            if (myth.SourceCivilization.Equals(civEntity)) return true;
            
            // Geographic proximity allows influence
            var distance = math.distance(myth.OriginLocation, civ.Position);
            if (distance < 100f * myth.Spread) return true;
            
            // Religious compatibility
            if (civ.Religion > 5f && myth.Type == MythType.CreationMyth) return true;
            if (civ.Religion > 7f && myth.Type == MythType.CreationMyth) return true;
            
            // Cultural compatibility
            if (civ.Culture > 6f && myth.CulturalImpact > 0.7f) return true;
            
            return false;
        }

        private void ApplySingleMythInfluence(ref CivilizationData civ, ref AdaptivePersonalityData personality, MythologyData myth)
        {
            var influence = myth.MoralWeight * myth.Believability * 0.01f; // Small but persistent influence
            
            switch (myth.Type)
            {
                case MythType.HeroMyth:
                    // Hero myths inspire courage and ambition
                    personality.CurrentPersonality.Ambition += influence;
                    personality.CurrentPersonality.Pride += influence * 0.5f;
                    civ.Military += influence * 2f;
                    civ.Culture += influence;
                    break;
                    
                case MythType.TragedyMyth:
                    // Tragedy myths increase caution and wisdom
                    personality.CurrentPersonality.Paranoia += influence;
                    personality.CurrentPersonality.Defensiveness += influence;
                    civ.Stability += influence;
                    break;
                    
                case MythType.CreationMyth:
                    // Creation myths strengthen religious and cultural identity
                    civ.Religion += influence * 1.5f;
                    civ.Culture += influence;
                    personality.CurrentPersonality.Pride += influence * 0.3f;
                    break;
                    
                case MythType.WarMyth:
                    // War myths glorify conflict and military strength
                    personality.CurrentPersonality.Aggressiveness += influence;
                    civ.Military += influence * 1.5f;
                    civ.Trade -= influence * 0.5f; // Less focus on peaceful commerce
                    break;
                    
                case MythType.LoveMyth:
                    // Love myths promote diplomacy and cultural exchange
                    civ.Diplomacy += influence * 1.2f;
                    civ.Culture += influence;
                    personality.CurrentPersonality.Hatred -= influence * 0.5f;
                    break;
                    
                case MythType.RedemptionMyth:
                    // Redemption myths offer hope and second chances
                    if (personality.Stage == PersonalityEvolutionStage.Broken)
                    {
                        personality.CurrentStress -= influence * 2f;
                        civ.Stability += influence;
                    }
                    break;
                    
                case MythType.CurseMyth:
                    // Curse myths create fear and superstition
                    personality.CurrentPersonality.Paranoia += influence;
                    personality.CurrentStress += influence * 0.5f;
                    civ.Religion += influence; // Turn to religion for protection
                    break;
                    
                case MythType.ProphecyMyth:
                    // Prophecy myths drive ambition and purpose
                    personality.CurrentPersonality.Ambition += influence;
                    civ.Innovation += influence;
                    break;
            }
            
            // Clamp values to reasonable ranges
            ClampCivilizationValues(ref civ);
            ClampPersonalityValues(ref personality);
        }

        private void ApplyFolkloreInfluences(EntityCommandBuffer ecb)
        {
            var folklores = _folkloreQuery.ToComponentDataArray<FolkloreGenerationData>(Allocator.Temp);
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                bool modified = false;
                
                for (int f = 0; f < folklores.Length; f++)
                {
                    var folklore = folklores[f];
                    
                    if (ShouldFolkloreInfluenceCivilization(folklore, civ, entity))
                    {
                        ApplySingleFolkloreInfluence(ref civ, ref personality, folklore);
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EntityManager.SetComponentData(entity, civ);
                    EntityManager.SetComponentData(entity, personality);
                }
            }
            
            folklores.Dispose();
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool ShouldFolkloreInfluenceCivilization(FolkloreGenerationData folklore, CivilizationData civ, Entity civEntity)
        {
            // Direct influence if this civ created the folklore
            if (folklore.SourceCivilization.Equals(civEntity)) return true;
            
            // Spreading folklore affects nearby civilizations
            if (folklore.IsSpreadingToOtherCivers && civ.Trade > 4f) return true;
            
            // Popular folklore has wider reach
            if (folklore.Popularity > 0.8f) return true;
            
            return false;
        }

        private void ApplySingleFolkloreInfluence(ref CivilizationData civ, ref AdaptivePersonalityData personality, FolkloreGenerationData folklore)
        {
            var influence = folklore.Popularity * folklore.CulturalRelevance * 0.005f; // Subtle but cumulative
            var moralInfluence = folklore.MoralLessons * 0.003f;
            
            switch (folklore.Type)
            {
                case FolkloreType.Legend:
                    // Legends inspire heroism and pride
                    personality.CurrentPersonality.Pride += influence;
                    personality.CurrentPersonality.Ambition += moralInfluence;
                    civ.Military += influence;
                    break;
                    
                case FolkloreType.Cautionary:
                    // Cautionary tales increase wisdom and caution
                    personality.CurrentPersonality.Paranoia += moralInfluence;
                    personality.CurrentPersonality.Defensiveness += moralInfluence;
                    civ.Stability += influence;
                    break;
                    
                case FolkloreType.Trickster:
                    // Trickster tales promote cunning and trade
                    civ.Trade += influence * 1.5f;
                    civ.Diplomacy += influence;
                    personality.CurrentPersonality.Greed += moralInfluence * 0.5f;
                    break;
                    
                case FolkloreType.Wisdom:
                    // Wisdom tales promote learning and culture
                    civ.Culture += influence * 1.2f;
                    civ.Technology += influence * 0.8f;
                    personality.TraumaResistance += moralInfluence;
                    break;
                    
                case FolkloreType.Sacred:
                    // Sacred stories strengthen religious devotion
                    civ.Religion += influence * 1.3f;
                    civ.Stability += influence * 0.8f;
                    break;
                    
                case FolkloreType.FairyTale:
                    // Fairy tales inspire hope and creativity
                    civ.Culture += influence;
                    if (personality.CurrentStress > 0.5f)
                    {
                        personality.CurrentStress -= moralInfluence;
                    }
                    break;
                    
                case FolkloreType.Ghost:
                    // Ghost stories increase superstition and fear
                    personality.CurrentPersonality.Paranoia += moralInfluence;
                    civ.Religion += influence * 0.7f; // Turn to religion for protection
                    break;
            }
            
            // Mood effects
            switch (folklore.OverallMood)
            {
                case FolkloreMood.Heroic:
                    personality.CurrentPersonality.Ambition += influence * 0.5f;
                    civ.Military += influence * 0.3f;
                    break;
                    
                case FolkloreMood.Dark:
                    personality.CurrentStress += influence * 0.3f;
                    personality.CurrentPersonality.Paranoia += influence * 0.2f;
                    break;
                    
                case FolkloreMood.Hopeful:
                    personality.CurrentStress -= influence * 0.4f;
                    civ.Stability += influence * 0.2f;
                    break;
                    
                case FolkloreMood.Wise:
                    civ.Culture += influence * 0.4f;
                    personality.TraumaResistance += influence * 0.3f;
                    break;
            }
        }

        private void ApplyLegacyInfluences(EntityCommandBuffer ecb)
        {
            var legacies = _legacyQuery.ToComponentDataArray<LegacyData>(Allocator.Temp);
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                bool modified = false;
                
                for (int l = 0; l < legacies.Length; l++)
                {
                    var legacy = legacies[l];
                    
                    if (ShouldLegacyInfluenceCivilization(legacy, civ, entity))
                    {
                        ApplySingleLegacyInfluence(ref civ, ref personality, legacy);
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EntityManager.SetComponentData(entity, civ);
                    EntityManager.SetComponentData(entity, personality);
                }
            }
            
            legacies.Dispose();
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool ShouldLegacyInfluenceCivilization(LegacyData legacy, CivilizationData civ, Entity civEntity)
        {
            // Direct influence if this is the founding civilization
            if (legacy.FoundingCivilization.Equals(civEntity)) return true;
            
            // Powerful legacies influence nearby civilizations
            var distance = math.distance(legacy.OriginLocation, civ.Position);
            if (distance < 80f && legacy.LegacyStrength > 0.7f) return true;
            
            return false;
        }

        private void ApplySingleLegacyInfluence(ref CivilizationData civ, ref AdaptivePersonalityData personality, LegacyData legacy)
        {
            var influence = legacy.LegacyStrength * legacy.ReputationScore * 0.003f; // Persistent but subtle
            var honorInfluence = legacy.Honor * 0.002f;
            
            switch (legacy.Type)
            {
                case LegacyType.Noble:
                    // Noble legacies promote honor and stability
                    personality.CurrentPersonality.Pride += honorInfluence;
                    civ.Stability += influence;
                    civ.Diplomacy += influence * 0.8f;
                    break;
                    
                case LegacyType.Military:
                    // Military legacies promote strength and conquest
                    civ.Military += influence * 1.5f;
                    personality.CurrentPersonality.Aggressiveness += honorInfluence * 0.5f;
                    personality.CurrentPersonality.Ambition += honorInfluence * 0.3f;
                    break;
                    
                case LegacyType.Merchant:
                    // Merchant legacies promote trade and wealth
                    civ.Trade += influence * 1.3f;
                    civ.Wealth += influence * 100f; // Direct wealth bonus
                    personality.CurrentPersonality.Greed += honorInfluence * 0.3f;
                    break;
                    
                case LegacyType.Scholar:
                    // Scholar legacies promote learning and innovation
                    civ.Technology += influence * 1.2f;
                    civ.Culture += influence;
                    civ.Innovation += influence;
                    break;
                    
                case LegacyType.Religious:
                    // Religious legacies strengthen faith
                    civ.Religion += influence * 1.4f;
                    civ.Stability += influence * 0.6f;
                    personality.TraumaResistance += honorInfluence;
                    break;
                    
                case LegacyType.Artisan:
                    // Artisan legacies promote culture and craftsmanship
                    civ.Culture += influence * 1.3f;
                    civ.Production += influence;
                    civ.Trade += influence * 0.7f; // Crafted goods for trade
                    break;
                    
                case LegacyType.Criminal:
                    // Criminal legacies promote cunning but reduce stability
                    civ.Trade += influence * 0.8f; // Black market trade
                    civ.Stability -= influence * 0.5f;
                    personality.CurrentPersonality.Paranoia += honorInfluence * 0.3f;
                    break;
                    
                case LegacyType.Rebel:
                    // Rebel legacies promote independence and resistance
                    personality.CurrentPersonality.Aggressiveness += honorInfluence * 0.4f;
                    personality.CurrentPersonality.Defensiveness += honorInfluence * 0.6f;
                    civ.Military += influence * 0.8f;
                    civ.Stability -= influence * 0.3f; // Rebellious spirit
                    break;
            }
        }

        private void ApplyNarrativeArcInfluences(EntityCommandBuffer ecb)
        {
            var arcs = _narrativeArcQuery.ToComponentDataArray<NarrativeArcData>(Allocator.Temp);
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                bool modified = false;
                
                for (int a = 0; a < arcs.Length; a++)
                {
                    var arc = arcs[a];
                    
                    if (arc.ProtagonistCivilization.Equals(entity))
                    {
                        ApplyNarrativeArcInfluence(ref civ, ref personality, arc);
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EntityManager.SetComponentData(entity, civ);
                    EntityManager.SetComponentData(entity, personality);
                }
            }
            
            arcs.Dispose();
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private void ApplyNarrativeArcInfluence(ref CivilizationData civ, ref AdaptivePersonalityData personality, NarrativeArcData arc)
        {
            var influence = arc.Tension * arc.Stakes * 0.002f; // Active narrative creates pressure
            
            // Stage-based effects
            switch (arc.CurrentStage)
            {
                case NarrativeStage.RisingAction:
                    // Building tension increases stress but also ambition
                    personality.CurrentStress += influence * 0.5f;
                    personality.CurrentPersonality.Ambition += influence * 0.3f;
                    break;
                    
                case NarrativeStage.Climax:
                    // Peak moments have intense effects
                    personality.CurrentStress += influence;
                    
                    // Type-specific climax effects
                    switch (arc.Type)
                    {
                        case NarrativeArcType.Rise:
                            civ.Influence += influence * 2f;
                            personality.CurrentPersonality.Pride += influence;
                            break;
                            
                        case NarrativeArcType.Fall:
                            civ.Stability -= influence;
                            personality.CurrentStress += influence * 0.5f;
                            break;
                            
                        case NarrativeArcType.Epic:
                            civ.Military += influence;
                            civ.Culture += influence;
                            personality.CurrentPersonality.Ambition += influence * 0.5f;
                            break;
                    }
                    break;
                    
                case NarrativeStage.Resolution:
                    // Resolution reduces stress and provides closure
                    personality.CurrentStress -= influence * 0.8f;
                    
                    // Successful arc completion has lasting benefits
                    if (arc.ArcProgress > 0.8f)
                    {
                        switch (arc.Type)
                        {
                            case NarrativeArcType.Redemption:
                                if (personality.Stage == PersonalityEvolutionStage.Broken)
                                {
                                    personality.Stage = PersonalityEvolutionStage.Mature;
                                }
                                break;
                                
                            case NarrativeArcType.Rise:
                                civ.Prestige += influence * 3f;
                                break;
                        }
                    }
                    break;
            }
        }

        private void GenerateCulturalResonanceEvents(EntityCommandBuffer ecb)
        {
            // When multiple cultural elements align, create resonance events
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                // Check for cultural resonance conditions
                if (CheckForCulturalResonance(civ, personality, entity))
                {
                    GenerateCulturalResonanceEvent(civ, entity, ecb);
                }
            }
            
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool CheckForCulturalResonance(CivilizationData civ, AdaptivePersonalityData personality, Entity entity)
        {
            // Resonance occurs when culture, religion, and stability are all high
            return civ.Culture > 7f && civ.Religion > 6f && civ.Stability > 0.8f && _random.NextFloat() < 0.02f;
        }

        private void GenerateCulturalResonanceEvent(CivilizationData civ, Entity entity, EntityCommandBuffer ecb)
        {
            var eventTitle = $"Cultural Renaissance in {civ.Name}";
            var eventDescription = $"The convergence of myths, folklore, and ancestral legacies creates a " +
                                 $"cultural renaissance in {civ.Name}. Art, philosophy, and spiritual understanding " +
                                 $"reach new heights as the people find deep meaning in their shared stories.";
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = 2.5f, // High significance for cultural resonance
                CivilizationId = entity
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void ClampCivilizationValues(ref CivilizationData civ)
        {
            civ.Military = math.clamp(civ.Military, 0f, 10f);
            civ.Culture = math.clamp(civ.Culture, 0f, 10f);
            civ.Religion = math.clamp(civ.Religion, 0f, 10f);
            civ.Trade = math.clamp(civ.Trade, 0f, 10f);
            civ.Technology = math.clamp(civ.Technology, 0f, 10f);
            civ.Diplomacy = math.clamp(civ.Diplomacy, 0f, 10f);
            civ.Stability = math.clamp(civ.Stability, 0f, 1f);
            civ.Innovation = math.clamp(civ.Innovation, 0f, 10f);
            civ.Production = math.clamp(civ.Production, 0f, 10f);
            civ.Influence = math.clamp(civ.Influence, 0f, 10f);
            civ.Prestige = math.clamp(civ.Prestige, 0f, 10f);
        }

        private void ClampPersonalityValues(ref AdaptivePersonalityData personality)
        {
            personality.CurrentPersonality.Aggressiveness = math.clamp(personality.CurrentPersonality.Aggressiveness, 0f, 10f);
            personality.CurrentPersonality.Defensiveness = math.clamp(personality.CurrentPersonality.Defensiveness, 0f, 10f);
            personality.CurrentPersonality.Greed = math.clamp(personality.CurrentPersonality.Greed, 0f, 10f);
            personality.CurrentPersonality.Paranoia = math.clamp(personality.CurrentPersonality.Paranoia, 0f, 10f);
            personality.CurrentPersonality.Ambition = math.clamp(personality.CurrentPersonality.Ambition, 0f, 10f);
            personality.CurrentPersonality.Desperation = math.clamp(personality.CurrentPersonality.Desperation, 0f, 10f);
            personality.CurrentPersonality.Hatred = math.clamp(personality.CurrentPersonality.Hatred, 0f, 10f);
            personality.CurrentPersonality.Pride = math.clamp(personality.CurrentPersonality.Pride, 0f, 10f);
            personality.CurrentPersonality.Vengefulness = math.clamp(personality.CurrentPersonality.Vengefulness, 0f, 10f);
            
            personality.CurrentStress = math.clamp(personality.CurrentStress, 0f, 1f);
            personality.TraumaResistance = math.clamp(personality.TraumaResistance, 0f, 1f);
        }
    }
} 