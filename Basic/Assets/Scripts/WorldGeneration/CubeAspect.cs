using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct CubeAspect : IAspect
{
    private readonly RefRO<CubeData> _cubeData;
    private readonly RefRO<LocalTransform> _cubeTransform;
    
    public Entity GetCubePrefab()
    {
       return _cubeData.ValueRO.CubePrefab;
    }
}