using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.UI;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(Core.SimulationSystemGroup))]
    public partial class CivilizationInteractionSystem : SystemBase
    {
        private EntityQuery _civilizationQuery;
        private WorldHistorySystem _historySystem;
        private EntityQuery _terrainQuery;
        private EntityQuery _religionQuery;
        private EntityQuery _economyQuery;
        private EntityQuery _territoryQuery;
        private BeginSimulationEntityCommandBufferSystem _ecbSystem;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds for much more dynamic simulation
        private Unity.Mathematics.Random _random;

        protected override void OnCreate()
        {
            _civilizationQuery = GetEntityQuery(ComponentType.ReadWrite<CivilizationData>());
            _historySystem = World.GetOrCreateSystemManaged<WorldHistorySystem>();
            _terrainQuery = GetEntityQuery(ComponentType.ReadOnly<WorldTerrainData>());
            _religionQuery = GetEntityQuery(ComponentType.ReadWrite<ReligionData>());
            _economyQuery = GetEntityQuery(ComponentType.ReadWrite<EconomyData>());
            _territoryQuery = GetEntityQuery(ComponentType.ReadWrite<TerritoryData>());
            _ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            _nextUpdate = 0f;
            _random = Unity.Mathematics.Random.CreateFromIndex(1234);
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.Time.ElapsedTime < _nextUpdate)
                return;

            _nextUpdate = (float)SystemAPI.Time.ElapsedTime + UPDATE_INTERVAL;

            if (!SystemAPI.HasSingleton<SimulationConfig>())
                return;

            var config = SystemAPI.GetSingleton<SimulationConfig>();
            if (!config.EnableCivilizationInteractions)
                return;

            var civilizations = _civilizationQuery.ToEntityArray(Allocator.Temp);
            var civDataList = new NativeList<CivilizationData>(civilizations.Length, Allocator.Temp);

            // Get all civilization data
            for (int i = 0; i < civilizations.Length; i++)
            {
                var civData = EntityManager.GetComponentData<CivilizationData>(civilizations[i]);
                if (civData.IsActive && civData.Population > 0f)
                {
                    civDataList.Add(civData);
                }
            }

            if (civDataList.Length == 0)
            {
                civilizations.Dispose();
                civDataList.Dispose();
                return;
            }

            float deltaTime = UPDATE_INTERVAL;

            // DRAMATIC NEW SYSTEMS - Process in order of drama!
            ProcessWorldShakingEvents(civilizations, civDataList, deltaTime, _historySystem);
            ProcessHeroicLeaders(civilizations, civDataList, deltaTime, _historySystem);
            ProcessCoalitionWars(civilizations, civDataList, deltaTime, _historySystem);
            ProcessReligiousWars(civilizations, civDataList, deltaTime, _historySystem);
            ProcessBetrayalAndTreachery(civilizations, civDataList, deltaTime, _historySystem);
            ProcessCascadingEvents(civilizations, civDataList, deltaTime, _historySystem);
            
            // NEW: CIVILIZATION EMERGENCE - Revolts, civil wars, independence movements
            ProcessCivilizationEmergence(civilizations, civDataList, deltaTime, _historySystem);
            
            // Original systems (enhanced)
            ProcessCivilizationGrowth(civilizations, civDataList, deltaTime, _historySystem);
            ProcessWarfareAndConflicts(civilizations, civDataList, deltaTime, _historySystem);
            ProcessDiplomacyAndAlliances(civilizations, civDataList, deltaTime, _historySystem);
            ProcessTradeAndEconomics(civilizations, civDataList, deltaTime, _historySystem);
            ProcessReligionDynamics(civilizations, civDataList, deltaTime, _historySystem);
            ProcessCityExpansion(civilizations, civDataList, deltaTime, _historySystem);
            ProcessTechnologicalAdvancement(civilizations, civDataList, deltaTime, _historySystem);
            ProcessMonumentsAndWonders(civilizations, civDataList, deltaTime, _historySystem);
            ProcessAggressiveExpansion(civilizations, civDataList, deltaTime, _historySystem);
            ProcessCivilizationCollapse(civilizations, civDataList, deltaTime, _historySystem);
            ProcessPersonalityUpdates(civilizations, civDataList, deltaTime, _historySystem);

            // Update all civilization data back to entities
            for (int i = 0; i < civDataList.Length && i < civilizations.Length; i++)
            {
                EntityManager.SetComponentData(civilizations[i], civDataList[i]);
            }

            civilizations.Dispose();
            civDataList.Dispose();
        }

        // NEW: WORLD-SHAKING EVENTS - Plagues, disasters, golden ages
        private void ProcessWorldShakingEvents(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Rare but MASSIVE events that affect everyone
            if (_random.NextFloat() < 0.001f * deltaTime) // Very rare
            {
                int eventType = _random.NextInt(0, 6);
                
                switch (eventType)
                {
                    case 0: // GREAT PLAGUE
                        ProcessGreatPlague(civilizations, civDataList, historySystem);
                        break;
                    case 1: // GOLDEN AGE
                        ProcessGoldenAge(civilizations, civDataList, historySystem);
                        break;
                    case 2: // GREAT DISASTER
                        ProcessGreatDisaster(civilizations, civDataList, historySystem);
                        break;
                    case 3: // TECHNOLOGICAL REVOLUTION
                        ProcessTechRevolution(civilizations, civDataList, historySystem);
                        break;
                    case 4: // RELIGIOUS AWAKENING
                        ProcessReligiousAwakening(civilizations, civDataList, historySystem);
                        break;
                    case 5: // DARK AGE
                        ProcessDarkAge(civilizations, civDataList, historySystem);
                        break;
                }
            }
        }

        // NEW: HEROIC LEADERS - Great figures that change history
        private void ProcessHeroicLeaders(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Chance for a great leader to emerge (higher for large, stable civs)
                float leaderChance = (civ.Population / 10000f) * civ.Stability * 0.0005f * deltaTime;
                if (civ.Population > 15000f) leaderChance *= 2f; // Great civs breed great leaders
                
                if (_random.NextFloat() < leaderChance)
                {
                    int leaderType = _random.NextInt(0, 5);
                    ProcessHeroicLeader(civilizations[i], civ, leaderType, historySystem);
                    civDataList[i] = civ;
                }
            }
        }

        // NEW: COALITION WARS - Multiple civs gang up on powerful enemies
        private void ProcessCoalitionWars(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Find the most powerful civilization
            int strongestIndex = -1;
            float maxPower = 0f;
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                float power = civDataList[i].Population * civDataList[i].Military * civDataList[i].Technology;
                if (power > maxPower)
                {
                    maxPower = power;
                    strongestIndex = i;
                }
            }
            
            if (strongestIndex == -1 || maxPower < 50000f) return; // Need a truly powerful civ
            
            var strongest = civDataList[strongestIndex];
            
            // Check if others should form a coalition against them
            var coalitionMembers = new NativeList<int>(Allocator.Temp);
            float coalitionPower = 0f;
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (i == strongestIndex) continue;
                
                var civ = civDataList[i];
                float distance = math.distance(civ.Position, strongest.Position);
                
                // Join coalition if: close enough, threatened, or ambitious
                bool threatened = distance < 200f && strongest.Aggressiveness > 6f;
                bool ambitious = civ.Ambition > 7f && civ.Military > 5f;
                bool fearful = civ.Paranoia > 6f && strongest.Population > civ.Population * 2f;
                
                if (threatened || ambitious || fearful)
                {
                    coalitionMembers.Add(i);
                    coalitionPower += civ.Population * civ.Military * civ.Technology;
                }
            }
            
            // Coalition war if they have enough combined power
            if (coalitionMembers.Length >= 2 && coalitionPower > maxPower * 0.8f)
            {
                ProcessCoalitionWar(civilizations, civDataList, strongestIndex, coalitionMembers, historySystem);
            }
            
            coalitionMembers.Dispose();
        }

        private void ProcessCivilizationGrowth(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // REALISTIC CARRYING CAPACITY AND RESOURCE LIMITS
                float carryingCapacity = CalculateCarryingCapacity(civ);
                float populationPressure = civ.Population / carryingCapacity;
                
                // Population growth with civilization-specific bonuses
                float baseGrowthRate = GetCivilizationGrowthRate(civ);
                
                // Apply specialization bonuses
                if (civ.Technology > 6f && civ.Population < 20000f)
                {
                    baseGrowthRate += 0.05f; // Tech civilizations grow faster
                }
                if (civ.Trade > 6f && civ.Population < 15000f)
                {
                    baseGrowthRate += 0.04f; // Trade brings prosperity and growth
                }
                if (civ.Religion > 6f && civ.Population < 12000f)
                {
                    baseGrowthRate += 0.03f; // Religious unity encourages families
                }
                if (civ.Military > 6f && civ.Population > 3000f)
                {
                    baseGrowthRate += 0.02f; // Military protection enables growth
                }
                
                // Apply carrying capacity pressure (exponential decline when over capacity)
                if (populationPressure > 1f)
                {
                    float overpopulationPenalty = math.pow(populationPressure - 1f, 2f) * -0.1f;
                    baseGrowthRate += overpopulationPenalty;
                }
                
                // Resource depletion for large populations
                float resourceConsumption = civ.Population / 1000f; // Each 1000 people consume 1 resource unit
                float resourceDeficit = math.max(0f, resourceConsumption - civ.Resources);
                if (resourceDeficit > 0f)
                {
                    // Starvation and resource wars
                    float starvationPenalty = resourceDeficit / civ.Population * -2f;
                    baseGrowthRate += starvationPenalty;
                }
                
                // Apply modifiers
                float stabilityModifier = (civ.Stability - 0.5f) * 0.1f;
                float technologyModifier = civ.Technology * 0.01f;
                
                float finalGrowthRate = baseGrowthRate + stabilityModifier + technologyModifier;
                finalGrowthRate = math.clamp(finalGrowthRate, -0.15f, 0.2f);
                
                float oldPopulation = civ.Population;
                civ.Population = math.max(100f, civ.Population * (1f + finalGrowthRate * deltaTime));
                
                // RESOURCE DEPLETION - Large populations consume resources
                float resourceConsumptionRate = civ.Population / 2000f; // Each 2000 people consume 1 resource per update
                civ.Resources = math.max(0f, civ.Resources - resourceConsumptionRate * deltaTime);
                
                // Resource regeneration with specialization bonuses
                float baseRegeneration = (civ.Technology * 0.5f + civ.Stability * 2f) * deltaTime;
                
                // Specialization bonuses for resource generation
                if (civ.Technology > 6f) baseRegeneration += 2.0f * deltaTime; // Tech civs are very efficient
                if (civ.Trade > 6f) baseRegeneration += 1.5f * deltaTime; // Trade civs get resource bonuses
                if (civ.Military > 6f) baseRegeneration += 1.0f * deltaTime; // Military civs can extract resources
                if (civ.Religion > 6f) baseRegeneration += 0.8f * deltaTime; // Religious civs have organized labor
                
                // Environmental bonuses
                baseRegeneration *= GetEnvironmentalMultiplier(civ);
                
                civ.Resources = math.min(15000f, civ.Resources + baseRegeneration);
                
                // Technology advancement - harder for large civilizations
                float techGrowth = 0f;
                if (civ.Population < 10000f)
                {
                    techGrowth = (civ.Wealth / 5000f + civ.Stability * 0.5f) * deltaTime * 0.05f;
                }
                else
                {
                    // Large civilizations struggle with technological advancement due to complexity
                    techGrowth = (civ.Wealth / 10000f) * deltaTime * 0.02f;
                }
                civ.Technology = math.min(10f, civ.Technology + techGrowth);
                
                // Wealth generation with specialization bonuses
                float baseIncome = math.min(civ.Population * 0.1f, 1000f); // Diminishing returns
                float maintenanceCost = civ.Population * 0.15f; // Large populations are expensive to maintain
                
                // Specialization bonuses for wealth generation
                float specializationBonus = 0f;
                if (civ.Trade > 6f) specializationBonus += civ.Trade * 150f; // Trade civs generate lots of wealth
                if (civ.Technology > 6f) specializationBonus += civ.Technology * 120f; // Tech civs are efficient
                if (civ.Military > 6f) specializationBonus += civ.Military * 80f; // Military civs can raid/tax
                if (civ.Religion > 6f) specializationBonus += civ.Religion * 60f; // Religious civs collect tithes
                if (civ.Culture > 6f) specializationBonus += civ.Culture * 70f; // Cultural civs attract tourism/trade
                
                // Environmental wealth bonuses
                specializationBonus *= GetEnvironmentalMultiplier(civ);
                
                float wealthChange = (baseIncome + specializationBonus - maintenanceCost) * deltaTime;
                civ.Wealth = math.max(0f, civ.Wealth + wealthChange);
                
                // REALISTIC STABILITY SYSTEM
                float stabilityChange = 0f;
                
                // Population size penalties (large civilizations are inherently unstable)
                if (civ.Population > 20000f)
                {
                    stabilityChange -= 0.15f; // Massive civilizations are very unstable
                }
                else if (civ.Population > 10000f)
                {
                    stabilityChange -= 0.08f; // Large civilizations are unstable
                }
                else if (civ.Population > 5000f)
                {
                    stabilityChange -= 0.03f; // Medium civilizations have some instability
                }
                else if (civ.Population < 2000f)
                {
                    stabilityChange += 0.05f; // Small civilizations are more stable
                }
                
                // Resource scarcity causes massive instability
                if (resourceDeficit > 0f)
                {
                    float scarcityPenalty = (resourceDeficit / civ.Population) * -5f;
                    stabilityChange += scarcityPenalty;
                }
                
                // Overpopulation instability
                if (populationPressure > 1.2f)
                {
                    stabilityChange -= (populationPressure - 1.2f) * 0.3f;
                }
                
                // Wealth effects
                if (civ.Wealth < civ.Population * 0.1f) // Not enough wealth per person
                {
                    stabilityChange -= 0.1f; // Poverty causes instability
                }
                else if (civ.Wealth > civ.Population * 0.5f)
                {
                    stabilityChange += 0.03f; // Prosperity helps stability
                }
                
                // Specialization stability bonuses
                if (civ.Technology > 6f && civ.Population < 15000f)
                {
                    stabilityChange += 0.04f; // Tech civs are more stable
                }
                if (civ.Religion > 6f)
                {
                    stabilityChange += 0.05f; // Religious unity provides stability
                }
                if (civ.Culture > 6f)
                {
                    stabilityChange += 0.03f; // Cultural identity helps stability
                }
                if (civ.Military > 6f && civ.Population > 3000f)
                {
                    stabilityChange += 0.02f; // Military provides order
                }
                if (civ.Trade > 6f)
                {
                    stabilityChange += 0.02f; // Trade brings prosperity and stability
                }
                
                civ.Stability = math.clamp(civ.Stability + stabilityChange * deltaTime, 0.05f, 1f);
                
                // CATASTROPHIC COLLAPSE for unsustainable civilizations
                if (civ.Population > 25000f || (civ.Population > 15000f && civ.Stability < 0.2f) || 
                    (resourceDeficit > civ.Population * 0.5f))
                {
                    // Civilization is doomed - trigger catastrophic collapse
                    ProcessCatastrophicCollapse(civilizations[i], civ, historySystem);
                    civ.Population *= 0.3f; // 70% population loss
                    civ.Wealth *= 0.1f; // 90% wealth loss
                    civ.Stability = 0.1f; // Near total instability
                    civ.Resources *= 0.2f; // Resource depletion
                }
                
                // Check for major milestones
                if (civ.Population > oldPopulation * 2f && !civ.HasReachedPopulationMilestone)
                {
                    civ.HasReachedPopulationMilestone = true;
                    var growthEvent = new HistoricalEventRecord
                    {
                        Title = $"{civ.Name} Population Boom",
                        Description = $"{civ.Name} has experienced massive population growth, doubling in size!",
                        Year = (int)SystemAPI.Time.ElapsedTime,
                        Type = ProceduralWorld.Simulation.Core.EventType.Social,
                        Category = EventCategory.Growth,
                        Location = civ.Position,
                        Significance = 1.5f,
                        SourceEntityId = Entity.Null,
                        Size = 1.0f,
                        CivilizationId = civilizations[i]
                    };
                    historySystem.AddEvent(growthEvent);
                }
                
                civDataList[i] = civ;
            }
        }

        private void ProcessTradeAndEconomics(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Establish trade routes between nearby civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civA = civDataList[i];
                for (int j = i + 1; j < civDataList.Length; j++)
                {
                    var civB = civDataList[j];
                    float distance = math.distance(civA.Position, civB.Position);
                    
                    // Trade is more likely between nearby, stable civilizations
                    if (distance < 300f && civA.Stability > 0.4f && civB.Stability > 0.4f) // Increased range and lower requirements
                    {
                        float tradeChance = (civA.Stability + civB.Stability) * 0.5f * deltaTime * 2.0f; // 20x more likely!
                        if (_random.NextFloat() < tradeChance)
                        {
                            // Establish trade
                            float tradeValue = math.min(civA.Wealth, civB.Wealth) * 0.1f;
                            civA.Wealth += tradeValue * 0.1f;
                            civB.Wealth += tradeValue * 0.1f;
                            civA.Trade = math.min(10f, civA.Trade + 0.1f);
                            civB.Trade = math.min(10f, civB.Trade + 0.1f);
                            
                            // Create trade event more frequently
                            if (_random.NextFloat() < 0.3f) // 3x more trade events
                            {
                                // Create safe names for the trade event
                                string nameA = GetSafeCivilizationName(civA.Name);
                                string nameB = GetSafeCivilizationName(civB.Name);
                                
                                string tradeTitle = $"Trade: {nameA} & {nameB}";
                                string tradeDesc = $"{nameA} and {nameB} have established profitable trade relations.";
                                
                                var tradeEvent = new HistoricalEventRecord
                                {
                                    Title = new FixedString128Bytes(tradeTitle),
                                    Description = new FixedString512Bytes(tradeDesc),
                                    Year = (int)SystemAPI.Time.ElapsedTime,
                                    Type = ProceduralWorld.Simulation.Core.EventType.Economic,
                                    Category = EventCategory.Trade,
                                    Location = (civA.Position + civB.Position) * 0.5f,
                                    Significance = 1.0f,
                                    SourceEntityId = Entity.Null,
                                    Size = 1.0f,
                                    CivilizationId = civilizations[i]
                                };
                                historySystem.AddEvent(tradeEvent);
                            }
                            
                            civDataList[i] = civA;
                            civDataList[j] = civB;
                        }
                    }
                }
            }
        }

        private void ProcessWarfareAndConflicts(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civA = civDataList[i];
                for (int j = i + 1; j < civDataList.Length; j++)
                {
                    var civB = civDataList[j];
                    float distance = math.distance(civA.Position, civB.Position);
                    
                    if (distance < 150f) // Conflict range
                    {
                        // Small civilizations are less likely to fight
                        bool bothSmall = civA.Population < 4000f && civB.Population < 4000f;
                        bool oneSmall = civA.Population < 2000f || civB.Population < 2000f;
                        
                        if (bothSmall) continue; // Small civs don't fight each other
                        
                        // PERSONALITY-DRIVEN WAR TRIGGERS
                        bool resourceCompetition = math.abs(civA.Wealth - civB.Wealth) > 3000f;
                        bool techDisparity = math.abs(civA.Technology - civB.Technology) > 3f;
                        bool instability = civA.Stability < 0.3f || civB.Stability < 0.3f;
                        
                        // NEW: Personality-based war triggers
                        bool aggressivePersonality = civA.Aggressiveness > 6f || civB.Aggressiveness > 6f;
                        bool desperateForResources = (civA.Desperation > 5f && civA.Greed > 4f) || (civB.Desperation > 5f && civB.Greed > 4f);
                        bool hatefulVengeance = (civA.Hatred > 6f && civA.Vengefulness > 5f) || (civB.Hatred > 6f && civB.Vengefulness > 5f);
                        bool resourceStress = civA.ResourceStressLevel > 0.8f || civB.ResourceStressLevel > 0.8f;
                        
                        float warChance = 0f;
                        if (resourceCompetition) warChance += 0.05f;
                        if (techDisparity) warChance += 0.08f;
                        if (instability) warChance += 0.1f;
                        
                        // PERSONALITY-BASED WAR CHANCES
                        if (aggressivePersonality) warChance += 0.15f; // Aggressive civs start wars
                        if (desperateForResources) warChance += 0.2f; // Desperate civs attack for resources
                        if (hatefulVengeance) warChance += 0.25f; // Hateful civs seek revenge
                        if (resourceStress) warChance += 0.12f; // Resource stress drives conflict
                        
                        // Personality modifiers
                        float aggressorAggression = math.max(civA.Aggressiveness, civB.Aggressiveness);
                        float aggressorDesperation = math.max(civA.Desperation, civB.Desperation);
                        warChance += (aggressorAggression / 10f) * 0.1f;
                        warChance += (aggressorDesperation / 10f) * 0.15f;
                        
                        // Large civilization competition bonus
                        if (civA.Population > 10000f && civB.Population > 10000f) warChance *= 2f;
                        
                        // Small civilization protection
                        if (oneSmall) warChance *= 0.3f;
                        
                        warChance *= deltaTime;
                        
                        if (_random.NextFloat() < warChance)
                        {
                            // Determine winner based on military strength, technology, and population
                            float civAStrength = civA.Military * civA.Technology * (civA.Population / 1000f);
                            float civBStrength = civB.Military * civB.Technology * (civB.Population / 1000f);
                            
                            var winner = civAStrength > civBStrength ? civA : civB;
                            var loser = civAStrength > civBStrength ? civB : civA;
                            int winnerIndex = civAStrength > civBStrength ? i : j;
                            int loserIndex = civAStrength > civBStrength ? j : i;
                            
                            // ENHANCED: Much more dramatic war consequences
                            bool loserIsSmall = loser.Population < 3000f;
                            bool isDesperateWar = loser.Desperation > 7f || winner.Aggressiveness > 8f;
                            bool isVengeanceWar = loser.Vengefulness > 6f || winner.Hatred > 6f;
                            
                            // Base losses - now much higher!
                            float populationLoss = loserIsSmall ? _random.NextFloat(0.15f, 0.35f) : _random.NextFloat(0.25f, 0.55f);
                            float resourceTransfer = loserIsSmall ? _random.NextFloat(0.2f, 0.4f) : _random.NextFloat(0.3f, 0.7f);
                            
                            // Dramatic modifiers
                            if (isDesperateWar) 
                            {
                                populationLoss *= 1.5f; // Desperate wars are bloodier
                                resourceTransfer *= 1.3f;
                            }
                            if (isVengeanceWar)
                            {
                                populationLoss *= 1.4f; // Vengeance wars are brutal
                            }
                            
                            winner.Population += loser.Population * 0.1f; // Some population absorbed
                            winner.Wealth += loser.Wealth * resourceTransfer;
                            winner.Military = math.min(10f, winner.Military + 0.5f); // Military experience
                            
                            loser.Population *= (1f - populationLoss);
                            loser.Wealth *= (1f - resourceTransfer);
                            float stabilityLoss = loserIsSmall ? 0.15f : 0.3f; // Less stability loss for small civs
                            loser.Stability = math.max(0.2f, loser.Stability - stabilityLoss);
                            
                            // PERSONALITY CHANGES FROM WAR
                            // Winner becomes more aggressive and proud
                            winner.Aggressiveness = math.min(10f, winner.Aggressiveness + 0.5f);
                            winner.Pride = math.min(10f, winner.Pride + 0.8f);
                            winner.Ambition = math.min(10f, winner.Ambition + 0.3f);
                            winner.SuccessfulWars++;
                            
                            // Loser becomes more defensive, paranoid, and potentially hateful
                            loser.Defensiveness = math.min(10f, loser.Defensiveness + 1.0f);
                            loser.Paranoia = math.min(10f, loser.Paranoia + 0.8f);
                            loser.Hatred = math.min(10f, loser.Hatred + 1.2f);
                            loser.Vengefulness = math.min(10f, loser.Vengefulness + 1.0f);
                            loser.TimesAttacked++;
                            loser.LostWars++;
                            loser.LastAttackedYear = (float)SystemAPI.Time.ElapsedTime;
                            
                            // Major defeat causes humiliation
                            if (populationLoss > 0.2f || resourceTransfer > 0.4f)
                            {
                                loser.HasBeenHumiliated = true;
                                loser.Hatred = math.min(10f, loser.Hatred + 2.0f);
                                loser.Vengefulness = math.min(10f, loser.Vengefulness + 1.5f);
                            }
                            
                            // TERRITORY CONQUEST AND DESTRUCTION
                            ProcessTerritoryConquest(civilizations[winnerIndex], civilizations[loserIndex], winner, loser, historySystem);
                            
                            // ENHANCED: Higher chance of complete destruction
                            bool totalDestruction = loser.Population <= 1500f || populationLoss > 0.7f || loser.Stability < 0.1f;
                            
                            if (totalDestruction)
                            {
                                // DRAMATIC CIVILIZATION DESTRUCTION
                                string destructionType = populationLoss > 0.8f ? "utterly annihilated" : 
                                                       loser.Stability < 0.1f ? "collapsed into chaos" : 
                                                       "been completely destroyed";
                                
                                var extinctionEvent = new HistoricalEventRecord
                                {
                                    Title = new FixedString128Bytes($"FALL OF {GetSafeCivilizationName(loser.Name, 30)}"),
                                    Description = new FixedString512Bytes($"The great civilization of {GetSafeCivilizationName(loser.Name)} has {destructionType} by {GetSafeCivilizationName(winner.Name)}! Their cities burn, their people scattered to the winds. An entire way of life has been erased from history."),
                                    Year = (int)SystemAPI.Time.ElapsedTime,
                                    Type = ProceduralWorld.Simulation.Core.EventType.Military,
                                    Category = EventCategory.Collapse,
                                    Location = loser.Position,
                                    Significance = 4.5f, // Much higher significance
                                    SourceEntityId = Entity.Null,
                                    Size = 2.0f,
                                    CivilizationId = civilizations[loserIndex]
                                };
                                historySystem.AddEvent(extinctionEvent);
                                
                                // Complete destruction
                                loser.Population = 0f;
                                loser.IsActive = false;
                                
                                // Winner gains massive benefits from total victory
                                winner.Wealth += loser.Wealth; // Take everything
                                winner.Pride = math.min(10f, winner.Pride + 3f); // Massive pride boost
                                winner.Aggressiveness = math.min(10f, winner.Aggressiveness + 1f); // Taste for conquest
                            }
                            else
                            {
                                // Regular war event
                                string safeNameA = GetSafeCivilizationName(civA.Name, 30);
                                string safeNameB = GetSafeCivilizationName(civB.Name, 30);
                                string safeWinnerName = GetSafeCivilizationName(winner.Name, 40);
                                
                                string warTitle = $"War: {safeNameA} vs {safeNameB}";
                                string warDesc = $"A brutal war erupted between {safeNameA} and {safeNameB}. {safeWinnerName} emerged victorious, claiming territory and resources.";
                                
                                var warEvent = new HistoricalEventRecord
                                {
                                    Title = new FixedString128Bytes(warTitle),
                                    Description = new FixedString512Bytes(warDesc),
                                    Year = (int)SystemAPI.Time.ElapsedTime,
                                    Type = ProceduralWorld.Simulation.Core.EventType.Military,
                                    Category = EventCategory.Conflict,
                                    Location = (civA.Position + civB.Position) * 0.5f,
                                    Significance = 2.0f,
                                    SourceEntityId = Entity.Null,
                                    Size = 1.0f,
                                    CivilizationId = civilizations[winnerIndex]
                                };
                                historySystem.AddEvent(warEvent);
                            }
                            
                            civDataList[winnerIndex] = winner;
                            civDataList[loserIndex] = loser;
                        }
                    }
                }
            }
        }

        private void ProcessDiplomacyAndAlliances(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Similar civilizations form alliances
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civA = civDataList[i];
                for (int j = i + 1; j < civDataList.Length; j++)
                {
                    var civB = civDataList[j];
                    float distance = math.distance(civA.Position, civB.Position);
                    
                    if (distance < 400f && civA.Stability > 0.5f && civB.Stability > 0.5f) // Increased range, lower requirements
                    {
                        // Alliance chance based on similar tech levels and mutual benefit
                        float techSimilarity = 1f - math.abs(civA.Technology - civB.Technology) / 10f;
                        float allianceChance = techSimilarity * 0.5f * deltaTime; // 50x more likely!
                        
                        if (_random.NextFloat() < allianceChance)
                        {
                            // Form alliance - mutual benefits
                            civA.Diplomacy = math.min(10f, civA.Diplomacy + 0.2f);
                            civB.Diplomacy = math.min(10f, civB.Diplomacy + 0.2f);
                            civA.Stability = math.min(1f, civA.Stability + 0.1f);
                            civB.Stability = math.min(1f, civB.Stability + 0.1f);
                            
                            // Create safe names for the alliance event
                            string nameA = GetSafeCivilizationName(civA.Name);
                            string nameB = GetSafeCivilizationName(civB.Name);
                            
                            string allianceTitle = $"Alliance: {nameA} & {nameB}";
                            string allianceDesc = $"{nameA} and {nameB} have formed a strategic alliance, promising mutual aid and cooperation.";
                            
                            var allianceEvent = new HistoricalEventRecord
                            {
                                Title = new FixedString128Bytes(allianceTitle),
                                Description = new FixedString512Bytes(allianceDesc),
                                Year = (int)SystemAPI.Time.ElapsedTime,
                                Type = ProceduralWorld.Simulation.Core.EventType.Political,
                                Category = EventCategory.Diplomacy,
                                    Location = (civA.Position + civB.Position) * 0.5f,
                                    Significance = 1.5f,
                                    SourceEntityId = Entity.Null,
                                    Size = 1.0f,
                                    CivilizationId = civilizations[i]
                                };
                            historySystem.AddEvent(allianceEvent);
                            
                            civDataList[i] = civA;
                            civDataList[j] = civB;
                        }
                    }
                }
            }
        }

        private void ProcessReligionDynamics(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Religion spreads and influences civilizations
            var religions = _religionQuery.ToComponentDataArray<ReligionData>(Allocator.Temp);
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Find nearby religions
                for (int r = 0; r < religions.Length; r++)
                {
                    var religion = religions[r];
                    float distance = math.distance(civ.Position, religion.Position);
                    
                    if (distance < 150f) // Increased range
                    {
                        // Religion influence spreads
                        float influenceSpread = religion.Influence * 0.1f * deltaTime; // 10x faster spread
                        civ.Religion = math.min(10f, civ.Religion + influenceSpread);
                        
                        // Religious conversion events
                        if (civ.Religion > 3f && _random.NextFloat() < 0.2f * deltaTime) // Lower threshold, 4x more likely
                        {
                            var conversionEvent = new HistoricalEventRecord
                            {
                                Title = $"{civ.Name} Embraces {religion.Name}",
                                Description = $"The people of {civ.Name} have largely converted to {religion.Name}, changing their cultural practices.",
                    Year = (int)SystemAPI.Time.ElapsedTime,
                                Type = ProceduralWorld.Simulation.Core.EventType.Religious,
                                Category = EventCategory.Cultural,
                                Location = civ.Position,
                                Significance = 1.3f,
                    SourceEntityId = Entity.Null,
                    Size = 1.0f,
                                CivilizationId = civilizations[i]
                            };
                            historySystem.AddEvent(conversionEvent);
                        }
                    }
                }
                
                civDataList[i] = civ;
            }
            
            religions.Dispose();
        }

        private void ProcessCityExpansion(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // GLOBAL LIMIT: Maximum civilizations/settlements allowed on the map
            const int MAX_CIVILIZATIONS = 15; // Hard limit to prevent map overcrowding
            
            if (civilizations.Length >= MAX_CIVILIZATIONS)
            {
                // Map is full - trigger aggressive expansion through conquest instead
                ProcessAggressiveExpansion(civilizations, civDataList, deltaTime, historySystem);
                return;
            }
            
            // Large, prosperous civilizations found new cities
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Additional check: only allow expansion if we're well below the limit
                if (civilizations.Length >= MAX_CIVILIZATIONS - 2)
                {
                    break; // Stop expansion when approaching limit
                }
                
                if (civ.Population > 12000f && civ.Wealth > 8000f && civ.Stability > 0.85f) // Even higher requirements
                {
                    float expansionChance = (civ.Population / 30000f) * 0.005f * deltaTime; // Even less likely - only for truly massive civs
                    
                    if (_random.NextFloat() < expansionChance)
                    {
                        // Found new settlement
                        var ecb = _ecbSystem.CreateCommandBuffer();
                        var newCityPos = civ.Position + new float3(
                            _random.NextFloat(-100f, 100f),
                            0,
                            _random.NextFloat(-100f, 100f)
                        );
                        
                        var newCiv = civ;
                        // Prevent recursive colony naming
                        string baseName = GetSafeCivilizationName(civ.Name, 100);
                        newCiv.Name = new FixedString128Bytes($"{baseName} Colony");
                        newCiv.Position = newCityPos;
                        newCiv.Population = civ.Population * 0.2f; // 20% of parent population
                        newCiv.Wealth = civ.Wealth * 0.3f;
                        
                        // Reduce parent civilization
                        civ.Population *= 0.8f;
                        civ.Wealth *= 0.7f;
                        
                        var newEntity = ecb.CreateEntity();
                        ecb.AddComponent(newEntity, newCiv);
                        ecb.AddComponent(newEntity, LocalTransform.FromPosition(newCityPos));
                        
                        // Create territory entity for the new city
                        var territoryEntity = ecb.CreateEntity();
                        
                        // Use the same baseName for territory naming
                        string colonyName = $"{baseName} Colony";
                        
                        ecb.AddComponent(territoryEntity, new TerritoryData
                        {
                            OwnerCivilization = newEntity,
                            Position = newCityPos,
                            ControlRadius = 50f,
                            TerritoryName = new FixedString128Bytes(colonyName),
                            Type = TerritoryType.City,
                            DefenseStrength = newCiv.Military * 0.5f,
                            Population = newCiv.Population,
                            Wealth = newCiv.Wealth,
                            IsRuined = false
                        });
                        
                        var expansionEvent = new HistoricalEventRecord
                        {
                            Title = $"{civ.Name} Founds New Settlement",
                            Description = $"Driven by prosperity and population growth, {civ.Name} has established a new colony to expand their influence.",
                            Year = (int)SystemAPI.Time.ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.Social,
                            Category = EventCategory.Expansion,
                            Location = newCityPos,
                            Significance = 1.8f,
                            SourceEntityId = Entity.Null,
                            Size = 1.0f,
                            CivilizationId = civilizations[i]
                        };
                        historySystem.AddEvent(expansionEvent);
                        
                        civDataList[i] = civ;
                    }
                }
            }
        }

        private void ProcessTechnologicalAdvancement(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Major technological breakthroughs
                if (civ.Technology > 3f && !civ.HasReachedTechnologyMilestone)
                {
                    civ.HasReachedTechnologyMilestone = true;
                    civ.Production = math.min(10f, civ.Production + 2f);
                    civ.Military = math.min(10f, civ.Military + 1f);
                    
                    var techEvent = new HistoricalEventRecord
                    {
                        Title = $"{civ.Name} Technological Revolution",
                        Description = $"{civ.Name} has achieved major technological breakthroughs, revolutionizing their production and military capabilities.",
                        Year = (int)SystemAPI.Time.ElapsedTime,
                        Type = ProceduralWorld.Simulation.Core.EventType.Technological,
                        Category = EventCategory.Innovation,
                        Location = civ.Position,
                        Significance = 2.5f,
                        SourceEntityId = Entity.Null,
                        Size = 1.0f,
                        CivilizationId = civilizations[i]
                    };
                    historySystem.AddEvent(techEvent);
                    
                    civDataList[i] = civ;
                }
            }
        }

        private void ProcessMonumentsAndWonders(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Large, wealthy, stable civilizations build monuments
                if (civ.Population > 4000f && civ.Wealth > 3000f && civ.Stability > 0.7f && civ.Technology > 2.5f)
                {
                    float monumentChance = (civ.Population / 8000f + civ.Wealth / 10000f + civ.Technology / 10f) * 0.3f * deltaTime;
                    
                    if (_random.NextFloat() < monumentChance)
                    {
                        // Determine monument type based on civilization characteristics
                        string monumentType = "";
                        string monumentName = "";
                        
                        if (civ.Religion > 5f)
                        {
                            monumentType = "Great Temple";
                            monumentName = $"Temple of {civ.Name}";
                        }
                        else if (civ.Military > 6f)
                        {
                            monumentType = "Victory Monument";
                            monumentName = $"Monument of {civ.Name} Triumph";
                        }
                        else if (civ.Technology > 6f)
                        {
                            monumentType = "Academy of Sciences";
                            monumentName = $"Great Academy of {civ.Name}";
                        }
                        else if (civ.Trade > 6f)
                        {
                            monumentType = "Grand Marketplace";
                            monumentName = $"Great Market of {civ.Name}";
                        }
                        else
                        {
                            monumentType = "Palace Complex";
                            monumentName = $"Royal Palace of {civ.Name}";
                        }
                        
                        // Monument costs resources but provides benefits
                        civ.Wealth *= 0.8f; // Costs 20% of wealth
                        civ.Culture = math.min(10f, civ.Culture + 1f);
                        civ.Stability = math.min(1f, civ.Stability + 0.1f);
                        civ.Prestige = math.min(10f, civ.Prestige + 2f);
                        
                        var monumentEvent = new HistoricalEventRecord
                        {
                            Title = $"{civ.Name} Builds {monumentType}",
                            Description = $"The great civilization of {civ.Name} has completed construction of the magnificent {monumentName}, a testament to their power and culture.",
                            Year = (int)SystemAPI.Time.ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                            Category = EventCategory.Innovation,
                            Location = civ.Position,
                            Significance = 2.5f,
                            SourceEntityId = Entity.Null,
                            Size = 1.0f,
                            CivilizationId = civilizations[i]
                        };
                        historySystem.AddEvent(monumentEvent);
                        
                        // Create territory entity for the monument
                        var ecb = _ecbSystem.CreateCommandBuffer();
                        var monumentEntity = ecb.CreateEntity();
                        var monumentType_enum = monumentType == "Great Temple" ? TerritoryType.Temple :
                                              monumentType == "Victory Monument" ? TerritoryType.Monument :
                                              monumentType == "Academy of Sciences" ? TerritoryType.Academy :
                                              monumentType == "Grand Marketplace" ? TerritoryType.Marketplace :
                                              TerritoryType.Palace;
                        
                        ecb.AddComponent(monumentEntity, new TerritoryData
                        {
                            OwnerCivilization = civilizations[i],
                            Position = civ.Position + new float3(_random.NextFloat(-20f, 20f), 0, _random.NextFloat(-20f, 20f)),
                            ControlRadius = 30f,
                            TerritoryName = new FixedString128Bytes(monumentName),
                            Type = monumentType_enum,
                            DefenseStrength = civ.Military * 0.3f,
                            Population = 0f,
                            Wealth = civ.Wealth * 0.1f,
                            IsRuined = false
                        });
                        
                        civDataList[i] = civ;
                    }
                }
                
                // Wonders for truly great civilizations
                if (civ.Population > 15000f && civ.Wealth > 8000f && civ.Stability > 0.8f && civ.Technology > 5f)
                {
                    float wonderChance = (civ.Population / 20000f) * 0.1f * deltaTime;
                    
                    if (_random.NextFloat() < wonderChance)
                    {
                        string[] wonders = {
                            "Hanging Gardens", "Great Library", "Colossus", "Lighthouse", 
                            "Great Wall", "Pyramids", "Oracle", "Mausoleum"
                        };
                        
                        string wonder = wonders[_random.NextInt(0, wonders.Length)];
                        
                        // Wonder costs are massive but provide huge benefits
                        civ.Wealth *= 0.5f; // Costs 50% of wealth
                        civ.Culture = math.min(10f, civ.Culture + 3f);
                        civ.Stability = math.min(1f, civ.Stability + 0.2f);
                        civ.Prestige = math.min(10f, civ.Prestige + 5f);
                        civ.Technology = math.min(10f, civ.Technology + 1f);
                        
                        var wonderEvent = new HistoricalEventRecord
                        {
                            Title = $"{civ.Name} Completes the {wonder}",
                            Description = $"After years of monumental effort, {civ.Name} has completed the legendary {wonder}, one of the great wonders of the world!",
                            Year = (int)SystemAPI.Time.ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                            Category = EventCategory.Innovation,
                            Location = civ.Position,
                            Significance = 4.0f,
                            SourceEntityId = Entity.Null,
                            Size = 1.0f,
                            CivilizationId = civilizations[i]
                        };
                        historySystem.AddEvent(wonderEvent);
                        
                        // Create territory entity for the wonder
                        var ecb = _ecbSystem.CreateCommandBuffer();
                        var wonderEntity = ecb.CreateEntity();
                        
                        ecb.AddComponent(wonderEntity, new TerritoryData
                        {
                            OwnerCivilization = civilizations[i],
                            Position = civ.Position + new float3(_random.NextFloat(-30f, 30f), 0, _random.NextFloat(-30f, 30f)),
                            ControlRadius = 50f,
                            TerritoryName = new FixedString128Bytes(wonder),
                            Type = TerritoryType.Wonder,
                            DefenseStrength = civ.Military * 0.5f,
                            Population = 0f,
                            Wealth = civ.Wealth * 0.2f,
                            IsRuined = false
                        });
                        
                        civDataList[i] = civ;
                    }
                }
            }
        }

        private void ProcessTerritoryConquest(Entity winnerEntity, Entity loserEntity, CivilizationData winner, CivilizationData loser, WorldHistorySystem historySystem)
        {
            var territories = _territoryQuery.ToComponentDataArray<TerritoryData>(Allocator.Temp);
            var territoryEntities = _territoryQuery.ToEntityArray(Allocator.Temp);
            var ecb = _ecbSystem.CreateCommandBuffer();
            
            for (int t = 0; t < territories.Length; t++)
            {
                var territory = territories[t];
                
                // Skip if not owned by the loser
                if (territory.OwnerCivilization != loserEntity || territory.IsRuined)
                    continue;
                
                float distance = math.distance(territory.Position, loser.Position);
                
                // Territories close to the loser's capital are more likely to be affected
                if (distance < 100f)
                {
                    float conquestChance = 0.6f; // 60% chance to be conquered/destroyed
                    
                    if (_random.NextFloat() < conquestChance)
                    {
                        // Decide: Conquer or Destroy?
                        bool shouldDestroy = false;
                        
                        // Monuments dedicated to enemy heroes/culture are more likely to be destroyed
                        if (territory.Type == TerritoryType.Monument || territory.Type == TerritoryType.Temple)
                        {
                            shouldDestroy = _random.NextFloat() < 0.7f; // 70% chance to destroy monuments/temples
                        }
                        else if (territory.Type == TerritoryType.Wonder)
                        {
                            shouldDestroy = _random.NextFloat() < 0.3f; // 30% chance to destroy wonders (too valuable)
                        }
                        else if (territory.Type == TerritoryType.City)
                        {
                            shouldDestroy = _random.NextFloat() < 0.2f; // 20% chance to destroy cities (usually conquered)
                        }
                        else
                        {
                            shouldDestroy = _random.NextFloat() < 0.4f; // 40% chance for other structures
                        }
                        
                        if (shouldDestroy)
                        {
                            // DESTROY - Turn into ruins
                            var ruinedTerritory = territory;
                            ruinedTerritory.IsRuined = true;
                            ruinedTerritory.OriginalName = territory.TerritoryName;
                            ruinedTerritory.OriginalOwner = territory.OwnerCivilization;
                            ruinedTerritory.OwnerCivilization = Entity.Null;
                            ruinedTerritory.Type = TerritoryType.Ruins;
                            ruinedTerritory.Population = 0f;
                            ruinedTerritory.Wealth = 0f;
                            ruinedTerritory.DefenseStrength = 0f;
                            
                            // Generate ruin name
                            string ruinType = territory.Type == TerritoryType.City ? "Ruins" :
                                            territory.Type == TerritoryType.Temple ? "Ruined Temple" :
                                            territory.Type == TerritoryType.Monument ? "Fallen Monument" :
                                            territory.Type == TerritoryType.Wonder ? "Lost Wonder" :
                                            territory.Type == TerritoryType.Academy ? "Abandoned Academy" :
                                            territory.Type == TerritoryType.Marketplace ? "Desolate Market" :
                                            "Ancient Ruins";
                            
                            string safeName = GetSafeTerritoryName(territory.OriginalName, 100);
                            ruinedTerritory.TerritoryName = new FixedString128Bytes($"{ruinType} of {safeName}");
                            
                            SystemAPI.SetComponent(territoryEntities[t], ruinedTerritory);
                            
                            // Create destruction event
                            string safeWinnerName = GetSafeCivilizationName(winner.Name, 30);
                            string safeLoserName = GetSafeCivilizationName(loser.Name, 30);
                            string safeTerritoryName = GetSafeTerritoryName(territory.TerritoryName, 40);
                            
                            var destructionEvent = new HistoricalEventRecord
                            {
                                Title = new FixedString128Bytes($"{safeWinnerName} Destroys {safeTerritoryName}"),
                                Description = new FixedString512Bytes($"In their conquest of {safeLoserName}, {safeWinnerName} has razed the {safeTerritoryName} to the ground. It now stands as a monument to the brutality of war."),
                                Year = (int)SystemAPI.Time.ElapsedTime,
                                Type = ProceduralWorld.Simulation.Core.EventType.Military,
                                Category = EventCategory.Conflict,
                                Location = territory.Position,
                                Significance = territory.Type == TerritoryType.Wonder ? 3.5f : 2.0f,
                                SourceEntityId = Entity.Null,
                                Size = 1.0f,
                                CivilizationId = winnerEntity
                            };
                            historySystem.AddEvent(destructionEvent);
                        }
                        else
                        {
                            // CONQUER - Change ownership
                            var conqueredTerritory = territory;
                            conqueredTerritory.OwnerCivilization = winnerEntity;
                            conqueredTerritory.DefenseStrength = winner.Military * 0.3f;
                            
                            // Rename conquered cities
                            if (territory.Type == TerritoryType.City)
                            {
                                string safeWinnerName = GetSafeCivilizationName(winner.Name, 50);
                                string safeTerritoryName = GetSafeTerritoryName(territory.TerritoryName, 60);
                                conqueredTerritory.TerritoryName = new FixedString128Bytes($"{safeWinnerName} {safeTerritoryName}");
                            }
                            
                            SystemAPI.SetComponent(territoryEntities[t], conqueredTerritory);
                            
                            // Create conquest event
                            string safeWinnerName2 = GetSafeCivilizationName(winner.Name, 30);
                            string safeLoserName2 = GetSafeCivilizationName(loser.Name, 30);
                            string safeTerritoryName2 = GetSafeTerritoryName(territory.TerritoryName, 40);
                            
                            var conquestEvent = new HistoricalEventRecord
                            {
                                Title = new FixedString128Bytes($"{safeWinnerName2} Conquers {safeTerritoryName2}"),
                                Description = new FixedString512Bytes($"{safeWinnerName2} has successfully conquered {safeTerritoryName2} from {safeLoserName2}, expanding their territorial control."),
                                Year = (int)SystemAPI.Time.ElapsedTime,
                                Type = ProceduralWorld.Simulation.Core.EventType.Military,
                                Category = EventCategory.Conflict,
                                Location = territory.Position,
                                Significance = territory.Type == TerritoryType.Wonder ? 3.0f : 1.5f,
                                SourceEntityId = Entity.Null,
                                Size = 1.0f,
                                CivilizationId = winnerEntity
                            };
                            historySystem.AddEvent(conquestEvent);
                        }
                    }
                }
            }
            
            territories.Dispose();
            territoryEntities.Dispose();
        }

        private string GetSafeCivilizationName(FixedString64Bytes civName, int maxLength = 40)
        {
            return GetSafeStringName(civName.ToString(), maxLength);
        }
        
        private string GetSafeTerritoryName(FixedString128Bytes territoryName, int maxLength = 40)
        {
            return GetSafeStringName(territoryName.ToString(), maxLength);
        }
        
        // Overload to handle when we accidentally pass the wrong type
        private string GetSafeCivilizationName(FixedString128Bytes name, int maxLength = 40)
        {
            return GetSafeStringName(name.ToString(), maxLength);
        }
        
        private void ProcessCatastrophicCollapse(Entity civilizationEntity, CivilizationData civ, WorldHistorySystem historySystem)
        {
            // Create dramatic collapse event
            string[] collapseReasons = {
                "catastrophic overpopulation and resource depletion",
                "complete societal breakdown due to unsustainable growth",
                "massive famine and civil unrest",
                "total economic collapse under the weight of an oversized population",
                "environmental devastation and resource wars",
                "administrative collapse of an unmanageably large civilization"
            };
            
            string reason = collapseReasons[_random.NextInt(0, collapseReasons.Length)];
            string safeName = GetSafeCivilizationName(civ.Name, 50);
            
            var collapseEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"The Great Collapse of {safeName}"),
                Description = new FixedString512Bytes($"The mighty civilization of {safeName} has suffered a catastrophic collapse due to {reason}. Their once-great cities lie in ruins, their population scattered and decimated."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Social,
                Category = EventCategory.Collapse,
                Location = civ.Position,
                Significance = 4.5f, // Very high significance
                SourceEntityId = Entity.Null,
                Size = 1.0f,
                CivilizationId = civilizationEntity
            };
            historySystem.AddEvent(collapseEvent);
        }

        private float CalculateCarryingCapacity(CivilizationData civ)
        {
            // Base carrying capacity varies significantly by civilization type
            float baseCapacity = GetCivilizationTypeCapacity(civ);
            
            float technologyMultiplier = 1f + (civ.Technology * 0.8f); // Technology is more impactful
            float resourceMultiplier = math.min(3f, civ.Resources / 1500f); // Resources more important
            float stabilityMultiplier = 0.3f + (civ.Stability * 0.7f); // Stability more critical
            
            // Environmental bonuses/penalties based on position
            float environmentalMultiplier = GetEnvironmentalMultiplier(civ);
            
            // Religious/cultural bonuses
            float culturalMultiplier = GetCulturalMultiplier(civ);
            
            float carryingCapacity = baseCapacity * technologyMultiplier * resourceMultiplier * 
                                   stabilityMultiplier * environmentalMultiplier * culturalMultiplier;
            
            // More varied hard caps based on civilization type
            float maxCapacity = GetMaxCapacityForType(civ);
            return math.min(carryingCapacity, maxCapacity);
        }
        
        private float GetCivilizationTypeCapacity(CivilizationData civ)
        {
            // Different base capacities based on civilization characteristics
            if (civ.Trade > 6f) return 8000f; // Trading civilizations support more people
            if (civ.Military > 6f) return 6000f; // Military civilizations are efficient but harsh
            if (civ.Religion > 6f) return 7000f; // Religious civilizations have social cohesion
            if (civ.Technology > 6f) return 9000f; // Tech civilizations are most efficient
            if (civ.Culture > 6f) return 7500f; // Cultural civilizations have good organization
            return 5000f; // Generic civilizations
        }
        
        private float GetEnvironmentalMultiplier(CivilizationData civ)
        {
            // Simulate different biomes/environments based on position
            float x = civ.Position.x;
            float z = civ.Position.z;
            
            // Create environmental zones
            float distanceFromCenter = math.sqrt(x * x + z * z);
            
            if (distanceFromCenter < 50f) return 1.5f; // Fertile center regions
            if (distanceFromCenter < 100f) return 1.2f; // Good regions
            if (distanceFromCenter < 150f) return 1.0f; // Average regions
            if (distanceFromCenter < 200f) return 0.8f; // Harsh regions
            return 0.6f; // Extreme/desert regions
        }
        
        private float GetCulturalMultiplier(CivilizationData civ)
        {
            // Religious civilizations get bonuses based on their faith
            if (civ.Religion > 7f) return 1.3f; // Strong faith provides social cohesion
            if (civ.Religion > 4f) return 1.1f; // Moderate faith helps
            
            // Cultural civilizations get organization bonuses
            if (civ.Culture > 7f) return 1.2f; // High culture = better organization
            if (civ.Culture > 4f) return 1.05f; // Some culture helps
            
            return 1.0f; // No bonus
        }
        
        private float GetCivilizationGrowthRate(CivilizationData civ)
        {
            // Base growth rate depends on population size and environmental factors
            float baseRate = 0f;
            
            if (civ.Population < 1000f)
            {
                baseRate = 0.12f; // Small populations grow fast
            }
            else if (civ.Population < 3000f)
            {
                baseRate = 0.08f; // Medium populations grow moderately
            }
            else if (civ.Population < 8000f)
            {
                baseRate = 0.05f; // Large populations grow slowly
            }
            else if (civ.Population < 20000f)
            {
                baseRate = 0.02f; // Very large populations barely grow
            }
            else
            {
                baseRate = -0.01f; // Massive populations start declining
            }
            
            // Environmental modifier
            float environmentalBonus = (GetEnvironmentalMultiplier(civ) - 1f) * 0.1f;
            
            return baseRate + environmentalBonus;
        }

        private float GetMaxCapacityForType(CivilizationData civ)
        {
            // Different maximum populations based on specialization
            if (civ.Technology > 7f) return 40000f; // Tech civilizations can grow largest
            if (civ.Trade > 7f) return 35000f; // Trading empires can be very large
            if (civ.Religion > 7f) return 30000f; // Religious empires have good cohesion
            if (civ.Culture > 7f) return 32000f; // Cultural empires are well-organized
            if (civ.Military > 7f) return 25000f; // Military empires are efficient but harsh
            return 20000f; // Generic civilizations have lower caps
        }

        private string GetSafeStringName(string name, int maxLength = 40)
        {
            // Remove duplicate "Colony" suffixes
            while (name.Contains(" Colony Colony"))
            {
                name = name.Replace(" Colony Colony", " Colony");
            }
            
            // If still too long, truncate
            if (name.Length > maxLength)
            {
                name = name.Substring(0, maxLength).Trim();
            }
            
            return name;
        }

        private void ProcessAggressiveExpansion(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // When the map is full, large civilizations become aggressive and target smaller ones for conquest
            for (int i = 0; i < civDataList.Length; i++)
            {
                var aggressor = civDataList[i];
                
                // Only large, wealthy civilizations with expansion desires become aggressive
                if (aggressor.Population > 10000f && aggressor.Wealth > 6000f && aggressor.Stability > 0.7f)
                {
                    // Find smaller, weaker civilizations to target
                    for (int j = 0; j < civDataList.Length; j++)
                    {
                        if (i == j) continue;
                        
                        var target = civDataList[j];
                        float distance = math.distance(aggressor.Position, target.Position);
                        
                        // Target smaller civilizations within range
                        if (distance < 200f && target.Population < aggressor.Population * 0.6f) // Target must be significantly smaller
                        {
                            // Calculate aggression chance based on size difference and expansion pressure
                            float sizeDifference = aggressor.Population / math.max(target.Population, 1000f);
                            float expansionPressure = (aggressor.Population / 15000f) * (aggressor.Wealth / 8000f);
                            float aggressionChance = sizeDifference * expansionPressure * 0.1f * deltaTime;
                            
                            if (_random.NextFloat() < aggressionChance)
                            {
                                // Launch aggressive expansion war
                                ProcessExpansionWar(civilizations[i], civilizations[j], aggressor, target, historySystem);
                                
                                // Update the data
                                civDataList[i] = aggressor;
                                civDataList[j] = target;
                                
                                // Only one aggressive action per civilization per update
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessExpansionWar(Entity aggressorEntity, Entity targetEntity, CivilizationData aggressor, CivilizationData target, WorldHistorySystem historySystem)
        {
            // Calculate military strength including expansion motivation
            float aggressorStrength = aggressor.Military * aggressor.Technology * (aggressor.Population / 1000f) * 1.5f; // Aggressor bonus
            float targetStrength = target.Military * target.Technology * (target.Population / 1000f);
            
            // Aggressor usually wins due to preparation and motivation
            bool aggressorWins = aggressorStrength > targetStrength;
            
            if (aggressorWins)
            {
                // CONQUEST SUCCESS - Absorb the target civilization
                float populationGain = target.Population * 0.7f; // Absorb most population
                float wealthGain = target.Wealth * 0.8f; // Take most wealth
                
                aggressor.Population += populationGain;
                aggressor.Wealth += wealthGain;
                aggressor.Military = math.min(10f, aggressor.Military + 0.8f); // Military experience
                aggressor.Prestige = math.min(10f, aggressor.Prestige + 2f); // Conquest prestige
                
                // Target civilization is completely absorbed/destroyed
                target.Population = 0f;
                target.IsActive = false;
                
                // Transfer all territories from target to aggressor
                ProcessTerritoryConquest(aggressorEntity, targetEntity, aggressor, target, historySystem);
                
                // Create expansion conquest event
                string safeAggressorName = GetSafeCivilizationName(aggressor.Name, 40);
                string safeTargetName = GetSafeCivilizationName(target.Name, 40);
                
                var conquestEvent = new HistoricalEventRecord
                {
                    Title = new FixedString128Bytes($"{safeAggressorName} Conquers {safeTargetName}"),
                    Description = new FixedString512Bytes($"Driven by the need for expansion in an overcrowded world, {safeAggressorName} has completely conquered and absorbed {safeTargetName}, claiming all their territories and population."),
                    Year = (int)SystemAPI.Time.ElapsedTime,
                    Type = ProceduralWorld.Simulation.Core.EventType.Military,
                    Category = EventCategory.Conflict,
                    Location = target.Position,
                    Significance = 3.5f, // High significance for complete conquest
                    SourceEntityId = Entity.Null,
                    Size = 1.0f,
                    CivilizationId = aggressorEntity
                };
                historySystem.AddEvent(conquestEvent);
            }
            else
            {
                // CONQUEST FAILED - Target successfully defends
                float aggressorLoss = aggressor.Population * 0.15f; // Heavy losses for failed aggression
                float targetLoss = target.Population * 0.1f; // Lighter losses for defender
                
                aggressor.Population -= aggressorLoss;
                aggressor.Wealth *= 0.8f; // War costs
                aggressor.Stability = math.max(0.2f, aggressor.Stability - 0.2f); // Instability from failure
                
                target.Population -= targetLoss;
                target.Military = math.min(10f, target.Military + 0.5f); // Defensive experience
                target.Stability = math.min(1f, target.Stability + 0.1f); // Unity from successful defense
                
                // Create failed expansion event
                string safeAggressorName = GetSafeCivilizationName(aggressor.Name, 40);
                string safeTargetName = GetSafeCivilizationName(target.Name, 40);
                
                var defenseEvent = new HistoricalEventRecord
                {
                    Title = new FixedString128Bytes($"{safeTargetName} Repels {safeAggressorName}"),
                    Description = new FixedString512Bytes($"{safeTargetName} has successfully defended against an aggressive expansion attempt by {safeAggressorName}, proving that even smaller civilizations can resist conquest through determination and unity."),
                    Year = (int)SystemAPI.Time.ElapsedTime,
                    Type = ProceduralWorld.Simulation.Core.EventType.Military,
                    Category = EventCategory.Conflict,
                    Location = target.Position,
                    Significance = 2.5f,
                    SourceEntityId = Entity.Null,
                    Size = 1.0f,
                    CivilizationId = targetEntity
                };
                historySystem.AddEvent(defenseEvent);
            }
        }

        private void ProcessCivilizationCollapse(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Remove collapsed civilizations and handle recovery
            var ecb = _ecbSystem.CreateCommandBuffer();
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                if (civ.Population <= 0f || !civ.IsActive)
                {
                    // Remove collapsed civilization
                    ecb.DestroyEntity(civilizations[i]);
                    continue;
                }
                
                // CLEANUP: Remove very small colonies that failed to thrive
                bool isColony = civ.Name.ToString().Contains("Colony");
                if (isColony && civ.Population < 1500f && civ.Wealth < 1000f)
                {
                    // Small colony has failed - remove it to clean up the map
                    var collapseEvent = new HistoricalEventRecord
                    {
                        Title = new FixedString128Bytes($"{GetSafeCivilizationName(civ.Name)} Abandoned"),
                        Description = new FixedString512Bytes($"The struggling colony of {GetSafeCivilizationName(civ.Name)} has been abandoned due to lack of resources and population decline."),
                        Year = (int)SystemAPI.Time.ElapsedTime,
                        Type = ProceduralWorld.Simulation.Core.EventType.Social,
                        Category = EventCategory.Collapse,
                        Location = civ.Position,
                        Significance = 0.8f,
                        SourceEntityId = Entity.Null,
                        Size = 1.0f,
                        CivilizationId = civilizations[i]
                    };
                    historySystem.AddEvent(collapseEvent);
                    
                    ecb.DestroyEntity(civilizations[i]);
                    continue;
                }
                
                // Recovery from low stability
                if (civ.Stability < 0.3f && civ.Population > 1000f)
                {
                    float recoveryChance = 0.01f * deltaTime;
                    if (_random.NextFloat() < recoveryChance)
                    {
                        civ.Stability = math.min(1f, civ.Stability + 0.3f);
                        
                        var recoveryEvent = new HistoricalEventRecord
                        {
                            Title = $"{civ.Name} Political Reform",
                            Description = $"After a period of instability, {civ.Name} has undergone political reforms, restoring order and stability.",
                            Year = (int)SystemAPI.Time.ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.Political,
                            Category = EventCategory.Recovery,
                            Location = civ.Position,
                            Significance = 1.5f,
                            SourceEntityId = Entity.Null,
                            Size = 1.0f,
                            CivilizationId = civilizations[i]
                        };
                        historySystem.AddEvent(recoveryEvent);
                        
                        civDataList[i] = civ;
                    }
                }
            }
        }

        private void ProcessPersonalityUpdates(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Update resource stress level
                float resourcePerPerson = civ.Resources / math.max(1f, civ.Population);
                civ.ResourceStressLevel = math.clamp(1f - (resourcePerPerson / 2f), 0f, 1f);
                
                // ENVIRONMENTAL STRESS affects personality
                float environmentalMultiplier = GetEnvironmentalMultiplier(civ);
                if (environmentalMultiplier < 1f) // Harsh environment
                {
                    civ.Aggressiveness += (1f - environmentalMultiplier) * deltaTime * 0.5f;
                    civ.Desperation += (1f - environmentalMultiplier) * deltaTime * 0.3f;
                    civ.Greed += (1f - environmentalMultiplier) * deltaTime * 0.4f;
                }
                
                // RESOURCE STRESS affects personality
                if (civ.ResourceStressLevel > 0.7f)
                {
                    civ.Desperation += civ.ResourceStressLevel * deltaTime * 0.8f;
                    civ.Aggressiveness += civ.ResourceStressLevel * deltaTime * 0.6f;
                    civ.Greed += civ.ResourceStressLevel * deltaTime * 0.7f;
                    civ.Paranoia += civ.ResourceStressLevel * deltaTime * 0.4f;
                }
                
                // POPULATION PRESSURE affects personality
                float carryingCapacity = CalculateCarryingCapacity(civ);
                float populationPressure = civ.Population / carryingCapacity;
                if (populationPressure > 1.2f)
                {
                    float pressureStress = populationPressure - 1.2f;
                    civ.Aggressiveness += pressureStress * deltaTime * 0.5f;
                    civ.Desperation += pressureStress * deltaTime * 0.6f;
                    civ.Ambition += pressureStress * deltaTime * 0.3f; // Need to expand
                }
                
                // WEALTH DISPARITY affects personality
                float averageWealth = 0f;
                for (int j = 0; j < civDataList.Length; j++)
                {
                    if (j != i) averageWealth += civDataList[j].Wealth;
                }
                averageWealth /= math.max(1f, civDataList.Length - 1);
                
                if (civ.Wealth < averageWealth * 0.5f) // Much poorer than others
                {
                    civ.Greed += deltaTime * 0.4f;
                    civ.Hatred += deltaTime * 0.3f;
                    civ.Desperation += deltaTime * 0.2f;
                }
                else if (civ.Wealth > averageWealth * 2f) // Much richer than others
                {
                    civ.Pride += deltaTime * 0.3f;
                    civ.Ambition += deltaTime * 0.2f;
                }
                
                // STABILITY affects personality
                if (civ.Stability < 0.3f)
                {
                    civ.Desperation += deltaTime * 0.8f;
                    civ.Paranoia += deltaTime * 0.6f;
                    civ.Aggressiveness += deltaTime * 0.4f;
                }
                
                // RECENT ATTACKS increase hatred and paranoia
                float currentYear = (float)SystemAPI.Time.ElapsedTime;
                if (civ.LastAttackedYear > 0 && (currentYear - civ.LastAttackedYear) < 10f)
                {
                    float recentAttackEffect = 1f - ((currentYear - civ.LastAttackedYear) / 10f);
                    civ.Hatred += recentAttackEffect * deltaTime * 0.5f;
                    civ.Paranoia += recentAttackEffect * deltaTime * 0.4f;
                    civ.Vengefulness += recentAttackEffect * deltaTime * 0.6f;
                    civ.Defensiveness += recentAttackEffect * deltaTime * 0.3f;
                }
                
                // SUCCESSFUL WARS increase pride and aggressiveness
                if (civ.SuccessfulWars > civ.LostWars)
                {
                    float warSuccessRatio = (float)civ.SuccessfulWars / math.max(1f, civ.LostWars + civ.SuccessfulWars);
                    civ.Pride += warSuccessRatio * deltaTime * 0.3f;
                    civ.Aggressiveness += warSuccessRatio * deltaTime * 0.2f;
                    civ.Ambition += warSuccessRatio * deltaTime * 0.25f;
                }
                
                // LOST WARS increase defensiveness and paranoia
                if (civ.LostWars > civ.SuccessfulWars)
                {
                    float warLossRatio = (float)civ.LostWars / math.max(1f, civ.LostWars + civ.SuccessfulWars);
                    civ.Defensiveness += warLossRatio * deltaTime * 0.4f;
                    civ.Paranoia += warLossRatio * deltaTime * 0.3f;
                    if (civ.HasBeenHumiliated)
                    {
                        civ.Hatred += warLossRatio * deltaTime * 0.5f;
                        civ.Vengefulness += warLossRatio * deltaTime * 0.6f;
                    }
                }
                
                // NATURAL DECAY - personalities slowly return to baseline over time
                civ.Aggressiveness = math.max(1f, civ.Aggressiveness - deltaTime * 0.1f);
                civ.Desperation = math.max(0f, civ.Desperation - deltaTime * 0.15f);
                civ.Hatred = math.max(0f, civ.Hatred - deltaTime * 0.05f);
                civ.Paranoia = math.max(0.5f, civ.Paranoia - deltaTime * 0.08f);
                
                // Clamp all personality values
                civ.Aggressiveness = math.clamp(civ.Aggressiveness, 0f, 10f);
                civ.Defensiveness = math.clamp(civ.Defensiveness, 0f, 10f);
                civ.Greed = math.clamp(civ.Greed, 0f, 10f);
                civ.Paranoia = math.clamp(civ.Paranoia, 0f, 10f);
                civ.Ambition = math.clamp(civ.Ambition, 0f, 10f);
                civ.Desperation = math.clamp(civ.Desperation, 0f, 10f);
                civ.Hatred = math.clamp(civ.Hatred, 0f, 10f);
                civ.Pride = math.clamp(civ.Pride, 0f, 10f);
                civ.Vengefulness = math.clamp(civ.Vengefulness, 0f, 10f);
                
                civDataList[i] = civ;
            }
        }

        // NEW: RELIGIOUS WARS - Crusades and holy wars
        private void ProcessReligiousWars(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civA = civDataList[i];
                if (civA.Religion < 7f) continue; // Need strong religious conviction
                
                for (int j = i + 1; j < civDataList.Length; j++)
                {
                    var civB = civDataList[j];
                    float distance = math.distance(civA.Position, civB.Position);
                    
                    if (distance < 250f)
                    {
                        // Religious war triggers
                        bool religiousConflict = math.abs(civA.Religion - civB.Religion) > 4f;
                        bool holyWarChance = civA.Religion > 8f && civA.Military > 6f;
                        bool crusadeSpirit = civA.Aggressiveness > 5f && civA.Religion > 7f;
                        
                        float holyWarChance_f = 0f;
                        if (religiousConflict) holyWarChance_f += 0.08f;
                        if (holyWarChance) holyWarChance_f += 0.12f;
                        if (crusadeSpirit) holyWarChance_f += 0.15f;
                        
                        holyWarChance_f *= deltaTime;
                        
                        if (_random.NextFloat() < holyWarChance_f)
                        {
                            ProcessHolyWar(civilizations[i], civilizations[j], civA, civB, historySystem);
                            civDataList[i] = civA;
                            civDataList[j] = civB;
                        }
                    }
                }
            }
        }

        // NEW: BETRAYAL AND TREACHERY - Allies backstabbing each other
        private void ProcessBetrayalAndTreachery(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Betrayal chance based on greed, ambition, and desperation
                float betrayalChance = (civ.Greed / 10f + civ.Ambition / 10f + civ.Desperation / 10f) * 0.001f * deltaTime;
                
                if (_random.NextFloat() < betrayalChance)
                {
                    // Find a potential victim (nearby ally or trade partner)
                    for (int j = 0; j < civDataList.Length; j++)
                    {
                        if (i == j) continue;
                        
                        var target = civDataList[j];
                        float distance = math.distance(civ.Position, target.Position);
                        
                        if (distance < 200f && target.Wealth > civ.Wealth * 1.5f)
                        {
                            ProcessBetrayal(civilizations[i], civilizations[j], civ, target, historySystem);
                            civDataList[i] = civ;
                            civDataList[j] = target;
                            break;
                        }
                    }
                }
            }
        }

        // NEW: CASCADING EVENTS - One event triggers massive consequences
        private void ProcessCascadingEvents(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            // Check for conditions that trigger cascading events
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                
                // Cascading collapse - one civ's fall triggers others
                if (civ.Stability < 0.15f && civ.Population > 8000f)
                {
                    ProcessCascadingCollapse(civilizations, civDataList, i, historySystem);
                }
                
                // Cascading revolution - tech breakthrough spreads
                if (civ.Technology > 9f && _random.NextFloat() < 0.002f * deltaTime)
                {
                    ProcessTechnologicalRevolution(civilizations, civDataList, i, historySystem);
                }
                
                // Cascading war - one war triggers others
                if (civ.TimesAttacked > 0 && civ.LastAttackedYear > SystemAPI.Time.ElapsedTime - 5f)
                {
                    ProcessWarCascade(civilizations, civDataList, i, historySystem);
                }
            }
        }

        // IMPLEMENTATION: Great Plague
        private void ProcessGreatPlague(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            var plagueEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("The Great Plague"),
                Description = new FixedString512Bytes("A devastating plague sweeps across the world, bringing death and chaos to all civilizations. Cities are abandoned, trade routes collapse, and the very fabric of society trembles."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Natural,
                Category = EventCategory.Disaster,
                Location = float3.zero,
                Significance = 5.0f,
                SourceEntityId = Entity.Null,
                Size = 2.0f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(plagueEvent);
            
            // Affect all civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                float populationLoss = _random.NextFloat(0.2f, 0.6f); // 20-60% population loss!
                civ.Population *= (1f - populationLoss);
                civ.Stability = math.max(0.1f, civ.Stability - 0.4f);
                civ.Wealth *= 0.6f; // Economic collapse
                civ.Trade = math.max(0f, civ.Trade - 3f); // Trade routes disrupted
                civDataList[i] = civ;
            }
        }

        // IMPLEMENTATION: Golden Age
        private void ProcessGoldenAge(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            var goldenEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("The Golden Age"),
                Description = new FixedString512Bytes("A golden age of prosperity, art, and learning dawns across the world. Trade flourishes, cities grow magnificent, and knowledge spreads like wildfire."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Golden,
                Location = float3.zero,
                Significance = 4.0f,
                SourceEntityId = Entity.Null,
                Size = 2.0f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(goldenEvent);
            
            // Boost all civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                civ.Wealth *= 1.5f; // Economic boom
                civ.Technology = math.min(10f, civ.Technology + 2f);
                civ.Culture = math.min(10f, civ.Culture + 2f);
                civ.Trade = math.min(10f, civ.Trade + 1.5f);
                civ.Stability = math.min(1f, civ.Stability + 0.3f);
                civDataList[i] = civ;
            }
        }

        // IMPLEMENTATION: Great Disaster
        private void ProcessGreatDisaster(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            string[] disasters = { "Great Earthquake", "Volcanic Eruption", "Massive Flood", "Meteor Strike", "Great Fire" };
            string disasterName = disasters[_random.NextInt(0, disasters.Length)];
            
            var disasterEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes($"The {disasterName}"),
                Description = new FixedString512Bytes($"A catastrophic {disasterName.ToLower()} devastates the world, destroying cities and reshaping the landscape. Civilizations struggle to survive in the aftermath."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Natural,
                Category = EventCategory.Disaster,
                Location = float3.zero,
                Significance = 4.5f,
                SourceEntityId = Entity.Null,
                Size = 2.0f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(disasterEvent);
            
            // Randomly devastate some civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (_random.NextFloat() < 0.4f) // 40% chance to be affected
                {
                    var civ = civDataList[i];
                    civ.Population *= _random.NextFloat(0.3f, 0.8f);
                    civ.Wealth *= _random.NextFloat(0.2f, 0.6f);
                    civ.Stability = math.max(0.1f, civ.Stability - 0.5f);
                    civDataList[i] = civ;
                }
            }
        }

        // IMPLEMENTATION: Tech Revolution
        private void ProcessTechRevolution(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            var techEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("Technological Revolution"),
                Description = new FixedString512Bytes("A breakthrough in knowledge spreads rapidly across civilizations, ushering in a new era of innovation and progress. The world will never be the same."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Technological,
                Category = EventCategory.Discovery,
                Location = float3.zero,
                Significance = 3.5f,
                SourceEntityId = Entity.Null,
                Size = 1.5f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(techEvent);
            
            // Boost technology for all civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                civ.Technology = math.min(10f, civ.Technology + _random.NextFloat(1f, 3f));
                civ.Military = math.min(10f, civ.Military + 1f); // Better weapons
                civ.Production = math.min(10f, civ.Production + 1f); // Better tools
                civDataList[i] = civ;
            }
        }

        // IMPLEMENTATION: Religious Awakening
        private void ProcessReligiousAwakening(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            var religiousEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("Great Religious Awakening"),
                Description = new FixedString512Bytes("A wave of religious fervor sweeps across the world. New prophets arise, old faiths are renewed, and the spiritual landscape is forever changed."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Religious,
                Category = EventCategory.Spiritual,
                Location = float3.zero,
                Significance = 3.0f,
                SourceEntityId = Entity.Null,
                Size = 1.5f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(religiousEvent);
            
            // Boost religion and affect stability
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                civ.Religion = math.min(10f, civ.Religion + _random.NextFloat(1f, 3f));
                civ.Stability = math.min(1f, civ.Stability + 0.2f); // Religious unity
                civ.Culture = math.min(10f, civ.Culture + 1f); // Religious art and culture
                civDataList[i] = civ;
            }
        }

        // IMPLEMENTATION: Dark Age
        private void ProcessDarkAge(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, WorldHistorySystem historySystem)
        {
            var darkEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("The Dark Age"),
                Description = new FixedString512Bytes("Knowledge is lost, trade routes collapse, and civilization itself seems to retreat. A dark age of ignorance and strife descends upon the world."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Social,
                Category = EventCategory.Decline,
                Location = float3.zero,
                Significance = 4.0f,
                SourceEntityId = Entity.Null,
                Size = 2.0f,
                CivilizationId = Entity.Null
            };
            historySystem.AddEvent(darkEvent);
            
            // Reduce technology and culture
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                civ.Technology = math.max(0f, civ.Technology - _random.NextFloat(1f, 3f));
                civ.Culture = math.max(0f, civ.Culture - _random.NextFloat(1f, 2f));
                civ.Trade = math.max(0f, civ.Trade - 2f);
                civ.Stability = math.max(0.1f, civ.Stability - 0.2f);
                civDataList[i] = civ;
            }
        }

        // IMPLEMENTATION: Heroic Leader
        private void ProcessHeroicLeader(Entity civilizationEntity, CivilizationData civ, int leaderType, WorldHistorySystem historySystem)
        {
            // Convert int to HeroicLeaderType enum
            HeroicLeaderType heroType = (HeroicLeaderType)leaderType;
            
            // Create personality traits from civilization data
            var personality = new PersonalityTraits
            {
                Aggressiveness = civ.Aggressiveness,
                Defensiveness = civ.Defensiveness,
                Greed = civ.Greed,
                Paranoia = civ.Paranoia,
                Ambition = civ.Ambition,
                Desperation = civ.Desperation,
                Hatred = civ.Hatred,
                Pride = civ.Pride,
                Vengefulness = civ.Vengefulness
            };
            
            // Generate EPIC hero name based on type and personality
            var epicHeroName = NameGenerator.GenerateEpicHeroName(heroType, personality, civ.SuccessfulWars);
            
            // Generate EPIC event name
            var epicEventName = NameGenerator.GenerateEpicEventName(EventCategory.Hero, 8, new[] { civ.Name.ToString() });
            
            string[] leaderTypeDescriptions = { 
                "legendary conqueror who reshapes the world through conquest", 
                "wise philosopher whose teachings enlighten generations", 
                "master builder who creates wonders that defy imagination", 
                "holy prophet who speaks with the voice of the divine", 
                "brilliant inventor whose creations revolutionize civilization" 
            };
            
            var leaderEvent = new HistoricalEventRecord
            {
                Title = epicEventName,
                Description = new FixedString512Bytes($"{epicHeroName}, a {leaderTypeDescriptions[leaderType]}, has risen in {civ.Name}. Their legend will echo through the ages, forever changing the destiny of their people."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Social,
                Category = EventCategory.Hero,
                Location = civ.Position,
                Significance = 4.0f, // Increased significance for epic heroes
                SourceEntityId = civilizationEntity,
                Size = 2.0f, // Larger size for epic events
                CivilizationId = civilizationEntity
            };
            historySystem.AddEvent(leaderEvent);
            
            // Apply heroic effects based on leader type
            switch (leaderType)
            {
                case 0: // Great Conqueror
                    civ.Military = math.min(10f, civ.Military + 3f);
                    civ.Aggressiveness = math.min(10f, civ.Aggressiveness + 2f);
                    civ.Ambition = math.min(10f, civ.Ambition + 2f);
                    civ.Population *= 1.2f; // Attracts followers
                    break;
                case 1: // Wise Philosopher
                    civ.Culture = math.min(10f, civ.Culture + 3f);
                    civ.Stability = math.min(1f, civ.Stability + 0.3f);
                    civ.Technology = math.min(10f, civ.Technology + 1.5f);
                    break;
                case 2: // Master Builder
                    civ.Production = math.min(10f, civ.Production + 3f);
                    civ.Wealth *= 1.3f;
                    civ.Infrastructure = math.min(10f, civ.Infrastructure + 2f);
                    break;
                case 3: // Holy Prophet
                    civ.Religion = math.min(10f, civ.Religion + 4f);
                    civ.Stability = math.min(1f, civ.Stability + 0.4f);
                    civ.Culture = math.min(10f, civ.Culture + 2f);
                    break;
                case 4: // Brilliant Inventor
                    civ.Technology = math.min(10f, civ.Technology + 4f);
                    civ.Military = math.min(10f, civ.Military + 1.5f);
                    civ.Production = math.min(10f, civ.Production + 2f);
                    break;
            }
        }

        // IMPLEMENTATION: Coalition War
        private void ProcessCoalitionWar(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, int strongestIndex, NativeList<int> coalitionMembers, WorldHistorySystem historySystem)
        {
            var strongest = civDataList[strongestIndex];
            
            // Build coalition names
            string coalitionNames = "";
            for (int i = 0; i < coalitionMembers.Length; i++)
            {
                if (i > 0) coalitionNames += i == coalitionMembers.Length - 1 ? " & " : ", ";
                coalitionNames += GetSafeCivilizationName(civDataList[coalitionMembers[i]].Name, 20);
            }
            
            // Generate EPIC coalition war name
            var epicCoalitionName = NameGenerator.GenerateEpicEventName(EventCategory.Coalition, 9, new[] { strongest.Name.ToString() });
            
            var coalitionEvent = new HistoricalEventRecord
            {
                Title = epicCoalitionName,
                Description = new FixedString512Bytes($"A mighty coalition of {coalitionNames} has united against the powerful {GetSafeCivilizationName(strongest.Name)}. The fate of the world hangs in the balance as armies clash in this epic struggle for dominance!"),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Military,
                Category = EventCategory.Coalition,
                Location = strongest.Position,
                Significance = 5.0f, // Maximum significance for world-shaking events
                SourceEntityId = civilizations[strongestIndex],
                Size = 3.0f, // Massive size for coalition wars
                CivilizationId = civilizations[strongestIndex]
            };
            historySystem.AddEvent(coalitionEvent);
            
            // Calculate total coalition power
            float coalitionPower = 0f;
            for (int i = 0; i < coalitionMembers.Length; i++)
            {
                var member = civDataList[coalitionMembers[i]];
                coalitionPower += member.Population * member.Military * member.Technology;
            }
            
            float strongestPower = strongest.Population * strongest.Military * strongest.Technology;
            
            // Determine outcome
            if (coalitionPower > strongestPower * 1.2f)
            {
                // Coalition wins - strongest civ is devastated
                strongest.Population *= 0.4f; // 60% population loss
                strongest.Wealth *= 0.2f; // 80% wealth loss
                strongest.Stability = 0.1f; // Near collapse
                strongest.Military *= 0.5f; // Military destroyed
                
                // Coalition members gain power
                for (int i = 0; i < coalitionMembers.Length; i++)
                {
                    var member = civDataList[coalitionMembers[i]];
                    member.Wealth += strongest.Wealth * 0.1f; // Share the spoils
                    member.Military = math.min(10f, member.Military + 0.5f);
                    member.Pride = math.min(10f, member.Pride + 1f);
                    civDataList[coalitionMembers[i]] = member;
                }
                
                civDataList[strongestIndex] = strongest;
            }
            else
            {
                // Strongest civ survives but is weakened
                strongest.Population *= 0.8f;
                strongest.Wealth *= 0.7f;
                strongest.Stability = math.max(0.2f, strongest.Stability - 0.3f);
                
                // Coalition members are defeated
                for (int i = 0; i < coalitionMembers.Length; i++)
                {
                    var member = civDataList[coalitionMembers[i]];
                    member.Population *= 0.7f;
                    member.Wealth *= 0.6f;
                    member.Stability = math.max(0.1f, member.Stability - 0.4f);
                    member.Hatred = math.min(10f, member.Hatred + 2f);
                    civDataList[coalitionMembers[i]] = member;
                }
                
                civDataList[strongestIndex] = strongest;
            }
        }

        // IMPLEMENTATION: Holy War
        private void ProcessHolyWar(Entity civAEntity, Entity civBEntity, CivilizationData civA, CivilizationData civB, WorldHistorySystem historySystem)
        {
            // Generate EPIC holy war name
            var epicHolyWarName = NameGenerator.GenerateEpicEventName(EventCategory.HolyWar, 8, new[] { civA.Name.ToString(), civB.Name.ToString() });
            
            var holyWarEvent = new HistoricalEventRecord
            {
                Title = epicHolyWarName,
                Description = new FixedString512Bytes($"A devastating holy war has erupted between {GetSafeCivilizationName(civA.Name)} and {GetSafeCivilizationName(civB.Name)}. Divine wrath and religious fervor drive both sides to apocalyptic violence as the gods themselves seem to wage war through mortal hands."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Religious,
                Category = EventCategory.HolyWar,
                Location = (civA.Position + civB.Position) * 0.5f,
                Significance = 4.5f, // Higher significance for holy wars
                SourceEntityId = civAEntity,
                Size = 2.5f, // Larger size for religious conflicts
                CivilizationId = civAEntity
            };
            historySystem.AddEvent(holyWarEvent);
            
            // Holy wars are more brutal than regular wars
            float civAStrength = civA.Military * civA.Religion * (civA.Population / 1000f);
            float civBStrength = civB.Military * civB.Religion * (civB.Population / 1000f);
            
            var winner = civAStrength > civBStrength ? civA : civB;
            var loser = civAStrength > civBStrength ? civB : civA;
            
            // Extreme consequences for holy wars
            float populationLoss = _random.NextFloat(0.3f, 0.6f); // 30-60% loss
            float wealthTransfer = _random.NextFloat(0.4f, 0.7f); // 40-70% transfer
            
            winner.Population += loser.Population * 0.05f; // Less absorption due to religious differences
            winner.Wealth += loser.Wealth * wealthTransfer;
            winner.Religion = math.min(10f, winner.Religion + 1f); // Religious victory strengthens faith
            winner.Military = math.min(10f, winner.Military + 0.8f);
            
            loser.Population *= (1f - populationLoss);
            loser.Wealth *= (1f - wealthTransfer);
            loser.Stability = math.max(0.1f, loser.Stability - 0.5f);
            loser.Religion = math.max(0f, loser.Religion - 2f); // Faith shaken
            loser.Hatred = math.min(10f, loser.Hatred + 3f); // Extreme hatred from religious persecution
        }

        // IMPLEMENTATION: Betrayal
        private void ProcessBetrayal(Entity betrayerEntity, Entity victimEntity, CivilizationData betrayer, CivilizationData victim, WorldHistorySystem historySystem)
        {
            // Generate EPIC betrayal name
            var epicBetrayalName = NameGenerator.GenerateEpicEventName(EventCategory.Betrayal, 7, new[] { betrayer.Name.ToString(), victim.Name.ToString() });
            
            var betrayalEvent = new HistoricalEventRecord
            {
                Title = epicBetrayalName,
                Description = new FixedString512Bytes($"In a shocking act of treachery that will be remembered for generations, {GetSafeCivilizationName(betrayer.Name)} has betrayed {GetSafeCivilizationName(victim.Name)}, striking like a serpent in the night and seizing their wealth. Trust lies shattered, and vengeance burns in the hearts of the betrayed."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Political,
                Category = EventCategory.Betrayal,
                Location = victim.Position,
                Significance = 3.5f, // Higher significance for dramatic betrayals
                SourceEntityId = betrayerEntity,
                Size = 1.5f, // Larger size for impactful events
                CivilizationId = betrayerEntity
            };
            historySystem.AddEvent(betrayalEvent);
            
            // Betrayal effects
            float stolenWealth = victim.Wealth * _random.NextFloat(0.3f, 0.6f);
            betrayer.Wealth += stolenWealth;
            victim.Wealth -= stolenWealth;
            
            // Personality changes
            betrayer.Greed = math.min(10f, betrayer.Greed + 1f);
            betrayer.Pride = math.min(10f, betrayer.Pride + 0.5f);
            betrayer.TimesBetrayed++; // Track betrayals committed
            
            victim.Paranoia = math.min(10f, victim.Paranoia + 2f);
            victim.Hatred = math.min(10f, victim.Hatred + 2.5f);
            victim.Vengefulness = math.min(10f, victim.Vengefulness + 2f);
            victim.Stability = math.max(0.1f, victim.Stability - 0.3f);
            victim.TimesBetrayed++; // Track betrayals suffered
        }

        // IMPLEMENTATION: Cascading Collapse
        private void ProcessCascadingCollapse(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, int collapsingIndex, WorldHistorySystem historySystem)
        {
            var collapsing = civDataList[collapsingIndex];
            
            var cascadeEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("Cascading Collapse"),
                Description = new FixedString512Bytes($"The fall of {GetSafeCivilizationName(collapsing.Name)} sends shockwaves across the region, triggering instability and chaos in neighboring civilizations."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Social,
                Category = EventCategory.Cascade,
                Location = collapsing.Position,
                Significance = 3.0f,
                SourceEntityId = civilizations[collapsingIndex],
                Size = 1.5f,
                CivilizationId = civilizations[collapsingIndex]
            };
            historySystem.AddEvent(cascadeEvent);
            
            // Affect nearby civilizations
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (i == collapsingIndex) continue;
                
                var civ = civDataList[i];
                float distance = math.distance(civ.Position, collapsing.Position);
                
                if (distance < 300f) // Within collapse radius
                {
                    float effect = 1f - (distance / 300f); // Closer = more affected
                    civ.Stability = math.max(0.1f, civ.Stability - (0.2f * effect));
                    civ.Trade = math.max(0f, civ.Trade - (1f * effect)); // Trade routes disrupted
                    civ.Wealth *= (1f - 0.1f * effect); // Economic impact
                    civ.Paranoia = math.min(10f, civ.Paranoia + (1f * effect)); // Fear spreads
                    civDataList[i] = civ;
                }
            }
        }

        // IMPLEMENTATION: Technological Revolution (Cascading)
        private void ProcessTechnologicalRevolution(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, int innovatorIndex, WorldHistorySystem historySystem)
        {
            var innovator = civDataList[innovatorIndex];
            
            var techEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("Revolutionary Discovery"),
                Description = new FixedString512Bytes($"{GetSafeCivilizationName(innovator.Name)} has made a groundbreaking technological discovery that rapidly spreads to other civilizations, changing the world forever."),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = ProceduralWorld.Simulation.Core.EventType.Technological,
                Category = EventCategory.Revolution,
                Location = innovator.Position,
                Significance = 3.5f,
                SourceEntityId = civilizations[innovatorIndex],
                Size = 1.5f,
                CivilizationId = civilizations[innovatorIndex]
            };
            historySystem.AddEvent(techEvent);
            
            // Spread technology based on distance and trade connections
            for (int i = 0; i < civDataList.Length; i++)
            {
                var civ = civDataList[i];
                float distance = math.distance(civ.Position, innovator.Position);
                float spreadChance = math.max(0f, 1f - (distance / 400f)); // Technology spreads over distance
                
                if (civ.Trade > 5f) spreadChance += 0.3f; // Trade helps spread technology
                
                if (_random.NextFloat() < spreadChance)
                {
                    float techBoost = _random.NextFloat(0.5f, 2f);
                    civ.Technology = math.min(10f, civ.Technology + techBoost);
                    civ.Military = math.min(10f, civ.Military + techBoost * 0.5f); // Better weapons
                    civDataList[i] = civ;
                }
            }
        }

        // IMPLEMENTATION: War Cascade
        private void ProcessWarCascade(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, int attackedIndex, WorldHistorySystem historySystem)
        {
            var attacked = civDataList[attackedIndex];
            
            // Find potential allies who might join the war
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (i == attackedIndex) continue;
                
                var potential = civDataList[i];
                float distance = math.distance(potential.Position, attacked.Position);
                
                // Join war if: close ally, similar culture, or opportunistic
                bool closeAlly = distance < 150f && potential.Diplomacy > 6f;
                bool similarCulture = math.abs(potential.Culture - attacked.Culture) < 2f;
                bool opportunistic = potential.Aggressiveness > 7f && potential.Military > 6f;
                
                if (closeAlly || similarCulture || opportunistic)
                {
                    if (_random.NextFloat() < 0.3f) // 30% chance to join
                    {
                        var cascadeEvent = new HistoricalEventRecord
                        {
                            Title = new FixedString128Bytes("War Spreads"),
                            Description = new FixedString512Bytes($"The conflict draws in {GetSafeCivilizationName(potential.Name)}, escalating the war and threatening regional stability."),
                            Year = (int)SystemAPI.Time.ElapsedTime,
                            Type = ProceduralWorld.Simulation.Core.EventType.Military,
                            Category = EventCategory.Escalation,
                            Location = potential.Position,
                            Significance = 2.0f,
                            SourceEntityId = civilizations[i],
                            Size = 1.0f,
                            CivilizationId = civilizations[i]
                        };
                        historySystem.AddEvent(cascadeEvent);
                        
                        // Increase military readiness and aggression
                        potential.Military = math.min(10f, potential.Military + 1f);
                        potential.Aggressiveness = math.min(10f, potential.Aggressiveness + 0.5f);
                        potential.Stability = math.max(0.1f, potential.Stability - 0.1f);
                        civDataList[i] = potential;
                    }
                }
            }
        }
        
        // NEW: CIVILIZATION EMERGENCE - Rare but meaningful ways new civilizations are born
        private void ProcessCivilizationEmergence(NativeArray<Entity> civilizations, NativeList<CivilizationData> civDataList, float deltaTime, WorldHistorySystem historySystem)
        {
            for (int i = 0; i < civDataList.Length; i++)
            {
                var parentCiv = civDataList[i];
                
                // Only large, established civilizations can spawn breakaways
                if (parentCiv.Population < 8000f) continue;
                
                // Check for different types of emergence events
                CheckForColonialRevolt(civilizations[i], parentCiv, historySystem);
                CheckForCivilWar(civilizations[i], parentCiv, historySystem);
                CheckForReligiousSchism(civilizations[i], parentCiv, historySystem);
                CheckForCulturalIndependence(civilizations[i], parentCiv, historySystem);
            }
        }
        
        private void CheckForColonialRevolt(Entity parentEntity, CivilizationData parentCiv, WorldHistorySystem historySystem)
        {
            // Colonial revolts happen when:
            // 1. Civilization is very large (overstretched)
            // 2. Low stability or high oppression
            // 3. Distance from core territory
            
            float revoltChance = 0f;
            
            // Size factor - larger empires are harder to control
            if (parentCiv.Population > 15000f)
            {
                revoltChance += (parentCiv.Population - 15000f) / 50000f * 0.001f; // Max +0.6% chance
            }
            
            // Instability factor
            if (parentCiv.Stability < 0.4f)
            {
                revoltChance += (0.4f - parentCiv.Stability) * 0.002f; // Up to +0.08% chance
            }
            
            // Oppressive government (high military, low culture)
            if (parentCiv.Military > 7f && parentCiv.Culture < 4f)
            {
                revoltChance += 0.0005f; // +0.05% chance
            }
            
            // Economic hardship
            if (parentCiv.Resources < parentCiv.Population / 2000f) // Not enough resources per capita
            {
                revoltChance += 0.0008f; // +0.08% chance
            }
            
            // Personality factors - despotic rulers cause revolts
            if (parentCiv.Aggressiveness > 7f || parentCiv.Greed > 8f)
            {
                revoltChance += 0.0003f; // +0.03% chance
            }
            
            if (_random.NextFloat() < revoltChance)
            {
                CreateRevoltCivilization(parentEntity, parentCiv, "Colonial Revolt", historySystem);
            }
        }
        
        private void CheckForCivilWar(Entity parentEntity, CivilizationData parentCiv, WorldHistorySystem historySystem)
        {
            // Civil wars happen when:
            // 1. Very low stability
            // 2. High internal tensions
            // 3. Succession crises
            
            float civilWarChance = 0f;
            
            // Extreme instability
            if (parentCiv.Stability < 0.25f)
            {
                civilWarChance += (0.25f - parentCiv.Stability) * 0.004f; // Up to +0.1% chance
            }
            
            // Large population with poor governance
            if (parentCiv.Population > 12000f && parentCiv.Culture < 3f)
            {
                civilWarChance += 0.0006f; // +0.06% chance
            }
            
            // Religious conflicts within the civilization
            if (parentCiv.Religion > 6f && parentCiv.Culture < parentCiv.Religion - 2f)
            {
                civilWarChance += 0.0004f; // Religious extremism vs secular culture
            }
            
            // Economic collapse leading to civil unrest
            if (parentCiv.Wealth < 1000f && parentCiv.Population > 10000f)
            {
                civilWarChance += 0.0008f; // +0.08% chance
            }
            
            if (_random.NextFloat() < civilWarChance)
            {
                CreateRevoltCivilization(parentEntity, parentCiv, "Civil War", historySystem);
            }
        }
        
        private void CheckForReligiousSchism(Entity parentEntity, CivilizationData parentCiv, WorldHistorySystem historySystem)
        {
            // Religious schisms happen when:
            // 1. High religious development
            // 2. Cultural diversity
            // 3. External religious influence
            
            float schismChance = 0f;
            
            // High religious development creates theological disputes
            if (parentCiv.Religion > 7f)
            {
                schismChance += (parentCiv.Religion - 7f) * 0.0002f; // Up to +0.06% chance
            }
            
            // Cultural diversity within religious unity creates tension
            if (parentCiv.Religion > parentCiv.Culture + 2f)
            {
                schismChance += 0.0003f; // +0.03% chance
            }
            
            // Large populations develop diverse beliefs
            if (parentCiv.Population > 18000f && parentCiv.Religion > 5f)
            {
                schismChance += 0.0002f; // +0.02% chance
            }
            
            // Personality factors - pride and hatred fuel religious splits
            if (parentCiv.Pride > 7f && parentCiv.Hatred > 5f)
            {
                schismChance += 0.0001f; // +0.01% chance
            }
            
            if (_random.NextFloat() < schismChance)
            {
                CreateRevoltCivilization(parentEntity, parentCiv, "Religious Schism", historySystem);
            }
        }
        
        private void CheckForCulturalIndependence(Entity parentEntity, CivilizationData parentCiv, WorldHistorySystem historySystem)
        {
            // Cultural independence movements happen when:
            // 1. High cultural development
            // 2. Different cultural identity from ruling class
            // 3. Desire for self-determination
            
            float independenceChance = 0f;
            
            // High cultural development breeds nationalism
            if (parentCiv.Culture > 6f)
            {
                independenceChance += (parentCiv.Culture - 6f) * 0.0002f; // Up to +0.08% chance
            }
            
            // Technology advancement creates educated classes that want independence
            if (parentCiv.Technology > 5f && parentCiv.Culture > 4f)
            {
                independenceChance += 0.0003f; // +0.03% chance
            }
            
            // Trade connections expose them to other ways of life
            if (parentCiv.Trade > 6f && parentCiv.Culture > parentCiv.Military)
            {
                independenceChance += 0.0002f; // +0.02% chance
            }
            
            // Personality factors - ambition and pride drive independence
            if (parentCiv.Ambition > 6f && parentCiv.Pride > 5f)
            {
                independenceChance += 0.0001f; // +0.01% chance
            }
            
            if (_random.NextFloat() < independenceChance)
            {
                CreateRevoltCivilization(parentEntity, parentCiv, "Independence Movement", historySystem);
            }
        }
        
        private void CreateRevoltCivilization(Entity parentEntity, CivilizationData parentCiv, string emergenceType, WorldHistorySystem historySystem)
        {
            // Don't create too many civilizations
            if (_civilizationQuery.CalculateEntityCount() >= 12) return;
            
            // Create the new civilization entity
            var newCivEntity = EntityManager.CreateEntity();
            
            // Calculate the split - revolts take a portion of the parent's population and resources
            float splitRatio = _random.NextFloat(0.15f, 0.35f); // 15-35% of parent
            float newPopulation = parentCiv.Population * splitRatio;
            float newWealth = parentCiv.Wealth * splitRatio * 0.8f; // Revolts are costly
            float newResources = parentCiv.Resources * splitRatio * 0.7f;
            
            // Reduce parent civilization
            var reducedParent = parentCiv;
            reducedParent.Population *= (1f - splitRatio);
            reducedParent.Wealth *= (1f - splitRatio * 0.6f); // Parent loses less wealth (keeps infrastructure)
            reducedParent.Resources *= (1f - splitRatio * 0.5f);
            reducedParent.Stability = math.max(0.1f, reducedParent.Stability - 0.3f); // Major instability
            reducedParent.Aggressiveness += 2f; // Angry about the revolt
            reducedParent.Hatred += 3f; // Hates the rebels
            EntityManager.SetComponentData(parentEntity, reducedParent);
            
            // Create new civilization with different characteristics
            var newCiv = new CivilizationData
            {
                Name = GenerateRevoltName(parentCiv.Name, emergenceType),
                Population = newPopulation,
                Wealth = newWealth,
                Resources = newResources,
                Position = parentCiv.Position + new float3(_random.NextFloat(-50f, 50f), 0, _random.NextFloat(-50f, 50f)),
                
                // Inherit some traits but with variations
                Technology = parentCiv.Technology * _random.NextFloat(0.7f, 1.1f),
                Military = parentCiv.Military * _random.NextFloat(0.8f, 1.2f),
                Culture = parentCiv.Culture * _random.NextFloat(0.9f, 1.3f),
                Religion = parentCiv.Religion * _random.NextFloat(0.6f, 1.4f),
                Trade = parentCiv.Trade * _random.NextFloat(0.8f, 1.1f),
                
                // Different personality - rebels are often more extreme
                Aggressiveness = math.clamp(parentCiv.Aggressiveness + _random.NextFloat(-2f, 4f), 0f, 10f),
                Defensiveness = math.clamp(parentCiv.Defensiveness + _random.NextFloat(1f, 3f), 0f, 10f),
                Ambition = math.clamp(parentCiv.Ambition + _random.NextFloat(2f, 4f), 0f, 10f),
                Pride = math.clamp(parentCiv.Pride + _random.NextFloat(1f, 3f), 0f, 10f),
                Hatred = math.clamp(parentCiv.Hatred + _random.NextFloat(2f, 5f), 0f, 10f),
                Paranoia = math.clamp(parentCiv.Paranoia + _random.NextFloat(1f, 3f), 0f, 10f),
                
                Stability = _random.NextFloat(0.3f, 0.6f), // New nations are unstable
                Type = DetermineRevoltType(parentCiv, emergenceType),
                IsActive = true
            };
            
            EntityManager.AddComponentData(newCivEntity, newCiv);
            
            // Record this momentous historical event
            var eventName = $"The {emergenceType} of {GetSafeStringName(newCiv.Name.ToString())}";
            var eventDesc = GenerateEmergenceEventDescription(parentCiv.Name.ToString(), newCiv.Name.ToString(), emergenceType, splitRatio);
            
            var emergenceEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes(eventName),
                Description = new FixedString512Bytes(eventDesc),
                Year = (int)SystemAPI.Time.ElapsedTime,
                Type = emergenceType == "Civil War" ? ProceduralWorld.Simulation.Core.EventType.Political : ProceduralWorld.Simulation.Core.EventType.Political,
                Category = emergenceType == "Civil War" ? EventCategory.Revolution : EventCategory.Political,
                Location = newCiv.Position,
                Significance = 8f, // High significance
                SourceEntityId = Entity.Null,
                Size = 1.0f,
                CivilizationId = newCivEntity
            };
            historySystem.AddEvent(emergenceEvent);
            
            Debug.Log($"[CivilizationInteraction] {emergenceType}: {newCiv.Name} broke away from {parentCiv.Name} with {newPopulation:F0} people");
        }
        
        private FixedString128Bytes GenerateRevoltName(FixedString128Bytes parentName, string emergenceType)
        {
            string parent = parentName.ToString();
            string[] prefixes = emergenceType switch
            {
                "Colonial Revolt" => new[] { "Free", "Independent", "Liberated", "Rebel", "New" },
                "Civil War" => new[] { "Northern", "Southern", "Eastern", "Western", "True", "Reformed" },
                "Religious Schism" => new[] { "Orthodox", "Reformed", "Pure", "Sacred", "Divine" },
                "Independence Movement" => new[] { "United", "Democratic", "People's", "National", "Sovereign" },
                _ => new[] { "New", "Free", "Independent" }
            };
            
            string prefix = prefixes[_random.NextInt(0, prefixes.Length)];
            return new FixedString128Bytes($"{prefix} {parent}");
        }
        
        
        private CivilizationType DetermineRevoltType(CivilizationData parent, string emergenceType)
        {
            // Revolts often develop different specializations than their parent
            return emergenceType switch
            {
                "Colonial Revolt" => _random.NextFloat() < 0.4f ? CivilizationType.Military : parent.Type,
                "Civil War" => _random.NextFloat() < 0.3f ? CivilizationType.Military : CivilizationType.Cultural,
                "Religious Schism" => CivilizationType.Religious,
                "Independence Movement" => CivilizationType.Cultural,
                _ => parent.Type
            };
        }
        
        private string GenerateEmergenceEventDescription(string parentName, string newName, string emergenceType, float splitRatio)
        {
            int percentage = (int)(splitRatio * 100);
            
            return emergenceType switch
            {
                "Colonial Revolt" => $"The distant territories of {parentName} have risen in revolt, declaring independence as {newName}. {percentage}% of the population has joined the rebellion, seeking freedom from imperial rule.",
                
                "Civil War" => $"Internal strife has torn {parentName} apart! A devastating civil war has split the nation, with {percentage}% of the population forming the breakaway state of {newName}.",
                
                "Religious Schism" => $"A great religious schism has divided {parentName}. The orthodox believers have formed {newName}, taking {percentage}% of the faithful with them in their quest for spiritual purity.",
                
                "Independence Movement" => $"The cultural awakening in {parentName} has led to a peaceful independence movement. {newName} has emerged as a sovereign nation, representing {percentage}% of the former population.",
                
                _ => $"{newName} has emerged from {parentName} through {emergenceType.ToLower()}, taking {percentage}% of the population."
            };
        }
    }
} 