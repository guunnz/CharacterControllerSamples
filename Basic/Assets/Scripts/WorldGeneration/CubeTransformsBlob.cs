using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
public struct CubeTransforms : IComponentData
{
    public BlobAssetReference<CubeTransformsBlob> Value;
}

public struct CubeTransformsBlob
{
    public BlobArray<LocalTransform> Value;
}


public struct CubeEntities : IComponentData
{
    public BlobAssetReference<CubeEntityBlob> Value;
}

public struct CubeEntityBlob
{
    public BlobArray<Entity> Value;
}