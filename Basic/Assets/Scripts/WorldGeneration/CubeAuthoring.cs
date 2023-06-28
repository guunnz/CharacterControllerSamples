using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

public class CubeAuthoring : MonoBehaviour
{
    public GameObject CubePrefab;

    public class Baker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CubeTag());
            AddComponent(entity, new CubeData()
            {
                CubePrefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct CubeTag : IComponentData
{
    
}

public struct CubeData : IComponentData
{
    public Entity CubePrefab;
    public LocalTransform position;
}

public struct SpawnedCube : IComponentData
{
}