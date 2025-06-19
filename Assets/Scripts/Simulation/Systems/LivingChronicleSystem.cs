using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralWorld.Simulation.Systems
{
    // Helper class for organizing events into narrative chapters
    public class ChronicleChapter
    {
        public string Title { get; set; }
        public List<HistoricalEventRecord> Events { get; set; } = new List<HistoricalEventRecord>();
    }

    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    public partial class LivingChronicleSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _narrativeThreadQuery;
        private EntityQuery _chronicleEntryQuery;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 2f;
        private Unity.Mathematics.Random _random;
        
        // Chronicle management
        private NativeList<NarrativeThread> _activeThreads;
        private NativeList<ChronicleEntry> _chronicleEntries;
        private NativeHashMap<Entity, NativeList<int>> _civilizationThreads; // Civ -> Thread IDs
        private NativeHashMap<int, NativeList<int>> _threadConnections; // Thread -> Connected Threads
        private int _nextThreadId;
        private int _nextEntryId;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadWrite<AdaptivePersonalityData>()
            );
            
            _narrativeThreadQuery = GetEntityQuery(ComponentType.ReadWrite<NarrativeThreadData>());
            _chronicleEntryQuery = GetEntityQuery(ComponentType.ReadWrite<ChronicleEntry>());
            
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(777);
            _nextThreadId = 1;
            _nextEntryId = 1;
            
            _activeThreads = new NativeList<NarrativeThread>(Allocator.Persistent);
            _chronicleEntries = new NativeList<ChronicleEntry>(Allocator.Persistent);
            _civilizationThreads = new NativeHashMap<Entity, NativeList<int>>(64, Allocator.Persistent);
            _threadConnections = new NativeHashMap<int, NativeList<int>>(128, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_activeThreads.IsCreated) _activeThreads.Dispose();
            if (_chronicleEntries.IsCreated) _chronicleEntries.Dispose();
            if (_civilizationThreads.IsCreated) _civilizationThreads.Dispose();
            if (_threadConnections.IsCreated) _threadConnections.Dispose();
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
            
            // Analyze civilizations for new narrative threads
            AnalyzeForNewThreads(ecb);
            
            // Update existing threads
            UpdateNarrativeThreads(ecb);
            
            // Weave thread intersections
            WeaveThreadIntersections(ecb);
            
            // Generate chronicle entries from thread developments
            GenerateChronicleEntries(ecb);
            
            // Compile readable chronicles
            CompileReadableChronicles();
        }

        // ==== CORE EVENT PROCESSING ====
        public void ProcessHistoricalEvent(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            Debug.Log($"[LivingChronicleSystem] Processing historical event: {evt.Title} for {civ.Name} (Year {evt.Year})");
            
            // Transform raw event into rich chronicle entry with full context
            var chronicleEntry = CreateRichChronicleEntry(evt, civ, personality);
            
            // Find or create appropriate narrative threads
            var relevantThreads = FindRelevantThreads(evt, civ, personality);
            if (relevantThreads.Length == 0)
            {
                // Create new thread for this event
                var newThread = CreateNarrativeThread(evt, civ, personality);
                relevantThreads = new NativeArray<int>(1, Allocator.Temp);
                relevantThreads[0] = newThread.ThreadId;
            }
            
            // Add entry to threads and update causality
            foreach (var threadId in relevantThreads)
            {
                AddEntryToThread(chronicleEntry, threadId);
                UpdateThreadCausality(threadId, chronicleEntry);
            }
            
            // Check for thread intersections
            CheckThreadIntersections(relevantThreads, chronicleEntry);
            
            relevantThreads.Dispose();
        }

        private ChronicleEntry CreateRichChronicleEntry(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            var entry = new ChronicleEntry
            {
                EntryId = _nextEntryId++,
                OriginalEvent = evt,
                Year = evt.Year,
                PrimaryCivilization = evt.CivilizationId,
                
                // Rich contextual information
                ContextSummary = TruncateToFixedString128(GenerateEventContext(evt, civ, personality).ToString()),
                MotivationSummary = TruncateToFixedString128(AnalyzeEventMotivation(evt, civ, personality).ToString()),
                ConsequencesSummary = TruncateToFixedString128(PredictEventConsequences(evt, civ, personality).ToString()),
                EmotionalTone = DetermineEmotionalTone(evt, personality, civ),
                CulturalSignificance = CalculateCulturalSignificance(evt, civ),
                
                // Narrative elements
                NarrativeTension = CalculateNarrativeTension(evt, personality),
                CharacterDevelopment = TruncateToFixedString128(AnalyzeCharacterDevelopment(evt, personality).ToString()),
                ThematicElementCount = IdentifyThematicElements(evt, civ, personality),
                
                // Causality tracking
                DirectCauseCount = IdentifyDirectCauses(evt, civ, personality),
                ContributingFactorCount = IdentifyContributingFactors(evt, civ, personality),
                PredictedEffectCount = PredictLongTermEffects(evt, civ, personality),
                
                // Literary quality
                DramaticWeight = CalculateDramaticWeight(evt, personality),
                SymbolicMeaning = TruncateToFixedString128(ExtractSymbolicMeaning(evt, civ).ToString()),
                HistoricalParallelCount = FindHistoricalParallels(evt),
                
                IsProcessed = false,
                NeedsFollowUp = DetermineIfNeedsFollowUp(evt, civ, personality)
            };
            
            // Add to local list for system tracking
            _chronicleEntries.Add(entry);
            
            // Create ECS entity so it can be queried by the UI
            var chronicleEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(chronicleEntity, entry);
            
            Debug.Log($"[LivingChronicleSystem] Created chronicle entry {entry.EntryId} for {civ.Name} (Year {entry.Year})");
            
            return entry;
        }

        private FixedString512Bytes GenerateEventContext(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            var context = new StringBuilder();
            
            // Environmental context
            context.Append($"In the {GetBiomeDescription(civ.Position)} lands of {civ.Name}, ");
            
            // Political/social context
            if (civ.Stability < 0.3f)
                context.Append("amid growing unrest and social upheaval, ");
            else if (civ.Stability > 0.8f)
                context.Append("during a time of peace and prosperity, ");
            
            // Cultural context
            if (civ.Culture > 7f)
                context.Append("in an age of cultural flowering, ");
            else if (civ.Culture < 3f)
                context.Append("in culturally turbulent times, ");
            
            // Personality context
            context.Append(GetPersonalityContext(personality));
            
            // Population context
            if (civ.Population > 15000f)
                context.Append("the great civilization ");
            else if (civ.Population < 3000f)
                context.Append("the struggling settlement ");
            else
                context.Append("the growing community ");
            
            context.Append($"faced {GetEventContextualDescription(evt)}.");
            
            return new FixedString512Bytes(context.ToString());
        }

        private FixedString512Bytes AnalyzeEventMotivation(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            string motivation = evt.Type switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military when personality.CurrentPersonality.Aggressiveness > 7f => 
                    $"Driven by an insatiable hunger for conquest and glory, {civ.Name} sought to expand their dominion.",
                
                ProceduralWorld.Simulation.Core.EventType.Military when personality.CurrentPersonality.Defensiveness > 7f => 
                    $"Fearing for their survival, {civ.Name} took desperate measures to protect their people.",
                
                ProceduralWorld.Simulation.Core.EventType.Cultural when civ.Culture > 7f => 
                    $"In pursuit of artistic and intellectual excellence, {civ.Name} embarked on this cultural endeavor.",
                
                ProceduralWorld.Simulation.Core.EventType.Religious when civ.Religion > 7f => 
                    $"Guided by divine inspiration and unwavering faith, {civ.Name} followed their sacred calling.",
                
                ProceduralWorld.Simulation.Core.EventType.Economic when personality.CurrentPersonality.Greed > 6f => 
                    $"Motivated by the promise of wealth and prosperity, {civ.Name} pursued this economic opportunity.",
                
                ProceduralWorld.Simulation.Core.EventType.Diplomatic when civ.Diplomacy > 6f => 
                    $"Seeking harmony and mutual benefit, {civ.Name} extended the hand of diplomacy.",
                
                ProceduralWorld.Simulation.Core.EventType.Social when personality.CurrentStress > 0.7f => 
                    $"Under immense pressure and social strain, {civ.Name} was compelled to act.",
                
                _ => $"Circumstances and the spirit of the times moved {civ.Name} to take this momentous step."
            };
            
            return new FixedString512Bytes(motivation);
        }

        private FixedString512Bytes PredictEventConsequences(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            var consequences = new StringBuilder();
            
            // Immediate consequences
            consequences.Append("This event would immediately ");
            
            switch (evt.Type)
            {
                case ProceduralWorld.Simulation.Core.EventType.Military:
                    if (evt.Significance > 2f)
                        consequences.Append("reshape the balance of power in the region, ");
                    else
                        consequences.Append("influence local military dynamics, ");
                    break;
                    
                case ProceduralWorld.Simulation.Core.EventType.Cultural:
                    consequences.Append("inspire new forms of artistic and intellectual expression, ");
                    break;
                    
                case ProceduralWorld.Simulation.Core.EventType.Religious:
                    consequences.Append("deepen spiritual beliefs and religious practices, ");
                    break;
                    
                case ProceduralWorld.Simulation.Core.EventType.Economic:
                    consequences.Append("alter trade patterns and economic relationships, ");
                    break;
            }
            
            // Long-term consequences based on significance
            if (evt.Significance > 3f)
            {
                consequences.Append("and its effects would echo through generations, ");
                consequences.Append("fundamentally changing the course of history for ");
                consequences.Append($"{civ.Name} and their neighbors.");
            }
            else if (evt.Significance > 1.5f)
            {
                consequences.Append("with lasting effects that would shape ");
                consequences.Append($"the future development of {civ.Name}.");
            }
            else
            {
                consequences.Append("though its impact would be primarily local and temporary.");
            }
            
            return new FixedString512Bytes(consequences.ToString());
        }

        private EmotionalTone DetermineEmotionalTone(HistoricalEventRecord evt, AdaptivePersonalityData personality, CivilizationData civ)
        {
            // Analyze the emotional resonance of the event
            if (evt.Type == ProceduralWorld.Simulation.Core.EventType.Military && evt.Significance > 2f)
            {
                return personality.CurrentPersonality.Aggressiveness > 6f ? 
                    EmotionalTone.Triumphant : EmotionalTone.Grim;
            }
            
            if (evt.Category == EventCategory.Collapse || evt.Category == EventCategory.Military)
                return EmotionalTone.Tragic;
            
            if (evt.Category == EventCategory.Growth || evt.Category == EventCategory.Cultural)
                return EmotionalTone.Hopeful;
            
            if (personality.CurrentStress > 0.7f)
                return EmotionalTone.Tense;
            
            if (civ.Stability > 0.8f)
                return EmotionalTone.Peaceful;
            
            return EmotionalTone.Neutral;
        }

        // ==== NARRATIVE THREAD MANAGEMENT ====
        private void AnalyzeForNewThreads(EntityCommandBuffer ecb)
        {
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                // Check for conditions that warrant new narrative threads
                CheckForNewThreadConditions(civ, personality, entity, ecb);
            }
            
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private void CheckForNewThreadConditions(CivilizationData civ, AdaptivePersonalityData personality, Entity entity, EntityCommandBuffer ecb)
        {
            // Political intrigue thread
            if (civ.Stability < 0.4f && !HasActiveThread(entity, ThreadType.PoliticalIntrigue))
            {
                CreatePoliticalIntrigueThread(civ, personality, entity);
            }
            
            // Religious movement thread
            if (civ.Religion > 7f && !HasActiveThread(entity, ThreadType.ReligiousMovement))
            {
                CreateReligiousMovementThread(civ, personality, entity);
            }
            
            // Dynasty thread
            if (civ.Population > 10000f && civ.Culture > 6f && !HasActiveThread(entity, ThreadType.Dynasty))
            {
                CreateDynastyThread(civ, personality, entity);
            }
            
            // Technological revolution thread
            if (civ.Technology > 6f && civ.Innovation > 5f && !HasActiveThread(entity, ThreadType.TechnologicalRevolution))
            {
                CreateTechnologicalThread(civ, personality, entity);
            }
            
            // Cultural renaissance thread
            if (civ.Culture > 8f && civ.Stability > 0.7f && !HasActiveThread(entity, ThreadType.CulturalRenaissance))
            {
                CreateCulturalRenaissanceThread(civ, personality, entity);
            }
        }

        private NarrativeThread CreateNarrativeThread(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality)
        {
            var threadType = DetermineThreadType(evt, civ, personality);
            var thread = new NarrativeThread
            {
                ThreadId = _nextThreadId++,
                Type = threadType,
                PrimaryCivilization = evt.CivilizationId,
                ThreadName = GenerateThreadName(threadType, civ),
                Description = GenerateThreadDescription(threadType, civ, personality),
                StartYear = evt.Year,
                CurrentStage = ThreadStage.Genesis,
                Tension = 0.3f,
                Momentum = 0.5f,
                Significance = evt.Significance,
                IsActive = true,
                LastUpdateYear = evt.Year,
                EntryCount = 0,
                ThematicFocus = DetermineThematicFocus(threadType, personality)
            };
            
            _activeThreads.Add(thread);
            
            // Add to civilization's thread list
            if (!_civilizationThreads.ContainsKey(evt.CivilizationId))
            {
                _civilizationThreads[evt.CivilizationId] = new NativeList<int>(Allocator.Persistent);
            }
            _civilizationThreads[evt.CivilizationId].Add(thread.ThreadId);
            
            return thread;
        }

        // ==== CHRONICLE COMPILATION ====
        public NativeArray<CompiledChronicle> CompileChroniclesForCivilization(Entity civilizationEntity, Allocator allocator)
        {
            var chronicles = new NativeList<CompiledChronicle>(allocator);
            
            // Check if we have specific threads for this civilization
            if (_civilizationThreads.ContainsKey(civilizationEntity))
            {
                var threadIds = _civilizationThreads[civilizationEntity];
                
                // Group related threads into chronicle volumes
                var chronicleVolumes = GroupThreadsIntoVolumes(threadIds);
                
                foreach (var volume in chronicleVolumes)
                {
                    var compiledChronicle = CompileChronicleVolume(volume, civilizationEntity);
                    chronicles.Add(compiledChronicle);
                }
            }
            else
            {
                // No specific threads found, generate a general chronicle from historical events
                Debug.Log($"[LivingChronicleSystem] No threads found for civilization, generating general chronicle");
                
                var generalChronicle = GenerateGeneralChronicle(civilizationEntity);
                if (generalChronicle.ChronicleText.Length > 0)
                {
                    chronicles.Add(generalChronicle);
                }
            }
            
            return chronicles.AsArray();
        }

        private CompiledChronicle CompileChronicleVolume(NativeList<int> threadIds, Entity civilizationEntity)
        {
            var chronicle = new CompiledChronicle
            {
                ChronicleId = GenerateChronicleId(),
                CivilizationEntity = civilizationEntity,
                Title = GenerateChronicleTitle(threadIds, civilizationEntity),
                Subtitle = GenerateChronicleSubtitle(threadIds),
                ChronicleText = CompileNarrativeText(threadIds),
                StartYear = GetEarliestYear(threadIds),
                EndYear = GetLatestYear(threadIds),
                TotalEntries = CountTotalEntries(threadIds),
                DramaticIntensity = CalculateOverallDramaticIntensity(threadIds),
                HistoricalSignificance = CalculateOverallSignificance(threadIds),
                ThematicElementCount = ExtractOverallThemes(threadIds),
                CharacterArcCount = ExtractCharacterArcs(threadIds),
                MajorEventCount = ExtractMajorEvents(threadIds),
                IsComplete = DetermineIfComplete(threadIds)
            };
            
            return chronicle;
        }

        private CompiledChronicle GenerateGeneralChronicle(Entity civilizationEntity)
        {
            // Get civilization data
            string civilizationName = "Unknown Civilization";
            if (EntityManager.HasComponent<CivilizationData>(civilizationEntity))
            {
                var civData = EntityManager.GetComponentData<CivilizationData>(civilizationEntity);
                civilizationName = civData.Name.ToString();
            }
            
            // Get historical events from the world history system
            var historySystem = World.GetExistingSystemManaged<WorldHistorySystem>();
            if (historySystem == null)
            {
                return new CompiledChronicle
                {
                    ChronicleId = GenerateChronicleId(),
                    CivilizationEntity = civilizationEntity,
                    Title = new FixedString128Bytes($"Chronicles of {civilizationName}"),
                    Subtitle = new FixedString512Bytes("A Tale Yet Untold"),
                    ChronicleText = new FixedString4096Bytes("The chronicles remain unwritten, for the keepers of history have yet to arrive..."),
                    StartYear = 0,
                    EndYear = 0,
                    TotalEntries = 0,
                    DramaticIntensity = 0f,
                    HistoricalSignificance = 0f,
                    ThematicElementCount = 0,
                    CharacterArcCount = 0,
                    MajorEventCount = 0,
                    IsComplete = false
                };
            }
            
            var allEvents = historySystem.GetHistoricalEvents(Allocator.Temp);
            
            // Filter events for this civilization (or use all if none specific)
            var relevantEvents = new List<HistoricalEventRecord>();
            for (int i = 0; i < allEvents.Length; i++)
            {
                var evt = allEvents[i];
                // Include events that are either for this civilization or general world events
                if (evt.CivilizationId.Equals(civilizationEntity) || evt.CivilizationId.Equals(Entity.Null))
                {
                    relevantEvents.Add(evt);
                }
            }
            
            // If no specific events, use all events as world context
            if (relevantEvents.Count == 0)
            {
                for (int i = 0; i < allEvents.Length; i++)
                {
                    relevantEvents.Add(allEvents[i]);
                }
            }
            
            var chronicleText = new FixedString4096Bytes();
            if (relevantEvents.Count > 0)
            {
                // Generate chronicle text from events
                var narrative = GenerateChronicleFromEvents(relevantEvents, civilizationName);
                chronicleText = new FixedString4096Bytes(narrative);
            }
            else
            {
                chronicleText = new FixedString4096Bytes($"The Chronicles of {civilizationName}\n\nIn the beginning, there was silence. The world waits for the first chapter of history to be written...\n\nLet the simulation run to generate historical events.");
            }
            
            var chronicle = new CompiledChronicle
            {
                ChronicleId = GenerateChronicleId(),
                CivilizationEntity = civilizationEntity,
                Title = new FixedString128Bytes($"Chronicles of {civilizationName}"),
                Subtitle = new FixedString512Bytes("The Rise and Deeds of a Great Civilization"),
                ChronicleText = chronicleText,
                StartYear = relevantEvents.Count > 0 ? relevantEvents.Min(e => e.Year) : 0,
                EndYear = relevantEvents.Count > 0 ? relevantEvents.Max(e => e.Year) : 0,
                TotalEntries = relevantEvents.Count,
                DramaticIntensity = 1f,
                HistoricalSignificance = 1f,
                ThematicElementCount = 3,
                CharacterArcCount = 1,
                MajorEventCount = relevantEvents.Count,
                IsComplete = false
            };
            
            allEvents.Dispose();
            return chronicle;
        }
        
        private string GenerateChronicleFromEvents(List<HistoricalEventRecord> events, string civilizationName)
        {
            var narrative = new StringBuilder();
            
            // Sort events by year
            events.Sort((a, b) => a.Year.CompareTo(b.Year));
            
            // Chronicle opening
            narrative.AppendLine($"THE CHRONICLES OF {civilizationName.ToUpper()}");
            narrative.AppendLine("═══════════════════════════════════════════════════════════");
            narrative.AppendLine();
            narrative.AppendLine("Here are recorded the deeds and destinies of a great civilization,");
            narrative.AppendLine("their triumphs and tragedies, their rise to glory and the trials");
            narrative.AppendLine("that shaped their eternal legacy...");
            narrative.AppendLine();
            
            // Group events into chapters
            var chapters = OrganizeEventsIntoChapters(events);
            
            for (int chapterIndex = 0; chapterIndex < chapters.Count; chapterIndex++)
            {
                var chapter = chapters[chapterIndex];
                
                narrative.AppendLine($"CHAPTER {chapterIndex + 1}: {chapter.Title}");
                narrative.AppendLine(new string('─', 50));
                narrative.AppendLine();
                
                // Generate narrative prose for each event in the chapter
                for (int eventIndex = 0; eventIndex < chapter.Events.Count; eventIndex++)
                {
                    var evt = chapter.Events[eventIndex];
                    var narrativeParagraph = GenerateEventNarrative(evt, eventIndex == 0);
                    narrative.AppendLine(narrativeParagraph);
                    narrative.AppendLine();
                }
                
                // Add chapter conclusion
                if (chapterIndex < chapters.Count - 1)
                {
                    narrative.AppendLine("Thus ended this chapter of their story, but greater deeds were yet to come...");
                }
                else
                {
                    narrative.AppendLine("And so the chronicles record these deeds for all time, that future generations might remember the glory and wisdom of those who came before.");
                }
                
                narrative.AppendLine();
                narrative.AppendLine();
            }
            
            var fullText = narrative.ToString();
            
            // Ensure we don't exceed the FixedString4096Bytes limit
            if (System.Text.Encoding.UTF8.GetByteCount(fullText) > 4000)
            {
                // Truncate at a reasonable paragraph break
                var lines = fullText.Split('\n');
                var truncatedText = new StringBuilder();
                int byteCount = 0;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var lineBytes = System.Text.Encoding.UTF8.GetByteCount(lines[i] + "\n");
                    if (byteCount + lineBytes > 3800)
                    {
                        truncatedText.AppendLine();
                        truncatedText.AppendLine("...and many more tales besides, too numerous to record in these pages.");
                        break;
                    }
                    truncatedText.AppendLine(lines[i]);
                    byteCount += lineBytes;
                }
                
                fullText = truncatedText.ToString();
            }
            
            return fullText;
        }

        // ==== NARRATIVE INTELLIGENCE ====
        private FixedString4096Bytes CompileNarrativeText(NativeList<int> threadIds)
        {
            // Get the world history system to access actual events
            var historySystem = World.GetExistingSystemManaged<WorldHistorySystem>();
            if (historySystem == null)
            {
                return new FixedString4096Bytes("The chronicles remain unwritten, for the keepers of history have yet to arrive...");
            }
            
            // Get all historical events
            var allEvents = historySystem.GetHistoricalEvents(Allocator.Temp);
            if (allEvents.Length == 0)
            {
                allEvents.Dispose();
                return new FixedString4096Bytes("In the beginning, there was silence. The world waits for the first chapter of history to be written...");
            }
            
            // Sort events by year for chronological narrative
            var eventArray = allEvents.AsArray();
            
            // Create a list for sorting since NativeArray.Sort has issues with lambda expressions
            var eventList = new List<HistoricalEventRecord>();
            for (int i = 0; i < eventArray.Length; i++)
            {
                eventList.Add(eventArray[i]);
            }
            eventList.Sort((a, b) => a.Year.CompareTo(b.Year));
            
            var narrative = new StringBuilder();
            
            // Chronicle opening - get civilization name from first event
            string civilizationName = "The Ancient Realm";
            if (eventList.Count > 0)
            {
                var firstEvent = eventList[0];
                if (EntityManager.HasComponent<CivilizationData>(firstEvent.CivilizationId))
                {
                    var civData = EntityManager.GetComponentData<CivilizationData>(firstEvent.CivilizationId);
                    civilizationName = civData.Name.ToString();
                }
            }
            
            narrative.AppendLine($"THE CHRONICLES OF {civilizationName.ToUpper()}");
            narrative.AppendLine("═══════════════════════════════════════════════════════════");
            narrative.AppendLine();
            narrative.AppendLine("Here are recorded the deeds and destinies of a great civilization,");
            narrative.AppendLine("their triumphs and tragedies, their rise to glory and the trials");
            narrative.AppendLine("that shaped their eternal legacy...");
            narrative.AppendLine();
            
            // Group events into narrative chapters by time periods
            var chapters = OrganizeEventsIntoChapters(eventList);
            
            for (int chapterIndex = 0; chapterIndex < chapters.Count; chapterIndex++)
            {
                var chapter = chapters[chapterIndex];
                
                narrative.AppendLine($"CHAPTER {chapterIndex + 1}: {chapter.Title}");
                narrative.AppendLine(new string('─', 50));
                narrative.AppendLine();
                
                // Generate narrative prose for each event in the chapter
                for (int eventIndex = 0; eventIndex < chapter.Events.Count; eventIndex++)
                {
                    var evt = chapter.Events[eventIndex];
                    var narrativeParagraph = GenerateEventNarrative(evt, eventIndex == 0);
                    narrative.AppendLine(narrativeParagraph);
                    narrative.AppendLine();
                }
                
                // Add chapter conclusion
                if (chapterIndex < chapters.Count - 1)
                {
                    narrative.AppendLine("Thus ended this chapter of their story, but greater deeds were yet to come...");
                }
                else
                {
                    narrative.AppendLine("And so the chronicles record these deeds for all time, that future generations might remember the glory and wisdom of those who came before.");
                }
                
                narrative.AppendLine();
                narrative.AppendLine();
            }
            
            allEvents.Dispose();
            
            // Ensure we don't exceed the FixedString4096Bytes limit
            var fullText = narrative.ToString();
            if (System.Text.Encoding.UTF8.GetByteCount(fullText) > 4000) // Leave some safety margin
            {
                // Truncate at a reasonable paragraph break
                var lines = fullText.Split('\n');
                var truncatedText = new StringBuilder();
                int byteCount = 0;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var lineBytes = System.Text.Encoding.UTF8.GetByteCount(lines[i] + "\n");
                    if (byteCount + lineBytes > 3800) // Leave margin for ending
                    {
                        truncatedText.AppendLine();
                        truncatedText.AppendLine("...and many more tales besides, too numerous to record in these pages.");
                        break;
                    }
                    truncatedText.AppendLine(lines[i]);
                    byteCount += lineBytes;
                }
                
                fullText = truncatedText.ToString();
            }
            
            return new FixedString4096Bytes(fullText);
        }

        private string GenerateNarrativeParagraph(ChronicleEntry entry)
        {
            var paragraph = new StringBuilder();
            
            // Temporal context
            paragraph.Append($"In the year {entry.Year}, ");
            
            // Contextual setup
            paragraph.Append(entry.ContextSummary.ToString());
            paragraph.Append(" ");
            
            // Main event with motivation
            paragraph.Append(entry.MotivationSummary.ToString());
            paragraph.Append(" ");
            
            // Event description from original event
            paragraph.Append(entry.OriginalEvent.Description.ToString());
            paragraph.Append(" ");
            
            // Consequences and meaning
            paragraph.Append(entry.ConsequencesSummary.ToString());
            
            // Add symbolic/thematic meaning if significant
            if (entry.DramaticWeight > 2f)
            {
                paragraph.Append(" ");
                paragraph.Append(entry.SymbolicMeaning.ToString());
            }
            
            return paragraph.ToString();
        }

        // ==== HELPER METHODS ====
        private string GetBiomeDescription(float3 position)
        {
            // This would integrate with your biome system
            return "verdant"; // Placeholder
        }

        private string GetPersonalityContext(AdaptivePersonalityData personality)
        {
            if (personality.Stage == PersonalityEvolutionStage.Broken)
                return "scarred by past traumas, ";
            if (personality.Stage == PersonalityEvolutionStage.Enlightened)
                return "guided by hard-won wisdom, ";
            if (personality.CurrentStress > 0.7f)
                return "under tremendous pressure, ";
            return "";
        }

        private string GetEventContextualDescription(HistoricalEventRecord evt)
        {
            return evt.Type switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military => "the crucible of war",
                ProceduralWorld.Simulation.Core.EventType.Cultural => "a moment of cultural transformation",
                ProceduralWorld.Simulation.Core.EventType.Religious => "a test of faith and devotion",
                ProceduralWorld.Simulation.Core.EventType.Economic => "the challenges of prosperity and want",
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => "the delicate dance of diplomacy",
                ProceduralWorld.Simulation.Core.EventType.Social => "the winds of social change",
                _ => "the turning of fate's wheel"
            };
        }

        // Placeholder methods - these would be fully implemented
        private NativeArray<int> FindRelevantThreads(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => new NativeArray<int>(0, Allocator.Temp);
        private void AddEntryToThread(ChronicleEntry entry, int threadId) { }
        private void UpdateThreadCausality(int threadId, ChronicleEntry entry) { }
        private void CheckThreadIntersections(NativeArray<int> threadIds, ChronicleEntry entry) { }
        private void UpdateNarrativeThreads(EntityCommandBuffer ecb) { }
        private void WeaveThreadIntersections(EntityCommandBuffer ecb) { }
        private void GenerateChronicleEntries(EntityCommandBuffer ecb) { }
        private void CompileReadableChronicles() { }
        private bool HasActiveThread(Entity entity, ThreadType type) => false;
        private void CreatePoliticalIntrigueThread(CivilizationData civ, AdaptivePersonalityData personality, Entity entity) { }
        private void CreateReligiousMovementThread(CivilizationData civ, AdaptivePersonalityData personality, Entity entity) { }
        private void CreateDynastyThread(CivilizationData civ, AdaptivePersonalityData personality, Entity entity) { }
        private void CreateTechnologicalThread(CivilizationData civ, AdaptivePersonalityData personality, Entity entity) { }
        private void CreateCulturalRenaissanceThread(CivilizationData civ, AdaptivePersonalityData personality, Entity entity) { }
        
        // Additional placeholder methods
        private ThreadType DetermineThreadType(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => ThreadType.General;
        private FixedString128Bytes GenerateThreadName(ThreadType type, CivilizationData civ) => new FixedString128Bytes("Thread");
        private FixedString512Bytes GenerateThreadDescription(ThreadType type, CivilizationData civ, AdaptivePersonalityData personality) => new FixedString512Bytes("Description");
        private ThematicFocus DetermineThematicFocus(ThreadType type, AdaptivePersonalityData personality) => ThematicFocus.Power;
        private float CalculateCulturalSignificance(HistoricalEventRecord evt, CivilizationData civ) => 0.5f;
        private float CalculateNarrativeTension(HistoricalEventRecord evt, AdaptivePersonalityData personality) => 0.5f;
        private FixedString512Bytes AnalyzeCharacterDevelopment(HistoricalEventRecord evt, AdaptivePersonalityData personality) => new FixedString512Bytes("Development");
        private int IdentifyThematicElements(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => 0;
        private int IdentifyDirectCauses(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => 0;
        private int IdentifyContributingFactors(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => 0;
        private int PredictLongTermEffects(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => 0;
        private float CalculateDramaticWeight(HistoricalEventRecord evt, AdaptivePersonalityData personality) => 1f;
        private FixedString512Bytes ExtractSymbolicMeaning(HistoricalEventRecord evt, CivilizationData civ) => new FixedString512Bytes("Meaning");
        private int FindHistoricalParallels(HistoricalEventRecord evt) => 0;
        private bool DetermineIfNeedsFollowUp(HistoricalEventRecord evt, CivilizationData civ, AdaptivePersonalityData personality) => false;
        private NativeList<NativeList<int>> GroupThreadsIntoVolumes(NativeList<int> threadIds) => new NativeList<NativeList<int>>(Allocator.Temp);
        private int GenerateChronicleId() => _random.NextInt();
        private FixedString128Bytes GenerateChronicleTitle(NativeList<int> threadIds, Entity entity) => new FixedString128Bytes("Chronicle");
        private FixedString512Bytes GenerateChronicleSubtitle(NativeList<int> threadIds) => new FixedString512Bytes("Subtitle");
        private int GetEarliestYear(NativeList<int> threadIds) => 0;
        private int GetLatestYear(NativeList<int> threadIds) => 100;
        private int CountTotalEntries(NativeList<int> threadIds) => 0;
        private float CalculateOverallDramaticIntensity(NativeList<int> threadIds) => 1f;
        private float CalculateOverallSignificance(NativeList<int> threadIds) => 1f;
        private int ExtractOverallThemes(NativeList<int> threadIds) => 0;
        private int ExtractCharacterArcs(NativeList<int> threadIds) => 0;
        private int ExtractMajorEvents(NativeList<int> threadIds) => 0;
        private bool DetermineIfComplete(NativeList<int> threadIds) => false;
        private List<ChronicleChapter> OrganizeEventsIntoChapters(List<HistoricalEventRecord> events)
        {
            var chapters = new List<ChronicleChapter>();
            if (events.Count == 0) return chapters;
            
            // Group events by time periods (every 20-30 years or by significance)
            var currentChapter = new ChronicleChapter();
            int chapterStartYear = events[0].Year;
            var currentEvents = new List<HistoricalEventRecord>();
            
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                
                // Start a new chapter if enough time has passed or we have enough events
                bool shouldStartNewChapter = (evt.Year - chapterStartYear > 25) || 
                                           (currentEvents.Count >= 8) ||
                                           (evt.Significance >= 4.0f && currentEvents.Count > 0);
                
                if (shouldStartNewChapter && currentEvents.Count > 0)
                {
                    currentChapter.Events = currentEvents;
                    currentChapter.Title = GenerateChapterTitle(currentEvents, chapterStartYear, currentEvents[currentEvents.Count - 1].Year);
                    chapters.Add(currentChapter);
                    
                    // Start new chapter
                    currentChapter = new ChronicleChapter();
                    currentEvents = new List<HistoricalEventRecord>();
                    chapterStartYear = evt.Year;
                }
                
                currentEvents.Add(evt);
            }
            
            // Add the final chapter
            if (currentEvents.Count > 0)
            {
                currentChapter.Events = currentEvents;
                currentChapter.Title = GenerateChapterTitle(currentEvents, chapterStartYear, currentEvents[currentEvents.Count - 1].Year);
                chapters.Add(currentChapter);
            }
            
            return chapters;
        }
        
        private string GenerateChapterTitle(List<HistoricalEventRecord> events, int startYear, int endYear)
        {
            if (events.Count == 0) return "The Silent Years";
            
            // Find the most significant event type in this chapter
            var eventTypes = new Dictionary<ProceduralWorld.Simulation.Core.EventType, int>();
            var eventCategories = new Dictionary<EventCategory, int>();
            float maxSignificance = 0f;
            HistoricalEventRecord mostSignificantEvent = events[0];
            
            foreach (var evt in events)
            {
                if (!eventTypes.ContainsKey(evt.Type)) eventTypes[evt.Type] = 0;
                eventTypes[evt.Type]++;
                
                if (!eventCategories.ContainsKey(evt.Category)) eventCategories[evt.Category] = 0;
                eventCategories[evt.Category]++;
                
                if (evt.Significance > maxSignificance)
                {
                    maxSignificance = evt.Significance;
                    mostSignificantEvent = evt;
                }
            }
            
            // Generate EPIC title based on dominant themes
            var dominantType = eventTypes.OrderByDescending(kvp => kvp.Value).First().Key;
            var dominantCategory = eventCategories.OrderByDescending(kvp => kvp.Value).First().Key;
            
            string title = dominantType switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military when dominantCategory == EventCategory.Conflict => 
                    GetRandomFromArray(new[] { "The Blood-Soaked Conquest", "The War of Shattered Crowns", "The Great Devastation", "The Crimson Campaigns", "The Age of Iron and Blood" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Military when dominantCategory == EventCategory.Coalition => 
                    GetRandomFromArray(new[] { "The Sacred Alliance", "The Brotherhood of Steel", "The Unbreakable Pact", "The Coalition of Destiny", "The United Banners" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Cultural => 
                    GetRandomFromArray(new[] { "The Golden Renaissance", "The Age of Wonders", "The Cultural Awakening", "The Time of Great Artists", "The Flowering of Genius" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Religious => 
                    GetRandomFromArray(new[] { "The Divine Revelation", "The Sacred Reformation", "The Holy Awakening", "The Spiritual Revolution", "The Age of Prophets" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Economic => 
                    GetRandomFromArray(new[] { "The Age of Gold", "The Merchant Princes", "The Great Prosperity", "The Trading Empire", "The Golden Current" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => 
                    GetRandomFromArray(new[] { "The Great Negotiations", "The Web of Alliances", "The Diplomatic Revolution", "The Peace Makers", "The Silent Wars" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Social when dominantCategory == EventCategory.Collapse => 
                    GetRandomFromArray(new[] { "The Great Collapse", "The Shattering", "The Time of Ashes", "The Dark Descent", "The Broken Foundations" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Social => 
                    GetRandomFromArray(new[] { "The Social Revolution", "The Great Transformation", "The People's Awakening", "The New Order", "The Changing Tide" }),
                    
                ProceduralWorld.Simulation.Core.EventType.Political => 
                    GetRandomFromArray(new[] { "The Throne Wars", "The Political Upheaval", "The Game of Crowns", "The Rise of New Powers", "The Struggle for Dominion" }),
                    
                _ => GetRandomFromArray(new[] { "The Turning of Fate", "The Age of Legends", "The Time of Wonders", "The Great Change", "The Epoch of Destiny" })
            };
            
            // Add dramatic year range
            if (startYear == endYear)
                title += $" (The Year {startYear})";
            else
                title += $" ({startYear}-{endYear} A.D.)";
            
            return title;
        }
        
        private string GetRandomFromArray(string[] array)
        {
            return array[_random.NextInt(0, array.Length)];
        }
        
        private string GenerateEventNarrative(HistoricalEventRecord evt, bool isChapterOpening)
        {
            var narrative = new StringBuilder();
            
            // Get civilization name for narrative
            string civName = "the realm";
            if (EntityManager.HasComponent<CivilizationData>(evt.CivilizationId))
            {
                var civData = EntityManager.GetComponentData<CivilizationData>(evt.CivilizationId);
                civName = civData.Name.ToString();
            }
            
            // Opening phrase
            if (isChapterOpening)
            {
                narrative.Append($"In the year {evt.Year}, ");
            }
            else
            {
                var timeTransitions = new[] { 
                    "Soon after, ", "In time, ", "As the seasons passed, ", 
                    "Not long thereafter, ", "In those days, ", "Meanwhile, " 
                };
                narrative.Append(timeTransitions[_random.NextInt(0, timeTransitions.Length)]);
            }
            
            // Generate narrative based on event type and category
            string eventNarrative = GenerateEventSpecificNarrative(evt, civName);
            narrative.Append(eventNarrative);
            
            // Add consequence or impact if significant
            if (evt.Significance > 2.0f)
            {
                narrative.Append(" ");
                narrative.Append(GenerateEventConsequence(evt, civName));
            }
            
            return narrative.ToString();
        }
        
        private string GenerateEventSpecificNarrative(HistoricalEventRecord evt, string civName)
        {
            // COMPLETELY IGNORE the boring base description and generate epic narrative instead
            string baseDescription = evt.Description.ToString();
            
            // Generate DRAMATIC narrative based on event type and category
            return evt.Type switch
            {
                ProceduralWorld.Simulation.Core.EventType.Military when evt.Category == EventCategory.Conflict => 
                    GenerateMilitaryConflictNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Military when evt.Category == EventCategory.Coalition => 
                    GenerateAllianceNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Cultural => 
                    GenerateCulturalNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Religious => 
                    GenerateReligiousNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Economic => 
                    GenerateEconomicNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Diplomatic => 
                    GenerateDiplomaticNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Social when evt.Category == EventCategory.Collapse => 
                    GenerateSocialCollapseNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Social => 
                    GenerateSocialTransformationNarrative(evt, civName),
                    
                ProceduralWorld.Simulation.Core.EventType.Political => 
                    GeneratePoliticalNarrative(evt, civName),
                    
                _ => GenerateGenericEpicNarrative(evt, civName)
            };
        }
        
        private string GenerateMilitaryConflictNarrative(HistoricalEventRecord evt, string civName)
        {
            var epicEvents = new string[]
            {
                $"The war horns echoed across the lands as {civName} marshaled their greatest armies. Steel clashed against steel in battles that would be sung of for generations, as heroes rose and fell beneath banners soaked in blood and glory.",
                $"In the darkest hour, when all seemed lost, the warriors of {civName} made their legendary stand. Against impossible odds, they fought with the fury of their ancestors, turning the tide of a war that threatened to consume all they held dear.",
                $"The siege lasted for months, with {civName} either defending their sacred walls or breaking down those of their enemies. Catapults hurled death through the air, while brave souls scaled walls under cover of darkness, writing their names in the annals of legend.",
                $"A great betrayal shook the foundations of {civName} as trusted allies turned their blades upon those they once called brothers. The resulting conflict tore families apart and reshaped the very soul of the nation.",
                $"The final battle approached like a storm on the horizon. {civName} gathered their mightiest champions for a confrontation that would determine the fate of kingdoms. When the dust settled, the world itself had changed."
            };
            
            return epicEvents[_random.NextInt(0, epicEvents.Length)];
        }
        
        private string GenerateAllianceNarrative(HistoricalEventRecord evt, string civName)
        {
            var allianceEvents = new string[]
            {
                $"In a grand ceremony witnessed by thousands, {civName} forged an alliance that would reshape the balance of power. Ancient enemies became brothers-in-arms, united by sacred oaths and shared destiny.",
                $"Secret negotiations in moonlit chambers led to an unprecedented pact. {civName} and their newfound allies exchanged hostages, treasures, and promises that would bind their fates together through triumph and tragedy.",
                $"A royal marriage united the houses, but behind the celebration lay deeper currents of political intrigue. {civName} had gained powerful allies, but at what cost to their independence?",
                $"When faced with a common threat that dwarfed their petty squabbles, {civName} and their former rivals put aside generations of bloodshed. The alliance born of necessity would prove stronger than any forged in friendship.",
                $"The great council convened at the sacred neutral ground, where representatives of {civName} and allied nations hammered out a treaty written in gold and sealed with ancient magic."
            };
            
            return allianceEvents[_random.NextInt(0, allianceEvents.Length)];
        }
        
        private string GenerateCulturalNarrative(HistoricalEventRecord evt, string civName)
        {
            var culturalEvents = new string[]
            {
                $"A renaissance of art and learning swept through {civName} like wildfire. Master craftsmen created works of such beauty that neighboring kingdoms wept with envy, while scholars unlocked secrets that had been lost for centuries.",
                $"The great festival of {civName} became legendary, drawing pilgrims and artists from across the known world. For seven days and nights, the streets flowed with music, wine, and wonder as the people celebrated their cultural awakening.",
                $"A mysterious bard arrived in {civName} carrying tales of distant lands and forgotten magic. Their songs and stories transformed the very soul of the people, inspiring a new age of creativity and imagination.",
                $"The grand library of {civName} was completed, its towers reaching toward the heavens and its halls filled with the collected wisdom of ages. Scholars came from far and wide to study in its hallowed chambers.",
                $"A new artistic movement emerged in {civName}, challenging old traditions and creating works so revolutionary that they divided the population into passionate supporters and fierce critics."
            };
            
            return culturalEvents[_random.NextInt(0, culturalEvents.Length)];
        }
        
        private string GenerateReligiousNarrative(HistoricalEventRecord evt, string civName)
        {
            var religiousEvents = new string[]
            {
                $"A prophet arose in {civName}, speaking with a voice that seemed to echo from the divine realm itself. Their words sparked a religious awakening that transformed hearts, toppled false idols, and established new sacred traditions.",
                $"The great temple of {civName} was consecrated under a sky filled with miraculous signs. Pilgrims reported visions and healings, while the faithful gathered in numbers that stretched beyond the horizon.",
                $"A heretical movement challenged the established faith of {civName}, leading to passionate debates, secret meetings, and ultimately a schism that would reshape the spiritual landscape for generations to come.",
                $"The sacred relic was discovered buried beneath the ancient ruins, its power immediately recognized by the faithful of {civName}. Wars would be fought over this holy artifact, and kingdoms would rise and fall in its shadow.",
                $"The high priest of {civName} received a divine vision that demanded a great pilgrimage. Thousands answered the call, abandoning their homes to follow a sacred path fraught with danger and revelation."
            };
            
            return religiousEvents[_random.NextInt(0, religiousEvents.Length)];
        }
        
        private string GenerateEconomicNarrative(HistoricalEventRecord evt, string civName)
        {
            var economicEvents = new string[]
            {
                $"Gold flowed through {civName} like a river of liquid sunlight. New trade routes brought exotic goods and unprecedented wealth, but also attracted the attention of pirates, bandits, and envious neighbors.",
                $"The great merchant houses of {civName} formed a powerful guild that could make or break kingdoms with their economic decisions. Their influence spread far beyond mere commerce, reaching into politics, warfare, and even matters of the heart.",
                $"A revolutionary invention in {civName} transformed how business was conducted. This innovation spread like wildfire, creating fortunes overnight while destroying traditional ways of life.",
                $"The discovery of precious resources beneath the lands of {civName} sparked a rush that brought fortune-seekers from across the world. Boom towns sprang up overnight, filled with dreamers, schemers, and those seeking a new destiny.",
                $"Economic disaster struck {civName} like a plague, bringing once-mighty merchants to their knees and forcing the common people to find new ways to survive. Yet from this crisis emerged unexpected opportunities and unlikely heroes."
            };
            
            return economicEvents[_random.NextInt(0, economicEvents.Length)];
        }
        
        private string GenerateDiplomaticNarrative(HistoricalEventRecord evt, string civName)
        {
            var diplomaticEvents = new string[]
            {
                $"The master diplomats of {civName} orchestrated a complex web of negotiations that would have impressed the most cunning spiders. Through words alone, they reshaped borders, prevented wars, and secured advantages that armies could not have won.",
                $"A diplomatic scandal rocked {civName} when secret correspondence was revealed, exposing hidden agendas and betrayals that reached to the highest levels of government. Trust, once broken, would take generations to rebuild.",
                $"The great peace conference hosted by {civName} brought together representatives from dozens of nations. For months they debated, argued, and negotiated, ultimately forging agreements that would reshape the political landscape.",
                $"An ambitious diplomatic marriage was arranged, uniting {civName} with a powerful foreign dynasty. But behind the ceremonial splendor lay deeper currents of political intrigue and personal passion that would affect the fate of nations.",
                $"The diplomatic immunity of {civName}'s ambassadors was tested when they became embroiled in a foreign court's conspiracy. Their actions would either secure lasting peace or ignite a war that would consume continents."
            };
            
            return diplomaticEvents[_random.NextInt(0, diplomaticEvents.Length)];
        }
        
        private string GenerateSocialCollapseNarrative(HistoricalEventRecord evt, string civName)
        {
            var collapseEvents = new string[]
            {
                $"The very foundations of {civName} cracked and crumbled as ancient social orders collapsed overnight. Nobles fled their estates, merchants abandoned their shops, and common folk wondered if the world itself was ending.",
                $"Plague swept through {civName} like a dark angel, claiming rich and poor alike. The social fabric that had held the civilization together for centuries unraveled as survivors struggled to rebuild from the ashes of their former lives.",
                $"A great uprising shook {civName} to its core as the oppressed masses finally rose against their masters. The revolution that followed would either forge a new and better society or reduce everything to chaos and ruin.",
                $"Natural disaster struck {civName} with biblical fury, forcing a complete reimagining of how society could function. From the ruins of the old world, new leaders emerged with radical ideas about how people should live together.",
                $"The old ways died hard in {civName}, but die they did. As traditional authorities lost their power and ancient customs were abandoned, the people faced the terrifying freedom of having to create their society anew."
            };
            
            return collapseEvents[_random.NextInt(0, collapseEvents.Length)];
        }
        
        private string GenerateSocialTransformationNarrative(HistoricalEventRecord evt, string civName)
        {
            var transformationEvents = new string[]
            {
                $"A new social movement swept through {civName} like a gentle but irresistible tide. Old prejudices gave way to fresh perspectives, and traditional hierarchies evolved into more just and equitable arrangements.",
                $"The youth of {civName} embraced radical new ideas that their elders found both thrilling and terrifying. This generational shift would transform everything from art and philosophy to politics and religion.",
                $"A charismatic leader emerged in {civName}, preaching a vision of social justice that resonated deeply with the common people. Their movement would either reform society peacefully or tear it apart in the attempt.",
                $"The great social experiment in {civName} challenged everything people thought they knew about human nature. Would this bold new way of organizing society prove to be utopia or disaster?",
                $"Immigration brought new peoples to {civName}, along with their customs, beliefs, and ways of life. The resulting cultural fusion created both beautiful synthesis and explosive tensions."
            };
            
            return transformationEvents[_random.NextInt(0, transformationEvents.Length)];
        }
        
        private string GeneratePoliticalNarrative(HistoricalEventRecord evt, string civName)
        {
            var politicalEvents = new string[]
            {
                $"The halls of power in {civName} echoed with whispered conspiracies and hidden agendas. A game of thrones was being played where the stakes were nothing less than the soul of the nation.",
                $"A new ruler ascended to power in {civName} through means both cunning and controversial. Their reign would be marked by dramatic reforms, bitter opposition, and the constant threat of rebellion.",
                $"The political system of {civName} underwent radical transformation as old institutions crumbled and new ones rose to take their place. Democracy, tyranny, or something entirely unprecedented - only time would tell.",
                $"A great scandal erupted in {civName} when corruption at the highest levels was exposed. The resulting political earthquake would topple governments and reshape the entire power structure.",
                $"Revolutionary ideas about governance spread through {civName} like wildfire. The old ways of ruling were challenged by new philosophies that promised either enlightenment or chaos."
            };
            
            return politicalEvents[_random.NextInt(0, politicalEvents.Length)];
        }
        
        private string GenerateGenericEpicNarrative(HistoricalEventRecord evt, string civName)
        {
            var epicEvents = new string[]
            {
                $"Destiny itself seemed to take notice of {civName} as extraordinary events unfolded that would be remembered for ages. Heroes emerged from humble origins while legends walked among mortals.",
                $"The fates conspired to test {civName} with challenges that would either forge them into something greater or break them entirely. What emerged from this crucible would surprise both friends and enemies.",
                $"A mysterious phenomenon touched the lands of {civName}, bringing with it changes that defied explanation. Some called it magic, others called it madness, but none could deny its transformative power.",
                $"The chronicles of {civName} record this as the moment when everything changed. What had been predictable became uncertain, what had been impossible became inevitable.",
                $"Ancient prophecies spoke of such times, when {civName} would stand at the crossroads of destiny. The choices made in these crucial moments would echo through the corridors of history."
            };
            
            return epicEvents[_random.NextInt(0, epicEvents.Length)];
        }
        
        private string GenerateEventConsequence(HistoricalEventRecord evt, string civName)
        {
            return evt.Significance switch
            {
                >= 4.5f => "This momentous event would echo through the ages, forever changing the course of history.",
                >= 3.5f => $"The effects of these deeds spread far beyond the borders of {civName}, influencing neighboring realms.",
                >= 2.5f => $"The people of {civName} would long remember this turning point in their destiny.",
                _ => "Such events shaped the character and future of the realm."
            };
        }
        
        // Helper method to safely truncate strings for FixedString128Bytes (Burst-compatible)
        private FixedString128Bytes TruncateToFixedString128(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new FixedString128Bytes();
            }
                
            // FixedString128Bytes can hold up to 110 bytes safely to account for UTF-8 encoding overhead
            const int maxSafeBytes = 110;
            
            // Pre-check byte count and truncate if necessary
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            if (bytes.Length <= maxSafeBytes)
            {
                return new FixedString128Bytes(input);
            }
            
            // Truncate character by character until it fits within safe limits
            string truncated = input;
            while (truncated.Length > 0 && System.Text.Encoding.UTF8.GetByteCount(truncated) > maxSafeBytes)
            {
                truncated = truncated.Substring(0, truncated.Length - 1);
            }
            
            // Add ellipsis if we truncated and there's room
            if (truncated.Length < input.Length && truncated.Length > 3)
            {
                string withEllipsis = truncated.Substring(0, truncated.Length - 3) + "...";
                if (System.Text.Encoding.UTF8.GetByteCount(withEllipsis) <= maxSafeBytes)
                {
                    return new FixedString128Bytes(withEllipsis);
                }
            }
            
            // Return truncated version without ellipsis if ellipsis doesn't fit
            return truncated.Length > 0 ? new FixedString128Bytes(truncated) : new FixedString128Bytes();
        }
    }
} 