using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using UnityEngine;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    public partial class NarrativeArcSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _narrativeArcQuery;
        private EntityQuery _historyQuery;
        private WorldHistorySystem _historySystem;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 2f; // Check every 2 seconds for story developments
        private Unity.Mathematics.Random _random;
        
        // Story state tracking
        private NativeList<Entity> _activeArcs;
        private NativeHashMap<Entity, float> _civilizationStoryPotential;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadWrite<AdaptivePersonalityData>(),
                ComponentType.ReadOnly<NarrativeArcBuffer>()
            );
            
            _narrativeArcQuery = GetEntityQuery(ComponentType.ReadWrite<NarrativeArcData>());
            _historyQuery = GetEntityQuery(ComponentType.ReadOnly<HistoricalEventData>());
            
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(1337);
            
            _activeArcs = new NativeList<Entity>(Allocator.Persistent);
            _civilizationStoryPotential = new NativeHashMap<Entity, float>(128, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_activeArcs.IsCreated) _activeArcs.Dispose();
            if (_civilizationStoryPotential.IsCreated) _civilizationStoryPotential.Dispose();
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
            
            // Update existing narrative arcs
            UpdateActiveNarrativeArcs(ecb);
            
            // Analyze civilizations for new story potential
            AnalyzeCivilizationStoryPotential();
            
            // Create new narrative arcs when conditions are right
            CreateNewNarrativeArcs(ecb);
            
            // Manage story pacing and tension
            ManageStoryTension();
            
            // Resolve completed arcs
            ResolveCompletedArcs(ecb);
        }

        private void UpdateActiveNarrativeArcs(EntityCommandBuffer ecb)
        {
            var arcs = _narrativeArcQuery.ToComponentDataArray<NarrativeArcData>(Allocator.Temp);
            var arcEntities = _narrativeArcQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < arcs.Length; i++)
            {
                var arc = arcs[i];
                var entity = arcEntities[i];
                
                // Progress the arc based on current events and time
                ProgressNarrativeArc(ref arc, entity, ecb);
                
                // Update tension based on current state
                UpdateArcTension(ref arc);
                
                // Check for stage transitions
                CheckStageTransitions(ref arc, entity, ecb);
                
                EntityManager.SetComponentData(entity, arc);
            }
            
            arcs.Dispose();
            arcEntities.Dispose();
        }

        private void ProgressNarrativeArc(ref NarrativeArcData arc, Entity arcEntity, EntityCommandBuffer ecb)
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var timeElapsed = currentTime - arc.StartYear;
            
            // Calculate natural progression based on time and expected duration
            var timeProgress = math.clamp(timeElapsed / arc.ExpectedDuration, 0f, 1f);
            
            // Adjust progression based on related events
            var eventBonus = CalculateEventProgressionBonus(arc);
            
            // Update progress with some organic variation
            var progressDelta = (timeProgress + eventBonus) * _random.NextFloat(0.8f, 1.2f);
            arc.ArcProgress = math.clamp(arc.ArcProgress + progressDelta * 0.1f, 0f, 1f);
            
            // Generate events if the arc demands it
            if (ShouldGenerateArcEvent(arc))
            {
                GenerateArcDrivenEvent(arc, arcEntity, ecb);
            }
        }

        private void UpdateArcTension(ref NarrativeArcData arc)
        {
            // Tension follows a dramatic curve based on narrative stage
            arc.Tension = arc.CurrentStage switch
            {
                NarrativeStage.Setup => 0.2f + (arc.ArcProgress * 0.3f),
                NarrativeStage.IncitingIncident => 0.5f + (arc.ArcProgress * 0.2f),
                NarrativeStage.RisingAction => 0.3f + (arc.ArcProgress * 0.5f), // Build to climax
                NarrativeStage.Climax => 0.9f + (_random.NextFloat(-0.1f, 0.1f)), // Peak tension
                NarrativeStage.FallingAction => 0.8f - (arc.ArcProgress * 0.4f), // Tension decreases
                NarrativeStage.Resolution => 0.1f + (_random.NextFloat(-0.05f, 0.05f)), // Low tension
                _ => 0.3f
            };
            
            // Adjust based on arc type
            if (arc.Type == NarrativeArcType.Thriller || arc.Type == NarrativeArcType.Tragedy)
                arc.Tension *= 1.3f;
            else if (arc.Type == NarrativeArcType.Comedy || arc.Type == NarrativeArcType.Romance)
                arc.Tension *= 0.8f;
            
            arc.Tension = math.clamp(arc.Tension, 0.1f, 1.0f);
        }

        private void CheckStageTransitions(ref NarrativeArcData arc, Entity arcEntity, EntityCommandBuffer ecb)
        {
            var shouldTransition = false;
            var nextStage = arc.CurrentStage;
            
            // Progress through narrative stages based on arc progress and type
            switch (arc.CurrentStage)
            {
                case NarrativeStage.Setup:
                    if (arc.ArcProgress > 0.15f || HasSignificantEvent(arc))
                    {
                        nextStage = NarrativeStage.IncitingIncident;
                        shouldTransition = true;
                    }
                    break;
                    
                case NarrativeStage.IncitingIncident:
                    if (arc.ArcProgress > 0.25f)
                    {
                        nextStage = NarrativeStage.RisingAction;
                        shouldTransition = true;
                    }
                    break;
                    
                case NarrativeStage.RisingAction:
                    if (arc.ArcProgress > 0.7f || arc.Tension > 0.8f)
                    {
                        nextStage = NarrativeStage.Climax;
                        shouldTransition = true;
                    }
                    break;
                    
                case NarrativeStage.Climax:
                    if (arc.ArcProgress > 0.8f)
                    {
                        nextStage = NarrativeStage.FallingAction;
                        shouldTransition = true;
                    }
                    break;
                    
                case NarrativeStage.FallingAction:
                    if (arc.ArcProgress > 0.9f)
                    {
                        nextStage = NarrativeStage.Resolution;
                        shouldTransition = true;
                    }
                    break;
            }
            
            if (shouldTransition)
            {
                arc.CurrentStage = nextStage;
                GenerateStageTransitionEvent(arc, arcEntity, ecb);
            }
        }

        private void AnalyzeCivilizationStoryPotential()
        {
            var civilizations = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            _civilizationStoryPotential.Clear();
            
            for (int i = 0; i < civilizations.Length; i++)
            {
                var civ = civilizations[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                var potential = CalculateStoryPotential(civ, personality);
                _civilizationStoryPotential[entity] = potential;
            }
            
            civilizations.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private float CalculateStoryPotential(CivilizationData civ, AdaptivePersonalityData personality)
        {
            float potential = 0f;
            
            // Extreme personality traits create story potential
            potential += CalculatePersonalityExtremes(personality.CurrentPersonality);
            
            // Rapid changes create story opportunities
            if (civ.Population > 10000f && civ.Stability < 0.3f) potential += 0.3f; // Revolution potential
            if (civ.Military > 8f && civ.Aggressiveness > 7f) potential += 0.25f; // Conquest potential
            if (civ.Culture > 7f && civ.Wealth > 8000f) potential += 0.2f; // Golden age potential
            if (civ.Technology > 8f) potential += 0.2f; // Innovation potential
            
            // Personality evolution stages affect story potential
            potential += personality.Stage switch
            {
                PersonalityEvolutionStage.Naive => 0.15f, // Young civs are unpredictable
                PersonalityEvolutionStage.Developing => 0.25f, // Most story potential
                PersonalityEvolutionStage.Broken => 0.3f, // Trauma creates compelling stories
                PersonalityEvolutionStage.Enlightened => 0.1f, // Stable, less dramatic
                _ => 0.1f
            };
            
            // Recent traumatic events increase story potential
            if (personality.CurrentStress > 0.7f) potential += 0.2f;
            
            return math.clamp(potential, 0f, 1f);
        }

        private float CalculatePersonalityExtremes(PersonalityTraits traits)
        {
            float extremeness = 0f;
            
            // High values in any trait create story potential
            if (traits.Aggressiveness > 8f) extremeness += 0.15f;
            if (traits.Paranoia > 8f) extremeness += 0.12f;
            if (traits.Ambition > 8f) extremeness += 0.1f;
            if (traits.Pride > 8f) extremeness += 0.08f;
            if (traits.Hatred > 8f) extremeness += 0.15f;
            if (traits.Vengefulness > 8f) extremeness += 0.12f;
            
            // Very low values also create potential (naive, defenseless)
            if (traits.Defensiveness < 2f) extremeness += 0.1f;
            if (traits.Paranoia < 1f) extremeness += 0.08f;
            
            return extremeness;
        }

        private void CreateNewNarrativeArcs(EntityCommandBuffer ecb)
        {
            var worldTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Don't create too many arcs at once
            if (_activeArcs.Length >= 3) return;
            
            // Find the most story-worthy civilization
            var bestEntity = Entity.Null;
            var bestPotential = 0.6f; // Minimum threshold
            
            foreach (var kvp in _civilizationStoryPotential)
            {
                if (kvp.Value > bestPotential && !IsInActiveArc(kvp.Key))
                {
                    bestPotential = kvp.Value;
                    bestEntity = kvp.Key;
                }
            }
            
            if (bestEntity == Entity.Null) return;
            
            // Create a narrative arc for this civilization
            var civ = EntityManager.GetComponentData<CivilizationData>(bestEntity);
            var personality = EntityManager.GetComponentData<AdaptivePersonalityData>(bestEntity);
            
            var arcType = DetermineArcType(civ, personality, worldTime);
            var arc = CreateNarrativeArc(arcType, bestEntity, civ, personality, worldTime);
            
            var arcEntity = ecb.CreateEntity();
            ecb.AddComponent(arcEntity, arc);
            
            // Add this civilization to the arc
            var buffer = ecb.AddBuffer<NarrativeArcBuffer>(bestEntity);
            buffer.Add(new NarrativeArcBuffer 
            { 
                ArcEntity = arcEntity, 
                ParticipationLevel = 1f, 
                Role = NarrativeRole.Protagonist 
            });
            
            _activeArcs.Add(arcEntity);
            
            Debug.Log($"[NarrativeArcSystem] Created {arcType} arc for {civ.Name}: '{arc.ArcName}'");
        }

        private NarrativeArcType DetermineArcType(
            CivilizationData civ, 
            AdaptivePersonalityData personality, 
            float worldTime)
        {
            // Analyze current state to determine most appropriate arc type
            if (personality.Stage == PersonalityEvolutionStage.Broken)
                return _random.NextFloat() < 0.7f ? NarrativeArcType.Redemption : NarrativeArcType.Tragedy;
            
            if (civ.Population > 15000f && civ.Military > 7f && personality.CurrentPersonality.Aggressiveness > 7f)
                return NarrativeArcType.Epic;
            
            if (civ.Stability < 0.3f && civ.Population > 8000f)
                return NarrativeArcType.Fall;
            
            if (civ.Culture > 7f && civ.Wealth > 8000f && civ.Stability > 0.7f)
                return NarrativeArcType.Rise;
            
            if (personality.CurrentPersonality.Ambition > 8f && civ.Technology > 6f)
                return NarrativeArcType.Adventure;
            
            if (civ.Trade > 6f && civ.Diplomacy > 6f)
                return _random.NextFloat() < 0.4f ? NarrativeArcType.Romance : NarrativeArcType.Comedy;
            
            // Default to rise or adventure for active civilizations
            return _random.NextFloat() < 0.6f ? NarrativeArcType.Rise : NarrativeArcType.Adventure;
        }

        private NarrativeArcData CreateNarrativeArc(
            NarrativeArcType type,
            Entity protagonist,
            CivilizationData civ,
            AdaptivePersonalityData personality,
            float startTime)
        {
            var arcName = GenerateArcName(type, civ);
            var description = GenerateArcDescription(type, civ, personality);
            var duration = CalculateArcDuration(type, civ);
            
            return new NarrativeArcData
            {
                ArcName = arcName,
                ArcDescription = description,
                Type = type,
                ProtagonistCivilization = protagonist,
                AntagonistCivilization = Entity.Null, // Will be determined later if needed
                CurrentStage = NarrativeStage.Setup,
                ArcProgress = 0f,
                Tension = 0.2f,
                Stakes = CalculateStakes(type, civ),
                IsEpic = IsEpicArc(type, civ),
                ExpectedDuration = duration,
                StartYear = startTime,
                ActorsInvolved = 1,
                AudienceEngagement = CalculateInitialEngagement(type, civ, personality)
            };
        }

        private FixedString128Bytes GenerateArcName(NarrativeArcType type, CivilizationData civ)
        {
            var civName = civ.Name.ToString();
            
            string arcName = type switch
            {
                NarrativeArcType.Rise => $"The Ascension of {civName}",
                NarrativeArcType.Fall => $"The Twilight of {civName}",
                NarrativeArcType.Redemption => $"The Redemption of {civName}",
                NarrativeArcType.Tragedy => $"The Tragedy of {civName}",
                NarrativeArcType.Comedy => $"The Fortune of {civName}",
                NarrativeArcType.Romance => $"The Alliance of {civName}",
                NarrativeArcType.Adventure => $"The Quest of {civName}",
                NarrativeArcType.Thriller => $"The Trials of {civName}",
                NarrativeArcType.Mystery => $"The Mystery of {civName}",
                NarrativeArcType.Epic => $"The Epic of {civName}",
                _ => $"The Tale of {civName}"
            };
            
            return new FixedString128Bytes(arcName);
        }

        private FixedString512Bytes GenerateArcDescription(
            NarrativeArcType type,
            CivilizationData civ,
            AdaptivePersonalityData personality)
        {
            var civName = civ.Name.ToString();
            
            string description = type switch
            {
                NarrativeArcType.Rise => 
                    $"From humble beginnings, {civName} embarks on a journey toward greatness, " +
                    $"facing challenges that will test their resolve and shape their destiny.",
                
                NarrativeArcType.Fall => 
                    $"Once mighty {civName} faces the twilight of their golden age, " +
                    $"as internal strife and external pressures threaten their very existence.",
                
                NarrativeArcType.Redemption => 
                    $"Having fallen from grace, {civName} seeks to reclaim their honor " +
                    $"and find redemption through trials that will forge them anew.",
                
                NarrativeArcType.Tragedy => 
                    $"The tale of {civName} unfolds as a cautionary story of hubris and fate, " +
                    $"where noble intentions lead to unforeseen consequences.",
                
                NarrativeArcType.Epic => 
                    $"The legendary saga of {civName} begins, destined to reshape the world " +
                    $"through deeds that will echo through the ages.",
                
                _ => $"The story of {civName} unfolds with unexpected turns and discoveries."
            };
            
            return new FixedString512Bytes(description);
        }

        private float CalculateArcDuration(NarrativeArcType type, CivilizationData civ)
        {
            float baseDuration = type switch
            {
                NarrativeArcType.Epic => 80f,
                NarrativeArcType.Tragedy => 60f,
                NarrativeArcType.Rise => 50f,
                NarrativeArcType.Fall => 40f,
                NarrativeArcType.Redemption => 45f,
                NarrativeArcType.Adventure => 35f,
                _ => 30f
            };
            
            // Adjust based on civilization size and development
            if (civ.Population > 10000f) baseDuration *= 1.3f;
            if (civ.Culture > 7f) baseDuration *= 1.1f;
            
            return baseDuration;
        }

        private float CalculateStakes(NarrativeArcType type, CivilizationData civ)
        {
            float stakes = type switch
            {
                NarrativeArcType.Epic => 1.0f,
                NarrativeArcType.Tragedy => 0.9f,
                NarrativeArcType.Fall => 0.8f,
                NarrativeArcType.Rise => 0.7f,
                NarrativeArcType.Redemption => 0.75f,
                _ => 0.5f
            };
            
            // Higher stakes for larger civilizations
            stakes += (civ.Population / 20000f) * 0.2f;
            
            return math.clamp(stakes, 0.1f, 1.0f);
        }

        private bool IsEpicArc(NarrativeArcType type, CivilizationData civ)
        {
            return type == NarrativeArcType.Epic || 
                   (civ.Population > 15000f && civ.Influence > 8f);
        }

        private float CalculateInitialEngagement(
            NarrativeArcType type,
            CivilizationData civ,
            AdaptivePersonalityData personality)
        {
            float engagement = 0.5f;
            
            // More dramatic types are more engaging
            if (type == NarrativeArcType.Epic || type == NarrativeArcType.Tragedy)
                engagement += 0.3f;
            
            // Extreme personalities are more engaging
            engagement += CalculatePersonalityExtremes(personality.CurrentPersonality) * 0.5f;
            
            // Larger civilizations draw more attention
            engagement += math.clamp(civ.Population / 20000f, 0f, 0.2f);
            
            return math.clamp(engagement, 0.1f, 1.0f);
        }

        private bool IsInActiveArc(Entity civilizationEntity)
        {
            return EntityManager.HasComponent<NarrativeArcBuffer>(civilizationEntity) &&
                   EntityManager.GetBuffer<NarrativeArcBuffer>(civilizationEntity).Length > 0;
        }

        private float CalculateEventProgressionBonus(NarrativeArcData arc)
        {
            // Check recent events related to the protagonist
            // This would integrate with the history system
            return 0f; // Placeholder
        }

        private bool ShouldGenerateArcEvent(NarrativeArcData arc)
        {
            // Generate events more frequently during high-tension stages
            float chance = arc.CurrentStage switch
            {
                NarrativeStage.IncitingIncident => 0.3f,
                NarrativeStage.Climax => 0.5f,
                _ => 0.1f
            };
            
            return _random.NextFloat() < chance;
        }

        private void GenerateArcDrivenEvent(NarrativeArcData arc, Entity arcEntity, EntityCommandBuffer ecb)
        {
            // Create events that advance the narrative
            var eventDescription = GenerateArcEvent(arc);
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"Arc Event: {arc.ArcName}"),
                Description = eventDescription,
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = DetermineEventType(arc.Type),
                Category = DetermineEventCategory(arc.Type),
                Significance = arc.Stakes * arc.Tension,
                CivilizationId = arc.ProtagonistCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private FixedString512Bytes GenerateArcEvent(NarrativeArcData arc)
        {
            // Generate events appropriate to the arc type and stage
            return new FixedString512Bytes("A significant development occurs in the ongoing narrative.");
        }

        private ProceduralWorld.Simulation.Core.EventType DetermineEventType(NarrativeArcType arcType)
        {
            return arcType switch
            {
                NarrativeArcType.Epic => ProceduralWorld.Simulation.Core.EventType.Military,
                NarrativeArcType.Rise => ProceduralWorld.Simulation.Core.EventType.Political,
                NarrativeArcType.Fall => ProceduralWorld.Simulation.Core.EventType.Social,
                NarrativeArcType.Romance => ProceduralWorld.Simulation.Core.EventType.Diplomatic,
                _ => ProceduralWorld.Simulation.Core.EventType.Cultural
            };
        }

        private EventCategory DetermineEventCategory(NarrativeArcType arcType)
        {
            return arcType switch
            {
                NarrativeArcType.Epic => EventCategory.Hero,
                NarrativeArcType.Tragedy => EventCategory.Decline,
                NarrativeArcType.Rise => EventCategory.Growth,
                _ => EventCategory.Cultural
            };
        }

        private bool HasSignificantEvent(NarrativeArcData arc)
        {
            // Check if any significant events have occurred for the protagonist
            return false; // Placeholder
        }

        private void GenerateStageTransitionEvent(NarrativeArcData arc, Entity arcEntity, EntityCommandBuffer ecb)
        {
            var transitionEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"{arc.ArcName}: {arc.CurrentStage}"),
                Description = new FixedString512Bytes($"The story of {arc.ArcName} enters a new chapter."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = arc.Stakes * 0.6f,
                CivilizationId = arc.ProtagonistCivilization
            };
            
            _historySystem.AddEvent(transitionEvent);
        }

        private void ManageStoryTension()
        {
            // Ensure we don't have too many high-tension arcs at once
            // This helps with pacing and prevents story fatigue
        }

        private void ResolveCompletedArcs(EntityCommandBuffer ecb)
        {
            var arcs = _narrativeArcQuery.ToComponentDataArray<NarrativeArcData>(Allocator.Temp);
            var arcEntities = _narrativeArcQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < arcs.Length; i++)
            {
                var arc = arcs[i];
                var entity = arcEntities[i];
                
                if (arc.CurrentStage == NarrativeStage.Resolution && arc.ArcProgress >= 0.95f)
                {
                    // Create resolution event
                    CreateArcResolutionEvent(arc, ecb);
                    
                    // Remove from active arcs
                    for (int j = 0; j < _activeArcs.Length; j++)
                    {
                        if (_activeArcs[j].Equals(entity))
                        {
                            _activeArcs.RemoveAtSwapBack(j);
                            break;
                        }
                    }
                    
                    // Clean up the arc entity
                    ecb.DestroyEntity(entity);
                    
                    Debug.Log($"[NarrativeArcSystem] Resolved arc: {arc.ArcName}");
                }
            }
            
            arcs.Dispose();
            arcEntities.Dispose();
        }

        private void CreateArcResolutionEvent(NarrativeArcData arc, EntityCommandBuffer ecb)
        {
            var resolutionEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"Conclusion: {arc.ArcName}"),
                Description = GenerateResolutionDescription(arc),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = arc.Stakes,
                CivilizationId = arc.ProtagonistCivilization
            };
            
            _historySystem.AddEvent(resolutionEvent);
        }

        private FixedString512Bytes GenerateResolutionDescription(NarrativeArcData arc)
        {
            string resolution = arc.Type switch
            {
                NarrativeArcType.Rise => "achieved greatness and established their place in history",
                NarrativeArcType.Fall => "met their end, leaving behind only memories and ruins",
                NarrativeArcType.Redemption => "found redemption and renewed purpose",
                NarrativeArcType.Tragedy => "fell to forces beyond their control, teaching future generations",
                NarrativeArcType.Comedy => "overcame all obstacles and found happiness",
                NarrativeArcType.Epic => "completed their legendary quest and changed the world forever",
                _ => "reached the end of their remarkable journey"
            };
            
            return new FixedString512Bytes($"The story of {arc.ArcName} concludes as they {resolution}.");
        }
    }
} 