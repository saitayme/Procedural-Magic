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
    public partial class AdaptiveFolkloreSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private EntityQuery _folkloreQuery;
        private EntityQuery _mythologyQuery;
        private WorldHistorySystem _historySystem;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 8f; // Folklore evolves slowly
        private Unity.Mathematics.Random _random;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(
                ComponentType.ReadWrite<CivilizationData>(),
                ComponentType.ReadWrite<AdaptivePersonalityData>(),
                ComponentType.ReadOnly<CulturalMemoryBuffer>()
            );
            
            _folkloreQuery = GetEntityQuery(ComponentType.ReadWrite<FolkloreGenerationData>());
            _mythologyQuery = GetEntityQuery(ComponentType.ReadOnly<MythologyData>());
            
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(123);
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
            
            // Generate new folklore based on recent experiences
            GenerateNewFolklore(ecb);
            
            // Update existing folklore
            UpdateFolklore(ecb);
            
            // Spread folklore between civilizations
            SpreadFolklore(ecb);
            
            // Create folklore-influenced events
            GenerateFolkloreEvents(ecb);
        }

        private void GenerateNewFolklore(EntityCommandBuffer ecb)
        {
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            var personalities = _civilizationQuery.ToComponentDataArray<AdaptivePersonalityData>(Allocator.Temp);
            var civEntities = _civilizationQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < civs.Length; i++)
            {
                var civ = civs[i];
                var personality = personalities[i];
                var entity = civEntities[i];
                
                // Check if this civilization should generate new folklore
                if (ShouldGenerateFolklore(civ, personality))
                {
                    CreateFolklore(civ, personality, entity, ecb);
                }
            }
            
            civs.Dispose();
            personalities.Dispose();
            civEntities.Dispose();
        }

        private bool ShouldGenerateFolklore(CivilizationData civ, AdaptivePersonalityData personality)
        {
            // Folklore emerges from cultural development and significant experiences
            float folkloreChance = 0f;
            
            if (civ.Culture > 5f) folkloreChance += 0.15f;
            if (civ.Population > 5000f) folkloreChance += 0.1f;
            if (personality.Stage == PersonalityEvolutionStage.Developing) folkloreChance += 0.2f;
            if (personality.CurrentStress > 0.5f) folkloreChance += 0.15f; // Stress creates stories
            
            // Recent traumatic or triumphant events inspire folklore
            if (personality.SuccessfulWars > 0) folkloreChance += 0.1f;
            if (personality.Betrayals > 0) folkloreChance += 0.12f;
            
            return _random.NextFloat() < folkloreChance;
        }

        private void CreateFolklore(CivilizationData civ, AdaptivePersonalityData personality, Entity civEntity, EntityCommandBuffer ecb)
        {
            // Determine folklore type based on civilization's experiences and values
            var folkloreType = DetermineFolkloreType(civ, personality);
            var mood = DetermineFolkloreMood(civ, personality);
            
            // Generate content based on their cultural memories and experiences
            var taleContent = GenerateFolkloreContent(folkloreType, mood, civ, personality);
            var taleName = GenerateFolkloreName(folkloreType, mood, civ);
            
            var folklore = new FolkloreGenerationData
            {
                CurrentTale = taleName,
                TaleContent = taleContent,
                Type = folkloreType,
                SourceCivilization = civEntity,
                Popularity = CalculateInitialPopularity(folkloreType, civ, personality),
                MoralLessons = CalculateMoralContent(folkloreType, personality),
                EntertainmentValue = CalculateEntertainmentValue(folkloreType, mood),
                HistoricalBasis = CalculateHistoricalBasis(folkloreType, personality),
                OverallMood = mood,
                AgeOfTale = 0f,
                CulturalRelevance = CalculateCulturalRelevance(folkloreType, civ),
                NumberOfVariations = 1,
                IsSpreadingToOtherCivers = false
            };
            
            var folkloreEntity = ecb.CreateEntity();
            ecb.AddComponent(folkloreEntity, folklore);
            
            // Create folklore creation event
            CreateFolkloreEvent(folklore, civ, ecb);
        }

        private FolkloreType DetermineFolkloreType(CivilizationData civ, AdaptivePersonalityData personality)
        {
            // Analyze civilization traits to determine most appropriate folklore type
            float[] typeWeights = new float[10]; // One for each FolkloreType
            
            // Base weights
            typeWeights[(int)FolkloreType.Legend] = 0.15f;
            typeWeights[(int)FolkloreType.FairyTale] = 0.1f;
            typeWeights[(int)FolkloreType.Fable] = 0.1f;
            
            // Adjust based on civilization characteristics
            if (civ.Military > 6f || personality.SuccessfulWars > 1)
                typeWeights[(int)FolkloreType.Legend] += 0.3f;
            
            if (civ.Religion > 7f)
                typeWeights[(int)FolkloreType.Sacred] += 0.25f;
            
            if (personality.Stage == PersonalityEvolutionStage.Broken || personality.Betrayals > 0)
                typeWeights[(int)FolkloreType.Cautionary] += 0.2f;
            
            if (civ.Culture > 7f)
                typeWeights[(int)FolkloreType.Wisdom] += 0.2f;
            
            if (personality.CurrentStress > 0.7f)
                typeWeights[(int)FolkloreType.Ghost] += 0.15f;
            
            if (civ.Trade > 6f || personality.CurrentPersonality.Greed > 7f)
                typeWeights[(int)FolkloreType.Trickster] += 0.18f;
            
            if (civ.Population < 3000f || civ.Technology < 3f)
                typeWeights[(int)FolkloreType.Origin] += 0.2f;
            
            // Seasonal stories for stable, agricultural civilizations
            if (civ.Stability > 0.7f && civ.Agriculture > 5f)
                typeWeights[(int)FolkloreType.Seasonal] += 0.15f;
            
            return (FolkloreType)SelectWeightedRandom(typeWeights);
        }

        private FolkloreMood DetermineFolkloreMood(CivilizationData civ, AdaptivePersonalityData personality)
        {
            // Mood reflects the civilization's current emotional state
            if (personality.Stage == PersonalityEvolutionStage.Broken)
                return FolkloreMood.Dark;
            
            if (civ.Stability > 0.8f && civ.Wealth > 8000f)
                return FolkloreMood.Hopeful;
            
            if (personality.CurrentPersonality.Pride > 8f && personality.SuccessfulWars > 2)
                return FolkloreMood.Heroic;
            
            if (civ.Culture > 8f && civ.Religion > 6f)
                return FolkloreMood.Mystical;
            
            if (civ.Trade > 7f && civ.Diplomacy > 6f)
                return FolkloreMood.Humorous;
            
            if (personality.Stage == PersonalityEvolutionStage.Enlightened)
                return FolkloreMood.Wise;
            
            if (civ.Stability > 0.8f && personality.CurrentStress < 0.3f)
                return FolkloreMood.Peaceful;
            
            if (personality.CurrentPersonality.Ambition > 8f)
                return FolkloreMood.Chaotic;
            
            // Check for love/romance potential
            if (civ.Culture > 6f && civ.Diplomacy > 6f && _random.NextFloat() < 0.3f)
                return FolkloreMood.Romantic;
            
            // Tragic mood for civilizations that have suffered
            if (personality.Betrayals > 0 || personality.NaturalDisasters > 1)
                return FolkloreMood.Tragic;
            
            return FolkloreMood.Hopeful; // Default optimistic
        }

        private FixedString512Bytes GenerateFolkloreContent(
            FolkloreType type, 
            FolkloreMood mood, 
            CivilizationData civ, 
            AdaptivePersonalityData personality)
        {
            string content = type switch
            {
                FolkloreType.Legend => GenerateLegendContent(mood, civ, personality),
                FolkloreType.FairyTale => GenerateFairyTaleContent(mood, civ),
                FolkloreType.Fable => GenerateFableContent(mood, personality),
                FolkloreType.Ghost => GenerateGhostStoryContent(mood, civ, personality),
                FolkloreType.Trickster => GenerateTricksterContent(mood, civ),
                FolkloreType.Origin => GenerateOriginStoryContent(mood, civ),
                FolkloreType.Cautionary => GenerateCautionaryTaleContent(mood, personality),
                FolkloreType.Wisdom => GenerateWisdomTaleContent(mood, civ, personality),
                FolkloreType.Seasonal => GenerateSeasonalStoryContent(mood, civ),
                FolkloreType.Sacred => GenerateSacredStoryContent(mood, civ),
                _ => "A tale passed down through generations, carrying the wisdom of the ancestors."
            };
            
            return new FixedString512Bytes(content);
        }

        private string GenerateLegendContent(FolkloreMood mood, CivilizationData civ, AdaptivePersonalityData personality)
        {
            string[] heroTypes = { "warrior", "sage", "leader", "guardian", "champion" };
            string[] challenges = { "great darkness", "terrible monster", "foreign invasion", "natural disaster", "ancient curse" };
            string[] victories = { "courage", "wisdom", "unity", "sacrifice", "determination" };
            
            var hero = heroTypes[_random.NextInt(0, heroTypes.Length)];
            var challenge = challenges[_random.NextInt(0, challenges.Length)];
            var victory = victories[_random.NextInt(0, victories.Length)];
            
            return $"Long ago, when {civ.Name} faced the {challenge}, there arose a great {hero} " +
                   $"who through {victory} saved the people and became legend. Their deeds remind us " +
                   $"that in the darkest hours, heroes emerge from among the common folk.";
        }

        private string GenerateFairyTaleContent(FolkloreMood mood, CivilizationData civ)
        {
            string[] creatures = { "magical bird", "wise old tree", "speaking river", "star-touched child", "gentle spirit" };
            string[] gifts = { "eternal wisdom", "pure heart", "golden voice", "healing touch", "sight beyond sight" };
            string[] lessons = { "kindness is stronger than force", "patience brings great rewards", "the humble shall be exalted", "love conquers all darkness", "generosity creates abundance" };
            
            var creature = creatures[_random.NextInt(0, creatures.Length)];
            var gift = gifts[_random.NextInt(0, gifts.Length)];
            var lesson = lessons[_random.NextInt(0, lessons.Length)];
            
            return $"In the enchanted lands near {civ.Name}, a {creature} bestowed {gift} upon " +
                   $"a worthy soul, teaching all who heard the tale that {lesson}. " +
                   $"Children still seek this magical being in the quiet places of the world.";
        }

        private string GenerateFableContent(FolkloreMood mood, AdaptivePersonalityData personality)
        {
            string[] animals = { "clever fox", "mighty bear", "wise owl", "swift rabbit", "proud eagle" };
            string[] situations = { "found a treasure", "faced a predator", "helped a friend", "made a mistake", "learned a lesson" };
            string[] morals = { "pride leads to downfall", "cooperation achieves more than competition", "honesty is the best policy", "preparation prevents problems", "small acts can have great consequences" };
            
            var animal = animals[_random.NextInt(0, animals.Length)];
            var situation = situations[_random.NextInt(0, situations.Length)];
            var moral = morals[_random.NextInt(0, morals.Length)];
            
            return $"The tale tells of a {animal} who {situation}, and through this experience " +
                   $"learned that {moral}. Parents tell this story to children to pass on " +
                   $"the wisdom of generations past.";
        }

        private string GenerateGhostStoryContent(FolkloreMood mood, CivilizationData civ, AdaptivePersonalityData personality)
        {
            string[] spirits = { "restless ancestor", "betrayed lover", "fallen warrior", "forgotten ruler", "lost child" };
            string[] manifestations = { "cold winds", "flickering lights", "whispered warnings", "moving shadows", "phantom sounds" };
            string[] purposes = { "seeks justice", "warns of danger", "guards ancient secrets", "mourns their loss", "protects the innocent" };
            
            var spirit = spirits[_random.NextInt(0, spirits.Length)];
            var manifestation = manifestations[_random.NextInt(0, manifestations.Length)];
            var purpose = purposes[_random.NextInt(0, purposes.Length)];
            
            return $"In the old places of {civ.Name}, people speak of a {spirit} whose presence " +
                   $"is known by {manifestation}. They say this spirit {purpose}, and wise folk " +
                   $"heed their signs, for the dead remember what the living forget.";
        }

        private string GenerateTricksterContent(FolkloreMood mood, CivilizationData civ)
        {
            string[] tricksters = { "clever merchant", "mischievous sprite", "cunning thief", "witty jester", "sly shapeshifter" };
            string[] victims = { "greedy lord", "pompous scholar", "arrogant warrior", "corrupt official", "stingy trader" };
            string[] tricks = { "switched their gold for stones", "convinced them to trade places", "led them in circles", "made them see illusions", "turned their words against them" };
            string[] lessons = { "greed blinds wisdom", "pride comes before a fall", "the clever outwit the strong", "laughter is the best medicine", "appearances deceive" };
            
            var trickster = tricksters[_random.NextInt(0, tricksters.Length)];
            var victim = victims[_random.NextInt(0, victims.Length)];
            var trick = tricks[_random.NextInt(0, tricks.Length)];
            var lesson = lessons[_random.NextInt(0, lessons.Length)];
            
            return $"The people of {civ.Name} love to tell of the {trickster} who {trick} " +
                   $"when facing a {victim}, proving that {lesson}. These tales bring laughter " +
                   $"and remind us not to take ourselves too seriously.";
        }

        private string GenerateOriginStoryContent(FolkloreMood mood, CivilizationData civ)
        {
            string[] origins = { "first fire", "sacred spring", "great mountain", "ancient tree", "blessed stone" };
            string[] givers = { "benevolent spirits", "wise ancestors", "sky father", "earth mother", "star children" };
            string[] purposes = { "light the darkness", "heal the sick", "guide the lost", "protect the innocent", "bring abundance" };
            
            var origin = origins[_random.NextInt(0, origins.Length)];
            var giver = givers[_random.NextInt(0, givers.Length)];
            var purpose = purposes[_random.NextInt(0, purposes.Length)];
            
            return $"The elders of {civ.Name} tell how the {origin} came to be, gifted by {giver} " +
                   $"to {purpose} for all who dwell in these lands. This sacred gift reminds us " +
                   $"of our connection to the eternal powers that shape our world.";
        }

        private string GenerateCautionaryTaleContent(FolkloreMood mood, AdaptivePersonalityData personality)
        {
            string[] warnings = { "those who betray trust", "those who seek power without wisdom", "those who ignore the warnings of elders", "those who break sacred oaths", "those who harm the innocent" };
            string[] fates = { "lose everything they hold dear", "become what they most despise", "wander forever without rest", "face the consequences of their choices", "learn too late the value of what they've lost" };
            
            var warning = warnings[_random.NextInt(0, warnings.Length)];
            var fate = fates[_random.NextInt(0, fates.Length)];
            
            return $"This tale serves as warning to {warning}, for such people inevitably {fate}. " +
                   $"The story is told so that each generation might learn from the mistakes of those who came before.";
        }

        private string GenerateWisdomTaleContent(FolkloreMood mood, CivilizationData civ, AdaptivePersonalityData personality)
        {
            string[] sages = { "ancient philosopher", "wise grandmother", "learned scholar", "spiritual teacher", "experienced elder" };
            string[] wisdom = { "true strength comes from understanding", "the greatest wealth is contentment", "patience achieves what force cannot", "knowledge without compassion is empty", "the journey matters more than the destination" };
            
            var sage = sages[_random.NextInt(0, sages.Length)];
            var insight = wisdom[_random.NextInt(0, wisdom.Length)];
            
            return $"A revered {sage} of {civ.Name} once taught that {insight}. " +
                   $"This profound truth has guided generations in making wise choices " +
                   $"and living lives of meaning and purpose.";
        }

        private string GenerateSeasonalStoryContent(FolkloreMood mood, CivilizationData civ)
        {
            string[] seasons = { "spring awakening", "summer abundance", "autumn harvest", "winter reflection" };
            string[] celebrations = { "plant new seeds", "gather in gratitude", "share the bounty", "rest and renew" };
            string[] meanings = { "hope returns", "life flourishes", "hard work pays off", "wisdom grows in stillness" };
            
            var season = seasons[_random.NextInt(0, seasons.Length)];
            var celebration = celebrations[_random.NextInt(0, celebrations.Length)];
            var meaning = meanings[_random.NextInt(0, meanings.Length)];
            
            return $"When {season} comes to {civ.Name}, the people {celebration}, " +
                   $"remembering that {meaning}. This ancient cycle connects us to the rhythms " +
                   $"of the earth and the eternal dance of renewal.";
        }

        private string GenerateSacredStoryContent(FolkloreMood mood, CivilizationData civ)
        {
            string[] divineActs = { "blessed the land", "spoke through visions", "sent miraculous signs", "answered prayers", "granted protection" };
            string[] responses = { "built a shrine", "established rituals", "changed their ways", "spread the word", "offered gratitude" };
            
            var act = divineActs[_random.NextInt(0, divineActs.Length)];
            var response = responses[_random.NextInt(0, responses.Length)];
            
            return $"The faithful of {civ.Name} tell how the divine once {act}, " +
                   $"and the people {response} in recognition of this sacred gift. " +
                   $"This holy tale strengthens faith and reminds all of the divine presence in their lives.";
        }

        private FixedString128Bytes GenerateFolkloreName(FolkloreType type, FolkloreMood mood, CivilizationData civ)
        {
            string baseName = type switch
            {
                FolkloreType.Legend => "The Legend of",
                FolkloreType.FairyTale => "The Tale of",
                FolkloreType.Fable => "The Fable of",
                FolkloreType.Ghost => "The Phantom of",
                FolkloreType.Trickster => "The Tricks of",
                FolkloreType.Origin => "The Origin of",
                FolkloreType.Cautionary => "The Warning of",
                FolkloreType.Wisdom => "The Wisdom of",
                FolkloreType.Seasonal => "The Festival of",
                FolkloreType.Sacred => "The Blessing of",
                _ => "The Story of"
            };
            
            string[] subjects = mood switch
            {
                FolkloreMood.Heroic => new[] { "the Brave Heart", "the Shining Blade", "the Noble Spirit" },
                FolkloreMood.Dark => new[] { "the Shadow's Price", "the Lost Soul", "the Cursed Path" },
                FolkloreMood.Mystical => new[] { "the Hidden Truth", "the Sacred Mystery", "the Ancient Power" },
                FolkloreMood.Wise => new[] { "the Elder's Gift", "the Learned Path", "the Deep Understanding" },
                FolkloreMood.Romantic => new[] { "the Eternal Bond", "the True Love", "the United Hearts" },
                _ => new[] { "the Golden Days", "the Kind Stranger", "the Simple Truth" }
            };
            
            var subject = subjects[_random.NextInt(0, subjects.Length)];
            return new FixedString128Bytes($"{baseName} {subject}");
        }

        private float CalculateInitialPopularity(FolkloreType type, CivilizationData civ, AdaptivePersonalityData personality)
        {
            float popularity = 0.5f;
            
            // Adjust based on type and civilization characteristics
            if (type == FolkloreType.Legend && civ.Military > 6f) popularity += 0.2f;
            if (type == FolkloreType.Sacred && civ.Religion > 7f) popularity += 0.25f;
            if (type == FolkloreType.Wisdom && civ.Culture > 6f) popularity += 0.15f;
            if (type == FolkloreType.Trickster && civ.Trade > 6f) popularity += 0.1f;
            
            // Personality factors
            if (personality.Stage == PersonalityEvolutionStage.Developing) popularity += 0.1f;
            
            return math.clamp(popularity, 0.1f, 1.0f);
        }

        private float CalculateMoralContent(FolkloreType type, AdaptivePersonalityData personality)
        {
            return type switch
            {
                FolkloreType.Fable => 0.9f,
                FolkloreType.Cautionary => 0.8f,
                FolkloreType.Wisdom => 0.85f,
                FolkloreType.Sacred => 0.75f,
                FolkloreType.FairyTale => 0.7f,
                _ => 0.4f
            };
        }

        private float CalculateEntertainmentValue(FolkloreType type, FolkloreMood mood)
        {
            float base_entertainment = type switch
            {
                FolkloreType.Trickster => 0.9f,
                FolkloreType.FairyTale => 0.8f,
                FolkloreType.Legend => 0.75f,
                FolkloreType.Ghost => 0.7f,
                _ => 0.6f
            };
            
            if (mood == FolkloreMood.Humorous) base_entertainment += 0.2f;
            if (mood == FolkloreMood.Heroic) base_entertainment += 0.15f;
            if (mood == FolkloreMood.Dark) base_entertainment += 0.1f; // Dark stories are compelling
            
            return math.clamp(base_entertainment, 0.1f, 1.0f);
        }

        private float CalculateHistoricalBasis(FolkloreType type, AdaptivePersonalityData personality)
        {
            float basis = type switch
            {
                FolkloreType.Legend => 0.7f, // Often based on real heroes
                FolkloreType.Cautionary => 0.6f, // Based on real consequences
                FolkloreType.Origin => 0.5f, // Mix of history and myth
                FolkloreType.Sacred => 0.4f, // More spiritual than historical
                FolkloreType.FairyTale => 0.2f, // Mostly fantasy
                _ => 0.4f
            };
            
            // Recent traumatic events increase historical basis
            if (personality.Betrayals > 0 || personality.NaturalDisasters > 0)
                basis += 0.2f;
            
            return math.clamp(basis, 0.1f, 1.0f);
        }

        private float CalculateCulturalRelevance(FolkloreType type, CivilizationData civ)
        {
            float relevance = 0.5f;
            
            // Higher culture = more relevant folklore
            relevance += civ.Culture * 0.05f;
            
            // Certain types are always culturally relevant
            if (type == FolkloreType.Sacred || type == FolkloreType.Origin)
                relevance += 0.2f;
            
            return math.clamp(relevance, 0.1f, 1.0f);
        }

        private void UpdateFolklore(EntityCommandBuffer ecb)
        {
            var folklores = _folkloreQuery.ToComponentDataArray<FolkloreGenerationData>(Allocator.Temp);
            var folkloreEntities = _folkloreQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < folklores.Length; i++)
            {
                var folklore = folklores[i];
                var entity = folkloreEntities[i];
                
                // Age the tale
                folklore.AgeOfTale += UPDATE_INTERVAL;
                
                // Older tales may lose popularity or gain reverence
                if (folklore.AgeOfTale > 50f)
                {
                    // Become classical/traditional
                    folklore.CulturalRelevance *= 1.05f;
                    folklore.Popularity *= 0.98f; // Slight decline in popularity
                }
                
                // Check for variations developing
                if (_random.NextFloat() < 0.05f)
                {
                    folklore.NumberOfVariations++;
                    GenerateFolkloreVariationEvent(folklore, ecb);
                }
                
                EntityManager.SetComponentData(entity, folklore);
            }
            
            folklores.Dispose();
            folkloreEntities.Dispose();
        }

        private void SpreadFolklore(EntityCommandBuffer ecb)
        {
            // Folklore spreads between civilizations through trade and cultural exchange
            var folklores = _folkloreQuery.ToComponentDataArray<FolkloreGenerationData>(Allocator.Temp);
            var civs = _civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            for (int i = 0; i < folklores.Length; i++)
            {
                var folklore = folklores[i];
                
                if (folklore.Popularity > 0.7f && !folklore.IsSpreadingToOtherCivers)
                {
                    // High popularity folklore starts spreading
                    folklore.IsSpreadingToOtherCivers = true;
                    CreateFolkloreSpreadEvent(folklore, ecb);
                }
            }
            
            folklores.Dispose();
            civs.Dispose();
        }

        private void GenerateFolkloreEvents(EntityCommandBuffer ecb)
        {
            // Folklore can influence civilization behavior and create events
            var folklores = _folkloreQuery.ToComponentDataArray<FolkloreGenerationData>(Allocator.Temp);
            
            for (int i = 0; i < folklores.Length; i++)
            {
                var folklore = folklores[i];
                
                if (folklore.CulturalRelevance > 0.8f && _random.NextFloat() < 0.03f)
                {
                    GenerateFolkloreInfluencedEvent(folklore, ecb);
                }
            }
            
            folklores.Dispose();
        }

        private void CreateFolkloreEvent(FolkloreGenerationData folklore, CivilizationData civ, EntityCommandBuffer ecb)
        {
            var eventTitle = $"New Tale in {civ.Name}: {folklore.CurrentTale}";
            var eventDescription = $"A new {folklore.Type} has emerged among the people of {civ.Name}. " +
                                 $"The tale spreads quickly, capturing the imagination and values of the community.";
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = folklore.CulturalRelevance * 0.6f,
                CivilizationId = folklore.SourceCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void GenerateFolkloreVariationEvent(FolkloreGenerationData folklore, EntityCommandBuffer ecb)
        {
            var eventTitle = $"New Version: {folklore.CurrentTale}";
            var eventDescription = $"The beloved tale '{folklore.CurrentTale}' has developed a new variation, " +
                                 $"reflecting the changing experiences and values of the people.";
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = 0.3f,
                CivilizationId = folklore.SourceCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void CreateFolkloreSpreadEvent(FolkloreGenerationData folklore, EntityCommandBuffer ecb)
        {
            var eventTitle = $"Tale Spreads: {folklore.CurrentTale}";
            var eventDescription = $"The popular tale '{folklore.CurrentTale}' begins to spread beyond its " +
                                 $"origins, carrying cultural values and entertainment to distant peoples.";
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = folklore.Popularity * 0.5f,
                CivilizationId = folklore.SourceCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private void GenerateFolkloreInfluencedEvent(FolkloreGenerationData folklore, EntityCommandBuffer ecb)
        {
            var eventTitle = $"Folklore Inspires Action: {folklore.CurrentTale}";
            var eventDescription = $"Inspired by the tale '{folklore.CurrentTale}', the people take action " +
                                 $"that reflects the story's moral lessons and cultural values.";
            
            var historicalEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventTitle),
                Description = new FixedString512Bytes(eventDescription),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Significance = folklore.MoralLessons * 0.7f,
                CivilizationId = folklore.SourceCivilization
            };
            
            _historySystem.AddEvent(historicalEvent);
        }

        private int SelectWeightedRandom(float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                total += weights[i];
            
            if (total <= 0f) return _random.NextInt(0, weights.Length);
            
            float randomValue = _random.NextFloat() * total;
            float cumulative = 0f;
            
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative)
                    return i;
            }
            
            return weights.Length - 1;
        }
    }
} 