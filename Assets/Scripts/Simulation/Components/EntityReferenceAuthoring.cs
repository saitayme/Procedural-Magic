using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public class EntityReferenceAuthoring : MonoBehaviour
    {
        [SerializeField] private string entityName;
        [SerializeField] private string description;

        public class EntityReferenceBaker : Baker<EntityReferenceAuthoring>
        {
            public override void Bake(EntityReferenceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntityReference
                {
                    Entity = entity,
                    Name = new FixedString64Bytes(authoring.entityName),
                    Description = new FixedString128Bytes(authoring.description)
                });
            }
        }
    }
} 