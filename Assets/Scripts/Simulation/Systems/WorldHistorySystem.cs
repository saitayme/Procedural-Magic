using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using System.Collections.Generic;
using System;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    public partial class WorldHistorySystem : SystemBase
    {
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextYearUpdate;
        private const float YEAR_UPDATE_INTERVAL = 1.0f;
        private int _currentYear;
        private int _nextEventId;
        private int _nextFigureId;
        private int _nextPeriodId;
        private NativeList<HistoricalEventRecord> _events;
        private NativeList<HistoricalFigure> _figures;
        private NativeList<HistoricalPeriod> _periods;
        
        // Cache for personality data warnings to avoid spam
        private HashSet<Entity> _personalityWarningsLogged = new HashSet<Entity>();

        protected override void OnCreate()
        {
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<SimulationConfig>()
            );

            _isInitialized = false;
            _currentYear = 0;
            _nextEventId = 0;
            _nextFigureId = 0;
            _nextPeriodId = 0;
            _nextYearUpdate = 0;

            _events = new NativeList<HistoricalEventRecord>(Allocator.Persistent);
            _figures = new NativeList<HistoricalFigure>(Allocator.Persistent);
            _periods = new NativeList<HistoricalPeriod>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (_events.IsCreated)
                _events.Dispose();
            if (_figures.IsCreated)
                _figures.Dispose();
            if (_periods.IsCreated)
                _periods.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<SimulationConfig>())
                {
                    Debug.LogWarning("[WorldHistorySystem] No SimulationConfig found");
                    return;
                }

                var currentConfig = SystemAPI.GetSingleton<SimulationConfig>();
                if (!currentConfig.EnableHistorySystem)
                {
                    Debug.LogWarning("[WorldHistorySystem] History system disabled in config");
                    return;
                }

                Debug.Log("[WorldHistorySystem] Initialized successfully");
                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextYearUpdate)
                return;

            _nextYearUpdate = currentTime + YEAR_UPDATE_INTERVAL;
            _currentYear++;
        }

        public void AddEvent(HistoricalEventRecord evt)
        {
            if (!_events.IsCreated)
            {
                Debug.LogWarning("[WorldHistorySystem] Cannot add event - events list not created");
                return;
            }

            evt.Id = _nextEventId++;
            _events.Add(evt);
            Debug.Log($"[WorldHistorySystem] Added event: {evt.Title} | Name: {evt.Name} | Description: {evt.Description} | Year: {evt.Year} (Total events: {_events.Length})");
            
            // Forward to Living Chronicle System for rich narrative processing
            ForwardToLivingChronicleSystem(evt);
        }
        
        private void ForwardToLivingChronicleSystem(HistoricalEventRecord evt)
        {
            // Get the Living Chronicle System
            var chronicleSystem = World.GetExistingSystemManaged<LivingChronicleSystem>();
            if (chronicleSystem == null) 
            {
                Debug.LogWarning("[WorldHistorySystem] LivingChronicleSystem not found - cannot forward event");
                return;
            }
            
            Debug.Log($"[WorldHistorySystem] Forwarding event to LivingChronicleSystem: {evt.Title}");
            
            // Check if we have civilization data
            if (!EntityManager.HasComponent<CivilizationData>(evt.CivilizationId))
            {
                Debug.LogWarning($"[WorldHistorySystem] Cannot forward event - missing CivilizationData for entity {evt.CivilizationId}");
                return;
            }
            
            var civData = EntityManager.GetComponentData<CivilizationData>(evt.CivilizationId);
            Debug.Log($"[WorldHistorySystem] Found civilization data for {civData.Name}");
            
            // Get or create adaptive personality data
            AdaptivePersonalityData personality;
            if (EntityManager.HasComponent<AdaptivePersonalityData>(evt.CivilizationId))
            {
                personality = EntityManager.GetComponentData<AdaptivePersonalityData>(evt.CivilizationId);
                Debug.Log($"[WorldHistorySystem] Found existing AdaptivePersonalityData for {civData.Name}");
            }
            else
            {
                // Only log warning once per civilization to avoid spam
                if (!_personalityWarningsLogged.Contains(evt.CivilizationId))
                {
                    Debug.LogWarning($"[WorldHistorySystem] Missing AdaptivePersonalityData for {civData.Name}, creating default personality");
                    _personalityWarningsLogged.Add(evt.CivilizationId);
                }
                
                // Create a default personality based on the civilization's current traits
                var basePersonality = new PersonalityTraits
                {
                    Aggressiveness = civData.Aggressiveness,
                    Defensiveness = civData.Defensiveness,
                    Greed = civData.Greed,
                    Paranoia = civData.Paranoia,
                    Ambition = civData.Ambition,
                    Desperation = civData.Desperation,
                    Hatred = civData.Hatred,
                    Pride = civData.Pride,
                    Vengefulness = civData.Vengefulness
                };
                
                personality = new AdaptivePersonalityData
                {
                    BasePersonality = basePersonality,
                    CurrentPersonality = basePersonality,
                    TemporaryModifiers = new PersonalityTraits(),
                    SuccessfulWars = civData.SuccessfulWars,
                    DefensiveVictories = 0,
                    TradeSuccesses = 0,
                    Betrayals = civData.TimesBetrayed,
                    NaturalDisasters = 0,
                    CulturalAchievements = 0,
                    ReligiousEvents = 0,
                    DiplomaticVictories = 0,
                    PersonalityFlexibility = 0.5f,
                    CurrentStress = civData.ResourceStressLevel,
                    TraumaResistance = 0.6f,
                    Stage = PersonalityEvolutionStage.Developing,
                    PreviousPersonality = basePersonality,
                    LastPersonalityChangeYear = civData.LastAttackedYear
                };
                
                // Add the component to the entity for future use
                EntityManager.AddComponentData(evt.CivilizationId, personality);
                Debug.Log($"[WorldHistorySystem] Created and added AdaptivePersonalityData for {civData.Name}");
            }
            
            // Process the event through the advanced chronicle system
            chronicleSystem.ProcessHistoricalEvent(evt, civData, personality);
            Debug.Log($"[WorldHistorySystem] Successfully processed event for {civData.Name}");
        }

        public void AddFigure(HistoricalFigure figure)
        {
            if (!_figures.IsCreated)
                return;

            figure.Id = _nextFigureId++;
            _figures.Add(figure);
        }

        public void AddPeriod(HistoricalPeriod period)
        {
            if (!_periods.IsCreated)
                return;

            period.Id = _nextPeriodId++;
            _periods.Add(period);
        }

        public NativeList<HistoricalEventRecord> GetHistoricalEvents(Allocator allocator)
        {
            Debug.Log($"[WorldHistorySystem] GetHistoricalEvents called - returning {_events.Length} events");
            var result = new NativeList<HistoricalEventRecord>(_events.Length, allocator);
            result.AddRange(_events.AsArray());
            
            // Debug: Log first few events to verify data
            for (int i = 0; i < math.min(3, _events.Length); i++)
            {
                var evt = _events[i];
                Debug.Log($"[WorldHistorySystem] Event {i}: {evt.Name} | {evt.Description} | Year: {evt.Year}");
            }
            
            return result;
        }

        public NativeList<HistoricalFigure> GetHistoricalFigures(Allocator allocator)
        {
            var result = new NativeList<HistoricalFigure>(_figures.Length, allocator);
            result.AddRange(_figures.AsArray());
            return result;
        }

        public NativeList<HistoricalPeriod> GetHistoricalPeriods(Allocator allocator)
        {
            var result = new NativeList<HistoricalPeriod>(_periods.Length, allocator);
            result.AddRange(_periods.AsArray());
            return result;
        }

        public NativeList<HistoricalEventRecord> GetFigureAchievements(HistoricalFigure figure, Allocator allocator)
        {
            var result = new NativeList<HistoricalEventRecord>(allocator);
            for (int i = 0; i < _events.Length; i++)
            {
                if (_events[i].FigureId.Equals(figure.Id))
                {
                    result.Add(_events[i]);
                }
            }
            return result;
        }

        public int GetCurrentYear() => _currentYear;

        public void SetCurrentYear(int year)
        {
            _currentYear = year;
        }

        public HistoricalFigure GetHistoricalFigure(Entity entity)
        {
            if (!entity.Equals(Entity.Null) && EntityManager.HasComponent<HistoricalFigure>(entity))
            {
                return EntityManager.GetComponentData<HistoricalFigure>(entity);
            }
            return default;
        }

        public CivilizationData GetCivilization(Entity entity)
        {
            if (!entity.Equals(Entity.Null) && EntityManager.HasComponent<CivilizationData>(entity))
            {
                return EntityManager.GetComponentData<CivilizationData>(entity);
            }
            return default;
        }
    }
} 