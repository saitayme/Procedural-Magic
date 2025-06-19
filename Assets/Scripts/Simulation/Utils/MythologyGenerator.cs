using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using System.Text;

namespace ProceduralWorld.Simulation.Utils
{
    public static class MythologyGenerator
    {
        // ==== ADAPTIVE MYTHOLOGY GENERATION ====
        // Creates unique myths that reflect each civilization's experiences and values
        
        public static MythologyData GenerateAdaptiveMyth(
            CivilizationData civilization,
            AdaptivePersonalityData personality,
            NativeList<HistoricalEventRecord> civilizationHistory,
            DynamicBuffer<CulturalMemoryBuffer> culturalMemories,
            Unity.Mathematics.Random random)
        {
            // Determine myth type based on civilization's experiences and personality
            var mythType = DetermineMythType(civilization, personality, culturalMemories, random);
            
            // Generate myth content based on the civilization's unique story
            var mythContent = GenerateMythContent(mythType, civilization, civilizationHistory, culturalMemories, random);
            
            // Create compelling myth name
            var mythName = GenerateMythName(mythType, civilization, random);
            
            return new MythologyData
            {
                MythName = mythName,
                MythContent = mythContent,
                Type = mythType,
                SourceCivilization = Entity.Null, // Set by calling system
                Believability = CalculateBelievability(mythType, civilization, personality),
                Spread = CalculateInitialSpread(civilization, personality),
                MoralWeight = CalculateMoralWeight(mythType, personality),
                CulturalImpact = CalculateCulturalImpact(mythType, civilization),
                YearCreated = 0, // Will be set by calling system
                Authenticity = CalculateAuthenticity(mythType, civilizationHistory),
                OriginLocation = civilization.Position
            };
        }

        private static MythType DetermineMythType(
            CivilizationData civ, 
            AdaptivePersonalityData personality,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            // Base chances for each myth type
            float[] typeChances = new float[10]; // One for each MythType
            
            // Adjust chances based on civilization experiences
            if (HasMemoryType(memories, MemoryType.Triumph))
                typeChances[(int)MythType.HeroMyth] += 0.3f;
            
            if (HasMemoryType(memories, MemoryType.Trauma))
                typeChances[(int)MythType.TragedyMyth] += 0.25f;
            
            if (HasMemoryType(memories, MemoryType.Betrayal))
                typeChances[(int)MythType.CurseMyth] += 0.2f;
            
            if (civ.Religion > 7f)
                typeChances[(int)MythType.CreationMyth] += 0.2f;
            
            if (personality.CurrentPersonality.Aggressiveness > 7f)
                typeChances[(int)MythType.WarMyth] += 0.25f;
            
            if (civ.Culture > 6f)
                typeChances[(int)MythType.LoveMyth] += 0.15f;
            
            if (personality.Stage == PersonalityEvolutionStage.Broken)
                typeChances[(int)MythType.RedemptionMyth] += 0.3f;
            
            if (civ.Technology > 5f)
                typeChances[(int)MythType.MagicMyth] += 0.1f; // Advanced tech seems like magic
            
            // Environmental influences
            if (HasNaturalDisasters(memories))
                typeChances[(int)MythType.MonsterMyth] += 0.2f;
            
            // Prophecy myths for civilizations with high ambition
            if (personality.CurrentPersonality.Ambition > 8f)
                typeChances[(int)MythType.ProphecyMyth] += 0.15f;
            
            // Select based on weighted random
            return (MythType)SelectWeightedRandom(typeChances, random);
        }

        private static FixedString512Bytes GenerateMythContent(
            MythType mythType,
            CivilizationData civ,
            NativeList<HistoricalEventRecord> history,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            var content = new StringBuilder();
            
            switch (mythType)
            {
                case MythType.CreationMyth:
                    content.Append(GenerateCreationMythContent(civ, random));
                    break;
                    
                case MythType.HeroMyth:
                    content.Append(GenerateHeroMythContent(civ, history, memories, random));
                    break;
                    
                case MythType.TragedyMyth:
                    content.Append(GenerateTragedyMythContent(civ, memories, random));
                    break;
                    
                case MythType.WarMyth:
                    content.Append(GenerateWarMythContent(civ, history, random));
                    break;
                    
                case MythType.LoveMyth:
                    content.Append(GenerateLoveMythContent(civ, random));
                    break;
                    
                case MythType.MonsterMyth:
                    content.Append(GenerateMonsterMythContent(civ, memories, random));
                    break;
                    
                case MythType.MagicMyth:
                    content.Append(GenerateMagicMythContent(civ, random));
                    break;
                    
                case MythType.ProphecyMyth:
                    content.Append(GenerateProphecyMythContent(civ, random));
                    break;
                    
                case MythType.CurseMyth:
                    content.Append(GenerateCurseMythContent(civ, memories, random));
                    break;
                    
                case MythType.RedemptionMyth:
                    content.Append(GenerateRedemptionMythContent(civ, memories, random));
                    break;
            }
            
            return new FixedString512Bytes(content.ToString());
        }

        private static string GenerateCreationMythContent(CivilizationData civ, Unity.Mathematics.Random random)
        {
            string[] beginnings = {
                "In the time before time, when the world was but shadow and silence",
                "From the great void came forth the first light",
                "When the ancient powers still walked among mortals",
                "In the age when the very stones could speak"
            };
            
            string[] creators = GetCreatorsByBiome(civ.Position, random);
            string[] actions = {
                "breathed life into the barren lands",
                "sang the world into existence with their eternal song",
                "wove reality from the threads of pure dream",
                "forged the earth with hammer blows that still echo"
            };
            
            string[] purposes = GetPurposesByCivilizationType(civ.Type, random);
            
            string beginning = beginnings[random.NextInt(0, beginnings.Length)];
            string creator = creators[random.NextInt(0, creators.Length)];
            string action = actions[random.NextInt(0, actions.Length)];
            string purpose = purposes[random.NextInt(0, purposes.Length)];
            
            return $"{beginning}, {creator} {action} so that {purpose}. " +
                   $"Thus did the people of {civ.Name} come to inherit this sacred charge.";
        }

        private static string GenerateHeroMythContent(
            CivilizationData civ,
            NativeList<HistoricalEventRecord> history,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            // Find the most significant positive event in their history
            var heroicEvent = FindMostTriumphantEvent(history, memories);
            
            string[] heroTitles = {
                "the Lightbringer", "the Unbreakable", "the Wise", "the Bold",
                "the Just", "the Fierce", "the Eternal", "the Pure"
            };
            
            string[] challenges = {
                "faced the armies of darkness that threatened to consume all",
                "stood alone against the tide of chaos and despair",
                "ventured into the realm of shadows to retrieve the lost hope",
                "battled the great evil that had plagued the land for generations"
            };
            
            string[] victories = {
                "with courage that burned brighter than the sun",
                "through wisdom that exceeded that of the ancient sages",
                "by uniting the hearts of all who followed the path of righteousness",
                "with strength drawn from the very soul of the people"
            };
            
            string heroName = GenerateHeroName(civ, random);
            string heroTitle = heroTitles[random.NextInt(0, heroTitles.Length)];
            string challenge = challenges[random.NextInt(0, challenges.Length)];
            string victory = victories[random.NextInt(0, victories.Length)];
            
            return $"In the darkest hour of {civ.Name}, there arose {heroName} {heroTitle}, who " +
                   $"{challenge}. Through trials beyond counting, they triumphed {victory}, " +
                   $"and their legacy became the guiding star for all who came after.";
        }

        private static string GenerateTragedyMythContent(
            CivilizationData civ,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            // Base on traumatic memories
            var traumaticMemory = FindMostTraumaticMemory(memories);
            
            string[] warnings = {
                "Pride comes before the fall, as the ancestors learned too late",
                "Those who forget the old ways invite calamity upon themselves",
                "The price of hubris is paid by generations yet unborn",
                "What is built without wisdom crumbles to dust"
            };
            
            string[] consequences = {
                "and the once-mighty city became as dust in the wind",
                "and the children wept for sins they did not commit",
                "and the land itself turned away from its people",
                "and darkness fell upon the realm for seven long seasons"
            };
            
            string warning = warnings[random.NextInt(0, warnings.Length)];
            string consequence = consequences[random.NextInt(0, consequences.Length)];
            
            return $"{warning}. The people of {civ.Name} tell this tale " +
                   $"to remind all who would listen that {consequence}. " +
                   $"Let no generation forget the cost of arrogance.";
        }

        private static string GenerateWarMythContent(
            CivilizationData civ,
            NativeList<HistoricalEventRecord> history,
            Unity.Mathematics.Random random)
        {
            string[] epicBattles = {
                "The Battle of a Thousand Tears", "The War That Shook the Heavens",
                "The Siege of Eternal Night", "The Campaign of Broken Crowns"
            };
            
            string[] battleDescriptions = {
                "where the very earth ran red with the blood of heroes",
                "that raged for seasons until the sun itself grew weary",
                "in which the fate of all civilized peoples hung in the balance",
                "where gods themselves took sides among mortal warriors"
            };
            
            string[] outcomes = {
                "Victory came at a price that still echoes through the ages",
                "From great sacrifice came the peace that followed",
                "The cost was terrible, but honor was preserved",
                "Though many fell, their names live on in eternal glory"
            };
            
            string battle = epicBattles[random.NextInt(0, epicBattles.Length)];
            string description = battleDescriptions[random.NextInt(0, battleDescriptions.Length)];
            string outcome = outcomes[random.NextInt(0, outcomes.Length)];
            
            return $"The chronicles of {civ.Name} speak of {battle}, {description}. " +
                   $"{outcome}, and the warriors of that day became the foundation " +
                   $"upon which all future glory was built.";
        }

        private static string GenerateLoveMythContent(CivilizationData civ, Unity.Mathematics.Random random)
        {
            string[] loveTypes = {
                "the love between star-crossed rulers of rival houses",
                "the devotion of a humble artisan to a divine muse",
                "the bond between a warrior and the spirit of the land",
                "the eternal romance that transcends death itself"
            };
            
            string[] obstacles = {
                "Though fate and tradition stood against them",
                "Despite the curses of jealous gods",
                "Even as war raged around their sanctuary",
                "Though the very elements conspired to keep them apart"
            };
            
            string[] triumphs = {
                "their love became the foundation of a new age of harmony",
                "their union brought peace to lands torn by ancient hatred",
                "their devotion inspired generations to follow their hearts",
                "their story became a beacon of hope in dark times"
            };
            
            string loveType = loveTypes[random.NextInt(0, loveTypes.Length)];
            string obstacle = obstacles[random.NextInt(0, obstacles.Length)];
            string triumph = triumphs[random.NextInt(0, triumphs.Length)];
            
            return $"Among the people of {civ.Name}, none is more cherished than the tale of " +
                   $"{loveType}. {obstacle}, {triumph}. Thus love conquered all, " +
                   $"as it always has and always will.";
        }

        private static string GenerateMonsterMythContent(
            CivilizationData civ,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            string[] monsters = GetMonstersByEnvironment(civ.Position, random);
            string[] threats = {
                "devoured the crops and brought famine to the land",
                "poisoned the wells with its very presence",
                "stole the dreams of children and left only nightmares",
                "cast shadows that grew longer with each passing day"
            };
            
            string[] resolutions = {
                "Only through unity and courage was the beast finally vanquished",
                "A price was paid in heroes' blood, but the land was cleansed",
                "The monster retreated to the deep places, but its threat lingers",
                "By ancient ritual and sacrifice, the evil was bound"
            };
            
            string monster = monsters[random.NextInt(0, monsters.Length)];
            string threat = threats[random.NextInt(0, threats.Length)];
            string resolution = resolutions[random.NextInt(0, resolutions.Length)];
            
            return $"In the early days of {civ.Name}, {monster} emerged from the wild places and " +
                   $"{threat}. {resolution}, and the tale serves as warning to all who " +
                   $"would venture unprepared into the unknown.";
        }

        private static string GenerateMagicMythContent(CivilizationData civ, Unity.Mathematics.Random random)
        {
            string[] magicalSources = {
                "the Well of Infinite Wisdom", "the Crown of Starlight",
                "the Song that Shapes Reality", "the Key to Hidden Doors"
            };
            
            string[] powers = {
                "could heal any wound and mend any broken heart",
                "granted the ability to speak with the voices of the past",
                "allowed its wielder to see the threads that bind all things",
                "opened pathways to realms beyond mortal understanding"
            };
            
            string[] costs = {
                "but demanded a terrible price in return",
                "yet could only be used by those pure of heart",
                "though it faded more with each use",
                "but chose its own masters, not the reverse"
            };
            
            string source = magicalSources[random.NextInt(0, magicalSources.Length)];
            string power = powers[random.NextInt(0, powers.Length)];
            string cost = costs[random.NextInt(0, costs.Length)];
            
            return $"The wisest among {civ.Name} speak of {source}, which {power} {cost}. " +
                   $"Though none have found it in living memory, some say it waits still " +
                   $"for one worthy of its gifts.";
        }

        private static string GenerateProphecyMythContent(CivilizationData civ, Unity.Mathematics.Random random)
        {
            string[] prophets = {
                "the Oracle of the Burning Sands", "the Last Seer of the Ancient Line",
                "the Dreamer Who Walks Between Worlds", "the Voice from the Eternal Silence"
            };
            
            string[] prophecies = {
                "foretold of a time when the old bonds would break and new alliances form",
                "spoke of a golden age that would follow the darkest hour",
                "warned of trials that would test the very soul of the people",
                "revealed the path to greatness hidden in plain sight"
            };
            
            string[] conditions = {
                "when the three moons align and the ancient tower crumbles",
                "if the people remain true to the wisdom of their ancestors",
                "should the worthy prove themselves through acts of courage",
                "when love conquers the hatred that divides the world"
            };
            
            string prophet = prophets[random.NextInt(0, prophets.Length)];
            string prophecy = prophecies[random.NextInt(0, prophecies.Length)];
            string condition = conditions[random.NextInt(0, conditions.Length)];
            
            return $"Long ago, {prophet} {prophecy}. The fulfillment will come {condition}, " +
                   $"and the children of {civ.Name} watch for the signs even now.";
        }

        private static string GenerateCurseMythContent(
            CivilizationData civ,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            string[] curseSources = {
                "the betrayed ally who died with vengeance in their heart",
                "the ancient power that was disturbed without proper tribute",
                "the innocent whose suffering went unavenged",
                "the natural order that was violated by mortal ambition"
            };
            
            string[] curseEffects = {
                "that no victory would bring lasting joy",
                "that prosperity would always carry the seeds of its own destruction",
                "that the sins of the past would echo through the generations",
                "that greatness would always exact a price in sorrow"
            };
            
            string[] redemptions = {
                "Only through genuine repentance can the curse be lifted",
                "The curse will end when balance is restored to the world",
                "True understanding of the past may yet break these chains",
                "Perhaps future generations will find the key to freedom"
            };
            
            string source = curseSources[random.NextInt(0, curseSources.Length)];
            string effect = curseEffects[random.NextInt(0, curseEffects.Length)];
            string redemption = redemptions[random.NextInt(0, redemptions.Length)];
            
            return $"The elders of {civ.Name} remember the curse laid by {source}, " +
                   $"decreeing {effect}. {redemption}, though the path remains hidden " +
                   $"in shadow and uncertainty.";
        }

        private static string GenerateRedemptionMythContent(
            CivilizationData civ,
            DynamicBuffer<CulturalMemoryBuffer> memories,
            Unity.Mathematics.Random random)
        {
            string[] fallReasons = {
                "the people had strayed from the path of wisdom",
                "pride had blinded them to their own failings",
                "they had forgotten the bonds that held them together",
                "darkness had taken root in the hearts of the mighty"
            };
            
            string[] redeemers = {
                "a child born in the lowest circumstances",
                "an outcast who remembered the old teachings",
                "a stranger who carried light from distant lands",
                "one who had lost everything yet kept faith"
            };
            
            string[] redemptions = {
                "reminded the people of their true nature",
                "showed them the way back to righteousness",
                "sacrificed themselves to break the cycle of despair",
                "united the scattered fragments of hope"
            };
            
            string fallReason = fallReasons[random.NextInt(0, fallReasons.Length)];
            string redeemer = redeemers[random.NextInt(0, redeemers.Length)];
            string redemption = redemptions[random.NextInt(0, redemptions.Length)];
            
            return $"When {fallReason}, the people of {civ.Name} thought all was lost. " +
                   $"But {redeemer} arose and {redemption}. From the ashes of the old, " +
                   $"a new and greater civilization was born.";
        }

        // Helper methods for environmental and contextual content generation
        private static string[] GetCreatorsByBiome(float3 position, Unity.Mathematics.Random random)
        {
            // This would be expanded to consider actual biome data
            return new string[] {
                "the Great Spirit of the Earth",
                "the Eternal Flame",
                "the Mother of All Waters",
                "the Wind Walker",
                "the Stone Singer"
            };
        }

        private static string[] GetPurposesByCivilizationType(CivilizationType type, Unity.Mathematics.Random random)
        {
            return type switch
            {
                CivilizationType.Military => new string[] {
                    "the strong might protect the weak",
                    "courage would triumph over fear",
                    "honor would guide all actions"
                },
                CivilizationType.Religious => new string[] {
                    "divine wisdom might flourish",
                    "the sacred flame would never die",
                    "souls might find their way to enlightenment"
                },
                CivilizationType.Trade => new string[] {
                    "prosperity might flow to all corners of the world",
                    "connections would bind all peoples together",
                    "abundance would replace scarcity"
                },
                CivilizationType.Cultural => new string[] {
                    "beauty and wisdom might flourish",
                    "knowledge would be preserved for future ages",
                    "art would capture the essence of truth"
                },
                CivilizationType.Technology => new string[] {
                    "innovation would solve the great challenges",
                    "understanding would illuminate the darkness",
                    "progress would lift all beings higher"
                },
                _ => new string[] { "balance would be maintained in all things" }
            };
        }

        private static string[] GetMonstersByEnvironment(float3 position, Unity.Mathematics.Random random)
        {
            // This would consider actual terrain/biome data
            return new string[] {
                "the Shadow That Devours Light",
                "the Twisted One of the Deep Places",
                "the Hunger That Walks Like a Man",
                "the Whisperer in Dark Dreams",
                "the Storm-Born Destroyer"
            };
        }

        private static string GenerateHeroName(CivilizationData civ, Unity.Mathematics.Random random)
        {
            string[] prefixes = { "Aeon", "Lyra", "Thane", "Vera", "Kael", "Zara" };
            string[] suffixes = { "dor", "wyn", "thor", "issa", "ian", "elle" };
            
            string prefix = prefixes[random.NextInt(0, prefixes.Length)];
            string suffix = suffixes[random.NextInt(0, suffixes.Length)];
            
            return prefix + suffix;
        }

        private static FixedString128Bytes GenerateMythName(
            MythType type, 
            CivilizationData civ, 
            Unity.Mathematics.Random random)
        {
            string baseName = type switch
            {
                MythType.CreationMyth => "The First Dawn",
                MythType.HeroMyth => "The Champion's Tale",
                MythType.TragedyMyth => "The Fallen Crown",
                MythType.WarMyth => "The Song of Blades",
                MythType.LoveMyth => "The Eternal Bond",
                MythType.MonsterMyth => "The Shadow's Warning",
                MythType.MagicMyth => "The Lost Arts",
                MythType.ProphecyMyth => "The Vision of Tomorrow",
                MythType.CurseMyth => "The Price of Pride",
                MythType.RedemptionMyth => "The Return to Light",
                _ => "The Ancient Tale"
            };
            
            // Sometimes add civilization-specific variations
            if (random.NextFloat() < 0.3f)
            {
                baseName = $"{baseName} of {civ.Name}";
            }
            
            return new FixedString128Bytes(baseName);
        }

        // Helper methods for analysis
        private static bool HasMemoryType(DynamicBuffer<CulturalMemoryBuffer> memories, MemoryType type)
        {
            // Simplified check - assume memories exist based on buffer presence
            return memories.Length > 0;
        }

        private static bool HasNaturalDisasters(DynamicBuffer<CulturalMemoryBuffer> memories)
        {
            // Simplified check - assume disasters exist if civilization has traumatic memories
            return memories.Length > 2;
        }

        private static HistoricalEventRecord FindMostTriumphantEvent(
            NativeList<HistoricalEventRecord> history,
            DynamicBuffer<CulturalMemoryBuffer> memories)
        {
            HistoricalEventRecord bestEvent = default;
            float bestScore = 0f;
            
            for (int i = 0; i < history.Length; i++)
            {
                var evt = history[i];
                if (evt.Significance > bestScore && 
                    (evt.Type == EventType.Military || evt.Type == EventType.Cultural || evt.Type == EventType.Political))
                {
                    bestScore = evt.Significance;
                    bestEvent = evt;
                }
            }
            
            return bestEvent;
        }

        private static Entity FindMostTraumaticMemory(DynamicBuffer<CulturalMemoryBuffer> memories)
        {
            // Return the first memory entity if any exist
            if (memories.Length > 0)
                return memories[0].MemoryEntity;
            return Entity.Null;
        }

        private static float CalculateBelievability(
            MythType type, 
            CivilizationData civ, 
            AdaptivePersonalityData personality)
        {
            float base_believability = 0.7f;
            
            // Religious civilizations believe myths more readily
            if (civ.Religion > 6f) base_believability += 0.2f;
            
            // Traumatized civilizations more likely to believe dark myths
            if (personality.Stage == PersonalityEvolutionStage.Broken)
            {
                if (type == MythType.CurseMyth || type == MythType.TragedyMyth)
                    base_believability += 0.3f;
            }
            
            // Cultural civilizations appreciate all types of stories
            if (civ.Culture > 6f) base_believability += 0.1f;
            
            return math.clamp(base_believability, 0.1f, 1.0f);
        }

        private static float CalculateInitialSpread(CivilizationData civ, AdaptivePersonalityData personality)
        {
            float spread = 0.3f;
            
            // Trade-focused civilizations spread stories faster
            if (civ.Trade > 5f) spread += 0.2f;
            
            // Culturally advanced civilizations tell better stories
            if (civ.Culture > 6f) spread += 0.15f;
            
            // Charismatic personalities spread stories better
            if (personality.CurrentPersonality.Pride > 7f) spread += 0.1f;
            
            return math.clamp(spread, 0.1f, 1.0f);
        }

        private static float CalculateMoralWeight(MythType type, AdaptivePersonalityData personality)
        {
            float weight = type switch
            {
                MythType.TragedyMyth => 0.8f,
                MythType.RedemptionMyth => 0.9f,
                MythType.CurseMyth => 0.7f,
                MythType.HeroMyth => 0.8f,
                MythType.CreationMyth => 0.6f,
                _ => 0.5f
            };
            
            // Mature personalities give more weight to moral lessons
            if (personality.Stage == PersonalityEvolutionStage.Mature || 
                personality.Stage == PersonalityEvolutionStage.Enlightened)
                weight += 0.2f;
            
            return math.clamp(weight, 0.1f, 1.0f);
        }

        private static float CalculateCulturalImpact(MythType type, CivilizationData civ)
        {
            float impact = 0.5f;
            
            // More culturally developed civilizations create more impactful myths
            impact += civ.Culture * 0.05f;
            
            // Certain myth types have inherently higher impact
            if (type == MythType.CreationMyth || type == MythType.ProphecyMyth)
                impact += 0.3f;
            
            return math.clamp(impact, 0.1f, 1.0f);
        }

        private static float CalculateAuthenticity(MythType type, NativeList<HistoricalEventRecord> history)
        {
            // Myths based on actual events are more "authentic"
            if (type == MythType.HeroMyth || type == MythType.WarMyth || type == MythType.TragedyMyth)
            {
                return history.Length > 0 ? 0.8f : 0.4f;
            }
            
            // Purely mythical types are less authentic but more imaginative
            return 0.3f;
        }

        private static int SelectWeightedRandom(float[] weights, Unity.Mathematics.Random random)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                total += weights[i];
            
            if (total <= 0f) return random.NextInt(0, weights.Length);
            
            float randomValue = random.NextFloat() * total;
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