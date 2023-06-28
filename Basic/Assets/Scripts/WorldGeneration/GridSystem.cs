using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial class GridSystem : SystemBase
{
    private bool shouldUpdate = true;
    private bool hasBasicPlayer = false;

    public void OnCreate(ref SystemState state)
    {
        RequireForUpdate<LePlayer>();
        // state.RequireForUpdate<BasicPlayerAuthoring.LePlayer>();
        // state.RequireForUpdate<BasicPlayer>();
        // state.RequireForUpdate<CubeTag>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    protected override void OnUpdate()
    {
        if (!hasBasicPlayer)
        {
            Entities.WithoutBurst().ForEach((Entity entity, in LePlayer basicPlayer) =>
            {
                // The entity has the BasicPlayerAuthoring component
                hasBasicPlayer = true;

                // Perform your logic here
            }).Run();
        }


        if (!hasBasicPlayer)
            return;

        // if (shouldUpdate)
        // {
        //     shouldUpdate = false;
        // }
        // else
        // {
        //     return;
        // }
        EntityQuery cubeQuery =
            GetEntityQuery(ComponentType.ReadOnly<SpawnedCube>(), ComponentType.ReadOnly<LocalTransform>());
        NativeArray<LocalTransform> cubeTransforms = cubeQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var cube = SystemAPI.GetSingletonEntity<CubeTag>();
        var player = SystemAPI.GetSingletonEntity<LePlayer>();

        var playerAspectPost = SystemAPI.GetAspect<GetPositionAspect>(player);

        Debug.Log(playerAspectPost.Transform.ValueRO.Position);
        var cubeAspect =
            SystemAPI.GetAspect<CubeAspect>(cube); //this is not the way to get the basic character aspect

        float3 playerPosition = playerAspectPost.Transform.ValueRO.Position;
        playerPosition.y = -5;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float minRadius = 1.0f; // Minimum radius
        float maxRadius = 10.0f; // Maximum radius
        float width = 1.0f; // Assuming the width of a cube is 1

        bool canSpawnFirst = true;
        for (int j = 0; j < cubeTransforms.Length; j++)
        {
            if (math.distance(playerPosition, cubeTransforms[j].Position) < 1)
            {
                canSpawnFirst = false;
            }
        }

        if (canSpawnFirst)
        {
            var playerCube = ecb.Instantiate(cubeAspect.GetCubePrefab());
            LocalTransform playerCubeTransform = new LocalTransform
                { Position = playerPosition, Scale = 1, Rotation = quaternion.identity };
            ecb.SetComponent(playerCube, playerCubeTransform);
            ecb.AddComponent(playerCube, new SpawnedCube());
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
                for (int j = 0; j < cubeTransforms.Length; j++)
                {
                    if (math.distance(cubePosition, cubeTransforms[j].Position) < 1)
                    {
                        canSpawn = false;
                    }
                }

                if (canSpawn)
                {
                    // Instantiate the cube
                    var newCube = ecb.Instantiate(cubeAspect.GetCubePrefab());
                    LocalTransform cubeTransform = new LocalTransform
                        { Position = cubePosition, Scale = 1, Rotation = quaternion.identity };

                    // Set the cube's position
                    ecb.SetComponent(newCube, cubeTransform);
                    ecb.AddComponent(newCube, new SpawnedCube());
                }
            }
        }

        ecb.Playback(this.EntityManager);
        cubeTransforms.Dispose();
    }
}