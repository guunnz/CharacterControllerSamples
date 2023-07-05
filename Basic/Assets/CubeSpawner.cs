using System.Collections;
using UnityEngine;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab;
    public float maxRange;
    public int cubesSpawnedAmount;
    public float minRadius;
    public float maxRadius;
    public float width;
    public bool canSpawnFirst;

    public float moveSpeed = 5f;

    private Rigidbody rb;

    private bool generating = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Character movement
        float horizontalMovement = Input.GetAxis("Horizontal");
        float verticalMovement = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalMovement, 0f, verticalMovement);
        rb.velocity = movement * moveSpeed;

        if (horizontalMovement == 0 && verticalMovement == 0)
            rb.velocity = Vector3.zero;

        if (!generating)
            StartCoroutine(SpawnCubes());
    }

    private IEnumerator SpawnCubes()
    {
        Vector3 playerPosition = transform.position;
        generating = true;

        if (canSpawnFirst)
        {
            canSpawnFirst = false;
            InstantiateCube(new Vector3(playerPosition.x, -5, playerPosition.z));
        }

        Cube[] existingCubes = FindObjectsOfType<Cube>();

        foreach (Cube cube in existingCubes)
        {
            float distance = Vector3.Distance(cube.transform.position, playerPosition);

            if (distance > maxRange)
            {
                Destroy(cube.gameObject);
            }
        }

        for (float radius = minRadius; radius <= maxRadius; radius += width)
        {
            float circumference = 2.0f * Mathf.PI * radius;
            int numCubes = Mathf.CeilToInt(circumference / width);

            for (int i = 0; i < numCubes; i++)
            {
                float angleDegrees = 360.0f / numCubes * i;
                float angleRadians = Mathf.Deg2Rad * angleDegrees;

                Vector3 cubePosition = new Vector3(
                    playerPosition.x + radius * Mathf.Cos(angleRadians),
                    -5,
                    playerPosition.z + radius * Mathf.Sin(angleRadians)
                );

                if (Vector3.Distance(cubePosition, playerPosition) < 1f)
                {
                    continue;
                }

                if (CanSpawnCube(cubePosition))
                {
                    InstantiateCube(cubePosition);
                }
            }

            playerPosition = transform.position; // Update player position
        }

        yield return null;
        generating = false;
    }

    private bool CanSpawnCube(Vector3 position)
    {
        Cube[] existingCubes = FindObjectsOfType<Cube>();

        foreach (Cube cube in existingCubes)
        {
            if (Vector3.Distance(cube.transform.position, position) < 1)
            {
                return false;
            }
        }

        return true;
    }

    private void InstantiateCube(Vector3 position)
    {
        Instantiate(cubePrefab, position, Quaternion.identity);
    }
}