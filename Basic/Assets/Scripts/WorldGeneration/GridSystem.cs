using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial class GridSystem : SystemBase
{
    private bool shouldUpdate = true;
    private bool hasBasicPlayer = false;

    protected override void OnCreate()
    {
        RequireForUpdate<LePlayer>();
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        if (!hasBasicPlayer)
        {
            Entities.WithoutBurst().ForEach((Entity entity, in LePlayer basicPlayer) => { hasBasicPlayer = true; })
                .Run();
        }

        if (!hasBasicPlayer)
            return;

        EntityQuery cubeQuery =
            GetEntityQuery(ComponentType.ReadOnly<SpawnedCube>(), ComponentType.ReadOnly<LocalTransform>());
        int entityCount = cubeQuery.CalculateEntityCount();

        var builder = new BlobBuilder(Allocator.Temp);
        var builder2 = new BlobBuilder(Allocator.Temp);
        ref var cubeTransformsBlob = ref builder.ConstructRoot<CubeTransformsBlob>();
        ref var cubeEntitiesBlob = ref builder2.ConstructRoot<CubeEntityBlob>();  
        var arrayBuilderTransforms = builder.Allocate(ref cubeTransformsBlob.Value, entityCount);
        // var arrayBuilderEntities = builder2.Allocate(ref cubeEntitiesBlob.Value, entityCount);

        NativeArray<LocalTransform> cubeTransforms = cubeQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        NativeArray<Entity> cubeEntities = cubeQuery.ToEntityArray(Allocator.TempJob);
        
        Debug.Log(cubeTransforms.Length);
        //
        for (int i = 0; i < entityCount; i++)
        {
            arrayBuilderTransforms[i] = cubeTransforms[i];
            // arrayBuilderEntities[i] = cubeEntities[i];
        }
        //
        var blobAssetTransform = builder.CreateBlobAssetReference<CubeTransformsBlob>(Allocator.Persistent);
        // var blobAssetEntity = builder2.CreateBlobAssetReference<CubeEntityBlob>(Allocator.Persistent);


        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var ecb2 = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();


        var cube = SystemAPI.GetSingletonEntity<CubeTag>();

        var player = SystemAPI.GetSingletonEntity<LePlayer>();

        var playerAspectPost = SystemAPI.GetAspect<GetPositionAspect>(player);

        var cubeAspect = SystemAPI.GetAspect<CubeAspect>(cube);
        float maxRange = 25.0f;

        float3 playerPosition = playerAspectPost.Transform.ValueRO.Position;
        playerPosition.y = -5;
        float minRadius = 1.0f; // Minimum radius
        float maxRadius = 20.0f; // Maximum radius
        float width = 1.0f; // Assuming the width of a cube is 1
        
        bool canSpawnFirst = true;

        for (int i = 0; i < cubeTransforms.Length; i++)
        {
            var cubeTransform = cubeTransforms[i];
            var cubeEntity = cubeEntities[i];

            if (math.distance(playerPosition, cubeTransform.Position) < 1)
            {
                canSpawnFirst = false;
            }

            float distanceToPlayer = math.distance(playerPosition, cubeTransform.Position);

            if (distanceToPlayer > maxRange)
            {
                // Delete the entity outside the range
                ecb.DestroyEntity(cubeEntity);
            }
        }
        
        new SpawnCubeJob
        {
            canSpawnFirst = canSpawnFirst,
            cubesSpawnedAmount = entityCount,
            playerPosition = playerPosition,
            maxRadius = maxRadius,
            minRadius = minRadius,
            maxRange = maxRange,
            ecb = ecb2.CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),
            width = width,
            cubeTransforms = blobAssetTransform
        }.ScheduleParallel();

        // if (canSpawnFirst)
        // {
        //     var playerCube = ecb.Instantiate(cubeAspect.GetCubePrefab());
        //     LocalTransform playerCubeTransform = new LocalTransform
        //         { Position = playerPosition, Scale = 1, Rotation = quaternion.identity };
        //     ecb.SetComponent(playerCube, playerCubeTransform);
        //     ecb.AddComponent(playerCube, new SpawnedCube());
        // }

        // for (float radius = minRadius; radius <= maxRadius; radius += width)
        // {
        //     // Calculate the circumference at the current radius
        //     float circumference = 2.0f * math.PI * radius;
        //
        //     // Calculate the number of cubes needed for the current radius
        //     int numCubes = Mathf.CeilToInt(circumference / width);
        //
        //     for (int i = 0; i < numCubes; i++)
        //     {
        //         float angleDegrees = 360.0f / numCubes * i;
        //         float angleRadians = math.radians(angleDegrees);
        //
        //         // Calculate the position of the cube
        //         float3 cubePosition = new float3(
        //             playerPosition.x + radius * math.cos(angleRadians),
        //             -5,
        //             playerPosition.z + radius * math.sin(angleRadians)
        //         );
        //
        //         // Skip spawning a cube at the player's position
        //         if (math.distance(cubePosition, playerPosition) < 0.01f)
        //             continue;
        //
        //         bool canSpawn = true;
        //         for (int j = 0; j < cubeTransforms.Length; j++)
        //         {
        //             if (math.distance(cubePosition, cubeTransforms[j].Position) < 1)
        //             {
        //                 canSpawn = false;
        //             }
        //         }
        //
        //         if (canSpawn)
        //         {
        //             // Instantiate the cube
        //             var newCube = ecb.Instantiate(cubeAspect.GetCubePrefab());
        //             LocalTransform newCubeTransform = new LocalTransform
        //                 { Position = cubePosition, Scale = 1, Rotation = quaternion.identity };
        //
        //             // Set the cube's position
        //             ecb.SetComponent(newCube, newCubeTransform);
        //             ecb.AddComponent(newCube, new SpawnedCube());
        //         }
        //     }
        // }

        ecb.Playback(EntityManager);
        cubeTransforms.Dispose();
        cubeEntities.Dispose();
        builder.Dispose();
        builder2.Dispose();
    }

    [BurstCompile]
    private partial struct SpawnCubeJob : IJobEntity
    {
        public float maxRange;
        public float3 playerPosition;

        public EntityCommandBuffer.ParallelWriter ecb;

        public float minRadius; // Minimum radius
        public float maxRadius; // Maximum radius
        public float width; // Assuming the width of a cube is 1
        public int cubesSpawnedAmount;
        public bool canSpawnFirst;
        public BlobAssetReference<CubeTransformsBlob> cubeTransforms;

        [BurstCompile]
        private void Execute(CubeAspect cubeAspect, [EntityIndexInChunk] int sortKey)
        {
            if (canSpawnFirst)
            {
                var playerCube = ecb.Instantiate(sortKey, cubeAspect.GetCubePrefab());
                LocalTransform playerCubeTransform = new LocalTransform
                    { Position = playerPosition, Scale = 1, Rotation = quaternion.identity };
                ecb.SetComponent(sortKey, playerCube, playerCubeTransform);
                ecb.AddComponent(sortKey, playerCube, new SpawnedCube());
            }

            for (float radius = minRadius; radius <= maxRadius; radius += width)
            {
                // Calculate the circumference at the current radius
                float circumference = 2.0f * math.PI * radius;

                // Calculate the number of cubes needed for the current radius
                int numCubes = Mathf.CeilToInt(circumference / width);

                for (int i = 0; i < numCubes; i++)
                {
                    float angleDegrees = 360.0f / numCubes * i;
                    float angleRadians = math.radians(angleDegrees);

                    // Calculate the position of the cube
                    float3 cubePosition = new float3(
                        playerPosition.x + radius * math.cos(angleRadians),
                        -5,
                        playerPosition.z + radius * math.sin(angleRadians)
                    );

                    // Skip spawning a cube at the player's position
                    if (math.distance(cubePosition, playerPosition) < 0.01f)
                        continue;

                    bool canSpawn = true;
                    for (int j = 0; j < cubesSpawnedAmount; j++)
                    {
                        if (math.distance(cubePosition, cubeTransforms.Value.Value[j].Position) < 1)
                        {
                            canSpawn = false;
                        }
                    }

                    if (canSpawn)
                    {
                        // Instantiate the cube
                        var newCube = ecb.Instantiate(sortKey, cubeAspect.GetCubePrefab());
                        LocalTransform newCubeTransform = new LocalTransform
                            { Position = cubePosition, Scale = 1, Rotation = quaternion.identity };

                        // Set the cube's position
                        ecb.SetComponent(sortKey, newCube, newCubeTransform);
                        ecb.AddComponent(sortKey, newCube, new SpawnedCube());
                    }
                }
            }
        }
    }
}