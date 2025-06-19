using Unity.Entities;

namespace ProceduralWorld.Simulation.Core
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    public partial class SimulationSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Add systems in the correct order
            var world = World;
            
            // First, add the bootstrap system
            world.GetOrCreateSystem<SimulationBootstrap>();
            
            // Then add terrain generation
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.TerrainGenerationSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.TerrainMeshSystem>();
            
            // Add resource systems
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.ResourceSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.ResourceManagementSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.ResourceGenerationSystem>();
            
            // Add core simulation systems
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.CivilizationSpawnSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.CivilizationInteractionSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.ReligionManagementSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.ReligionSpawnSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.EconomyManagementSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.EconomySpawnSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.TerritorySpawnSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.WorldNamingSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.StructureGenerationSystem>();
            
            // Add history and event systems
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.HistoryRecordingSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.HistoricalEventSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.SimulationEventSystem>();
            
            // Add enhanced storytelling systems
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.LegacySystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.AdaptiveFolkloreSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.NarrativeArcSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.CulturalInfluenceSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.LivingChronicleSystem>();
            
            // Add visualization systems
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.VisualizationSystem>();
            world.GetOrCreateSystem<ProceduralWorld.Simulation.Systems.DebugVisualizationSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.SimulationDebugSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Visualization.TerrainVisualizationSystem>();
            world.GetOrCreateSystemManaged<ProceduralWorld.Simulation.Systems.CursorInteractionSystem>();
        }
    }
} 