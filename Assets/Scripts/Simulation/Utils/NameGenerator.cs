using Unity.Mathematics;
using Unity.Collections;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralWorld.Simulation.Utils
{
    public static class NameGenerator
    {
        // ===== EPIC REALM NAMING SYSTEM =====
        
        // EPIC REALM PREFIXES AND NAMES - All arrays removed and replaced with Burst-compatible switch statements
        // See GetCivilizationPrefix, GetMilitaryPrefix, GetTechPrefix, GetReligiousPrefix, GetTradePrefix, GetCulturalPrefix methods
        // See GetWarlikeRealmName, GetPeacefulRealmName, GetGeneralRealmName methods

        // EPIC HERO NAMES - All arrays removed and replaced with Burst-compatible switch statements
        // See GetConquerorName, GetPhilosopherName, GetBuilderName, GetInventorName, GetProphetName methods
        // See GetConquerorTitle, GetPhilosopherTitle, GetBuilderTitle, GetInventorTitle, GetProphetTitle methods

        // EPIC CITY NAMES - Context-sensitive based on civilization type and biome
        public static FixedString128Bytes GenerateEpicCityName(CivilizationType civType, float3 position, Core.BiomeType biome)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            
            // 70% chance for type-specific name, 30% chance for biome-influenced name
            if (random.NextFloat() < 0.7f)
            {
                var cityName = GetCityNameByType(civType, random.NextInt(0, 20));
                return new FixedString128Bytes(cityName);
            }
            else
            {
                // Create biome-influenced name
                var biomeDesc = GetBiomeDescriptor(biome, random.NextInt(0, 10));
                var suffix = GetCitySuffix(random.NextInt(0, 10));
                string biomeName = $"{biomeDesc}{suffix}";
                return new FixedString128Bytes(biomeName);
            }
        }

        // Burst-compatible method to get city name by civilization type
        private static string GetCityNameByType(CivilizationType civType, int index)
        {
            return civType switch
            {
                CivilizationType.Military => GetMilitaryCityName(index),
                CivilizationType.Technology => GetTechCityName(index),
                CivilizationType.Religious => GetReligiousCityName(index),
                CivilizationType.Trade => GetTradeCityName(index),
                CivilizationType.Cultural => GetCulturalCityName(index),
                _ => GetMilitaryCityName(index)
            };
        }

        // Burst-compatible methods for different city types
        private static string GetMilitaryCityName(int index)
        {
            return index switch
            {
                0 => "Ironhold",
                1 => "Battlespire", 
                2 => "Warforge",
                3 => "Bloodstone Keep",
                4 => "Grimwall",
                5 => "Doomhammer Citadel",
                6 => "Skullcrusher Fortress",
                7 => "Rageclaw Stronghold",
                8 => "Ironclad Bastion",
                9 => "Stormbreak Castle",
                10 => "Shadowbane Tower",
                11 => "Flameheart Garrison",
                12 => "Frostbite Outpost",
                13 => "Thunderstrike Barracks",
                14 => "Nightfall Watchtower",
                15 => "Dawnbreaker Ramparts",
                16 => "Starfall Battlements",
                17 => "Voidwalker Redoubt",
                18 => "Soulfire Bulwark",
                19 => "Wraithbound Citadel",
                _ => "Ironhold"
            };
        }

        private static string GetTechCityName(int index)
        {
            return index switch
            {
                0 => "Gearwright",
                1 => "Steamhaven",
                2 => "Clockwork City",
                3 => "Mechanopolis",
                4 => "Techspire",
                5 => "Innovation Hub",
                6 => "Pioneer Station",
                7 => "Visionary Complex",
                8 => "Genius Labs",
                9 => "Prodigy Center",
                10 => "Sparkwright Works",
                11 => "Voltaic Grid",
                12 => "Magnetic Core",
                13 => "Atomic Nexus",
                14 => "Quantum Facility",
                15 => "Cyber District",
                16 => "Digital Plaza",
                17 => "Virtual Realm",
                18 => "Neural Network",
                19 => "Synthetic Sector",
                _ => "Gearwright"
            };
        }

        private static string GetReligiousCityName(int index)
        {
            return index switch
            {
                0 => "Lightspire",
                1 => "Dawnhaven",
                2 => "Starwhisper Sanctum",
                3 => "Moonchild Temple",
                4 => "Sunborn Cathedral",
                5 => "Celestial Monastery",
                6 => "Divine Basilica",
                7 => "Sacred Shrine",
                8 => "Holy Sanctuary",
                9 => "Blessed Abbey",
                10 => "Radiant Chapel",
                11 => "Luminous Cloister",
                12 => "Brilliant Tabernacle",
                13 => "Glorious Altar",
                14 => "Magnificent Pulpit",
                15 => "Sublime Chancel",
                16 => "Transcendent Nave",
                17 => "Enlightened Vestry",
                18 => "Ascended Choir",
                19 => "Exalted Presbytery",
                _ => "Lightspire"
            };
        }

        private static string GetTradeCityName(int index)
        {
            return index switch
            {
                0 => "Goldenheart",
                1 => "Silverport",
                2 => "Gemhaven",
                3 => "Wealthspire",
                4 => "Prosperity Plaza",
                5 => "Fortune's Gate",
                6 => "Merchant's Rest",
                7 => "Trader's Haven",
                8 => "Commerce Center",
                9 => "Market Square",
                10 => "Exchange Point",
                11 => "Bazaar District",
                12 => "Emporium Quarter",
                13 => "Marketplace",
                14 => "Trading Post",
                15 => "Commercial Hub",
                16 => "Business Center",
                17 => "Economic Zone",
                18 => "Financial District",
                19 => "Mercantile Quarter",
                _ => "Goldenheart"
            };
        }

        private static string GetCulturalCityName(int index)
        {
            return index switch
            {
                0 => "Artspire",
                1 => "Musehaven",
                2 => "Creativity Center",
                3 => "Inspiration Point",
                4 => "Imagination Plaza",
                5 => "Expression Quarter",
                6 => "Culture District",
                7 => "Arts Quarter",
                8 => "Creative Hub",
                9 => "Artistic Enclave",
                10 => "Gallery District",
                11 => "Museum Quarter",
                12 => "Theater Row",
                13 => "Concert Hall",
                14 => "Opera House",
                15 => "Ballet Center",
                16 => "Dance Studio",
                17 => "Music Hall",
                18 => "Art Gallery",
                19 => "Cultural Center",
                _ => "Artspire"
            };
        }

        // Burst-compatible method to get biome descriptor
        private static string GetBiomeDescriptor(Core.BiomeType biome, int index)
        {
            // Convert enum to int for Burst compatibility
            int biomeInt = (int)biome;
            
            if (biomeInt == 0) return GetForestDescriptor(index);       // Forest
            if (biomeInt == 1) return GetMountainDescriptor(index);     // Mountains  
            if (biomeInt == 2) return GetDesertDescriptor(index);       // Desert
            if (biomeInt == 3) return GetOceanDescriptor(index);        // Ocean
            if (biomeInt == 4) return GetTundraDescriptor(index);       // Tundra
            if (biomeInt == 5) return GetSwampDescriptor(index);        // Swamp
            if (biomeInt == 6) return GetRainforestDescriptor(index);   // Rainforest
            if (biomeInt == 7) return GetPlainsDescriptor(index);       // Plains
            if (biomeInt == 8) return GetCoastDescriptor(index);        // Coast
            
            return "Ancient"; // Default fallback
        }

        private static string GetForestDescriptor(int index)
        {
            return index switch
            {
                0 => "Verdant",
                1 => "Emerald",
                2 => "Thornwood",
                3 => "Wildwood",
                4 => "Greenwood",
                5 => "Shadowleaf",
                6 => "Ironbark",
                7 => "Goldenbough",
                8 => "Silverleaf",
                9 => "Moonwood",
                _ => "Verdant"
            };
        }

        private static string GetMountainDescriptor(int index)
        {
            return index switch
            {
                0 => "Ironpeak",
                1 => "Stormcrown",
                2 => "Frostspire",
                3 => "Goldensummit",
                4 => "Shadowpeak",
                5 => "Crystalcrag",
                6 => "Thunderhead",
                7 => "Snowcap",
                8 => "Rockspire",
                9 => "Stonecrown",
                _ => "Ironpeak"
            };
        }

        private static string GetDesertDescriptor(int index)
        {
            return index switch
            {
                0 => "Sunscorch",
                1 => "Sandstorm",
                2 => "Goldendune",
                3 => "Mirage",
                4 => "Oasisborn",
                5 => "Dunewalker",
                6 => "Sunbaked",
                7 => "Heatwave",
                8 => "Scorching",
                9 => "Blazing",
                _ => "Sunscorch"
            };
        }

        private static string GetOceanDescriptor(int index)
        {
            return index switch
            {
                0 => "Deepwater",
                1 => "Stormtide",
                2 => "Wavebreak",
                3 => "Saltwind",
                4 => "Seafoam",
                5 => "Tideborn",
                6 => "Oceandeep",
                7 => "Seaspray",
                8 => "Wavecrest",
                9 => "Saltborn",
                _ => "Deepwater"
            };
        }

        private static string GetTundraDescriptor(int index)
        {
            return index switch
            {
                0 => "Frostborn",
                1 => "Icewind",
                2 => "Snowfall",
                3 => "Winterhold",
                4 => "Frostbite",
                5 => "Iceheart",
                6 => "Snowdrift",
                7 => "Blizzard",
                8 => "Frozen",
                9 => "Glacial",
                _ => "Frostborn"
            };
        }

        private static string GetSwampDescriptor(int index)
        {
            return index switch
            {
                0 => "Mistborn",
                1 => "Bogwater",
                2 => "Marshland",
                3 => "Murkwater",
                4 => "Swampgas",
                5 => "Wetland",
                6 => "Mireborn",
                7 => "Fenland",
                8 => "Quagmire",
                9 => "Morass",
                _ => "Mistborn"
            };
        }

        private static string GetRainforestDescriptor(int index)
        {
            return index switch
            {
                0 => "Verdant",
                1 => "Canopy",
                2 => "Jungle",
                3 => "Tropical",
                4 => "Lush",
                5 => "Dense",
                6 => "Thick",
                7 => "Overgrown",
                8 => "Wild",
                9 => "Untamed",
                _ => "Verdant"
            };
        }

        private static string GetPlainsDescriptor(int index)
        {
            return index switch
            {
                0 => "Windswept",
                1 => "Grassland",
                2 => "Prairie",
                3 => "Meadow",
                4 => "Steppe",
                5 => "Savanna",
                6 => "Field",
                7 => "Pasture",
                8 => "Range",
                9 => "Expanse",
                _ => "Windswept"
            };
        }

        private static string GetCoastDescriptor(int index)
        {
            return index switch
            {
                0 => "Shoreborn",
                1 => "Cliffside",
                2 => "Baywatch",
                3 => "Harbourlight",
                4 => "Seacliff",
                5 => "Tidepool",
                6 => "Rockshore",
                7 => "Sandbar",
                8 => "Lighthouse",
                9 => "Beacon",
                _ => "Shoreborn"
            };
        }

        private static string GetCitySuffix(int index)
        {
            return index switch
            {
                0 => "hold",
                1 => "spire",
                2 => "haven",
                3 => "gate",
                4 => "watch",
                5 => "crown",
                6 => "heart",
                7 => "forge",
                8 => "ward",
                9 => "keep",
                _ => "hold"
            };
        }

        // EPIC EVENT NAMES - Arrays removed and replaced with Burst-compatible switch statements
        // See GetWorldEventPrefix, GetDisasterName, GetGoldenAgeName, GetWarName methods

        // ===== EPIC GENERATION METHODS =====

        // COOL SHORT CIVILIZATION NAME GENERATOR - No boring long prefixes!
        public static FixedString128Bytes GenerateEpicCivilizationName(CivilizationType civType, float3 position, Core.BiomeType biome, PersonalityTraits personality)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            
            // 90% chance for single cool name, 10% chance for two-word combo - heavily favor single names
            if (random.NextFloat() < 0.9f)
            {
                // Generate single cool name based on personality and biome
                string coolName = GenerateSingleCoolName(civType, biome, personality, random);
                return TruncateToFixedString128(coolName);
            }
            else
            {
                // Generate two-word combo (Adjective + Noun)
                string adjective = GetCoolAdjective(civType, personality, random);
                string noun = GetCoolNoun(biome, civType, random);
                string twoWordName = $"{adjective} {noun}";
                return TruncateToFixedString128(twoWordName);
            }
        }

        // Generate single cool civilization name
        private static string GenerateSingleCoolName(CivilizationType civType, Core.BiomeType biome, PersonalityTraits personality, Unity.Mathematics.Random random)
        {
            // Choose based on personality first, then type
            if (personality.Aggressiveness > 7 || personality.Hatred > 6)
            {
                return GetAggressiveName(random.NextInt(0, 30));
            }
            else if (personality.Pride > 8 || personality.Ambition > 8)
            {
                return GetProudName(random.NextInt(0, 30));
            }
            else if (civType == CivilizationType.Religious)
            {
                return GetMysticalName(random.NextInt(0, 30));
            }
            else if (civType == CivilizationType.Technology)
            {
                return GetTechName(random.NextInt(0, 30));
            }
            else
            {
                // Mix of biome-influenced and general cool names
                return random.NextFloat() < 0.5f ? 
                    GetBiomeInfluencedName(biome, random.NextInt(0, 20)) : 
                    GetGeneralCoolName(random.NextInt(0, 40));
            }
        }

        // Generate cool adjective based on type and personality
        private static string GetCoolAdjective(CivilizationType civType, PersonalityTraits personality, Unity.Mathematics.Random random)
        {
            if (personality.Aggressiveness > 7)
            {
                return GetAggressiveAdjective(random.NextInt(0, 15));
            }
            else if (personality.Pride > 8)
            {
                return GetProudAdjective(random.NextInt(0, 15));
            }
            else
            {
                return civType switch
                {
                    CivilizationType.Military => GetMilitaryAdjective(random.NextInt(0, 15)),
                    CivilizationType.Technology => GetTechAdjective(random.NextInt(0, 15)),
                    CivilizationType.Religious => GetMysticalAdjective(random.NextInt(0, 15)),
                    CivilizationType.Trade => GetWealthyAdjective(random.NextInt(0, 15)),
                    CivilizationType.Cultural => GetWiseAdjective(random.NextInt(0, 15)),
                    _ => GetGeneralAdjective(random.NextInt(0, 20))
                };
            }
        }

        // Generate cool noun based on biome and type
        private static string GetCoolNoun(Core.BiomeType biome, CivilizationType civType, Unity.Mathematics.Random random)
        {
            // 50% chance for biome-influenced, 50% for type-influenced
            if (random.NextFloat() < 0.5f)
            {
                return GetBiomeNoun(biome, random.NextInt(0, 15));
            }
            else
            {
                return GetTypeNoun(civType, random.NextInt(0, 15));
            }
        }

        private static string GetMilitaryPrefix(int index)
        {
            return index switch
            {
                0 => "The Iron Empire of",
                1 => "The Steel Dominion of",
                2 => "The War Machine of",
                3 => "The Militant Republic of",
                4 => "The Fortress State of",
                5 => "The Battle-Forged Kingdom of",
                6 => "The Warrior Clans of",
                7 => "The Conquest Empire of",
                8 => "The Blood Legion of",
                9 => "The Warband of",
                _ => "The Iron Empire of"
            };
        }

        private static string GetTechPrefix(int index)
        {
            return index switch
            {
                0 => "The Innovation Hub of",
                1 => "The Tech Consortium of",
                2 => "The Digital Empire of",
                3 => "The Cyber Collective of",
                4 => "The Advanced Republic of",
                5 => "The Engineering Guild of",
                6 => "The Scientific Union of",
                7 => "The Quantum State of",
                8 => "The Mechanical Federation of",
                9 => "The Progressive Alliance of",
                _ => "The Innovation Hub of"
            };
        }

        private static string GetReligiousPrefix(int index)
        {
            return index switch
            {
                0 => "The Sacred Empire of",
                1 => "The Divine Kingdom of",
                2 => "The Holy Order of",
                3 => "The Blessed Realm of",
                4 => "The Celestial Domain of",
                5 => "The Spiritual Union of",
                6 => "The Faithful Republic of",
                7 => "The Righteous Coalition of",
                8 => "The Consecrated State of",
                9 => "The Devout Federation of",
                _ => "The Sacred Empire of"
            };
        }

        private static string GetTradePrefix(int index)
        {
            return index switch
            {
                0 => "The Trading Empire of",
                1 => "The Merchant Republic of",
                2 => "The Commercial League of",
                3 => "The Economic Union of",
                4 => "The Prosperity Coalition of",
                5 => "The Wealth Federation of",
                6 => "The Market Dominion of",
                7 => "The Golden Alliance of",
                8 => "The Business Consortium of",
                9 => "The Trade Syndicate of",
                _ => "The Trading Empire of"
            };
        }

        private static string GetCulturalPrefix(int index)
        {
            return index switch
            {
                0 => "The Artistic Republic of",
                1 => "The Cultural Union of",
                2 => "The Creative Coalition of",
                3 => "The Enlightened State of",
                4 => "The Scholarly Empire of",
                5 => "The Intellectual Federation of",
                6 => "The Wisdom Council of",
                7 => "The Learning Alliance of",
                8 => "The Knowledge Collective of",
                9 => "The Academic League of",
                _ => "The Artistic Republic of"
            };
        }

        private static string GetWarlikeRealmName(int index)
        {
            return index switch
            {
                0 => "Bloodforge",
                1 => "Warhammer",
                2 => "Battlecry",
                3 => "Ironwill",
                4 => "Conquest",
                5 => "Savage Lands",
                6 => "the Blood Crown",
                7 => "the War Machine",
                8 => "the Battle Throne",
                9 => "the Iron Fist",
                10 => "the Warforge",
                11 => "the Conquest Realm",
                12 => "the Battle Empire",
                13 => "the War Domain",
                14 => "the Iron Kingdom",
                15 => "the Blood Dominion",
                16 => "the Steel Throne",
                17 => "the War Crown",
                18 => "the Battle Realm",
                19 => "the Conquest Domain",
                _ => "Bloodforge"
            };
        }

        private static string GetPeacefulRealmName(int index)
        {
            return index switch
            {
                0 => "the Peaceful Realm",
                1 => "Harmony",
                2 => "the Serene Crown",
                3 => "Tranquil",
                4 => "the Wise Throne",
                5 => "Enlightened",
                6 => "the Scholar's Domain",
                7 => "Mindful",
                8 => "the Sage Crown",
                9 => "Contemplative",
                10 => "the Meditative Realm",
                11 => "Balanced",
                12 => "the Zen Throne",
                13 => "Peaceful",
                14 => "the Calm Crown",
                15 => "Serene",
                16 => "the Quiet Domain",
                17 => "Stillwater",
                18 => "the Gentle Throne",
                19 => "Compassionate",
                _ => "the Peaceful Realm"
            };
        }

        // ===== COOL SHORT NAME METHODS =====
        
        private static string GetAggressiveName(int index)
        {
            return index switch
            {
                0 => "Bloodfang", 1 => "Ironwrath", 2 => "Stormbreak", 3 => "Darkbane", 4 => "Firebrand",
                5 => "Shadowclaw", 6 => "Thornspike", 7 => "Grimhold", 8 => "Razorwind", 9 => "Voidstrike",
                10 => "Bonecrusher", 11 => "Hellforge", 12 => "Doomhammer", 13 => "Blackstorm", 14 => "Warfang",
                15 => "Deathwatch", 16 => "Ravenclaw", 17 => "Skullbreak", 18 => "Nightfall", 19 => "Vengeance",
                20 => "Conquest", 21 => "Brutalis", 22 => "Savage", 23 => "Predator", 24 => "Destroyer",
                25 => "Annihilus", 26 => "Carnage", 27 => "Rampage", 28 => "Havoc", 29 => "Chaos",
                _ => "Bloodfang"
            };
        }
        
        private static string GetProudName(int index)
        {
            return index switch
            {
                0 => "Goldspire", 1 => "Sunthrone", 2 => "Starfall", 3 => "Crownhold", 4 => "Majesty",
                5 => "Grandeur", 6 => "Splendor", 7 => "Radiance", 8 => "Brilliance", 9 => "Luminous",
                10 => "Sovereign", 11 => "Imperial", 12 => "Regal", 13 => "Noble", 14 => "Exalted",
                15 => "Supreme", 16 => "Magnificent", 17 => "Glorious", 18 => "Triumphant", 19 => "Victorious",
                20 => "Ascendant", 21 => "Paramount", 22 => "Pinnacle", 23 => "Apex", 24 => "Zenith",
                25 => "Celestial", 26 => "Divine", 27 => "Eternal", 28 => "Infinite", 29 => "Transcendent",
                _ => "Goldspire"
            };
        }
        
        private static string GetMysticalName(int index)
        {
            return index switch
            {
                0 => "Whisperwind", 1 => "Moonhaven", 2 => "Starweave", 3 => "Soulforge", 4 => "Spiritfall",
                5 => "Dreamhold", 6 => "Visionspire", 7 => "Oraculum", 8 => "Prophecy", 9 => "Sanctuary",
                10 => "Ethereal", 11 => "Mystique", 12 => "Arcanum", 13 => "Enigma", 14 => "Serenity",
                15 => "Harmony", 16 => "Tranquil", 17 => "Peaceful", 18 => "Blessed", 19 => "Sacred",
                20 => "Hallowed", 21 => "Consecrated", 22 => "Revered", 23 => "Sanctified", 24 => "Purified",
                25 => "Enlightened", 26 => "Awakened", 27 => "Illuminated", 28 => "Transcended", 29 => "Ascended",
                _ => "Whisperwind"
            };
        }
        
        private static string GetTechName(int index)
        {
            return index switch
            {
                0 => "Forgemaster", 1 => "Artificer", 2 => "Crafthold", 3 => "Gearwork", 4 => "Steamspire",
                5 => "Clockwork", 6 => "Mechanica", 7 => "Engineheart", 8 => "Copperfall", 9 => "Brassgear",
                10 => "Ironforge", 11 => "Steelworks", 12 => "Goldwright", 13 => "Silversmith", 14 => "Runeforge",
                15 => "Crystalwork", 16 => "Gemcutter", 17 => "Stonecarver", 18 => "Masterwork", 19 => "Precision",
                20 => "Innovation", 21 => "Invention", 22 => "Creation", 23 => "Discovery", 24 => "Progress",
                25 => "Advancement", 26 => "Breakthrough", 27 => "Pioneer", 28 => "Frontier", 29 => "Evolution",
                _ => "Forgemaster"
            };
        }
        
        private static string GetBiomeInfluencedName(Core.BiomeType biome, int index)
        {
            return biome switch
            {
                Core.BiomeType.Forest => GetForestName(index),
                Core.BiomeType.Mountains => GetMountainName(index),
                Core.BiomeType.Desert => GetDesertName(index),
                Core.BiomeType.Ocean => GetOceanName(index),
                Core.BiomeType.Tundra => GetTundraName(index),
                Core.BiomeType.Swamp => GetSwampName(index),
                Core.BiomeType.Jungle => GetJungleName(index),
                Core.BiomeType.Grassland => GetPlainsName(index),
                _ => GetGeneralCoolName(index)
            };
        }
        
        private static string GetForestName(int index)
        {
            return index switch
            {
                0 => "Greenwood", 1 => "Oakenheart", 2 => "Pinespire", 3 => "Willowbend", 4 => "Cedarfall",
                5 => "Birchwind", 6 => "Elmshade", 7 => "Mapleglow", 8 => "Ashenvale", 9 => "Thornwick",
                10 => "Fernhaven", 11 => "Mossdeep", 12 => "Roothold", 13 => "Barkstone", 14 => "Leafwhisper",
                15 => "Treefall", 16 => "Woodland", 17 => "Wildwood", 18 => "Deepwood", 19 => "Darkwood",
                _ => "Greenwood"
            };
        }
        
        private static string GetMountainName(int index)
        {
            return index switch
            {
                0 => "Ironpeak", 1 => "Stonehold", 2 => "Rockfall", 3 => "Cliffwatch", 4 => "Summitreach",
                5 => "Peakwind", 6 => "Ridgeback", 7 => "Crestone", 8 => "Highspire", 9 => "Skyreach",
                10 => "Cloudtop", 11 => "Snowcap", 12 => "Icefall", 13 => "Frostpeak", 14 => "Coldstone",
                15 => "Granite", 16 => "Marble", 17 => "Basalt", 18 => "Obsidian", 19 => "Crystal",
                _ => "Ironpeak"
            };
        }
        
        private static string GetDesertName(int index)
        {
            return index switch
            {
                0 => "Sunspear", 1 => "Sandfall", 2 => "Dunewind", 3 => "Mirage", 4 => "Oasis",
                5 => "Scorching", 6 => "Blazing", 7 => "Burning", 8 => "Searing", 9 => "Sweltering",
                10 => "Arid", 11 => "Barren", 12 => "Desolate", 13 => "Wasteland", 14 => "Badlands",
                15 => "Dryland", 16 => "Dustbowl", 17 => "Sandstorm", 18 => "Heatwave", 19 => "Sunbaked",
                _ => "Sunspear"
            };
        }
        
        private static string GetOceanName(int index)
        {
            return index switch
            {
                0 => "Deepcurrent", 1 => "Wavebreak", 2 => "Tidefall", 3 => "Coral", 4 => "Saltwind",
                5 => "Seaspray", 6 => "Whitecap", 7 => "Bluedeep", 8 => "Aquamarine", 9 => "Nautilus",
                10 => "Tempest", 11 => "Maelstrom", 12 => "Hurricane", 13 => "Typhoon", 14 => "Cyclone",
                15 => "Storm", 16 => "Thunder", 17 => "Lightning", 18 => "Torrent", 19 => "Deluge",
                _ => "Deepcurrent"
            };
        }
        
        private static string GetTundraName(int index)
        {
            return index switch
            {
                0 => "Frostwind", 1 => "Icefall", 2 => "Snowdrift", 3 => "Blizzard", 4 => "Glacier",
                5 => "Permafrost", 6 => "Iceberg", 7 => "Frozen", 8 => "Chilled", 9 => "Arctic",
                10 => "Polar", 11 => "Boreal", 12 => "Frigid", 13 => "Bitter", 14 => "Harsh",
                15 => "Severe", 16 => "Extreme", 17 => "Intense", 18 => "Brutal", 19 => "Unforgiving",
                _ => "Frostwind"
            };
        }
        
        private static string GetSwampName(int index)
        {
            return index switch
            {
                0 => "Mistfall", 1 => "Bogwater", 2 => "Marshwind", 3 => "Murkdeep", 4 => "Fenland",
                5 => "Mosswater", 6 => "Slimepool", 7 => "Quicksand", 8 => "Darkwater", 9 => "Shadowmere",
                10 => "Gloomhaven", 11 => "Miasma", 12 => "Putrid", 13 => "Stagnant", 14 => "Fetid",
                15 => "Rotten", 16 => "Decay", 17 => "Corruption", 18 => "Blight", 19 => "Plague",
                _ => "Mistfall"
            };
        }
        
        private static string GetJungleName(int index)
        {
            return index switch
            {
                0 => "Vinefall", 1 => "Canopy", 2 => "Undergrowth", 3 => "Wildvine", 4 => "Thicket",
                5 => "Bramble", 6 => "Tangle", 7 => "Overgrowth", 8 => "Lush", 9 => "Verdant",
                10 => "Emerald", 11 => "Jade", 12 => "Viridian", 13 => "Malachite", 14 => "Peridot",
                15 => "Tropical", 16 => "Exotic", 17 => "Paradise", 18 => "Eden", 19 => "Utopia",
                _ => "Vinefall"
            };
        }
        
        private static string GetPlainsName(int index)
        {
            return index switch
            {
                0 => "Grasswind", 1 => "Meadow", 2 => "Prairie", 3 => "Steppe", 4 => "Savanna",
                5 => "Pasture", 6 => "Field", 7 => "Range", 8 => "Expanse", 9 => "Vastland",
                10 => "Opensky", 11 => "Freewind", 12 => "Horizon", 13 => "Endless", 14 => "Infinite",
                15 => "Boundless", 16 => "Limitless", 17 => "Unrestricted", 18 => "Unconfined", 19 => "Unbounded",
                _ => "Grasswind"
            };
        }
        
        private static string GetGeneralCoolName(int index)
        {
            return index switch
            {
                0 => "Apex", 1 => "Zenith", 2 => "Pinnacle", 3 => "Summit", 4 => "Peak",
                5 => "Crown", 6 => "Throne", 7 => "Scepter", 8 => "Orb", 9 => "Jewel",
                10 => "Diamond", 11 => "Ruby", 12 => "Sapphire", 13 => "Emerald", 14 => "Topaz",
                15 => "Onyx", 16 => "Opal", 17 => "Pearl", 18 => "Amber", 19 => "Crystal",
                20 => "Prism", 21 => "Spectrum", 22 => "Aurora", 23 => "Nova", 24 => "Stellar",
                25 => "Cosmic", 26 => "Galactic", 27 => "Universal", 28 => "Infinite", 29 => "Eternal",
                30 => "Phoenix", 31 => "Dragon", 32 => "Griffin", 33 => "Titan", 34 => "Colossus",
                35 => "Leviathan", 36 => "Behemoth", 37 => "Kraken", 38 => "Hydra", 39 => "Chimera",
                _ => "Apex"
            };
        }

        // ===== ADJECTIVE METHODS FOR TWO-WORD NAMES =====
        
        private static string GetAggressiveAdjective(int index)
        {
            return index switch
            {
                0 => "Brutal", 1 => "Savage", 2 => "Fierce", 3 => "Ruthless", 4 => "Merciless",
                5 => "Vicious", 6 => "Cruel", 7 => "Deadly", 8 => "Lethal", 9 => "Fatal",
                10 => "Violent", 11 => "Aggressive", 12 => "Hostile", 13 => "Menacing", 14 => "Threatening",
                _ => "Brutal"
            };
        }
        
        private static string GetProudAdjective(int index)
        {
            return index switch
            {
                0 => "Golden", 1 => "Royal", 2 => "Noble", 3 => "Grand", 4 => "Majestic",
                5 => "Glorious", 6 => "Radiant", 7 => "Brilliant", 8 => "Shining", 9 => "Luminous",
                10 => "Splendid", 11 => "Magnificent", 12 => "Supreme", 13 => "Exalted", 14 => "Divine",
                _ => "Golden"
            };
        }
        
        private static string GetMilitaryAdjective(int index)
        {
            return index switch
            {
                0 => "Iron", 1 => "Steel", 2 => "Bronze", 3 => "Armored", 4 => "Fortified",
                5 => "Veteran", 6 => "Elite", 7 => "Tactical", 8 => "Strategic", 9 => "Militant",
                10 => "Warrior", 11 => "Battle", 12 => "Combat", 13 => "Fighting", 14 => "War",
                _ => "Iron"
            };
        }
        
        private static string GetTechAdjective(int index)
        {
            return index switch
            {
                0 => "Ingenious", 1 => "Masterful", 2 => "Ironbound", 3 => "Forged", 4 => "Steamborn",
                5 => "Mechanical", 6 => "Gearwright", 7 => "Innovative", 8 => "Inventive", 9 => "Creative",
                10 => "Clockwork", 11 => "Runic", 12 => "Arcane", 13 => "Mystical", 14 => "Superior",
                _ => "Ingenious"
            };
        }
        
        private static string GetMysticalAdjective(int index)
        {
            return index switch
            {
                0 => "Sacred", 1 => "Holy", 2 => "Divine", 3 => "Blessed", 4 => "Spiritual",
                5 => "Mystical", 6 => "Ethereal", 7 => "Celestial", 8 => "Transcendent", 9 => "Enlightened",
                10 => "Serene", 11 => "Peaceful", 12 => "Harmonious", 13 => "Balanced", 14 => "Pure",
                _ => "Sacred"
            };
        }
        
        private static string GetWealthyAdjective(int index)
        {
            return index switch
            {
                0 => "Rich", 1 => "Wealthy", 2 => "Prosperous", 3 => "Affluent", 4 => "Opulent",
                5 => "Luxurious", 6 => "Profitable", 7 => "Commercial", 8 => "Trading", 9 => "Merchant",
                10 => "Economic", 11 => "Financial", 12 => "Monetary", 13 => "Valuable", 14 => "Precious",
                _ => "Rich"
            };
        }
        
        private static string GetWiseAdjective(int index)
        {
            return index switch
            {
                0 => "Wise", 1 => "Ancient", 2 => "Mystic", 3 => "Elder", 4 => "Sage",
                5 => "Arcane", 6 => "Luminous", 7 => "Starborn", 8 => "Moonlit", 9 => "Enlightened",
                10 => "Dreaming", 11 => "Whispering", 12 => "Eternal", 13 => "Timeless", 14 => "Profound",
                _ => "Wise"
            };
        }
        
        private static string GetGeneralAdjective(int index)
        {
            return index switch
            {
                0 => "Ancient", 1 => "Eternal", 2 => "Infinite", 3 => "Legendary", 4 => "Mythic",
                5 => "Epic", 6 => "Grand", 7 => "Great", 8 => "Mighty", 9 => "Powerful",
                10 => "Strong", 11 => "Bold", 12 => "Brave", 13 => "Courageous", 14 => "Heroic",
                15 => "Noble", 16 => "Proud", 17 => "Free", 18 => "Independent", 19 => "Sovereign",
                _ => "Ancient"
            };
        }
        
        // ===== NOUN METHODS FOR TWO-WORD NAMES =====
        
        private static string GetBiomeNoun(Core.BiomeType biome, int index)
        {
            return biome switch
            {
                Core.BiomeType.Forest => GetForestNoun(index),
                Core.BiomeType.Mountains => GetMountainNoun(index),
                Core.BiomeType.Desert => GetDesertNoun(index),
                Core.BiomeType.Ocean => GetOceanNoun(index),
                Core.BiomeType.Tundra => GetTundraNoun(index),
                Core.BiomeType.Swamp => GetSwampNoun(index),
                Core.BiomeType.Jungle => GetJungleNoun(index),
                Core.BiomeType.Grassland => GetPlainsNoun(index),
                _ => GetGeneralNoun(index)
            };
        }
        
        private static string GetTypeNoun(CivilizationType civType, int index)
        {
            return civType switch
            {
                CivilizationType.Military => GetMilitaryNoun(index),
                CivilizationType.Technology => GetTechNoun(index),
                CivilizationType.Religious => GetReligiousNoun(index),
                CivilizationType.Trade => GetTradeNoun(index),
                CivilizationType.Cultural => GetCulturalNoun(index),
                _ => GetGeneralNoun(index)
            };
        }
        
        private static string GetForestNoun(int index)
        {
            return index switch
            {
                0 => "Grove", 1 => "Wood", 2 => "Forest", 3 => "Glade", 4 => "Thicket",
                5 => "Wildwood", 6 => "Dell", 7 => "Vale", 8 => "Hollow", 9 => "Glen",
                10 => "Shadowwood", 11 => "Greenwood", 12 => "Heartwood", 13 => "Ironwood", 14 => "Weald",
                _ => "Grove"
            };
        }
        
        private static string GetMountainNoun(int index)
        {
            return index switch
            {
                0 => "Peak", 1 => "Summit", 2 => "Ridge", 3 => "Crest", 4 => "Pinnacle",
                5 => "Heights", 6 => "Cliff", 7 => "Bluff", 8 => "Tor", 9 => "Fell",
                10 => "Pike", 11 => "Spire", 12 => "Crown", 13 => "Dome", 14 => "Massif",
                _ => "Peak"
            };
        }
        
        private static string GetDesertNoun(int index)
        {
            return index switch
            {
                0 => "Sands", 1 => "Dunes", 2 => "Wastes", 3 => "Expanse", 4 => "Reach",
                5 => "Flats", 6 => "Basin", 7 => "Valley", 8 => "Plain", 9 => "Mesa",
                10 => "Plateau", 11 => "Badlands", 12 => "Wilderness", 13 => "Void", 14 => "Emptiness",
                _ => "Sands"
            };
        }
        
        private static string GetOceanNoun(int index)
        {
            return index switch
            {
                0 => "Seas", 1 => "Waters", 2 => "Depths", 3 => "Tide", 4 => "Current",
                5 => "Wave", 6 => "Bay", 7 => "Gulf", 8 => "Sound", 9 => "Strait",
                10 => "Channel", 11 => "Reef", 12 => "Atoll", 13 => "Lagoon", 14 => "Harbor",
                _ => "Seas"
            };
        }
        
        private static string GetTundraNoun(int index)
        {
            return index switch
            {
                0 => "Frost", 1 => "Ice", 2 => "Snow", 3 => "Tundra", 4 => "Permafrost",
                5 => "Glacier", 6 => "Icefield", 7 => "Snowfield", 8 => "Polar", 9 => "Arctic",
                10 => "Boreal", 11 => "Taiga", 12 => "Steppes", 13 => "Moors", 14 => "Highlands",
                _ => "Frost"
            };
        }
        
        private static string GetSwampNoun(int index)
        {
            return index switch
            {
                0 => "Marsh", 1 => "Bog", 2 => "Fen", 3 => "Mire", 4 => "Swamp",
                5 => "Wetlands", 6 => "Bayou", 7 => "Slough", 8 => "Morass", 9 => "Quagmire",
                10 => "Marshland", 11 => "Bogland", 12 => "Fenland", 13 => "Mireland", 14 => "Swampland",
                _ => "Marsh"
            };
        }
        
        private static string GetJungleNoun(int index)
        {
            return index switch
            {
                0 => "Jungle", 1 => "Rainforest", 2 => "Canopy", 3 => "Undergrowth", 4 => "Thicket",
                5 => "Vine", 6 => "Tangle", 7 => "Overgrowth", 8 => "Verdure", 9 => "Foliage",
                10 => "Greenery", 11 => "Vegetation", 12 => "Flora", 13 => "Wilderness", 14 => "Wild",
                _ => "Jungle"
            };
        }
        
        private static string GetPlainsNoun(int index)
        {
            return index switch
            {
                0 => "Plains", 1 => "Grasslands", 2 => "Prairie", 3 => "Steppe", 4 => "Savanna",
                5 => "Meadow", 6 => "Field", 7 => "Pasture", 8 => "Range", 9 => "Expanse",
                10 => "Vastness", 11 => "Openness", 12 => "Freedom", 13 => "Horizon", 14 => "Sky",
                _ => "Plains"
            };
        }
        
        private static string GetMilitaryNoun(int index)
        {
            return index switch
            {
                0 => "Legion", 1 => "Guard", 2 => "Watch", 3 => "Order", 4 => "Brigade",
                5 => "Battalion", 6 => "Regiment", 7 => "Company", 8 => "Corps", 9 => "Division",
                10 => "Force", 11 => "Army", 12 => "Host", 13 => "Warband", 14 => "Militia",
                _ => "Legion"
            };
        }
        
        private static string GetTechNoun(int index)
        {
            return index switch
            {
                0 => "Forge", 1 => "Workshop", 2 => "Guild", 3 => "Craft", 4 => "Works",
                5 => "Foundry", 6 => "Smithy", 7 => "Atelier", 8 => "Laboratory", 9 => "Academy",
                10 => "Institute", 11 => "College", 12 => "School", 13 => "Hall", 14 => "Chamber",
                _ => "Forge"
            };
        }
        
        private static string GetReligiousNoun(int index)
        {
            return index switch
            {
                0 => "Temple", 1 => "Shrine", 2 => "Sanctuary", 3 => "Abbey", 4 => "Monastery",
                5 => "Cathedral", 6 => "Chapel", 7 => "Basilica", 8 => "Altar", 9 => "Oracle",
                10 => "Prophet", 11 => "Vision", 12 => "Faith", 13 => "Belief", 14 => "Creed",
                _ => "Temple"
            };
        }
        
        private static string GetTradeNoun(int index)
        {
            return index switch
            {
                0 => "Market", 1 => "Exchange", 2 => "Trading", 3 => "Commerce", 4 => "Merchant",
                5 => "Guild", 6 => "Company", 7 => "Consortium", 8 => "Syndicate", 9 => "Cartel",
                10 => "Enterprise", 11 => "Venture", 12 => "Business", 13 => "Trade", 14 => "Deal",
                _ => "Market"
            };
        }
        
        private static string GetCulturalNoun(int index)
        {
            return index switch
            {
                0 => "Academy", 1 => "Institute", 2 => "School", 3 => "University", 4 => "College",
                5 => "Library", 6 => "Museum", 7 => "Gallery", 8 => "Theater", 9 => "Opera",
                10 => "Arts", 11 => "Culture", 12 => "Learning", 13 => "Knowledge", 14 => "Wisdom",
                _ => "Academy"
            };
        }
        
        private static string GetGeneralNoun(int index)
        {
            return index switch
            {
                0 => "Empire", 1 => "Kingdom", 2 => "Republic", 3 => "Nation", 4 => "State",
                5 => "Realm", 6 => "Domain", 7 => "Territory", 8 => "Land", 9 => "Country",
                10 => "Federation", 11 => "Union", 12 => "Alliance", 13 => "Coalition", 14 => "League",
                _ => "Empire"
            };
        }

        private static string GetGeneralRealmName(int index)
        {
            return index switch
            {
                0 => "the Ancient Throne",
                1 => "the Golden Crown",
                2 => "the Silver Realm",
                3 => "the Crystal Domain",
                4 => "the Emerald Kingdom",
                5 => "the Sapphire Empire",
                6 => "the Ruby Dominion",
                7 => "the Diamond Throne",
                8 => "the Platinum Crown",
                9 => "the Jade Realm",
                10 => "the Obsidian Domain",
                11 => "the Marble Kingdom",
                12 => "the Bronze Empire",
                13 => "the Copper Dominion",
                14 => "the Iron Throne",
                15 => "the Steel Crown",
                16 => "the Titanium Realm",
                17 => "the Adamant Domain",
                18 => "the Mythril Kingdom",
                19 => "the Orichalcum Empire",
                20 => "the Celestial Dominion",
                21 => "the Astral Throne",
                22 => "the Ethereal Crown",
                23 => "the Spectral Realm",
                24 => "the Phantom Domain",
                25 => "the Shadow Kingdom",
                26 => "the Light Empire",
                27 => "the Dawn Dominion",
                28 => "the Twilight Throne",
                29 => "the Eternal Crown",
                _ => "the Ancient Throne"
            };
        }

        // EPIC HERO NAME GENERATOR - Context-sensitive based on leader type and achievements
        public static FixedString128Bytes GenerateEpicHeroName(HeroicLeaderType leaderType, PersonalityTraits personality, int achievements = 0)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(UnityEngine.Time.time * 1000 + achievements));
            
            // Get name and title using Burst-compatible methods
            var name = GetHeroNameByType(leaderType, random.NextInt(0, 20));
            var title = GetHeroTitleByType(leaderType, random.NextInt(0, 20));
            
            // Modify title based on personality extremes
            if (personality.Aggressiveness > 8 && leaderType == HeroicLeaderType.GreatConqueror)
            {
                title = GetExtremeTitleByIndex(random.NextInt(0, 5));
            }
            else if (personality.Pride > 8)
            {
                title = GetPridefulTitleByIndex(random.NextInt(0, 5));
            }
            
            string fullName = $"{name} {title}";
            return TruncateToFixedString128(fullName);
        }

        // Burst-compatible method to get hero name by type
        private static string GetHeroNameByType(HeroicLeaderType leaderType, int index)
        {
            return leaderType switch
            {
                HeroicLeaderType.GreatConqueror => GetConquerorName(index),
                HeroicLeaderType.WisePhilosopher => GetPhilosopherName(index),
                HeroicLeaderType.MasterBuilder => GetBuilderName(index),
                HeroicLeaderType.HolyProphet => GetProphetName(index),
                HeroicLeaderType.BrilliantInventor => GetInventorName(index),
                _ => GetConquerorName(index)
            };
        }

        // Burst-compatible method to get hero title by type
        private static string GetHeroTitleByType(HeroicLeaderType leaderType, int index)
        {
            return leaderType switch
            {
                HeroicLeaderType.GreatConqueror => GetConquerorTitle(index),
                HeroicLeaderType.WisePhilosopher => GetPhilosopherTitle(index),
                HeroicLeaderType.MasterBuilder => GetBuilderTitle(index),
                HeroicLeaderType.HolyProphet => GetProphetTitle(index),
                HeroicLeaderType.BrilliantInventor => GetInventorTitle(index),
                _ => GetConquerorTitle(index)
            };
        }

        // Burst-compatible hero name methods
        private static string GetConquerorName(int index)
        {
            return index switch
            {
                0 => "Ironclad",
                1 => "Bloodbane",
                2 => "Stormbreaker",
                3 => "Shadowbane",
                4 => "Doomhammer",
                5 => "Skullcrusher",
                6 => "Rageclaw",
                7 => "Flameheart",
                8 => "Frostbite",
                9 => "Thunderstrike",
                10 => "Nightfall",
                11 => "Dawnbreaker",
                12 => "Starfall",
                13 => "Voidwalker",
                14 => "Soulfire",
                15 => "Wraithbound",
                16 => "Grimward",
                17 => "Darkbane",
                18 => "Ironwill",
                19 => "Battleborn",
                _ => "Ironclad"
            };
        }

        private static string GetPhilosopherName(int index)
        {
            return index switch
            {
                0 => "Wiseheart",
                1 => "Sagewind",
                2 => "Brightmind",
                3 => "Deepthought",
                4 => "Stargazer",
                5 => "Truthseeker",
                6 => "Mindforge",
                7 => "Soulwise",
                8 => "Dreamwalker",
                9 => "Visionkeeper",
                10 => "Loremaster",
                11 => "Bookwarden",
                12 => "Scrollkeeper",
                13 => "Wordsmith",
                14 => "Inkwell",
                15 => "Quillborn",
                16 => "Pageturner",
                17 => "Storyweaver",
                18 => "Talekeeper",
                19 => "Wisdomborn",
                _ => "Wiseheart"
            };
        }

        private static string GetBuilderName(int index)
        {
            return index switch
            {
                0 => "Stonewright",
                1 => "Ironforge",
                2 => "Mastercraft",
                3 => "Goldenhammer",
                4 => "Steelshaper",
                5 => "Rockcarver",
                6 => "Wallbuilder",
                7 => "Bridgemaker",
                8 => "Towerwright",
                9 => "Castleborn",
                10 => "Archwright",
                11 => "Pillarmake",
                12 => "Foundationlayer",
                13 => "Cornerstone",
                14 => "Keystone",
                15 => "Capstone",
                16 => "Mortarmaster",
                17 => "Brickwright",
                18 => "Stoneborn",
                19 => "Buildmaster",
                _ => "Stonewright"
            };
        }

        private static string GetInventorName(int index)
        {
            return index switch
            {
                0 => "Gearwright",
                1 => "Steamforge",
                2 => "Clockwork",
                3 => "Mechanicus",
                4 => "Techwright",
                5 => "Innovator",
                6 => "Pioneer",
                7 => "Visionary",
                8 => "Genius",
                9 => "Prodigy",
                10 => "Sparkwright",
                11 => "Voltaic",
                12 => "Magnetic",
                13 => "Atomic",
                14 => "Quantum",
                15 => "Cyber",
                16 => "Digital",
                17 => "Virtual",
                18 => "Neural",
                19 => "Synthetic",
                _ => "Gearwright"
            };
        }

        // Burst-compatible hero title methods
        private static string GetConquerorTitle(int index)
        {
            return index switch
            {
                0 => "the Worldbreaker",
                1 => "the Unconquered",
                2 => "the Ironclad",
                3 => "the Bloodthirsty",
                4 => "the Merciless",
                5 => "the Unstoppable",
                6 => "the Destroyer",
                7 => "the Annihilator",
                8 => "the Devastator",
                9 => "the Ruiner",
                10 => "the Warlord",
                11 => "the Conqueror",
                12 => "the Dominator",
                13 => "the Subjugator",
                14 => "the Overlord",
                15 => "the Tyrant",
                16 => "the Despot",
                17 => "the Dictator",
                18 => "the Autocrat",
                19 => "the Sovereign",
                _ => "the Worldbreaker"
            };
        }

        private static string GetPhilosopherTitle(int index)
        {
            return index switch
            {
                0 => "the Wise",
                1 => "the Enlightened",
                2 => "the Sage",
                3 => "the Scholar",
                4 => "the Learned",
                5 => "the Thoughtful",
                6 => "the Contemplative",
                7 => "the Meditative",
                8 => "the Reflective",
                9 => "the Insightful",
                10 => "the Brilliant",
                11 => "the Genius",
                12 => "the Intellectual",
                13 => "the Academic",
                14 => "the Erudite",
                15 => "the Scholarly",
                16 => "the Studious",
                17 => "the Bookish",
                18 => "the Literate",
                19 => "the Educated",
                _ => "the Wise"
            };
        }

        private static string GetBuilderTitle(int index)
        {
            return index switch
            {
                0 => "the Architect",
                1 => "the Engineer",
                2 => "the Constructor",
                3 => "the Creator",
                4 => "the Designer",
                5 => "the Inventor",
                6 => "the Innovator",
                7 => "the Pioneer",
                8 => "the Visionary",
                9 => "the Mastermind",
                10 => "the Craftsman",
                11 => "the Artisan",
                12 => "the Maker",
                13 => "the Builder",
                14 => "the Shaper",
                15 => "the Former",
                16 => "the Molder",
                17 => "the Sculptor",
                18 => "the Carver",
                19 => "the Wright",
                _ => "the Architect"
            };
        }

        private static string GetProphetTitle(int index)
        {
            return index switch
            {
                0 => "the Divine",
                1 => "the Sacred",
                2 => "the Holy",
                3 => "the Blessed",
                4 => "the Chosen",
                5 => "the Anointed",
                6 => "the Consecrated",
                7 => "the Sanctified",
                8 => "the Hallowed",
                9 => "the Revered",
                10 => "the Exalted",
                11 => "the Sublime",
                12 => "the Transcendent",
                13 => "the Enlightened",
                14 => "the Ascended",
                15 => "the Radiant",
                16 => "the Luminous",
                17 => "the Brilliant",
                18 => "the Glorious",
                19 => "the Magnificent",
                _ => "the Divine"
            };
        }

        private static string GetInventorTitle(int index)
        {
            return index switch
            {
                0 => "the Ingenious",
                1 => "the Brilliant",
                2 => "the Innovative",
                3 => "the Creative",
                4 => "the Inventive",
                5 => "the Resourceful",
                6 => "the Clever",
                7 => "the Cunning",
                8 => "the Shrewd",
                9 => "the Astute",
                10 => "the Perceptive",
                11 => "the Insightful",
                12 => "the Discerning",
                13 => "the Observant",
                14 => "the Analytical",
                15 => "the Logical",
                16 => "the Rational",
                17 => "the Systematic",
                18 => "the Methodical",
                19 => "the Precise",
                _ => "the Ingenious"
            };
        }

        // Extreme personality title methods
        private static string GetExtremeTitleByIndex(int index)
        {
            return index switch
            {
                0 => "the Worldbreaker",
                1 => "the Annihilator",
                2 => "the Devastator",
                3 => "the Merciless",
                4 => "the Unstoppable",
                _ => "the Worldbreaker"
            };
        }

        private static string GetPridefulTitleByIndex(int index)
        {
            return index switch
            {
                0 => "the Magnificent",
                1 => "the Glorious",
                2 => "the Supreme",
                3 => "the Exalted",
                4 => "the Divine",
                _ => "the Magnificent"
            };
        }

        // EPIC EVENT NAME GENERATOR - Context-sensitive based on event type and significance
        public static FixedString128Bytes GenerateEpicEventName(EventCategory category, int significance, string[] involvedCivNames = null)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(UnityEngine.Time.time * 1000 + significance));
            
            string prefix = "";
            string eventName = "";
            
            // Choose prefix based on significance using Burst-compatible methods
            if (significance > 8)
            {
                prefix = GetWorldEventPrefix(random.NextInt(0, 5)); // "The Great", "The Eternal", etc.
            }
            else if (significance > 5)
            {
                prefix = GetWorldEventPrefix(random.NextInt(5, 10)); // "The Legendary", "The Mythical", etc.
            }
            else
            {
                prefix = GetWorldEventPrefix(random.NextInt(10, 20)); // Lesser prefixes
            }
            
            // Choose event name based on category using Burst-compatible methods
            eventName = category switch
            {
                EventCategory.Disaster => GetDisasterName(random.NextInt(0, 10)),
                EventCategory.Golden => GetGoldenAgeName(random.NextInt(0, 10)),
                EventCategory.Military => $"Military {GetWarName(random.NextInt(0, 15))}",
                EventCategory.Conflict => GetWarName(random.NextInt(0, 15)),
                EventCategory.Coalition => $"Coalition {GetWarName(random.NextInt(0, 15))}",
                EventCategory.HolyWar => $"Holy {GetWarName(random.NextInt(0, 15))}",
                EventCategory.Betrayal => "Betrayal",
                EventCategory.Hero => "Ascension",
                EventCategory.Revolution => "Revolution",
                EventCategory.Cascade => "Cascade",
                EventCategory.Escalation => "Escalation",
                EventCategory.Spiritual => "Awakening",
                EventCategory.Decline => "Decline",
                EventCategory.Discovery => "Discovery",
                _ => "Event"
            };
            
            // Add location/civilization context if available
            if (involvedCivNames != null && involvedCivNames.Length > 0)
            {
                var civName = involvedCivNames[random.NextInt(0, involvedCivNames.Length)];
                // Extract the epic part of the civ name (after "of")
                var parts = civName.Split(new[] { " of " }, System.StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    eventName = $"{eventName} of {parts[1]}";
                }
                else
                {
                    eventName = $"{eventName} of {civName}";
                }
            }
            
            string fullName = $"{prefix} {eventName}";
            return TruncateToFixedString128(fullName);
        }

        // Burst-compatible methods for event naming
        private static string GetWorldEventPrefix(int index)
        {
            return index switch
            {
                0 => "The Great",
                1 => "The Eternal",
                2 => "The Infinite",
                3 => "The Immortal",
                4 => "The Ultimate",
                5 => "The Legendary",
                6 => "The Mythical",
                7 => "The Epic",
                8 => "The Heroic",
                9 => "The Noble",
                10 => "The Glorious",
                11 => "The Magnificent",
                12 => "The Wondrous",
                13 => "The Marvelous",
                14 => "The Spectacular",
                15 => "The Extraordinary",
                16 => "The Remarkable",
                17 => "The Notable",
                18 => "The Significant",
                19 => "The Historic",
                _ => "The Great"
            };
        }

        private static string GetDisasterName(int index)
        {
            return index switch
            {
                0 => "Cataclysm",
                1 => "Apocalypse",
                2 => "Devastation",
                3 => "Calamity",
                4 => "Catastrophe",
                5 => "Ruin",
                6 => "Collapse",
                7 => "Downfall",
                8 => "Destruction",
                9 => "Annihilation",
                _ => "Cataclysm"
            };
        }

        private static string GetGoldenAgeName(int index)
        {
            return index switch
            {
                0 => "Renaissance",
                1 => "Enlightenment",
                2 => "Awakening",
                3 => "Flourishing",
                4 => "Golden Age",
                5 => "Prosperity",
                6 => "Ascension",
                7 => "Pinnacle",
                8 => "Zenith",
                9 => "Apex",
                _ => "Renaissance"
            };
        }

        private static string GetWarName(int index)
        {
            return index switch
            {
                0 => "War",
                1 => "Conflict",
                2 => "Campaign",
                3 => "Crusade",
                4 => "Conquest",
                5 => "Struggle",
                6 => "Battle",
                7 => "Strife",
                8 => "Clash",
                9 => "Confrontation",
                10 => "Uprising",
                11 => "Rebellion",
                12 => "Revolution",
                13 => "Insurrection",
                14 => "Revolt",
                _ => "War"
            };
        }

        // Religion naming arrays
        // Religion naming arrays removed - using Burst-compatible switch statements instead

        // Example: Pull hero/figure/event names from ECS (expand for more context)
        public static List<string> GetHeroNames(EntityManager em)
        {
            var names = new List<string>();
            var query = em.CreateEntityQuery(typeof(HistoricalEventData));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var data = em.GetComponentData<HistoricalEventData>(entity);
                if (!data.Title.IsEmpty)
                    names.Add(data.Title.ToString());
                else if (!data.Name.IsEmpty)
                    names.Add(data.Name.ToString());
            }
            return names;
        }

        public static List<string> GetReligionNames(EntityManager em)
        {
            var names = new List<string>();
            var query = em.CreateEntityQuery(typeof(ReligionData));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var data = em.GetComponentData<ReligionData>(entity);
                if (!data.Name.IsEmpty)
                    names.Add(data.Name.ToString());
            }
            return names;
        }

        // Main generator for monuments (example)
        public static FixedString128Bytes GenerateMonumentName(EntityManager em, float3 position)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            var heroNames = GetHeroNames(em);
            var monumentType = GetMonumentType(random.NextInt(0, 16));
            string name;
            if (heroNames.Count > 0 && random.NextFloat() < 0.5f)
            {
                var hero = heroNames[random.NextInt(0, heroNames.Count)];
                name = $"{monumentType} of {hero}";
            }
            else
            {
                var adj = GetAdjective(random.NextInt(0, 21));
                name = $"{adj} {monumentType}";
            }
            return TruncateToFixedString128(name);
        }

        // LEGACY WRAPPER - Use epic naming system
        public static FixedString128Bytes GenerateCityName(float3 position, Core.BiomeType biome)
        {
            // Default to military type for legacy calls
            return GenerateEpicCityName(CivilizationType.Military, position, biome);
        }

        // Main generator for biomes/regions
        public static FixedString128Bytes GenerateRegionName(float3 position, Core.BiomeType biome)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            var adj = GetAdjective(random.NextInt(0, 21));
            var type = GetPlaceTypeFixed(random.NextInt(0, 25));
            
            // Use FixedString concatenation for Burst compatibility
            var result = new FixedString128Bytes();
            result.Append(new FixedString32Bytes("The "));
            result.Append(adj);
            result.Append(new FixedString32Bytes(" "));
            result.Append(type);
            return result;
        }

        // Burst-compatible method to get place type by index
        private static string GetPlaceType(int index)
        {
            return index switch
            {
                0 => "Plains",
                1 => "Forest",
                2 => "Tundra",
                3 => "Jungle",
                4 => "Desert",
                5 => "Mountains",
                6 => "Coast",
                7 => "Isle",
                8 => "Valley",
                9 => "Pass",
                10 => "Bay",
                11 => "Reach",
                12 => "Hollow",
                13 => "Peak",
                14 => "Falls",
                15 => "Grove",
                16 => "Marsh",
                17 => "Wastes",
                18 => "Wilds",
                19 => "Heights",
                20 => "Fields",
                21 => "Dale",
                22 => "Cove",
                23 => "Cliffs",
                24 => "Fjord",
                _ => "Lands"
            };
        }

        // Burst-compatible method to get place type by index (FixedString version)
        private static FixedString64Bytes GetPlaceTypeFixed(int index)
        {
            return index switch
            {
                0 => new FixedString64Bytes("Plains"),
                1 => new FixedString64Bytes("Forest"),
                2 => new FixedString64Bytes("Tundra"),
                3 => new FixedString64Bytes("Jungle"),
                4 => new FixedString64Bytes("Desert"),
                5 => new FixedString64Bytes("Mountains"),
                6 => new FixedString64Bytes("Coast"),
                7 => new FixedString64Bytes("Isle"),
                8 => new FixedString64Bytes("Valley"),
                9 => new FixedString64Bytes("Pass"),
                10 => new FixedString64Bytes("Bay"),
                11 => new FixedString64Bytes("Reach"),
                12 => new FixedString64Bytes("Hollow"),
                13 => new FixedString64Bytes("Peak"),
                14 => new FixedString64Bytes("Falls"),
                15 => new FixedString64Bytes("Grove"),
                16 => new FixedString64Bytes("Marsh"),
                17 => new FixedString64Bytes("Wastes"),
                18 => new FixedString64Bytes("Wilds"),
                19 => new FixedString64Bytes("Heights"),
                20 => new FixedString64Bytes("Fields"),
                21 => new FixedString64Bytes("Dale"),
                22 => new FixedString64Bytes("Cove"),
                23 => new FixedString64Bytes("Cliffs"),
                24 => new FixedString64Bytes("Fjord"),
                _ => new FixedString64Bytes("Lands")
            };
        }

        // LEGACY WRAPPER - Use epic naming system
        public static FixedString128Bytes GenerateCivilizationName(float3 position, Core.BiomeType biome)
        {
            // Default to military type and neutral personality for legacy calls
            var defaultPersonality = new PersonalityTraits { Aggressiveness = 5, Defensiveness = 5, Greed = 5, Paranoia = 5, Ambition = 5, Desperation = 5, Hatred = 5, Pride = 5, Vengefulness = 5 };
            return GenerateEpicCivilizationName(CivilizationType.Military, position, biome, defaultPersonality);
        }

        // Overload for jobs: no EntityManager, just use static pools
        public static FixedString128Bytes GenerateMonumentName(float3 position)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            var monumentType = GetMonumentType(random.NextInt(0, 16));
            var adj = GetAdjective(random.NextInt(0, 21));
            
            // Use FixedString concatenation for Burst compatibility
            var result = new FixedString128Bytes();
            result.Append(adj);
            result.Append(new FixedString32Bytes(" "));
            result.Append(monumentType);
            return result;
        }

        // Burst-compatible method to get monument type by index
        private static FixedString64Bytes GetMonumentType(int index)
        {
            return index switch
            {
                0 => new FixedString64Bytes("Statue"),
                1 => new FixedString64Bytes("Obelisk"), 
                2 => new FixedString64Bytes("Spire"),
                3 => new FixedString64Bytes("Temple"),
                4 => new FixedString64Bytes("Shrine"),
                5 => new FixedString64Bytes("Monolith"),
                6 => new FixedString64Bytes("Pillar"),
                7 => new FixedString64Bytes("Arch"),
                8 => new FixedString64Bytes("Gate"),
                9 => new FixedString64Bytes("Tower"),
                10 => new FixedString64Bytes("Library"),
                11 => new FixedString64Bytes("Sanctum"),
                12 => new FixedString64Bytes("Crypt"),
                13 => new FixedString64Bytes("Hall"),
                14 => new FixedString64Bytes("Bridge"),
                15 => new FixedString64Bytes("Fountain"),
                _ => new FixedString64Bytes("Monument")
            };
        }

        // Burst-compatible method to get adjective by index
        private static FixedString64Bytes GetAdjective(int index)
        {
            return index switch
            {
                0 => new FixedString64Bytes("Ancient"),
                1 => new FixedString64Bytes("Lost"),
                2 => new FixedString64Bytes("Sacred"),
                3 => new FixedString64Bytes("Shattered"),
                4 => new FixedString64Bytes("Whispering"),
                5 => new FixedString64Bytes("Frozen"),
                6 => new FixedString64Bytes("Emerald"),
                7 => new FixedString64Bytes("Golden"),
                8 => new FixedString64Bytes("Silent"),
                9 => new FixedString64Bytes("Stormy"),
                10 => new FixedString64Bytes("Radiant"),
                11 => new FixedString64Bytes("Shadow"),
                12 => new FixedString64Bytes("Eternal"),
                13 => new FixedString64Bytes("Fabled"),
                14 => new FixedString64Bytes("Hidden"),
                15 => new FixedString64Bytes("Sunken"),
                16 => new FixedString64Bytes("Celestial"),
                17 => new FixedString64Bytes("Dread"),
                18 => new FixedString64Bytes("Iron"),
                19 => new FixedString64Bytes("Silver"),
                20 => new FixedString64Bytes("Obsidian"),
                _ => new FixedString64Bytes("Mysterious")
            };
        }

        // Burst-compatible method to convert BiomeType to string
        private static string GetBiomeString(Core.BiomeType biome)
        {
            return biome switch
            {
                Core.BiomeType.Forest => "Forest",
                Core.BiomeType.Mountains => "Mountain",
                Core.BiomeType.Desert => "Desert",
                Core.BiomeType.Ocean => "Ocean",
                Core.BiomeType.Tundra => "Tundra",
                Core.BiomeType.Swamp => "Swamp",
                Core.BiomeType.Rainforest => "Rainforest",
                Core.BiomeType.Plains => "Plains",
                Core.BiomeType.Coast => "Coast",
                _ => "Unknown"
            };
        }

        // EntityManager overloads for non-Burst systems
        public static FixedString128Bytes GenerateCityName(EntityManager em, float3 position, Core.BiomeType biome)
        {
            return GenerateCityName(position, biome);
        }

        public static FixedString128Bytes GenerateRegionName(EntityManager em, float3 position, Core.BiomeType biome)
        {
            return GenerateRegionName(position, biome);
        }

        public static FixedString128Bytes GenerateCivilizationName(EntityManager em, float3 position, Core.BiomeType biome)
        {
            // Default to military type and neutral personality for legacy calls
            var defaultPersonality = new PersonalityTraits { Aggressiveness = 5, Defensiveness = 5, Greed = 5, Paranoia = 5, Ambition = 5, Desperation = 5, Hatred = 5, Pride = 5, Vengefulness = 5 };
            return GenerateEpicCivilizationName(CivilizationType.Military, position, biome, defaultPersonality);
        }

        // Main religion name generator
        public static FixedString128Bytes GenerateReligionName(EntityManager em, float3 position, Core.BiomeType biome, ReligionBelief primaryBelief)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            
            // Get historical context
            var heroNames = GetHeroNames(em);
            var historicalEvents = GetHistoricalEventNames(em);
            
            string name = "";
            float nameType = random.NextFloat();
            
            if (nameType < 0.25f && heroNames.Count > 0)
            {
                // Prophet-based religion (25% chance)
                var prophet = heroNames.Count > 0 ? heroNames[random.NextInt(0, heroNames.Count)] : GetProphetName(random.NextInt(0, 20));
                var suffix = random.NextFloat() < 0.5f ? "ism" : "ites";
                name = $"{prophet}{suffix}";
            }
            else if (nameType < 0.4f)
            {
                // Natural phenomenon-based religion (15% chance)
                var phenomenon = GetNaturalPhenomenon(random.NextInt(0, 30));
                var term = GetReligiousTerm(random.NextInt(0, 30));
                name = random.NextFloat() < 0.5f ? $"Order of the {phenomenon}" : $"The {term} {phenomenon}";
            }
            else if (nameType < 0.55f && historicalEvents.Count > 0)
            {
                // Historical event-based religion (15% chance)
                var eventName = historicalEvents[random.NextInt(0, historicalEvents.Count)];
                var prefix = random.NextFloat() < 0.3f ? GetReligionPrefix(random.NextInt(0, 20)) : "";
                name = $"{prefix}Church of {eventName}";
            }
            else if (nameType < 0.7f)
            {
                // Belief-based religion (15% chance)
                var beliefName = GetBeliefName(primaryBelief);
                var term = GetReligiousTerm(random.NextInt(0, 30));
                name = $"The {term} {beliefName}";
            }
            else if (nameType < 0.85f)
            {
                // Biome-influenced religion (15% chance)
                var biomeAdjective = GetBiomeReligiousAdjective(biome, random);
                var term = GetReligiousTerm(random.NextInt(0, 30));
                var suffix = GetReligionSuffix(random.NextInt(0, 20));
                name = $"{biomeAdjective} {term}{suffix}";
            }
            else
            {
                // Mystical/Abstract religion (15% chance)
                var adj1 = GetReligiousTerm(random.NextInt(0, 30));
                var adj2 = GetReligiousTerm(random.NextInt(0, 30));
                while (adj2 == adj1) adj2 = GetReligiousTerm(random.NextInt(0, 30));
                name = $"Cult of {adj1} {adj2}";
            }
            
            return TruncateToFixedString128(name);
        }
        
        // Overload for jobs (no EntityManager)
        public static FixedString128Bytes GenerateReligionName(float3 position, Core.BiomeType biome, ReligionBelief primaryBelief)
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(((int)(position.x * 73856093)) ^ ((int)(position.y * 19349663)) ^ ((int)(position.z * 83492791))));
            
            string name = "";
            float nameType = random.NextFloat();
            
            if (nameType < 0.3f)
            {
                // Prophet-based religion
                var prophet = GetProphetName(random.NextInt(0, 20));
                var suffix = random.NextFloat() < 0.5f ? "ism" : "ites";
                name = $"{prophet}{suffix}";
            }
            else if (nameType < 0.5f)
            {
                // Natural phenomenon-based religion
                var phenomenon = GetNaturalPhenomenon(random.NextInt(0, 30));
                var term = GetReligiousTerm(random.NextInt(0, 30));
                name = random.NextFloat() < 0.5f ? $"Order of the {phenomenon}" : $"The {term} {phenomenon}";
            }
            else if (nameType < 0.7f)
            {
                // Belief-based religion
                var beliefName = GetBeliefName(primaryBelief);
                var term = GetReligiousTerm(random.NextInt(0, 30));
                name = $"The {term} {beliefName}";
            }
            else if (nameType < 0.85f)
            {
                // Biome-influenced religion
                var biomeAdjective = GetBiomeReligiousAdjective(biome, random);
                var term = GetReligiousTerm(random.NextInt(0, 30));
                var suffix = GetReligionSuffix(random.NextInt(0, 20));
                name = $"{biomeAdjective} {term}{suffix}";
            }
            else
            {
                // Mystical/Abstract religion
                var adj1 = GetReligiousTerm(random.NextInt(0, 30));
                var adj2 = GetReligiousTerm(random.NextInt(0, 30));
                while (adj2 == adj1) adj2 = GetReligiousTerm(random.NextInt(0, 30));
                name = $"Cult of {adj1} {adj2}";
            }
            
            return TruncateToFixedString128(name);
        }

        // Burst-compatible method to get prophet name by index
        private static string GetProphetName(int index)
        {
            return index switch
            {
                0 => "Lightbringer",
                1 => "Dawnseeker",
                2 => "Starwhisper",
                3 => "Moonchild",
                4 => "Sunborn",
                5 => "Celestial",
                6 => "Divine",
                7 => "Sacred",
                8 => "Holy",
                9 => "Blessed",
                10 => "Radiant",
                11 => "Luminous",
                12 => "Brilliant",
                13 => "Glorious",
                14 => "Magnificent",
                15 => "Sublime",
                16 => "Transcendent",
                17 => "Enlightened",
                18 => "Ascended",
                19 => "Exalted",
                _ => "Prophet"
            };
        }

        // Burst-compatible method to get natural phenomenon by index
        private static string GetNaturalPhenomenon(int index)
        {
            return index switch
            {
                0 => "Eclipse",
                1 => "Aurora",
                2 => "Comet",
                3 => "Tempest",
                4 => "Volcano",
                5 => "Tsunami",
                6 => "Lightning",
                7 => "Thunder",
                8 => "Earthquake",
                9 => "Meteor",
                10 => "Blizzard",
                11 => "Wildfire",
                12 => "Flood",
                13 => "Drought",
                14 => "Avalanche",
                15 => "Tornado",
                16 => "Hurricane",
                17 => "Geyser",
                18 => "Tide",
                19 => "Solstice",
                20 => "Equinox",
                21 => "Nebula",
                22 => "Supernova",
                23 => "Constellation",
                24 => "Galaxy",
                25 => "Void",
                26 => "Prism",
                27 => "Crystal",
                28 => "Glacier",
                29 => "Oasis",
                _ => "Phenomenon"
            };
        }

        // Burst-compatible method to get religious term by index
        private static string GetReligiousTerm(int index)
        {
            return index switch
            {
                0 => "Faith",
                1 => "Divine",
                2 => "Sacred",
                3 => "Holy",
                4 => "Blessed",
                5 => "Eternal",
                6 => "Infinite",
                7 => "Transcendent",
                8 => "Enlightened",
                9 => "Ascended",
                10 => "Radiant",
                11 => "Luminous",
                12 => "Celestial",
                13 => "Mystical",
                14 => "Spiritual",
                15 => "Sanctified",
                16 => "Hallowed",
                17 => "Consecrated",
                18 => "Revered",
                19 => "Exalted",
                20 => "Sublime",
                21 => "Profound",
                22 => "Omniscient",
                23 => "Omnipotent",
                24 => "Benevolent",
                25 => "Merciful",
                26 => "Righteous",
                27 => "Pure",
                28 => "Immaculate",
                29 => "Pristine",
                _ => "Sacred"
            };
        }

        // Burst-compatible method to get religion suffix by index
        private static string GetReligionSuffix(int index)
        {
            return index switch
            {
                0 => "ism",
                1 => "ity",
                2 => "ism",
                3 => "ology",
                4 => "osophy",
                5 => "ancy",
                6 => "ence",
                7 => "ation",
                8 => "hood",
                9 => "ship",
                10 => "dom",
                11 => "ward",
                12 => "path",
                13 => "way",
                14 => "light",
                15 => "truth",
                16 => "order",
                17 => "unity",
                18 => "harmony",
                19 => "balance",
                _ => "ism"
            };
        }

        // Burst-compatible method to get religion prefix by index
        private static string GetReligionPrefix(int index)
        {
            return index switch
            {
                0 => "Neo",
                1 => "Proto",
                2 => "Arch",
                3 => "Meta",
                4 => "Ultra",
                5 => "Hyper",
                6 => "Omni",
                7 => "Pan",
                8 => "Uni",
                9 => "Multi",
                10 => "Trans",
                11 => "Inter",
                12 => "Super",
                13 => "Mega",
                14 => "Macro",
                15 => "Micro",
                16 => "Pseudo",
                17 => "Quasi",
                18 => "Semi",
                19 => "Anti",
                _ => "Neo"
            };
        }

        public static List<string> GetHistoricalEventNames(EntityManager em)
        {
            var names = new List<string>();
            var query = em.CreateEntityQuery(typeof(HistoricalEventData));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var data = em.GetComponentData<HistoricalEventData>(entity);
                if (!data.Name.IsEmpty)
                    names.Add(data.Name.ToString());
                if (!data.Title.IsEmpty && data.Title.ToString() != data.Name.ToString())
                    names.Add(data.Title.ToString());
            }
            return names;
        }
        
        private static string GetBeliefName(ReligionBelief belief)
        {
            return belief switch
            {
                ReligionBelief.Monotheism => "Unity",
                ReligionBelief.Polytheism => "Pantheon",
                ReligionBelief.Pantheism => "Cosmos",
                ReligionBelief.Animism => "Spirits",
                ReligionBelief.Atheism => "Reason",
                _ => "Mystery"
            };
        }
        
        private static string GetBiomeReligiousAdjective(Core.BiomeType biome, Unity.Mathematics.Random random)
        {
            return biome switch
            {
                Core.BiomeType.Forest => random.NextFloat() < 0.5f ? "Verdant" : "Sylvan",
                Core.BiomeType.Mountains => random.NextFloat() < 0.5f ? "Celestial" : "Stone",
                Core.BiomeType.Desert => random.NextFloat() < 0.5f ? "Solar" : "Mirage",
                Core.BiomeType.Ocean => random.NextFloat() < 0.5f ? "Tidal" : "Abyssal",
                Core.BiomeType.Tundra => random.NextFloat() < 0.5f ? "Frozen" : "Aurora",
                Core.BiomeType.Swamp => random.NextFloat() < 0.5f ? "Mist" : "Bog",
                Core.BiomeType.Rainforest => random.NextFloat() < 0.5f ? "Primal" : "Canopy",
                Core.BiomeType.Plains => random.NextFloat() < 0.5f ? "Wind" : "Horizon",
                Core.BiomeType.Coast => random.NextFloat() < 0.5f ? "Tide" : "Shore",
                _ => "Ancient"
            };
        }

        // More grounded religion name generator using simulation context
        public static FixedString128Bytes GenerateContextualReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            var civ = context.FoundingCivilization;
            string name = "";
            
            // Determine religion type based on civilization characteristics
            float techLevel = civ.Technology;
            float cultureLevel = civ.Culture;
            float populationSize = civ.Population;
            
            // More advanced civilizations tend toward organized religions
            bool isOrganizedReligion = techLevel > 0.6f && populationSize > 500f;
            bool isPrimitiveReligion = techLevel < 0.3f || populationSize < 200f;
            bool isNatureReligion = context.LocalBiome != Core.BiomeType.Ocean && 
                                   context.LocalBiome != Core.BiomeType.Desert;
            
            float nameTypeRoll = random.NextFloat();
            
            if (isPrimitiveReligion && nameTypeRoll < 0.4f)
            {
                // Primitive/Tribal religions - focus on nature and spirits
                name = GeneratePrimitiveReligionName(context, random);
            }
            else if (isOrganizedReligion && nameTypeRoll < 0.3f)
            {
                // Organized religions - formal structures
                name = GenerateOrganizedReligionName(context, random);
            }
            else if (isNatureReligion && nameTypeRoll < 0.5f)
            {
                // Nature-based religions
                name = GenerateNatureReligionName(context, random);
            }
            else if (cultureLevel > 0.7f && nameTypeRoll < 0.6f)
            {
                // Philosophical/Cultural religions
                name = GeneratePhilosophicalReligionName(context, random);
            }
            else if (context.WorldAge > 100f && nameTypeRoll < 0.7f)
            {
                // Ancient/Traditional religions (for older worlds)
                name = GenerateAncientReligionName(context, random);
            }
            else
            {
                // Fallback to general religion naming
                name = GenerateGeneralReligionName(context, random);
            }
            
            return TruncateToFixedString128(name);
        }
        
        private static string GeneratePrimitiveReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            string[] primitiveTerms = { "Spirits", "Ancestors", "Totems", "Shamans", "Elders", "Tribe" };
            string[] primitiveActions = { "Calling", "Dancing", "Singing", "Dreaming", "Walking", "Speaking" };
            
            var biomeSpirit = GetBiomeSpirit(context.LocalBiome, random);
            var term = primitiveTerms[random.NextInt(0, primitiveTerms.Length)];
            var action = primitiveActions[random.NextInt(0, primitiveActions.Length)];
            
            return random.NextFloat() < 0.5f ? 
                $"{biomeSpirit} {term}" : 
                $"The {action} {term}";
        }
        
        private static string GenerateOrganizedReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            string[] organizationTypes = { "Church", "Temple", "Order", "Brotherhood", "Sisterhood", "Assembly", "Council" };
            string[] formalTerms = { "Divine", "Sacred", "Holy", "Blessed", "Eternal", "Supreme", "Universal" };
            
            var orgType = organizationTypes[random.NextInt(0, organizationTypes.Length)];
            var term = formalTerms[random.NextInt(0, formalTerms.Length)];
            var beliefName = GetBeliefName(context.PrimaryBelief);
            
            if (random.NextFloat() < 0.4f)
            {
                return $"The {term} {orgType}";
            }
            else
            {
                return $"{orgType} of {term} {beliefName}";
            }
        }
        
        private static string GenerateNatureReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            var biomeElement = GetBiomeElement(context.LocalBiome, random);
            var natureTerms = new string[] { "Grove", "Circle", "Path", "Way", "Keepers", "Guardians", "Children" };
            var natureTerm = natureTerms[random.NextInt(0, natureTerms.Length)];
            
            return random.NextFloat() < 0.6f ? 
                $"{biomeElement} {natureTerm}" : 
                $"The {natureTerm} of {biomeElement}";
        }
        
        private static string GeneratePhilosophicalReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            string[] philosophicalTerms = { "Wisdom", "Truth", "Knowledge", "Understanding", "Enlightenment", "Harmony", "Balance" };
            string[] schoolTypes = { "School", "Academy", "Institute", "Society", "Fellowship", "Circle" };
            
            var philTerm = philosophicalTerms[random.NextInt(0, philosophicalTerms.Length)];
            var schoolType = schoolTypes[random.NextInt(0, schoolTypes.Length)];
            
            if (context.CivilizationTech > 0.8f)
            {
                return $"The {schoolType} of {philTerm}";
            }
            else
            {
                return $"Seekers of {philTerm}";
            }
        }
        
        private static string GenerateAncientReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            string[] ancientPrefixes = { "Old", "Ancient", "First", "Elder", "Primordial", "Forgotten", "Lost" };
            string[] ancientTerms = { "Ways", "Traditions", "Rites", "Mysteries", "Secrets", "Lore", "Wisdom" };
            
            var prefix = ancientPrefixes[random.NextInt(0, ancientPrefixes.Length)];
            var term = ancientTerms[random.NextInt(0, ancientTerms.Length)];
            var biomeAdjective = GetBiomeReligiousAdjective(context.LocalBiome, random);
            
            return random.NextFloat() < 0.5f ? 
                $"The {prefix} {term}" : 
                $"{prefix} {biomeAdjective} {term}";
        }
        
        private static string GenerateGeneralReligionName(
            ProceduralWorld.Simulation.Systems.ReligionContext context, 
            Unity.Mathematics.Random random)
        {
            // Fallback to the original system but with context
            var phenomenon = GetNaturalPhenomenon(random.NextInt(0, 30));
            var term = GetReligiousTerm(random.NextInt(0, 30));
            
            return $"The {term} {phenomenon}";
        }
        
        private static string GetBiomeSpirit(Core.BiomeType biome, Unity.Mathematics.Random random)
        {
            return biome switch
            {
                Core.BiomeType.Forest => random.NextFloat() < 0.5f ? "Tree" : "Wood",
                Core.BiomeType.Mountains => random.NextFloat() < 0.5f ? "Stone" : "Peak",
                Core.BiomeType.Desert => random.NextFloat() < 0.5f ? "Sand" : "Sun",
                Core.BiomeType.Ocean => random.NextFloat() < 0.5f ? "Wave" : "Deep",
                Core.BiomeType.Tundra => random.NextFloat() < 0.5f ? "Ice" : "Wind",
                Core.BiomeType.Swamp => random.NextFloat() < 0.5f ? "Mist" : "Bog",
                Core.BiomeType.Rainforest => random.NextFloat() < 0.5f ? "Vine" : "Canopy",
                Core.BiomeType.Plains => random.NextFloat() < 0.5f ? "Grass" : "Sky",
                Core.BiomeType.Coast => random.NextFloat() < 0.5f ? "Tide" : "Shell",
                _ => "Earth"
            };
        }
        
        private static string GetBiomeElement(Core.BiomeType biome, Unity.Mathematics.Random random)
        {
            return biome switch
            {
                Core.BiomeType.Forest => random.NextFloat() < 0.5f ? "the Ancient Oak" : "the Whispering Leaves",
                Core.BiomeType.Mountains => random.NextFloat() < 0.5f ? "the High Peaks" : "the Stone Throne",
                Core.BiomeType.Desert => random.NextFloat() < 0.5f ? "the Burning Sands" : "the Endless Dunes",
                Core.BiomeType.Ocean => random.NextFloat() < 0.5f ? "the Eternal Tide" : "the Deep Current",
                Core.BiomeType.Tundra => random.NextFloat() < 0.5f ? "the Frozen Wastes" : "the Northern Lights",
                Core.BiomeType.Swamp => random.NextFloat() < 0.5f ? "the Misty Marsh" : "the Hidden Waters",
                Core.BiomeType.Rainforest => random.NextFloat() < 0.5f ? "the Green Cathedral" : "the Living Canopy",
                Core.BiomeType.Plains => random.NextFloat() < 0.5f ? "the Open Sky" : "the Endless Horizon",
                Core.BiomeType.Coast => random.NextFloat() < 0.5f ? "the Meeting Waters" : "the Salt Wind",
                _ => "the Sacred Earth"
            };
        }

        // Burst-compatible method to get biome epic descriptor
        private static string GetBiomeEpicDescriptor(Core.BiomeType biome, int index)
        {
            return biome switch
            {
                Core.BiomeType.Forest => GetForestEpicDescriptor(index),
                Core.BiomeType.Mountains => GetMountainEpicDescriptor(index),
                Core.BiomeType.Desert => GetDesertEpicDescriptor(index),
                Core.BiomeType.Ocean => GetOceanEpicDescriptor(index),
                Core.BiomeType.Tundra => GetTundraEpicDescriptor(index),
                Core.BiomeType.Swamp => GetSwampEpicDescriptor(index),
                Core.BiomeType.Rainforest => GetRainforestEpicDescriptor(index),
                Core.BiomeType.Plains => GetPlainsEpicDescriptor(index),
                Core.BiomeType.Coast => GetCoastEpicDescriptor(index),
                _ => ""
            };
        }

        private static string GetForestEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Verdant",
                1 => "Emerald",
                2 => "Thornwood",
                3 => "Wildwood",
                4 => "Greenwood",
                5 => "Shadowleaf",
                6 => "Ironbark",
                7 => "Goldenbough",
                8 => "Silverleaf",
                9 => "Moonwood",
                _ => "Verdant"
            };
        }

        private static string GetMountainEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Ironpeak",
                1 => "Stormcrown",
                2 => "Frostspire",
                3 => "Goldensummit",
                4 => "Shadowpeak",
                5 => "Crystalcrag",
                6 => "Thunderhead",
                7 => "Snowcap",
                8 => "Rockspire",
                9 => "Stonecrown",
                _ => "Ironpeak"
            };
        }

        private static string GetDesertEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Sunscorch",
                1 => "Sandstorm",
                2 => "Goldendune",
                3 => "Mirage",
                4 => "Oasisborn",
                5 => "Dunewalker",
                6 => "Sunbaked",
                7 => "Heatwave",
                8 => "Scorching",
                9 => "Blazing",
                _ => "Sunscorch"
            };
        }

        private static string GetOceanEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Deepwater",
                1 => "Stormtide",
                2 => "Wavebreak",
                3 => "Saltwind",
                4 => "Seafoam",
                5 => "Tideborn",
                6 => "Oceandeep",
                7 => "Seaspray",
                8 => "Wavecrest",
                9 => "Saltborn",
                _ => "Deepwater"
            };
        }

        private static string GetTundraEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Frostborn",
                1 => "Icewind",
                2 => "Snowfall",
                3 => "Winterhold",
                4 => "Frostbite",
                5 => "Iceheart",
                6 => "Snowdrift",
                7 => "Blizzard",
                8 => "Frozen",
                9 => "Glacial",
                _ => "Frostborn"
            };
        }

        private static string GetSwampEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Mistborn",
                1 => "Bogwater",
                2 => "Marshland",
                3 => "Murkwater",
                4 => "Swampgas",
                5 => "Wetland",
                6 => "Mireborn",
                7 => "Fenland",
                8 => "Quagmire",
                9 => "Morass",
                _ => "Mistborn"
            };
        }

        private static string GetRainforestEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Verdant",
                1 => "Canopy",
                2 => "Jungle",
                3 => "Tropical",
                4 => "Lush",
                5 => "Dense",
                6 => "Thick",
                7 => "Overgrown",
                8 => "Wild",
                9 => "Untamed",
                _ => "Verdant"
            };
        }

        private static string GetPlainsEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Windswept",
                1 => "Grassland",
                2 => "Prairie",
                3 => "Meadow",
                4 => "Steppe",
                5 => "Savanna",
                6 => "Field",
                7 => "Pasture",
                8 => "Range",
                9 => "Expanse",
                _ => "Windswept"
            };
        }

        private static string GetCoastEpicDescriptor(int index)
        {
            return index switch
            {
                0 => "Shoreborn",
                1 => "Cliffside",
                2 => "Baywatch",
                3 => "Harbourlight",
                4 => "Seacliff",
                5 => "Tidepool",
                6 => "Rockshore",
                7 => "Sandbar",
                8 => "Lighthouse",
                9 => "Beacon",
                _ => "Shoreborn"
            };
        }
        
        // Helper method to safely truncate strings for FixedString128Bytes (Burst-compatible)
        private static FixedString128Bytes TruncateToFixedString128(string input)
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
