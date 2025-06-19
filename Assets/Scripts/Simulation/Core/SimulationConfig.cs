using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using System;

namespace ProceduralWorld.Simulation.Core
{
    [BurstCompile]
    public struct SimulationConfig : IComponentData, IDisposable
    {
        // System Enablement
        public bool EnableTerrainSystem;
        public bool EnableResourceSystem;
        public bool EnableCivilizationSystem;
        public bool EnableCivilizationInteractions;
        public bool EnableReligionSystem;
        public bool EnableEconomySystem;
        public bool EnableHistorySystem;
        public bool EnableVisualizationSystem;
        public bool EnableDebugVisualization;
        public bool EnableWorldNamingSystem;
        public bool EnableCursorSystem;
        public bool IsSimulationRunning;
        public bool EnableTerrainGenerationSystem;
        public bool EnableDebug;
        public bool EnableHistory;
        public bool EnableTerrainVisualization;
        public bool EnableSimulationBootstrap;
        public bool EnableSimulation;
        public bool EnableHistoricalEvents;
        public bool EnableHistoryRecording;
        public bool EnableCivilizationSpawning;

        // World Settings
        public int WorldSize;
        public int TerrainResolution;
        public float WorldHeight;
        public float WorldScale;
        public float WorldDetail;
        public float WorldComplexity;
        public float TerrainScale;
        public float HeightScale;
        public float TemperatureScale;
        public float MoistureScale;
        public float TimeScale;
        public float TerrainGrowthRate;
        public float MaxHeight;
        public float MaxMoisture;
        public float MaxTemperature;
        public float MaxPosition;
        public float MaxRotation;
        public float CursorSpeed;
        public float CursorRotationSpeed;
        public int CurrentYear;
        public float NoiseScale;
        public int Seed;

        // Resource Settings
        public NativeArray<ResourceType> ResourceTypes;
        public float ResourceGrowthRate;
        public float ResourceValueRate;
        public float MaxResourceAmount;
        public float MaxResourceValue;
        public float MinResourceAmount;
        public float MinResourceValue;
        public float ResourceDensity;
        public float ResourceScale;
        public float ResourceSpawnRate;
        public float ResourceDecayRate;
        public float ResourceRegenerationRate;
        public float ResourceConsumptionRate;
        public float ResourceProductionRate;
        public float ResourceStorageRate;
        public float ResourceTransportRate;
        public float ResourceTradeRate;
        public float ResourceExchangeRate;
        public float ResourceConversionRate;
        public float ResourceProcessingRate;
        public float ResourceRecyclingRate;
        public float ResourceWasteRate;
        public float ResourcePollutionRate;
        public float ResourceCleanupRate;
        public float ResourcePurificationRate;
        public float ResourceFilteringRate;
        public float ResourceTreatmentRate;
        public float ResourceDisposalRate;

        // Civilization Settings
        public int maxCivilizations;
        public float civilizationSpawnRadius;
        public int initialCivilizationPopulation;
        public int minHeightForCivilization;
        public int maxCivilizationPopulation;
        public float civilizationGrowthRate;
        public float civilizationDecayRate;
        public float civilizationStability;
        public float civilizationInfluence;
        public float civilizationTechnology;
        public float civilizationCulture;
        public float civilizationWealth;
        public float civilizationTrade;
        public float civilizationProduction;
        public float civilizationConsumption;
        public float civilizationStorage;
        public float civilizationTransport;
        public float civilizationExchange;
        public float civilizationConversion;
        public float civilizationProcessing;
        public float civilizationRecycling;
        public float civilizationWaste;
        public float civilizationPollution;
        public float civilizationCleanup;
        public float civilizationPurification;
        public float civilizationFiltering;
        public float civilizationTreatment;
        public float civilizationDisposal;
        public float initialCivilizationTechnology;
        public float initialCivilizationResources;

        // Religion Settings
        public float InitialReligionPopulation;
        public float MaxReligionPopulation;
        public float ReligionGrowthRate;
        public float ReligionDeathRate;
        public float ReligionMigrationRate;
        public float ReligionExpansionRate;
        public float ReligionResourceConsumptionRate;
        public float ReligionResourceProductionRate;
        public float ReligionResourceStorageRate;
        public float ReligionResourceTransportRate;
        public float ReligionResourceTradeRate;
        public float ReligionResourceExchangeRate;
        public float ReligionResourceConversionRate;
        public float ReligionResourceProcessingRate;
        public float ReligionResourceRecyclingRate;
        public float ReligionResourceWasteRate;
        public float ReligionResourcePollutionRate;
        public float ReligionResourceCleanupRate;
        public float ReligionResourcePurificationRate;
        public float ReligionResourceFilteringRate;
        public float ReligionResourceTreatmentRate;
        public float ReligionResourceDisposalRate;
        public float ReligionInfluenceRate;
        public float MaxReligionInfluence;
        public float ReligionStabilityRate;
        public float MaxReligionStability;
        public float MaxReligionFollowers;
        public float ReligionSpreadRate;
        public float MaxReligionSpread;

        // Economy Settings
        public float InitialEconomyWealth;
        public float MaxEconomyWealth;
        public float EconomyGrowthRate;
        public float EconomyDecayRate;
        public float EconomyTaxRate;
        public float MaxWealth;
        public float MaxTrade;
        public float MaxProduction;
        public float MaxPopulation;
        public float MaxTechnology;
        public float MaxCulture;
        public float MaxStability;
        public float MaxInfluence;
        public float EconomyResourceConsumptionRate;
        public float EconomyResourceProductionRate;
        public float EconomyResourceStorageRate;
        public float EconomyResourceTransportRate;
        public float EconomyResourceTradeRate;
        public float EconomyResourceExchangeRate;
        public float EconomyResourceConversionRate;
        public float EconomyResourceProcessingRate;
        public float EconomyResourceRecyclingRate;
        public float EconomyResourceWasteRate;
        public float EconomyResourcePollutionRate;
        public float EconomyResourceCleanupRate;
        public float EconomyResourcePurificationRate;
        public float EconomyResourceFilteringRate;
        public float EconomyResourceTreatmentRate;
        public float EconomyResourceDisposalRate;

        // History Settings
        public int MaxHistoricalEvents;
        public float HistoricalEventFrequency;
        public float HistoricalEventImpact;
        public float HistoricalEventDuration;
        public float HistoricalEventSpreadRate;
        public float HistoricalEventDecayRate;
        public float HistoricalEventRegenerationRate;
        public float HistoricalEventConsumptionRate;
        public float HistoricalEventProductionRate;
        public float HistoricalEventStorageRate;
        public float HistoricalEventTransportRate;
        public float HistoricalEventTradeRate;
        public float HistoricalEventExchangeRate;
        public float HistoricalEventConversionRate;
        public float HistoricalEventProcessingRate;
        public float HistoricalEventRecyclingRate;
        public float HistoricalEventWasteRate;
        public float HistoricalEventPollutionRate;
        public float HistoricalEventCleanupRate;
        public float HistoricalEventPurificationRate;
        public float HistoricalEventFilteringRate;
        public float HistoricalEventTreatmentRate;
        public float HistoricalEventDisposalRate;
        public float HistoryRecordingRate;
        public float MaxTimelineProgress;
        public float MaxRecordedEvents;
        public float EventSignificanceRate;
        public float MaxEventSignificance;
        public float EventInfluenceRate;
        public float MaxEventInfluence;
        public float TimelineProgressRate;

        // Visualization parameters
        public int VisualizationMode;
        public int ColorMode;
        public bool ShowResources;
        public bool ShowCivilizations;
        public bool ShowReligions;

        // Debug parameters
        public bool ShowDebugInfo;
        public bool ShowPerformanceStats;
        public bool ShowSystemStats;

        // Cursor parameters
        public bool EnableCursorHighlight;
        public float4 CursorHighlightColor;
        public float CursorHighlightIntensity;

        // World naming parameters
        public bool EnableWorldNaming;
        public FixedString64Bytes WorldNamePrefix;
        public FixedString64Bytes WorldNameSuffix;

        public void Dispose()
        {
            if (ResourceTypes.IsCreated)
            {
                ResourceTypes.Dispose();
            }
        }
    }
} 